using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    public enum MancoMode
    {
        Ground,
        Climb,
        Mantle,
        Leap,      // context jump or hang-drop along a TraversalLink
        Scripted,  // a beat is moving him
    }

    /// <summary>
    /// Manco, ~16 (pitch slice §2 "Walk, run, climb, crouch, carry a porter's load,
    /// one lethal strike from behind"). Movement numbers and the breath model come
    /// from Core (§5). Camera-relative, keyboard and gamepad.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class Manco : MonoBehaviour
    {
        const float StandHeight = 1.6f, CrouchHeight = 1.1f, CrawlHeight = 0.75f, Radius = 0.28f;
        const float MantleReach = 0.5f;   // the last half metre is the pull-over (keeps the 6 m wall at ~75 breath)
        const float WallGap = 0.36f;

        public CharacterController Controller { get; private set; }
        public Humanoid Body { get; private set; }
        public readonly BreathModel Breath = new BreathModel();
        public AudioDirector Audio;

        public MancoMode Mode { get; private set; } = MancoMode.Ground;
        public bool Crouched { get; private set; } = true;   // §5: crouch-walk is the default on roofs and in the street
        public bool Carrying { get; private set; }
        public bool Running { get; private set; }
        public bool Crawling { get; private set; }
        public float Speed { get; private set; }
        public Vector3 Velocity { get; private set; }
        public bool Grounded => Controller != null && Controller.isGrounded;
        public bool AllowClimb = true;
        public bool AllowRun = true;
        /// <summary>Set by the street beat: the porter line sets his pace while he walks in it.</summary>
        public float? SpeedCap;
        public ClimbSurface Wall => _wall;
        public float WallHeight => _v;
        public Vector3 Forward => transform.forward;
        public Vector3 Chest => transform.position + Vector3.up * (Crouched || Crawling ? 0.75f : 1.15f);
        public Vector3 Head => transform.position + Vector3.up * (Crawling ? 0.5f : Crouched ? 1.0f : 1.5f);

        /// <summary>Head down = carrying, walking normally, not looking around (§6).</summary>
        public bool HeadDown => Carrying && !Running;

        public event System.Action<ClimbSurface> ClimbStarted;
        public event System.Action ContextUsed;
        public event System.Action<float> Landed;      // fall height
        public event System.Action LoadDropped;

        ClimbSurface _wall;
        float _u, _v, _lastLedge;
        float _verticalVel;
        float _fallStartY;
        bool _airborne;
        float _noiseTimer, _stepTimer;
        float _slideTarget = -1f;
        bool _scrapedAtTop;
        // Leap / mantle
        Vector3 _from, _to;
        float _t, _duration, _arc;
        LinkKind _leapKind;
        float _leapDropHeight;
        GameObject _droppedLoad;
        float _loadBusy;

        public static Manco Create(Transform parent, AudioDirector audio)
        {
            var go = new GameObject("Manco");
            go.transform.SetParent(parent, false);
            go.layer = Layers.Player;
            var cc = go.AddComponent<CharacterController>();
            cc.height = StandHeight;
            cc.radius = Radius;
            cc.center = new Vector3(0, StandHeight * 0.5f, 0);
            cc.slopeLimit = 50f;
            cc.stepOffset = 0.35f;
            cc.skinWidth = 0.03f;
            var m = go.AddComponent<Manco>();
            m.Controller = cc;
            m.Audio = audio;
            m.Body = Humanoid.Build(go.transform, "Body", new Color(0.45f, 0.13f, 0.09f), new Color(0.42f, 0.29f, 0.2f), 1.58f, HeadGear.Llautu, HandProp.None, new Color(0.2f, 0.15f, 0.1f));
            Layers.SetRecursively(go, Layers.Player);
            m.ApplyHeight(CrouchHeight);
            return m;
        }

        // ------------------------------------------------------------------ public control

        public void Teleport(Vector3 feet, float yaw)
        {
            Controller.enabled = false;
            transform.SetPositionAndRotation(feet, Quaternion.Euler(0, yaw, 0));
            Controller.enabled = Mode != MancoMode.Scripted;
            _verticalVel = 0f;
            _airborne = false;
            _wall = null;
            if (Mode != MancoMode.Scripted) Mode = MancoMode.Ground;
        }

        public void SetScripted(bool scripted)
        {
            if (scripted)
            {
                Mode = MancoMode.Scripted;
                Controller.enabled = false;
                Body.HandsOnWall = null;
            }
            else if (Mode == MancoMode.Scripted)
            {
                Mode = MancoMode.Ground;
                Controller.enabled = true;
                _verticalVel = 0f;
            }
        }

        /// <summary>Scripted move: place him and face a direction (beats drive this every frame).</summary>
        public void ScriptedPose(Vector3 feet, Vector3 facing)
        {
            transform.position = feet;
            facing.y = 0f;
            if (facing.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
        }

        public void SetCrouched(bool c)
        {
            if (Crouched == c) return;
            if (!c && !Headroom(StandHeight)) return;
            Crouched = c;
        }

        public void SetCarrying(bool carrying)
        {
            Carrying = carrying;
            Body.SetLoad(carrying);
            if (carrying)
            {
                Crouched = false;
                if (_droppedLoad != null) _droppedLoad.SetActive(false);
            }
        }

        /// <summary>Where the load lies after a drop (the hide branch leaves it in the street).</summary>
        public GameObject DroppedLoad => _droppedLoad != null && _droppedLoad.activeSelf ? _droppedLoad : null;

        public void DropLoad(bool loud = true)
        {
            if (!Carrying) return;
            SetCarrying(false);
            _loadBusy = Tuning.LoadDropSeconds;
            if (_droppedLoad == null)
            {
                _droppedLoad = Greybox.Primitive(transform.parent, PrimitiveType.Cube, "Dropped load", Vector3.zero, new Vector3(0.55f, 0.45f, 0.38f), MaterialKit.Lit(Greybox.Palette.Cloth));
                Interactable.Add(_droppedLoad, 1.5f, PickUpLoad, () => !Carrying && Mode == MancoMode.Ground);
            }
            _droppedLoad.SetActive(true);
            _droppedLoad.transform.position = transform.position + transform.forward * 0.6f + Vector3.up * 0.22f;
            _droppedLoad.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y + 20f, 0);
            if (loud)
            {
                NoiseBus.Emit(transform.position, NoiseKind.LoadDrop);
                Audio?.PlayAt("load_drop", transform.position, 0.9f);
            }
            LoadDropped?.Invoke();
        }

        public void PickUpLoad()
        {
            if (Carrying) return;
            _loadBusy = 0.6f;
            SetCarrying(true);
        }

        // ------------------------------------------------------------------ tick

        public void Tick(in InputFrame input, float dt, float cameraYaw)
        {
            _loadBusy -= dt;
            switch (Mode)
            {
                case MancoMode.Ground: TickGround(input, dt, cameraYaw); break;
                case MancoMode.Climb: TickClimb(input, dt); break;
                case MancoMode.Mantle: TickMantle(dt); break;
                case MancoMode.Leap: TickLeap(dt); break;
                case MancoMode.Scripted: Breath.Tick(Exertion.Scripted, dt); break;
            }
            UpdatePosture();
        }

        void TickGround(in InputFrame input, float dt, float cameraYaw)
        {
            // Low ceilings (tunnel): forced crouch, then crawl. Look a little ahead so the capsule
            // lowers before it reaches a low lip instead of snagging on it.
            Vector3 aheadDir = Quaternion.Euler(0, cameraYaw, 0) * new Vector3(input.Move.x, 0, input.Move.y);
            float headroom = HeadroomAt(transform.position);
            if (aheadDir.sqrMagnitude > 0.01f) headroom = Mathf.Min(headroom, HeadroomAt(transform.position + aheadDir.normalized * 0.5f));
            Crawling = headroom < 1.05f;
            bool forcedCrouch = headroom < StandHeight + 0.05f;
            if (input.CrouchToggled && !Carrying) SetCrouched(!Crouched);
            float wantHeight = Crawling ? CrawlHeight : (Crouched || forcedCrouch || Breath.IsGasping) ? CrouchHeight : StandHeight;
            ApplyHeight(wantHeight);

            Vector3 move = Quaternion.Euler(0, cameraYaw, 0) * new Vector3(input.Move.x, 0, input.Move.y);
            bool wantsMove = move.sqrMagnitude > 0.01f && Breath.CanMove && _loadBusy <= 0f;
            Running = wantsMove && input.RunHeld && AllowRun && !Carrying && !Crawling && !forcedCrouch;

            float speed = Crawling ? Tuning.CrawlSpeed
                        : Running ? Tuning.RunSpeed
                        : Carrying ? Tuning.WalkSpeed
                        : (Crouched || forcedCrouch) ? Tuning.CrouchWalkSpeed : Tuning.WalkSpeed;
            if (SpeedCap.HasValue && !Running) speed = Mathf.Min(speed, SpeedCap.Value);
            if (Running && Crouched) Crouched = false; // running stands him up

            Vector3 horizontal = wantsMove ? move.normalized * (speed * Mathf.Clamp01(move.magnitude)) : Vector3.zero;
            if (wantsMove)
            {
                var targetRot = Quaternion.LookRotation(move.normalized, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, 540f * dt);
            }

            // Gravity and falls.
            if (Controller.isGrounded)
            {
                if (_airborne) Land(_fallStartY - transform.position.y);
                _verticalVel = -2f;
                _airborne = false;
            }
            else
            {
                if (!_airborne) { _airborne = true; _fallStartY = transform.position.y; }
                _verticalVel = Mathf.Max(_verticalVel - 9.81f * dt, -30f);
            }
            Velocity = horizontal + Vector3.up * _verticalVel;
            Controller.Move(Velocity * dt);
            Speed = new Vector3(Controller.velocity.x, 0, Controller.velocity.z).magnitude;

            // Breath (§5).
            var exertion = !wantsMove || Speed < 0.1f ? Exertion.Idle
                         : Running ? Exertion.Run
                         : (Crouched || forcedCrouch || Crawling) ? Exertion.CrouchWalk : Exertion.Walk;
            Breath.Tick(exertion, dt);

            // Noise: running is loud (§5 "soldiers within 15 m turn").
            _noiseTimer -= dt;
            if (Running && Speed > 2f && _noiseTimer <= 0f)
            {
                NoiseBus.Emit(transform.position, NoiseKind.Run);
                _noiseTimer = 0.35f;
            }
            Footsteps(dt);

            if (input.DropLoadPressed && Carrying) DropLoad();
            if (input.ContextPressed) Context();
        }

        void Context()
        {
            if (Breath.IsGasping || Carrying) return;
            var link = TraversalLink.Find(transform.position, transform.forward);
            if (link != null)
            {
                StartLeap(link);
                ContextUsed?.Invoke();
                return;
            }
            if (AllowClimb)
            {
                var wall = ClimbSurface.FindGrab(transform.position, transform.forward, 0.9f, out var u, out var v);
                if (wall != null)
                {
                    StartClimb(wall, u, v);
                    ContextUsed?.Invoke();
                    return;
                }
            }
            // A smooth wall in front: he tries, and slides off with an audible scrape (§5).
            if (Physics.Raycast(Chest, transform.forward, out var hit, 0.8f, 1 << Layers.Default, QueryTriggerInteraction.Ignore)
                && Mathf.Abs(hit.normal.y) < 0.3f)
            {
                Audio?.PlayAt("scrape", hit.point, 0.8f);
                NoiseBus.Emit(transform.position, NoiseKind.Scrape);
                ContextUsed?.Invoke();
            }
        }

        // ------------------------------------------------------------------ climbing

        void StartClimb(ClimbSurface wall, float u, float v)
        {
            _wall = wall;
            _u = u;
            _v = Mathf.Clamp(v, 0f, MaxClimbV(wall));
            _lastLedge = wall.LedgeBelow(_v);
            _slideTarget = -1f;
            _scrapedAtTop = false;
            Mode = MancoMode.Climb;
            Controller.enabled = false;
            Crouched = false;
            PlaceOnWall();
            Audio?.PlayAt("step", transform.position + Vector3.up, 0.6f, 0.7f);
            ClimbStarted?.Invoke(wall);
        }

        static float MaxClimbV(ClimbSurface w) => w.CanMantle ? w.Height - MantleReach : Mathf.Max(0f, w.Height - 1.4f);

        void TickClimb(in InputFrame input, float dt)
        {
            var w = _wall;
            if (w == null) { ExitToGround(); return; }

            // Falling back to the last ledge after running out of breath.
            if (_slideTarget >= 0f)
            {
                _v = Mathf.MoveTowards(_v, _slideTarget, 6f * dt);
                PlaceOnWall();
                if (Mathf.Approximately(_v, _slideTarget))
                {
                    float fell = _slideTarget;
                    _slideTarget = -1f;
                    Audio?.PlayAt(fell <= 0.01f ? "land_mid" : "land_small", transform.position, 0.8f);
                    NoiseBus.Emit(transform.position, NoiseKind.Drop, 2f);
                    if (fell <= 0.01f) { ExitToGround(); return; }
                }
                Breath.Tick(Exertion.LedgeRest, dt);
                return;
            }

            float maxV = MaxClimbV(w);
            float vInput = Breath.IsGasping ? 0f : input.Move.y;
            float hInput = Breath.IsGasping ? 0f : input.Move.x;
            float climbSpeed = Tuning.ClimbSpeed * Breath.ClimbSpeedMultiplier;
            _v = Mathf.Clamp(_v + vInput * climbSpeed * dt, 0f, maxV);
            _u = Mathf.Clamp(_u + hInput * Tuning.ClimbLateralSpeed * dt, 0.3f, Mathf.Max(0.3f, w.Width - 0.3f));
            PlaceOnWall();

            bool onLedge = w.NearLedge(_v, out var ledge) && Mathf.Abs(vInput) < 0.1f;
            if (onLedge)
            {
                _v = Mathf.MoveTowards(_v, ledge, dt);
                _lastLedge = ledge;
            }
            else if (_v > _lastLedge + 0.4f || _v < _lastLedge - 0.05f) _lastLedge = w.LedgeBelow(_v);

            var ev = Breath.Tick(onLedge ? Exertion.LedgeRest : Exertion.Climb, dt, w.DrainMultiplier);
            if ((ev & BreathEvent.Exhausted) != 0)
            {
                // §5: "At 0: ... On a wall, he falls to the last ledge."
                _slideTarget = w.LedgeBelow(_v - 0.01f);
                return;
            }

            if (vInput > 0.3f && _v >= maxV - 0.01f)
            {
                if (w.CanMantle) { StartMantle(); return; }
                if (!_scrapedAtTop) { Audio?.PlayAt("scrape", Head, 0.7f); _scrapedAtTop = true; }
            }
            if (vInput < -0.3f && _v <= 0.01f) { ExitToGround(); return; }
            if (input.ContextPressed) LetGo();
        }

        void PlaceOnWall()
        {
            Vector3 feet = _wall.PointAt(_u, _v) + _wall.Normal * WallGap;
            transform.position = feet;
            transform.rotation = Quaternion.LookRotation(-_wall.Normal, Vector3.up);
            Body.HandsOnWall = _wall.PointAt(_u, _v + 1.45f) + _wall.Normal * 0.05f;
        }

        void LetGo()
        {
            var w = _wall;
            ExitToGround();
            if (w != null) transform.position += w.Normal * 0.25f;
            _airborne = true;
            _fallStartY = transform.position.y;
        }

        void StartMantle()
        {
            Mode = MancoMode.Mantle;
            _from = transform.position;
            _to = _wall.PointAt(_u, _wall.Height) - _wall.Normal * 0.55f;
            _t = 0f;
            _duration = 0.5f;
        }

        void TickMantle(float dt)
        {
            _t += dt;
            float k = Mathf.Clamp01(_t / _duration);
            // Up first, then over.
            Vector3 up = new Vector3(_from.x, Mathf.Lerp(_from.y, _to.y, Mathf.SmoothStep(0, 1, k * 1.6f)), _from.z);
            transform.position = Vector3.Lerp(up, _to, Mathf.SmoothStep(0, 1, (k - 0.4f) / 0.6f));
            Breath.Tick(Exertion.Climb, dt, _wall != null ? _wall.DrainMultiplier : 1f);
            if (k >= 1f)
            {
                Crouched = true;
                ExitToGround();
                Audio?.PlayAt("step", transform.position, 0.7f);
            }
        }

        void ExitToGround()
        {
            _wall = null;
            Body.HandsOnWall = null;
            Mode = MancoMode.Ground;
            Controller.enabled = true;
            _verticalVel = -2f;
            _airborne = false;
        }

        // ------------------------------------------------------------------ jumps and drops

        void StartLeap(TraversalLink link)
        {
            Vector3 dir = link.Direction;
            float along = Vector3.Dot(link.Landing - link.From.center, dir);
            _from = transform.position;
            _to = _from + dir * (along + Vector3.Dot(link.From.center - _from, dir));
            _to.y = link.Landing.y;
            _leapKind = link.Kind;
            float drop = _from.y - _to.y;
            if (link.Kind == LinkKind.Jump)
            {
                _duration = 0.62f;
                _arc = 0.7f + Mathf.Max(0f, -drop);
                _leapDropHeight = Mathf.Max(0f, drop);
            }
            else
            {
                _duration = 0.35f + Mathf.Sqrt(Mathf.Max(0.1f, drop) / 4.9f);
                _arc = 0f;
                _leapDropHeight = Mathf.Max(0f, drop - 1.6f); // hanging first takes the height off
            }
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            Mode = MancoMode.Leap;
            Controller.enabled = false;
            Crouched = false;
            _t = 0f;
        }

        void TickLeap(float dt)
        {
            _t += dt;
            float k = Mathf.Clamp01(_t / _duration);
            Vector3 p;
            if (_leapKind == LinkKind.Jump)
            {
                p = Vector3.Lerp(_from, _to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * _arc;
            }
            else
            {
                // Over the edge and hang (first 0.35 s), then let go.
                float hang = Mathf.Clamp01(_t / 0.35f);
                Vector3 edge = _from + (_to - _from).WithY(0f).normalized * 0.5f;
                if (_t < 0.35f) p = Vector3.Lerp(_from, edge + Vector3.down * 1.4f, Mathf.SmoothStep(0, 1, hang));
                else
                {
                    float f = Mathf.Clamp01((_t - 0.35f) / (_duration - 0.35f));
                    Vector3 hangPos = edge + Vector3.down * 1.4f;
                    p = Vector3.Lerp(hangPos, _to, f);
                    p.y = Mathf.Lerp(hangPos.y, _to.y, f * f);
                }
            }
            transform.position = p;
            if (k >= 1f)
            {
                transform.position = _to;
                ExitToGround();
                Land(_leapDropHeight + (_leapKind == LinkKind.Jump ? 0.8f : 0f));
            }
        }

        // ------------------------------------------------------------------ landing, noise, footsteps

        void Land(float height)
        {
            if (height < 0.3f) return;
            Landed?.Invoke(height);
            NoiseBus.Emit(transform.position, NoiseKind.Drop, height);
            var cost = BreathModel.DropCost(height);
            if (cost > 0f) Breath.Spend(cost);
            string clip = height < 1.5f ? "land_small" : height < 3.5f ? "land_mid" : "land_big";
            Audio?.PlayAt(clip, transform.position, Mathf.Clamp01(0.4f + height * 0.15f));
        }

        void Footsteps(float dt)
        {
            if (Speed < 0.3f || !Controller.isGrounded) return;
            _stepTimer -= dt * Speed;
            if (_stepTimer > 0f) return;
            _stepTimer = 1.1f;
            float vol = Running ? 0.55f : Crouched ? 0.12f : 0.25f;
            Audio?.PlayAt("step", transform.position, vol, Random.Range(0.9f, 1.15f), 20f);
        }

        // ------------------------------------------------------------------ helpers

        float HeadroomAbove() => HeadroomAt(transform.position);

        float HeadroomAt(Vector3 feet)
        {
            Vector3 p = feet + Vector3.up * 0.2f;
            if (Physics.SphereCast(p, Radius * 0.9f, Vector3.up, out var hit, 2f, Layers.World, QueryTriggerInteraction.Ignore))
                return hit.distance + 0.2f + Radius * 0.9f;
            return 3f;
        }

        bool Headroom(float height) => HeadroomAbove() >= height + 0.02f;

        void ApplyHeight(float h)
        {
            if (Mathf.Abs(Controller.height - h) < 0.01f) return;
            Controller.height = h;
            Controller.center = new Vector3(0, h * 0.5f, 0);
        }

        void UpdatePosture()
        {
            var b = Body;
            float crouchTarget = Crawling ? 1f : (Crouched || Breath.IsGasping) && Mode == MancoMode.Ground ? 0.85f : 0f;
            b.Crouch = Mathf.MoveTowards(b.Crouch, crouchTarget, Time.deltaTime * 4f);
            b.HeadDown = Mathf.MoveTowards(b.HeadDown, HeadDown ? 1f : 0f, Time.deltaTime * 3f);
            b.HandShake = Breath.Band == BreathBand.Shaking || Breath.Band == BreathBand.Empty ? 1f : 0f;
            if (Mode != MancoMode.Climb && Mode != MancoMode.Mantle) b.HandsOnWall = null;
        }
    }

    static class VectorExtensions
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
    }
}
