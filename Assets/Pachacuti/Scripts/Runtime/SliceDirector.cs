using System.Collections.Generic;
using System.Text;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>
    /// Entry point (the only component in Boot.unity). Builds the grey-box slice
    /// from code and runs the beat FSM: cold → roofs → compound → street → yard →
    /// tunnel → roof → qenqo → title (pitch slice §13). The beats themselves are in
    /// SliceDirector.Beats.cs.
    ///
    /// Facilitator keys (never shown to testers): F1 debug overlay, F8 skip to the
    /// next beat, F9 toggle the reduced-flash option for the vision.
    /// </summary>
    public sealed partial class SliceDirector : MonoBehaviour
    {
        // Core state: the miniature of the full game's "world state = the cord".
        public readonly WorldState World = new WorldState();
        public readonly BeatMachine Beats = new BeatMachine();
        public readonly PlaytestLog Log = new PlaytestLog();

        GameInput _input;
        InputFrame _frame;
        Overlay _ui;
        AudioDirector _audio;
        CameraRig _rig;
        PostFx _post;
        Light _moon;
        District _district;
        QenqoScene _qenqo;
        SkyDome _sky;
        SkyMapView _skyMap;
        QuipuView _quipu;
        VisionDirector _vision;
        Manco _manco;
        PorterLine _line;
        Humanoid _woman, _tupa, _ninan;
        readonly List<Humanoid> _column = new List<Humanoid>();
        readonly List<Soldier> _roofSoldiers = new List<Soldier>();
        readonly List<Soldier> _streetSoldiers = new List<Soldier>();
        Soldier _torchSoldier, _grateSoldier;
        readonly List<Vector3> _shoutsLastFrame = new List<Vector3>();
        readonly List<Vector3> _shoutsThisFrame = new List<Vector3>();

        // Hooks for PlayMode tests (Assets/Pachacuti/Tests/PlayMode) and facilitators.
        public Manco Manco => _manco;
        public CameraRig Rig => _rig;
        public KinsmanPhase KinsmanPhase { get; private set; }
        /// <summary>When set, replaces device input for the next frames (automated tests).</summary>
        public InputFrame? DebugInput;
        public void RequestSkip() => _skipRequested = true;

        bool _started;
        bool _playerControl;
        bool _skipRequested;
        bool _routeInteractToKinsman;
        Soldier _strikeTarget;
        float _strikeHeld;

        void Awake()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer) Application.targetFrameRate = 60; // WebGL follows the browser's frame pacing
            MaterialKit.Init();
            ApplyNightRenderSettings();
            Log.SessionId = System.Guid.NewGuid().ToString("N").Substring(0, 12);
            Log.BuildVersion = Application.version;
            World.ReducedFlash = ReadReducedFlashOption();

            var root = transform;
            _input = new GameInput();
            _ui = Overlay.Create(root);
            _audio = AudioDirector.Create(root);
            _rig = CameraRig.Create(root);
            _post = PostFx.Create(root, _rig.Cam);
            _moon = CreateMoon(root);

            _district = DistrictBuilder.Build(root);
            _qenqo = QenqoBuilder.Build(root);
            _sky = SkyDome.Create(root, ObservationSite.CuscoSliceNight);
            _sky.Follow = _rig.Cam;
            _skyMap = SkyMapView.Create(root);
            _manco = Manco.Create(root, _audio);
            _manco.ContextUsed += () => { if (_ui != null) _ui.HidePrompt(); };

            SpawnPeople(root);
            _quipu = QuipuView.Create(root, Layout.NinanSeat + Vector3.forward * 0.55f + Vector3.up * 1.0f, Vector3.forward);
            _vision = new GameObject("Vision").AddComponent<VisionDirector>();
            _vision.transform.SetParent(root, false);
            _vision.Q = _qenqo;
            _vision.Sky = _sky;
            _vision.Rig = _rig;
            _vision.Audio = _audio;
            _vision.Post = _post;
            _vision.UI = _ui;
            _vision.Manco = _manco;
            _vision.World = World;
            _vision.Log = Log;
            _vision.Moon = _moon;
            _vision.Build();

            // Static grey-box geometry batches into far fewer draw calls (§13 budget: ≤300 in the street).
            BatchStatic(_district.Root, _district.DrainCover, _district.CompoundDoor);

            _ui.FadeTo(1f, 0f);
            // Browsers need a click before audio and pointer lock (WebGL); elsewhere start at once.
            _ui.ShowBeginScreen = Application.platform == RuntimePlatform.WebGLPlayer;
            if (!_ui.ShowBeginScreen) Begin();
        }

        /// <summary>Static-batch everything under root except things that move (and billboards).</summary>
        static void BatchStatic(Transform root, params GameObject[] movers)
        {
            var list = new List<GameObject>();
            foreach (var mf in root.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.GetComponent<Billboard>() != null) continue;
                bool moving = false;
                foreach (var m in movers)
                    if (m != null && (mf.transform == m.transform || mf.transform.IsChildOf(m.transform))) { moving = true; break; }
                if (!moving) list.Add(mf.gameObject);
            }
            StaticBatchingUtility.Combine(list.ToArray(), root.gameObject);
        }

        static Light CreateMoon(Transform parent)
        {
            var go = new GameObject("Moon");
            go.transform.SetParent(parent, false);
            go.transform.rotation = Quaternion.LookRotation(-SkyDome.Direction(48f, 75f));
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.color = new Color(0.62f, 0.72f, 0.95f);
            l.intensity = 1.0f;
            // §13 budgets one moon shadow map. Off in the grey-box: the first web build was missing the
            // URP Lit shadow variant and drew lit surfaces black. Turn back on with the art pass.
            l.shadows = LightShadows.None;
            return l;
        }

        static void ApplyNightRenderSettings()
        {
            // Fog clamps draw distance to ~60 m (§4). Exp² with density 0.033 is ~2 % visibility at 60 m.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Greybox.Palette.Night;
            RenderSettings.fogDensity = 0.033f;
            Ambient.Set(Greybox.Palette.NightAmbient);
            RenderSettings.skybox = null;
        }

        static bool ReadReducedFlashOption()
        {
            // ?reducedflash=1 on the WebGL page URL, or remembered from F9.
            string url = Application.absoluteURL ?? "";
            if (url.Contains("reducedflash=1")) return true;
            try { return PlayerPrefs.GetInt("pachacuti.reducedFlash", 0) == 1; }
            catch { return false; }
        }

        void Begin()
        {
            if (_started) return;
            _started = true;
            _ui.ShowBeginScreen = false;
            LockCursor(true);
            StartCoroutine(RunSlice());
        }

        static void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        // ------------------------------------------------------------------ people

        void SpawnPeople(Transform root)
        {
            var people = new GameObject("People").transform;
            people.SetParent(root, false);

            // Beat 1: a torch patrol below the roofs (two soldiers, one torch), plus the column in the cold open.
            var p1 = Soldier.Spawn(people, "Roof patrol (torch)", Layout.RooftopPatrol[0], 180f, SoldierRules.Rooftop, true, _ui, _audio);
            p1.Route = Layout.RooftopPatrol;
            var p2 = Soldier.Spawn(people, "Roof patrol", Layout.RooftopPatrol[0] + Vector3.forward * 1.8f, 180f, SoldierRules.Rooftop, false, _ui, _audio);
            p2.Route = Offset(Layout.RooftopPatrol, Vector3.forward * 1.8f);
            _roofSoldiers.Add(p1);
            _roofSoldiers.Add(p2);
            for (int i = 0; i < 6; i++)
            {
                var h = Humanoid.Build(people, "Column " + i, new Color(0.24f, 0.26f, 0.2f), new Color(0.38f, 0.26f, 0.18f), 1.66f, HeadGear.SoldierCrest, HandProp.Torch);
                h.transform.position = new Vector3(-10f, 0f, 176f + i * 3.2f);
                h.transform.rotation = Quaternion.Euler(0, 180f, 0);
                h.RaiseProp = 0.4f;
                h.gameObject.SetActive(false);
                _column.Add(h);
            }

            // Beat 2: three at the checkpoint (one torch, two maces), two patrolling north–south.
            _torchSoldier = Soldier.Spawn(people, "Checkpoint torch", Layout.TorchSoldierPost, -20f, SoldierRules.Street, true, _ui, _audio);
            _streetSoldiers.Add(_torchSoldier);
            _streetSoldiers.Add(Soldier.Spawn(people, "Checkpoint mace W", Layout.MaceSoldierWestPost, 25f, SoldierRules.Street, false, _ui, _audio));
            _streetSoldiers.Add(Soldier.Spawn(people, "Checkpoint mace E", Layout.MaceSoldierEastPost, -30f, SoldierRules.Street, false, _ui, _audio));
            var s4 = Soldier.Spawn(people, "Street patrol N", new Vector3(-11.3f, 0f, 44f), 180f, SoldierRules.Street, false, _ui, _audio);
            s4.Route = new[] { new Vector3(-11.3f, 0f, 44f), new Vector3(-11.3f, 0f, -64f) };
            var s5 = Soldier.Spawn(people, "Street patrol S", new Vector3(-8.7f, 0f, -64f), 0f, SoldierRules.Street, false, _ui, _audio);
            s5.Route = new[] { new Vector3(-8.7f, 0f, -64f), new Vector3(-8.7f, 0f, 44f) };
            _streetSoldiers.Add(s4);
            _streetSoldiers.Add(s5);

            // The tunnel: a soldier's torch above a grate.
            _grateSoldier = Soldier.Spawn(people, "Above the grate", new Vector3(Layout.GrateX + 0.8f, 0f, Layout.TunnelZ1 + 1.4f), 200f, SoldierRules.Rooftop, true, _ui, _audio);

            foreach (var s in Soldier.All) { s.Active = false; s.Captured = OnCaptured; s.Shouted = OnShout; }

            _line = PorterLine.Create(people);
            _line.gameObject.SetActive(false);

            // The woman of the house; Tupa (cousin, Huáscar's house); Ninan (old quipu keeper).
            _woman = Humanoid.Build(people, "The woman", new Color(0.32f, 0.2f, 0.25f), new Color(0.4f, 0.28f, 0.2f), 1.5f, HeadGear.Shawl, HandProp.None, new Color(0.45f, 0.12f, 0.1f));
            _woman.transform.position = Layout.WomanPost;
            _woman.transform.rotation = Quaternion.Euler(0, 60f, 0);
            _woman.SetLoad(false);
            _tupa = Humanoid.Build(people, "Tupa", new Color(0.4f, 0.14f, 0.1f), new Color(0.42f, 0.29f, 0.2f), 1.62f, HeadGear.Llautu, HandProp.None, new Color(0.2f, 0.15f, 0.1f));
            _tupa.transform.position = _district.TupaDoorway;
            _tupa.transform.rotation = Quaternion.Euler(0, -90f, 0);
            _ninan = Humanoid.Build(people, "Ninan", new Color(0.38f, 0.36f, 0.33f), new Color(0.36f, 0.26f, 0.2f), 1.52f, HeadGear.Hat, HandProp.None, new Color(0.3f, 0.28f, 0.25f));
            _ninan.transform.position = Layout.NinanSeat;
            _ninan.transform.rotation = Quaternion.Euler(0, 0f, 0);
            _ninan.Seated = 1f;
            _ninan.HeadDown = 0.4f;

            // ~20 ambient commons: pressed into doorways and against the street walls; a few in the yard.
            var commonColors = new[] { new Color(0.4f, 0.33f, 0.26f), new Color(0.33f, 0.3f, 0.28f), new Color(0.46f, 0.36f, 0.24f) };
            int c = 0;
            foreach (var door in _district.Doorways)
            {
                if (Mathf.Abs(door.z - Layout.TupaDoorZ) < 0.5f) continue;
                for (int k = 0; k < 2; k++)
                    Common(people, door + new Vector3(0f, 0f, k == 0 ? -0.3f : 0.3f), door.x < Layout.LaneX ? 90f : -90f, commonColors[c++ % 3]);
            }
            foreach (float z in new[] { 38f, 25f, 15f, -2f, -18f, -30f, -40f, -52f, -60f })
                Common(people, new Vector3(z % 2 == 0 ? -11.72f : -8.28f, 0f, z), z % 2 == 0 ? 90f : -90f, commonColors[c++ % 3]);
            Common(people, new Vector3(-6f, 0, -92f), 0f, commonColors[0]);
            Common(people, new Vector3(9f, 0, -90f), -60f, commonColors[1]);
            Common(people, new Vector3(-9f, 0, -84f), 90f, commonColors[2]);
        }

        void Common(Transform parent, Vector3 pos, float yaw, Color color)
        {
            var h = Humanoid.Build(parent, "Common", color, new Color(0.4f, 0.28f, 0.2f), Random.Range(1.42f, 1.62f), Random.value > 0.5f ? HeadGear.Shawl : HeadGear.None, HandProp.None, color * 0.8f);
            h.transform.position = pos;
            h.transform.rotation = Quaternion.Euler(0, yaw, 0);
            h.Crouch = Random.value * 0.4f;
            h.HeadDown = 0.6f;
            _district.Commons.Add(h.transform);
        }

        static Vector3[] Offset(Vector3[] pts, Vector3 by)
        {
            var o = new Vector3[pts.Length];
            for (int i = 0; i < pts.Length; i++) o[i] = pts[i] + by;
            return o;
        }

        // ------------------------------------------------------------------ frame

        void Update()
        {
            float dt = Time.deltaTime;
            _frame = DebugInput ?? _input.Poll(dt);
            _ui.UsingGamepad = _frame.UsingGamepad;

            if (!_started)
            {
                if (_frame.AnyPressed) Begin();
                return;
            }
            if (Cursor.lockState != CursorLockMode.Locked && _frame.SelectPressed && Application.isFocused) LockCursor(true);
            if (_frame.DebugToggle) _ui.ShowDebug = !_ui.ShowDebug;
            if (_frame.SkipBeat) _skipRequested = true;
            if (_frame.ReducedFlashToggle)
            {
                World.ReducedFlash = !World.ReducedFlash;
                try { PlayerPrefs.SetInt("pachacuti.reducedFlash", World.ReducedFlash ? 1 : 0); } catch { }
                _ui.Say(World.ReducedFlash ? "(reduced flash on)" : "(reduced flash off)", 1.5f);
            }

            Beats.Tick(dt);
            World.Beat = Beats.Current;

            if (_playerControl && (_rig.Mode == CameraMode.Follow || _rig.Mode == CameraMode.Rail)) _rig.Look(_frame.Look);
            var input = _playerControl ? _frame : default(InputFrame);
            if (!_playerControl) input.Digit = -1;
            _manco.Tick(input, dt, _rig.Yaw);
            _rig.BreathSway = _manco.Breath.Strain;
            _audio.TickBreath(_manco.Breath, dt);

            TickStealth(dt);
            if (_playerControl && !_routeInteractToKinsman) TickInteract(dt);

            Torch.ApplyBudget(_rig.transform.position);
            NoiseBus.Prune(1f);
            if (_ui.ShowDebug) _ui.DebugText = DebugText();
        }

        void TickStealth(float dt)
        {
            _shoutsLastFrame.Clear();
            _shoutsLastFrame.AddRange(_shoutsThisFrame);
            _shoutsThisFrame.Clear();
            bool onGround = _manco.transform.position.y < 1.2f && _manco.transform.position.y > -0.5f;
            var ctx = new StealthContext
            {
                Manco = _manco,
                WithLine = _line.gameObject.activeSelf && _line.IsWithLine(_manco),
                Reachable = onGround,
                Dt = dt,
            };
            foreach (var s in Soldier.All)
            {
                if (!s.Active) continue;
                bool alertNear = false;
                foreach (var p in _shoutsLastFrame)
                    if (Vector3.Distance(p, s.transform.position) <= Tuning.AlertConvergeRadius) { alertNear = true; break; }
                var c = ctx;
                if (s.Brain.Rules == SoldierRules.Rooftop) c.Reachable = false;
                s.Tick(c, alertNear);
            }
        }

        void OnShout(Soldier s) => _shoutsThisFrame.Add(s.transform.position);

        bool _captured;
        void OnCaptured(Soldier s) => _captured = true;

        /// <summary>Broadcast an alert from a position (Tupa saying the name).</summary>
        void RaiseAlarm(Vector3 at) => _shoutsThisFrame.Add(at);

        // ------------------------------------------------------------------ interact: tap uses things, hold strikes from behind

        void TickInteract(float dt)
        {
            if (_manco.Mode != MancoMode.Ground) return;
            var i = _frame.Interact;
            if (i.Pressed)
            {
                _strikeTarget = FindStrikeTarget();
                _strikeHeld = 0f;
                if (_strikeTarget == null) Interactable.Nearest(_manco.Chest, _manco.Forward)?.OnTap?.Invoke();
            }
            if (_strikeTarget != null)
            {
                if (i.Held)
                {
                    _strikeHeld += dt;
                    if (_strikeHeld >= Tuning.KinsmanStrikeHoldSeconds)
                    {
                        // §2 "one lethal strike from behind": no knockouts; a body can be found.
                        _strikeTarget.Kill();
                        NoiseBus.Emit(_manco.transform.position, NoiseKind.Strike);
                        _audio.PlayAt("land_small", _strikeTarget.transform.position + Vector3.up, 0.8f, 0.7f);
                        _strikeTarget = null;
                    }
                }
                else if (i.Released)
                {
                    Interactable.Nearest(_manco.Chest, _manco.Forward)?.OnTap?.Invoke();
                    _strikeTarget = null;
                }
            }
        }

        Soldier FindStrikeTarget()
        {
            if (_manco.Carrying) return null;
            foreach (var s in Soldier.All)
            {
                if (!s.Active || s.IsDead || s.Brain.IsChasing) continue;
                Vector3 to = _manco.transform.position - s.transform.position;
                to.y = 0f;
                if (to.magnitude > 1.4f) continue;
                if (Vector3.Angle(s.transform.forward, to) < 110f) continue; // must be behind him
                return s;
            }
            return null;
        }

        // ------------------------------------------------------------------ debug

        string DebugText()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"PACHACUTI slice  {Beats.Current}  t={Beats.Clock:0.0}s  beat {Beats.TimeInBeat:0.0}s");
            sb.AppendLine($"breath {_manco.Breath.Value:0} {_manco.Breath.Band}  mode {_manco.Mode}  crouch {_manco.Crouched}  carry {_manco.Carrying}  speed {_manco.Speed:0.0}");
            if (_manco.Wall != null) sb.AppendLine($"wall '{_manco.Wall.Label}' v={_manco.WallHeight:0.0}/{_manco.Wall.Height:0.0}");
            sb.AppendLine($"lit {Torch.IsLit(_manco.Chest)}  withLine {(_line.gameObject.activeSelf && _line.IsWithLine(_manco))}  tupa {World.Tupa}  resets {World.StreetResets}");
            foreach (var s in Soldier.All)
                if (s.Active) sb.AppendLine($"  {s.Label,-22} {s.Brain.State,-11} susp {s.Brain.Suspicion:0}");
            sb.AppendLine($"reduced flash {World.ReducedFlash}   F8 skip beat");
            return sb.ToString();
        }
    }
}
