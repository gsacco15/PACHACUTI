using System.Collections;
using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pachacuti
{
    /// <summary>
    /// The Qenqo vision (pitch slice §9 shot list). "The cut is a material/post swap,
    /// not a load: two lighting rigs and two post stacks pre-built; the vision rig is
    /// a single uniform toggle." The player keeps camera look (±30°) but not movement.
    /// Iconography is real Inca and Andean only: tocapu grids, the chakana as the
    /// channel's fork, the Yacana llama, the Mayu as a river.
    /// </summary>
    public sealed class VisionDirector : MonoBehaviour
    {
        public QenqoScene Q;
        public SkyDome Sky;
        public CameraRig Rig;
        public AudioDirector Audio;
        public PostFx Post;
        public Overlay UI;
        public Manco Manco;
        public WorldState World;
        public PlaytestLog Log;
        public Light Moon;
        /// <summary>Facilitator skip (F8): jumps to the return, then to the title.</summary>
        public System.Func<bool> ConsumeSkip;

        Light _sun;
        Transform _ceques, _llama, _condor;
        Mesh _cequeMesh, _llamaMesh;
        Material _cequeMat;
        readonly List<List<Vector3>> _cequePts = new List<List<Vector3>>();
        Vector3[] _llamaBase;
        Vector3 _vantage;
        int _snapshot = -1;
        public float Time01 { get; private set; }
        public float T { get; private set; }

        public void Build()
        {
            var sunGo = new GameObject("Vision sun");
            sunGo.transform.SetParent(transform, false);
            _sun = sunGo.AddComponent<Light>();
            _sun.type = LightType.Directional;
            _sun.shadows = LightShadows.None;
            _sun.enabled = false;
            _vantage = Q.ChannelHead + Vector3.up * 1.6f;
            BuildCeques();
            BuildLlama();
            BuildCondor();
        }

        // ------------------------------------------------------------------ the run

        public IEnumerator Run()
        {
            T = 0f;
            float yawSample = 0f;
            var shown = (VisionShot)(-1);
            bool emptied = false, crack = false, goldRoom = false, fadeUp = false;
            while (T < VisionTimeline.Start(VisionShot.Title))
            {
                var shot = VisionTimeline.ShotAt(T);
                float local = VisionTimeline.LocalTime(T);
                if (shot != shown)
                {
                    EnterShot(shot);
                    shown = shot;
                }
                switch (shot)
                {
                    case VisionShot.Channel:
                        Q.ChannelLight.SetFloat("_Reveal", Mathf.Clamp01(local / VisionTimeline.ChannelRunSeconds));
                        Q.ForkLight.SetFloat("_Reveal", Mathf.Clamp01((local - VisionTimeline.ChannelRunSeconds) / 0.8f));
                        break;
                    case VisionShot.Lines:
                        Snap(local);
                        break;
                    case VisionShot.Llama:
                        WalkLlama(local, VisionTimeline.End(VisionShot.Llama) - VisionTimeline.Start(VisionShot.Llama));
                        break;
                    case VisionShot.Stones:
                        RiseStones(local);
                        break;
                    case VisionShot.Pacha:
                        RiseStones(local + 15f);
                        int snap = VisionTimeline.StrobeSnapshot(T, World.ReducedFlash);
                        if (snap != _snapshot) { ApplySnapshot(snap); Audio.Play("land_small", 0.5f, 0.8f + 0.05f * _snapshot); }
                        _condor.localRotation = Quaternion.Euler(0, local * 25f, 0);
                        break;
                    case VisionShot.Square:
                        if (!crack && T >= VisionTimeline.HarquebusAt) { crack = true; Audio.Play("harquebus", 1f); }
                        if (!emptied && T >= VisionTimeline.SquareEmptiesAt)
                        {
                            // In one frame: empty except for bodies.
                            emptied = true;
                            Q.Crowd.gameObject.SetActive(false);
                            Q.Bodies.gameObject.SetActive(true);
                        }
                        if (!goldRoom && T >= VisionTimeline.GoldRoomAt) { goldRoom = true; EnterGoldRoom(); }
                        if (goldRoom)
                        {
                            float g = Mathf.Clamp01((T - VisionTimeline.GoldRoomAt) / 5f);
                            Q.GoldPlane.position = Layout.GoldRoomOrigin + Vector3.up * (2.4f * Mathf.SmoothStep(0, 1, g));
                            Q.GoldRunMaterial.SetFloat("_Reveal", Mathf.Clamp01(g * 1.6f));
                        }
                        break;
                    case VisionShot.Return:
                        if (!fadeUp && local > 1.2f) { fadeUp = true; UI.FadeTo(0f, 2f); }
                        break;
                }

                if (ConsumeSkip != null && ConsumeSkip())
                    T = shot < VisionShot.Return ? VisionTimeline.Start(VisionShot.Return) : VisionTimeline.Start(VisionShot.Title);

                yawSample += Time.deltaTime;
                if (yawSample >= 1f / PlaytestLog.VisionYawSampleHz)
                {
                    yawSample = 0f;
                    Log.AddVisionYaw(Rig.transform.eulerAngles.y);
                }
                T += Time.deltaTime;
                Time01 = T / Tuning.VisionSeconds;
                yield return null;
            }
            World.VisionSeen = true;
        }

        void EnterShot(VisionShot shot)
        {
            Rig.FreeLook = Vector2.zero;
            switch (shot)
            {
                case VisionShot.Touch:
                    // Manco's hand on the stone. One beat of nothing. Breath stops.
                    Audio.BreathMuted = true;
                    Audio.SetAmbience(Ambience.Silence);
                    Manco.Body.HandsOnWall = Q.Pulse.position;
                    RailLook(Q.ChannelHead - Q.Axis * 1.4f + Q.Across * 0.5f + Vector3.up * 1.7f, Q.Pulse.position);
                    break;
                case VisionShot.Cut:
                    // THE CUT: single frame. Fog gone; sky deep indigo; Milky Way ×3, a river with current.
                    ApplyVisionRig();
                    Audio.Play("subhit", 1f);
                    Audio.Play("quena", 0.7f);
                    RailLook(Q.ChannelHead - Q.Axis * 4.5f + Q.Across * 1.8f + Vector3.up * 3.4f, Q.Origin + Q.Axis * 3f + Vector3.up * 5f);
                    break;
                case VisionShot.Channel:
                    Audio.Loop(0, "water", 0.55f, 0.6f); // water-on-stone, pitched down
                    break;
                case VisionShot.Lines:
                    _ceques.gameObject.SetActive(true);
                    Audio.Loop(1, "hum", 0.0f);
                    Audio.SetChoir(0.5f); // a Quechua choir enters (placeholder pad)
                    RailLook(_vantage, Q.CoricanchaPoint + Vector3.up * 40f);
                    break;
                case VisionShot.Llama:
                    _llama.gameObject.SetActive(true);
                    Audio.SetDrone(0.6f, 0.5f); // the drone drops an octave
                    RailLook(_vantage, _vantage + SkyDome.Direction(32f, 165f) * 100f);
                    break;
                case VisionShot.Stones:
                    _llama.gameObject.SetActive(false);
                    Audio.StopLoop(0);
                    Audio.Loop(0, "grind", 0.8f, 0.8f);
                    SetChannelVisible(false);
                    Q.Interior.gameObject.SetActive(true);
                    Q.CaveLight.intensity = 4f;
                    RailLook(Q.Origin - Q.Across * 22f - Q.Axis * 6f + Vector3.up * 6f, Q.Origin + Vector3.up * 4f);
                    break;
                case VisionShot.Pacha:
                    // Time folds: noon, dawn, snow, rain, night, cycling faster; the three worlds stack.
                    _condor.gameObject.SetActive(true);
                    Q.Serpent.gameObject.SetActive(true);
                    _snapshot = -1;
                    RailLook(Q.Origin + Q.Axis * 22f - Q.Across * 10f + Vector3.up * 5f, Q.Origin + Q.Axis * 5f + Vector3.up * 5f);
                    break;
                case VisionShot.Square:
                    EnterCajamarca();
                    break;
                case VisionShot.Return:
                    Return();
                    break;
            }
        }

        void RailLook(Vector3 pos, Vector3 target)
        {
            Rig.SetRail(pos, Quaternion.LookRotation((target - pos).normalized, Vector3.up), Tuning.VisionFreeLookDegrees);
        }

        // ------------------------------------------------------------------ rigs

        void ApplyVisionRig()
        {
            RenderSettings.fog = false;
            Ambient.Set(new Color(0.11f, 0.08f, 0.24f));
            if (Moon != null) Moon.enabled = false;
            _sun.enabled = true;
            _sun.color = new Color(1f, 0.78f, 0.45f);
            _sun.intensity = 0.45f;
            _sun.transform.rotation = Quaternion.LookRotation(-(Q.CoricanchaPoint - Q.Origin).normalized + Vector3.down * 0.3f);
            Sky.SetVision();
            Post.Set(PostRig.Vision);
            Post.VisionTint(new Color(1f, 0.95f, 1.05f), 0.6f);
            Rig.Cam.farClipPlane = 4800f;
            Rig.Cam.backgroundColor = new Color(0.03f, 0.02f, 0.08f);
            foreach (var p in new[] { Q.Snow, Q.Rain }) p.gameObject.SetActive(false);
        }

        public void ApplyNightRig()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = Greybox.Palette.Night;
            RenderSettings.fogDensity = 0.033f;
            Ambient.Set(Greybox.Palette.NightAmbient);
            if (Moon != null) Moon.enabled = true;
            if (_sun != null) _sun.enabled = false;
            Sky.SetNight();
            Post.Set(PostRig.Night);
            Rig.Cam.farClipPlane = 1100f;
            Rig.Cam.backgroundColor = Greybox.Palette.Night;
        }

        /// <summary>The five pre-lit snapshots of the time-fold (§9 VFX "Time strobe").</summary>
        void ApplySnapshot(int i)
        {
            _snapshot = i;
            Q.Snow.gameObject.SetActive(i == 2);
            Q.Rain.gameObject.SetActive(i == 3);
            if (i == 2) Q.Snow.GetComponent<ParticleSystem>().Play();
            if (i == 3) Q.Rain.GetComponent<ParticleSystem>().Play();
            switch (i)
            {
                case 0: // noon
                    Snapshot(new Color(1f, 0.96f, 0.88f), 1.5f, 70f, 30f, new Color(0.5f, 0.52f, 0.58f), new Color(30f, 42f, 70f), 1.1f, false, 0f, Color.white);
                    break;
                case 1: // dawn
                    Snapshot(new Color(1f, 0.6f, 0.35f), 1f, 6f, 110f, new Color(0.28f, 0.2f, 0.2f), new Color(28f, 13f, 10f), 1f, false, 0f, Color.white);
                    break;
                case 2: // snow
                    Snapshot(new Color(0.9f, 0.92f, 1f), 0.45f, 40f, 200f, new Color(0.5f, 0.52f, 0.56f), new Color(22f, 24f, 28f), 1f, true, 0.02f, new Color(0.62f, 0.64f, 0.68f));
                    break;
                case 3: // rain
                    Snapshot(new Color(0.7f, 0.75f, 0.85f), 0.25f, 35f, 250f, new Color(0.17f, 0.19f, 0.23f), new Color(8f, 9f, 12f), 1f, true, 0.03f, new Color(0.2f, 0.22f, 0.27f));
                    break;
                default: // night (the vision's own)
                    ApplyVisionRig();
                    Post.VisionTint(new Color(1f, 0.95f, 1.05f), 0.6f);
                    return;
            }
            Post.VisionTint(Color.white, 0.2f);
        }

        void Snapshot(Color sun, float intensity, float alt, float az, Color ambient, Color sky, float exposure, bool fog, float fogDensity, Color fogColor)
        {
            _sun.enabled = true;
            _sun.color = sun;
            _sun.intensity = intensity;
            _sun.transform.rotation = Quaternion.LookRotation(-SkyDome.Direction(alt, az));
            Ambient.Set(ambient);
            RenderSettings.fog = fog;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = fogColor;
            Sky.SetDay(sky, exposure, 0f);
            Rig.Cam.backgroundColor = fogColor;
        }

        // ------------------------------------------------------------------ ceque threads

        void BuildCeques()
        {
            Vector3 C = Q.CoricanchaPoint;
            foreach (var line in CequeLayout.Generate())
            {
                var pts = new List<Vector3>();
                Vector3 dir = Layout.Dir((float)line.BearingDeg);
                const int n = 40;
                for (int i = 0; i <= n; i++)
                {
                    Vector3 p = C + dir * ((float)line.LengthM * i / n);
                    float ground = Q.HeightAt(p.x, p.z);
                    p.y = Mathf.Max(ground + 2.5f, i == 0 ? C.y + 6f : ground + 2.5f);
                    if (line.ThroughQenqo && Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(Q.Origin.x, 0, Q.Origin.z)) < 40f)
                        p.y = Mathf.Max(p.y, 4.15f);
                    pts.Add(p);
                }
                if (line.ThroughQenqo)
                {
                    // Make sure one thread runs exactly under Manco's feet.
                    int k = Closest(pts, Q.ChannelHead);
                    pts[k] = new Vector3(Q.ChannelHead.x, Q.ChannelHead.y + 0.12f, Q.ChannelHead.z);
                }
                _cequePts.Add(pts);
            }
            _cequeMesh = new Mesh { name = "Ceques", indexFormat = IndexFormat.UInt32 };
            _cequeMat = MaterialKit.Glow(new Color(1f, 0.78f, 0.35f), 1.6f);
            _cequeMat.SetFloat("_PulseSpeed", 0.25f);
            _cequeMat.SetFloat("_PulseAmount", 0.4f);
            _cequeMat.SetFloat("_PulseWidth", 0.05f);
            _cequeMat.SetFloat("_Wobble", 0.35f);
            var go = new GameObject("Ceque threads");
            go.transform.SetParent(Q.Root, false);
            go.AddComponent<MeshFilter>().sharedMesh = _cequeMesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = _cequeMat;
            _ceques = go.transform;
            RebuildCeques(0f);
            go.SetActive(false);
        }

        static int Closest(List<Vector3> pts, Vector3 p)
        {
            int best = 0;
            float bd = float.MaxValue;
            for (int i = 0; i < pts.Count; i++)
            {
                float d = (pts[i] - p).sqrMagnitude;
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        /// <summary>41 threads snap taut outward (§9 0:10): a damped sag, revealed from the Coricancha outward.</summary>
        void Snap(float local)
        {
            float sag = Mathf.Exp(-5f * local) * Mathf.Cos(13f * local);
            RebuildCeques(local < 2f ? sag : 0f);
            _cequeMat.SetFloat("_Reveal", Mathf.Clamp01(local / 0.45f));
            Audio.Loop(1, "hum", Mathf.Clamp01(local / 0.6f) * 0.45f);
        }

        void RebuildCeques(float sag)
        {
            var verts = new List<Vector3>();
            var uv = new List<Vector2>();
            var cols = new List<Color>();
            var tris = new List<int>();
            foreach (var pts in _cequePts)
            {
                int baseIndex = verts.Count;
                float len = Vector3.Distance(pts[0], pts[pts.Count - 1]);
                for (int i = 0; i < pts.Count; i++)
                {
                    float u = i / (float)(pts.Count - 1);
                    Vector3 p = pts[i] + Vector3.down * (sag * Mathf.Sin(u * Mathf.PI) * 0.05f * len);
                    Vector3 tangent = (pts[Mathf.Min(i + 1, pts.Count - 1)] - pts[Mathf.Max(i - 1, 0)]).normalized;
                    Vector3 view = (_vantage - p).normalized;
                    Vector3 side = Vector3.Cross(tangent, view).normalized * (0.5f * (0.7f + 0.0042f * Vector3.Distance(p, _vantage)));
                    verts.Add(p - side);
                    verts.Add(p + side);
                    uv.Add(new Vector2(u, 0));
                    uv.Add(new Vector2(u, 1));
                    cols.Add(Color.white);
                    cols.Add(Color.white);
                    if (i < pts.Count - 1)
                    {
                        int a = baseIndex + i * 2;
                        tris.Add(a); tris.Add(a + 2); tris.Add(a + 1);
                        tris.Add(a + 1); tris.Add(a + 2); tris.Add(a + 3);
                    }
                }
            }
            _cequeMesh.Clear();
            _cequeMesh.SetVertices(verts);
            _cequeMesh.SetUVs(0, uv);
            _cequeMesh.SetColors(cols);
            _cequeMesh.SetTriangles(tris, 0);
            _cequeMesh.RecalculateBounds();
        }

        // ------------------------------------------------------------------ the llama stands up and walks

        void BuildLlama()
        {
            var root = new GameObject("Yacana (vision)");
            root.transform.SetParent(Sky.transform, false);
            root.layer = Layers.Sky;
            _llamaMesh = MeshKit.Polygon(LlamaShape.Outline, Color.white, new Vector2(-150f, 150f), new Vector2(0.5f, 0.3f));
            _llamaBase = _llamaMesh.vertices;
            var body = new GameObject("Body");
            body.transform.SetParent(root.transform, false);
            body.AddComponent<MeshFilter>().sharedMesh = _llamaMesh;
            body.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Alpha(new Color(0.008f, 0.006f, 0.02f, 0.94f));
            // Its two eyes, α and β Centauri, stay lit.
            foreach (var eye in new[] { LlamaShape.EyeAlpha, LlamaShape.EyeBeta })
            {
                var e = new GameObject("Eye");
                e.transform.SetParent(root.transform, false);
                e.transform.localPosition = new Vector3((eye.X - 0.5f) * -150f, (eye.Y - 0.3f) * 150f, -1f);
                e.AddComponent<MeshFilter>().sharedMesh = MeshKit.Quad(7f, 7f, Color.white);
                e.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Glow(new Color(1f, 0.95f, 0.85f), 3f);
            }
            _llama = root.transform;
            root.SetActive(false);
        }

        void WalkLlama(float local, float duration)
        {
            // Detach (lying along the Mayu) → stand up (3 s) → walk across the sky.
            float stand = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(local / 3f));
            float walk = Mathf.Clamp01((local - 3f) / (duration - 3f));
            float alt = Mathf.Lerp(16f, 46f, walk), az = Mathf.Lerp(150f, 205f, walk);
            Vector3 dir = SkyDome.Direction(alt, az);
            _llama.localPosition = dir * 700f;
            Vector3 upOnSky = Vector3.ProjectOnPlane(Vector3.up, dir).normalized;
            _llama.localRotation = Quaternion.LookRotation(-dir, upOnSky) * Quaternion.Euler(0, 0, Mathf.Lerp(-80f, 0f, stand));
            var v = (Vector3[])_llamaBase.Clone();
            float phase = local * 5f;
            for (int i = 0; i < v.Length; i++)
            {
                float y01 = v[i].y / 150f + 0.3f;
                if (y01 > LlamaShape.LegTop) continue;
                bool front = (-v[i].x / 150f + 0.5f) < LlamaShape.FrontLegMaxX;
                float swing = Mathf.Sin(phase + (front ? 0f : Mathf.PI)) * 0.09f * (LlamaShape.LegTop - y01) / LlamaShape.LegTop;
                v[i].x += swing * 150f * walk;
            }
            _llamaMesh.vertices = v;
            _llamaMesh.RecalculateBounds();
        }

        // ------------------------------------------------------------------ condor above (Hanan Pacha)

        void BuildCondor()
        {
            var wing = new List<P2>
            {
                new P2(-1f, 0.05f), new P2(-0.7f, 0.12f), new P2(-0.35f, 0.08f), new P2(-0.12f, 0.15f), new P2(0f, 0.3f), new P2(0.12f, 0.15f),
                new P2(0.35f, 0.08f), new P2(0.7f, 0.12f), new P2(1f, 0.05f), new P2(0.6f, -0.02f), new P2(0.15f, -0.08f), new P2(0.06f, -0.3f),
                new P2(-0.06f, -0.3f), new P2(-0.15f, -0.08f), new P2(-0.6f, -0.02f),
            };
            var root = new GameObject("Condor");
            root.transform.SetParent(Sky.transform, false);
            root.layer = Layers.Sky;
            var body = new GameObject("Silhouette");
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = SkyDome.Direction(76f, 30f) * 640f;
            body.transform.localRotation = Quaternion.LookRotation(-SkyDome.Direction(76f, 30f), Vector3.forward);
            body.AddComponent<MeshFilter>().sharedMesh = MeshKit.Polygon(wing, Color.white, new Vector2(90f, 90f), Vector2.zero);
            body.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Alpha(new Color(0.01f, 0.01f, 0.015f, 0.95f));
            _condor = root.transform;
            root.SetActive(false);
        }

        // ------------------------------------------------------------------ stones

        void RiseStones(float local)
        {
            for (int i = 0; i < Q.Blocks.Count; i++)
            {
                if (i == 1) continue; // the block Manco stands on stays: his hand is still on the stone
                float start = (i % 6) * 0.35f;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((local - start) / 4f));
                float hang = Mathf.Sin(UnityEngine.Time.time * 0.7f + i) * 0.15f * k;
                Vector3 rest = Q.BlockRest[i];
                Vector3 outward = new Vector3(rest.x - Q.Origin.x, 0, rest.z - Q.Origin.z).normalized;
                Q.Blocks[i].position = rest + Vector3.up * ((3f + (i % 4) * 1.3f) * k + hang) + outward * (1.5f * k);
                Q.Blocks[i].rotation = Quaternion.LookRotation(Q.Axis) * Quaternion.Euler(8f * k * ((i % 3) - 1), 6f * k * ((i % 2) * 2 - 1), 5f * k);
            }
        }

        void SetChannelVisible(bool visible)
        {
            foreach (Transform t in Q.Root)
                if (t.name.StartsWith("Channel") || t.name.StartsWith("Fork")) t.gameObject.SetActive(visible);
        }

        // ------------------------------------------------------------------ the square and the gold

        void EnterCajamarca()
        {
            // Hard cut again: a daylight plaza Manco has never seen. Silence.
            Audio.HardSilence();
            Audio.StopLoop(0);
            Audio.StopLoop(1);
            _condor.gameObject.SetActive(false);
            foreach (var p in new[] { Q.Snow, Q.Rain }) p.gameObject.SetActive(false);
            Q.Cajamarca.gameObject.SetActive(true);
            Q.Crowd.gameObject.SetActive(true);
            Q.Bodies.gameObject.SetActive(false);
            RenderSettings.fog = false;
            Ambient.Set(new Color(0.55f, 0.55f, 0.58f));
            _sun.enabled = true;
            _sun.color = new Color(1f, 0.95f, 0.85f);
            _sun.intensity = 1.6f;
            _sun.transform.rotation = Quaternion.LookRotation(-SkyDome.Direction(55f, 300f));
            Sky.SetDay(new Color(30f, 40f, 62f), 1.1f, 0f);
            Post.Set(PostRig.Day);
            Rig.Cam.backgroundColor = new Color(0.55f, 0.65f, 0.8f);
            RailLook(Q.CajamarcaCamera, Q.CajamarcaLook);
        }

        void EnterGoldRoom()
        {
            Q.Cajamarca.gameObject.SetActive(false);
            Q.GoldRoom.gameObject.SetActive(true);
            Ambient.Set(new Color(0.05f, 0.04f, 0.03f));
            _sun.enabled = false;
            Post.Set(PostRig.Vision);
            Post.VisionTint(new Color(1f, 0.92f, 0.8f), 0.4f);
            Rig.Cam.backgroundColor = Color.black;
            RailLook(Q.GoldRoomCamera, Q.GoldRoomLook);
        }

        // ------------------------------------------------------------------ return

        void Return()
        {
            // Black. Breathing. Fog. Qenqo, dry, the Pleiades. Manco's hand still on the stone.
            UI.FadeTo(1f, 0f);
            Q.GoldRoom.gameObject.SetActive(false);
            Q.Cajamarca.gameObject.SetActive(false);
            _ceques.gameObject.SetActive(false);
            _llama.gameObject.SetActive(false);
            _condor.gameObject.SetActive(false);
            Q.Serpent.gameObject.SetActive(false);
            Q.Interior.gameObject.SetActive(false);
            Q.CaveLight.intensity = 0f;
            for (int i = 0; i < Q.Blocks.Count; i++)
            {
                Q.Blocks[i].position = Q.BlockRest[i];
                Q.Blocks[i].rotation = Quaternion.LookRotation(Q.Axis);
            }
            SetChannelVisible(true);
            Q.ChannelLight.SetFloat("_Reveal", 0f);
            Q.ForkLight.SetFloat("_Reveal", 0f);
            ApplyNightRig();
            Audio.StopLoop(0);
            Audio.StopLoop(1);
            Audio.SetChoir(0f);
            Audio.SetDrone(0f, 1f);
            Audio.SetAmbience(Ambience.District);
            Audio.BreathMuted = false;
            RailLook(Q.ChannelHead - Q.Axis * 1.4f + Q.Across * 0.5f + Vector3.up * 1.7f, Q.Pulse.position + Q.Axis * 6f + Vector3.up * 5f);
        }
    }
}
