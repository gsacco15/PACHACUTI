using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>What the street (or roofs) tells soldiers about Manco this frame.</summary>
    public struct StealthContext
    {
        public Manco Manco;
        public bool WithLine;     // walking in the porter line
        public bool Reachable;    // on the ground they patrol (not on a roof)
        public float Dt;
    }

    /// <summary>
    /// One of Quizquiz's soldiers. Pitch slice §6: suspicion is local and visible
    /// only as behaviour — glance → stare → step toward → shout. The rules live in
    /// Core.SoldierBrain; this class senses the scene and acts the state out.
    /// </summary>
    public sealed class Soldier : MonoBehaviour
    {
        public static readonly List<Soldier> All = new List<Soldier>();
        public static readonly List<Transform> Bodies = new List<Transform>();
        static float _lastBark = -99f;

        public SoldierBrain Brain;
        public Humanoid Body;
        public string Label;
        public Vector3 Post;
        public float PostYaw;
        public Vector3[] Route;
        public float RoutePause = 1.5f;
        public bool Active = true;
        public System.Action<Soldier> Captured;
        public System.Action<Soldier> Shouted;
        public Overlay Overlay;
        public AudioDirector Audio;

        int _wp;
        int _wpDir = 1;
        float _pause;
        float _yaw;
        Vector3 _lastKnown;
        Vector3 _noisePos;
        float _scan;
        SoldierState _shownState;
        Vector3? _scriptMove;
        Vector3? _scriptLook;
        float _scriptSpeed = 1.5f;

        public bool HasTorch => Body != null && Body.PropTorch != null;
        public Vector3 Eye => transform.position + Vector3.up * 1.55f;
        public bool IsDead => Brain.IsDead;

        public static Soldier Spawn(Transform parent, string label, Vector3 post, float yaw, SoldierRules rules, bool torch, Overlay overlay, AudioDirector audio)
        {
            var body = Humanoid.Build(parent, "Soldier " + label, new Color(0.24f, 0.26f, 0.2f), new Color(0.38f, 0.26f, 0.18f), 1.66f,
                                      HeadGear.SoldierCrest, torch ? HandProp.Torch : HandProp.Mace, new Color(0.15f, 0.12f, 0.1f));
            var s = body.gameObject.AddComponent<Soldier>();
            s.Body = body;
            s.Brain = new SoldierBrain(rules);
            s.Label = label;
            s.Post = post;
            s.PostYaw = yaw;
            s.Overlay = overlay;
            s.Audio = audio;
            body.transform.position = post;
            s._yaw = yaw;
            body.transform.rotation = Quaternion.Euler(0, yaw, 0);
            Layers.SetRecursively(body.gameObject, Layers.Characters);
            if (torch) audio?.Attach(body.PropTorch.transform, "torch", 0.35f, 14f);
            return s;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public void ResetToPost()
        {
            Brain.ResetToPost();
            transform.position = Post;
            _yaw = PostYaw;
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
            _wp = 0;
            _wpDir = 1;
            _scriptMove = null;
            _scriptLook = null;
            Body.RaiseProp = 0f;
            Body.HeadYaw = 0f;
            _shownState = SoldierState.Idle;
        }

        // ------------------------------------------------------------------ scripted control (kinsman beat)

        public void ScriptMoveTo(Vector3 p, float speed) { _scriptMove = p; _scriptSpeed = speed; }
        public void ScriptLookAt(Vector3 p) => _scriptLook = p;
        public void ScriptClear() { _scriptMove = null; _scriptLook = null; }
        public bool ScriptArrived => !_scriptMove.HasValue;

        public void Kill()
        {
            Brain.Kill();
            Body.Lying = 1f;
            Body.RaiseProp = 0f;
            if (Body.PropTorch != null) Body.PropTorch.Lit = false;
            Bodies.Add(transform);
        }

        // ------------------------------------------------------------------ tick

        public void Tick(in StealthContext ctx, bool alertCalledNearby)
        {
            if (!Active) return;
            float dt = ctx.Dt;
            var m = ctx.Manco;
            if (Brain.State == SoldierState.Dead) return;

            if (Brain.State == SoldierState.Scripted)
            {
                TickScripted(dt);
                return;
            }

            var senses = Sense(ctx, alertCalledNearby);
            var r = Brain.Tick(senses);
            if (senses.SeesManco) _lastKnown = m.transform.position;
            if (r.Shout)
            {
                Bark(SliceScript.BarkHere, force: true);
                Audio?.PlayAt("shout", Eye, 1f, Random.Range(0.95f, 1.1f), 40f);
                NoiseBus.Emit(transform.position, NoiseKind.Shout);
                Shouted?.Invoke(this);
            }
            if (r.InspectedLoad) Bark(SliceScript.BarkKeepMoving);
            if (r.Capture) Captured?.Invoke(this);
            if (Brain.State != _shownState) OnStateShown(Brain.State, senses);
            _shownState = Brain.State;
            Act(ctx, dt);
        }

        SoldierSenses Sense(in StealthContext ctx, bool alertCalled)
        {
            var m = ctx.Manco;
            Vector3 target = m.Chest;
            Vector3 to = target - Eye;
            float dist = new Vector3(to.x, 0, to.z).magnitude;
            Vector3 facing = Quaternion.Euler(0, _yaw + Body.HeadYaw, 0) * Vector3.forward;
            float hAngle = Vector3.Angle(facing, new Vector3(to.x, 0, to.z));
            float vAngle = Mathf.Atan2(to.y, Mathf.Max(0.01f, dist)) * Mathf.Rad2Deg;
            bool lit = Torch.IsLit(target);
            bool canSeeGeometrically = dist <= StealthRules.VisibleRange(lit) + 0.5f;
            bool los = canSeeGeometrically && !Physics.Linecast(Eye, target, Layers.SightBlockers, QueryTriggerInteraction.Ignore);
            bool sees = m.Mode != MancoMode.Scripted && StealthRules.CanSee(to.magnitude, hAngle, vAngle, lit, los);

            bool heard = NoiseBus.Heard(transform.position, 0.12f, out var noise);
            if (heard) _noisePos = noise.Position;

            bool seesBody = false;
            foreach (var b in Bodies)
            {
                if (b == transform) continue;
                Vector3 tb = b.position + Vector3.up * 0.3f - Eye;
                if (tb.magnitude > Tuning.SightRangeInFog) continue;
                bool bodyLit = Torch.IsLit(b.position);
                if (tb.magnitude > StealthRules.VisibleRange(bodyLit)) continue;
                if (Vector3.Angle(facing, new Vector3(tb.x, 0, tb.z)) > Tuning.SightConeDegrees * 0.5f) continue;
                if (Physics.Linecast(Eye, b.position + Vector3.up * 0.3f, Layers.SightBlockers, QueryTriggerInteraction.Ignore)) continue;
                seesBody = true;
            }

            return new SoldierSenses
            {
                Dt = ctx.Dt,
                SeesManco = sees,
                DistanceToManco = dist,
                MancoStopped = m.Speed < 0.2f && m.Mode == MancoMode.Ground,
                MancoWithLine = ctx.WithLine,
                MancoHeadDown = m.HeadDown,
                MancoCarrying = m.Carrying,
                MancoLit = Torch.IsLit(m.Chest),
                MancoReachable = ctx.Reachable,
                HeardNoise = heard,
                AlertCalled = alertCalled,
                SeesBody = seesBody,
            };
        }

        void OnStateShown(SoldierState s, in SoldierSenses senses)
        {
            switch (s)
            {
                case SoldierState.Glance: if (!senses.MancoHeadDown) Bark(SliceScript.BarkEyesDown); break;
                case SoldierState.Stare: Bark(SliceScript.BarkWho); break;
                case SoldierState.Approach: Bark(SliceScript.BarkStop); break;
                case SoldierState.Challenge: Bark(SliceScript.BarkLoad); break;
            }
        }

        public void Bark(ScriptLine line, bool force = false)
        {
            if (Overlay == null) return;
            if (!force && Time.time - _lastBark < 3.5f) return;
            _lastBark = Time.time;
            Overlay.Say(line.Text, 1.6f);
            Audio?.PlayAt(Random.value > 0.5f ? "shout_low" : "shout", Eye, force ? 1f : 0.45f, Random.Range(0.9f, 1.15f), 30f);
        }

        // ------------------------------------------------------------------ behaviour

        void Act(in StealthContext ctx, float dt)
        {
            var m = ctx.Manco;
            Vector3 mp = m.transform.position;
            float targetHeadYaw = 0f;
            Body.RaiseProp = Mathf.MoveTowards(Body.RaiseProp, Brain.State == SoldierState.Approach || Brain.State == SoldierState.Challenge || Brain.State == SoldierState.Alert ? 1f : 0f, dt * 2f);

            switch (Brain.State)
            {
                case SoldierState.Idle:
                    if (Route != null && Route.Length > 1) Patrol(dt);
                    else
                    {
                        MoveTowards(Post, Tuning.SoldierWalkSpeed, dt, 0.15f);
                        if ((transform.position - Post).sqrMagnitude < 0.05f) TurnTo(PostYaw, 120f, dt);
                        _scan += dt;
                        targetHeadYaw = Mathf.Sin(_scan * 0.6f) * 30f; // scan
                    }
                    break;
                case SoldierState.Glance:
                    targetHeadYaw = Mathf.Clamp(Mathf.DeltaAngle(_yaw, YawTo(mp)), -75f, 75f);
                    break;
                case SoldierState.Stare:
                    TurnTo(YawTo(mp), 180f, dt);
                    break;
                case SoldierState.Investigate:
                    TurnTo(YawTo(_noisePos), 200f, dt);
                    break;
                case SoldierState.Approach:
                    TurnTo(YawTo(mp), 220f, dt);
                    if (ctx.Reachable) MoveTowards(mp, Tuning.SoldierWalkSpeed * 1.2f, dt, 1.3f);
                    break;
                case SoldierState.Challenge:
                    TurnTo(YawTo(mp), 300f, dt);
                    Body.HandsOnWall = m.Chest + Vector3.up * 0.2f;
                    break;
                case SoldierState.Alert:
                    Vector3 chase = ctx.Reachable ? _lastKnown : new Vector3(_lastKnown.x, transform.position.y, _lastKnown.z);
                    TurnTo(YawTo(chase), 360f, dt);
                    MoveTowards(chase, Tuning.SoldierChaseSpeed, dt, ctx.Reachable ? 0.6f : 2.5f);
                    break;
            }
            if (Brain.State != SoldierState.Challenge) Body.HandsOnWall = null;
            Body.HeadYaw = Mathf.MoveTowards(Body.HeadYaw, targetHeadYaw, 240f * dt);
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
        }

        void TickScripted(float dt)
        {
            if (_scriptMove.HasValue)
            {
                Vector3 p = _scriptMove.Value;
                TurnTo(YawTo(p), 240f, dt);
                if (MoveTowards(p, _scriptSpeed, dt, 0.2f)) _scriptMove = null;
            }
            if (_scriptLook.HasValue) TurnTo(YawTo(_scriptLook.Value), 200f, dt);
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
        }

        void Patrol(float dt)
        {
            if (_pause > 0f) { _pause -= dt; return; }
            Vector3 target = Route[_wp];
            TurnTo(YawTo(target), 160f, dt);
            if (MoveTowards(target, Tuning.SoldierWalkSpeed, dt, 0.2f))
            {
                int next = _wp + _wpDir;
                if (next < 0 || next >= Route.Length) { _wpDir = -_wpDir; next = _wp + _wpDir; _pause = RoutePause; }
                _wp = next;
            }
        }

        bool MoveTowards(Vector3 target, float speed, float dt, float stopDistance)
        {
            Vector3 p = transform.position;
            Vector3 to = new Vector3(target.x - p.x, 0, target.z - p.z);
            float d = to.magnitude;
            if (d <= stopDistance) return true;
            Vector3 step = to / d * Mathf.Min(speed * dt, d - stopDistance);
            // Grey-box navigation: straight lines in streets and alleys; hold position if a wall is in the way.
            if (Physics.SphereCast(p + Vector3.up * 0.9f, 0.25f, step.normalized, out _, step.magnitude + 0.2f, Layers.World, QueryTriggerInteraction.Ignore))
                return false;
            transform.position = p + step;
            return false;
        }

        void TurnTo(float yaw, float degPerSec, float dt) => _yaw = Mathf.MoveTowardsAngle(_yaw, yaw, degPerSec * dt);

        float YawTo(Vector3 p)
        {
            Vector3 d = p - transform.position;
            return d.sqrMagnitude < 1e-4f ? _yaw : Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
        }
    }
}
