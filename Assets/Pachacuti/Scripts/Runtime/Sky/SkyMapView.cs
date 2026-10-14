using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>
    /// The map drawn on the sky above the Coricancha (pitch slice §8). Nothing is
    /// labelled with text. Stars are small; the Mayu and the dark llama are the two
    /// large shapes; three meaningful objects read instantly.
    /// </summary>
    public sealed class SkyMapView : MonoBehaviour
    {
        const float R = 690f;          // in front of the stars (800) so the map reads over them

        public static readonly float[] RoadAltitudes = { 90f, 83f, 77f, 71f, 65f, 59f, 52f }; // node 0 = Cusco … 6 = tambo
        public const float RoadAzimuth = 0f;               // the north road
        public static float ExitAzimuth => (float)CequeLayout.BearingCoricanchaToQenqo; // toward Qenqo, NE

        readonly List<Transform> _cuscoStars = new List<Transform>();
        readonly List<Transform> _roadStars = new List<Transform>();
        Transform _tupaStar, _fadingStar, _tambo, _llama, _exit, _thread, _path;
        readonly List<Transform> _smudge = new List<Transform>();
        Material _starMat, _tamboMat, _threadMat, _pathMat, _exitMat, _llamaRimMat;
        Mesh _threadMesh;
        Transform _cord;
        Material _cordMat;
        float _smudgeAmount = 1f, _threadTaut, _pathReveal;
        Vector3 _llamaDir;

        public SkyTarget Hovered { get; private set; }

        public static SkyMapView Create(Transform parent)
        {
            var go = new GameObject("Sky-map");
            go.transform.SetParent(parent, false);
            go.layer = Layers.Sky;
            var v = go.AddComponent<SkyMapView>();
            v.Build();
            go.SetActive(false);
            return v;
        }

        static Vector3 Dir(float alt, float az) => SkyDome.Direction(alt, az);

        Transform Star(string name, Vector3 dir, float size, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.layer = Layers.Sky;
            go.transform.localPosition = dir * R;
            go.transform.localRotation = Quaternion.LookRotation(dir);
            go.AddComponent<MeshFilter>().sharedMesh = MeshKit.Quad(size, size, Color.white);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go.transform;
        }

        void Build()
        {
            _starMat = MaterialKit.Glow(new Color(1f, 0.95f, 0.85f), 1.6f);
            _tamboMat = MaterialKit.Glow(new Color(1f, 0.9f, 0.7f), 1.4f);
            // The Cusco cluster: a tight bright cluster at the zenith, over the Coricancha.
            var rng = new System.Random(1438);
            for (int i = 0; i < 9; i++)
            {
                float a = (float)rng.NextDouble() * 360f, d = 0.4f + (float)rng.NextDouble() * 1.8f;
                _cuscoStars.Add(Star("Cusco", Dir(90f - d, a), 7f + (float)rng.NextDouble() * 4f, _starMat));
            }
            _fadingStar = _cuscoStars[4];
            _tupaStar = Star("Tupa's star (the compound)", Dir(88.4f, 200f), 6.5f, _starMat);
            _tupaStar.gameObject.SetActive(false);

            // The north road: five faint stars ending at a slightly brighter tambo.
            var faint = MaterialKit.Glow(new Color(0.9f, 0.9f, 1f), 0.7f);
            for (int i = 1; i <= SkyMapModel.FaintRoadStars; i++)
                _roadStars.Add(Star("Road " + i, Dir(RoadAltitudes[i], RoadAzimuth + (i % 2 == 0 ? 1.2f : -1.2f)), 5.5f, faint));
            _tambo = Star("Tambo", Dir(RoadAltitudes[SkyMapModel.TamboNode], RoadAzimuth), 10f, _tamboMat);

            // The army: Yacana, a dark-cloud llama on the road's second node, drawn as a smudge.
            _llamaDir = NodeDir(SkyMapModel.ArmyStartNode);
            var llamaRoot = new GameObject("Army (llama)").transform;
            llamaRoot.SetParent(transform, false);
            llamaRoot.gameObject.layer = Layers.Sky;
            llamaRoot.localPosition = _llamaDir * (R - 4f);
            llamaRoot.localRotation = Quaternion.LookRotation(-_llamaDir, Vector3.Cross(_llamaDir, Vector3.right));
            _llama = llamaRoot;
            // A faint pale patch behind it so a dark shape can be seen (Yacana is read against the Milky Way).
            var glow = new GameObject("Milky patch");
            glow.transform.SetParent(llamaRoot, false);
            glow.transform.localPosition = new Vector3(0, 0, 2f);
            glow.AddComponent<MeshFilter>().sharedMesh = MeshKit.Quad(85f, 60f, Color.white);
            var patch = MaterialKit.Alpha(new Color(0.5f, 0.55f, 0.75f, 0.16f));
            patch.SetFloat("_SoftEdge", 1f);
            glow.AddComponent<MeshRenderer>().sharedMaterial = patch;
            var shape = MeshKit.Polygon(LlamaShape.Outline, Color.white, new Vector2(-55f, 55f), new Vector2(0.5f, 0.45f));
            var dark = MaterialKit.Alpha(new Color(0.01f, 0.012f, 0.02f, 0.9f));
            for (int i = 0; i < 7; i++)
            {
                var s = new GameObject(i == 0 ? "Llama" : "Smudge");
                s.transform.SetParent(llamaRoot, false);
                s.AddComponent<MeshFilter>().sharedMesh = shape;
                s.AddComponent<MeshRenderer>().sharedMaterial = i == 0 ? dark : MaterialKit.Alpha(new Color(0.01f, 0.012f, 0.02f, 0.22f));
                if (i > 0)
                {
                    float a = i / 6f * Mathf.PI * 2f;
                    s.transform.localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * 4.5f + Vector3.back * 0.5f;
                    _smudge.Add(s.transform);
                }
            }
            _llamaRimMat = MaterialKit.Glow(new Color(0.8f, 0.85f, 1f), 0f);
            var rimPts = new List<Vector3>();
            foreach (var p in LlamaShape.Outline) rimPts.Add(new Vector3((p.X - 0.5f) * -55f, (p.Y - 0.45f) * 55f, -0.6f));
            rimPts.Add(rimPts[0]);
            var rim = new GameObject("Rim");
            rim.transform.SetParent(llamaRoot, false);
            rim.AddComponent<MeshFilter>().sharedMesh = MeshKit.Ribbon(rimPts, _ => 1.2f, new Vector3(0, 0, -100f), Color.white);
            rim.AddComponent<MeshRenderer>().sharedMaterial = _llamaRimMat;

            // The emperor's order thread: Cusco → llama, slack and dim.
            _threadMat = MaterialKit.Glow(new Color(1f, 0.78f, 0.35f), 0.35f);
            _threadMesh = new Mesh { name = "Thread" };
            _thread = MeshObj("Thread", _threadMesh, _threadMat);
            // The gold path along the road (lit when the army is ordered).
            _pathMat = MaterialKit.Glow(new Color(1f, 0.8f, 0.4f), 1.2f);
            _pathMat.SetFloat("_Reveal", 0f);
            var pathPts = new List<Vector3>();
            for (int n = SkyMapModel.ArmyStartNode; n <= SkyMapModel.TamboNode; n++) pathPts.Add(NodeDir(n) * (R - 2f));
            _path = MeshObj("Gold path", MeshKit.Ribbon(Densify(pathPts, 8), _ => 2.2f, Vector3.zero, Color.white), _pathMat);
            // The gold line out of the sky's edge toward the north-east: where Beat 5 is.
            _exitMat = MaterialKit.Glow(new Color(1f, 0.78f, 0.35f), 1.1f);
            _exitMat.SetFloat("_PulseSpeed", 0.5f);
            _exitMat.SetFloat("_PulseAmount", 0.7f);
            var exitPts = new List<Vector3>();
            for (int i = 0; i <= 24; i++) exitPts.Add(Dir(Mathf.Lerp(89f, 8f, i / 24f), ExitAzimuth) * (R - 2f));
            _exit = MeshObj("Exit line", MeshKit.Ribbon(exitPts, _ => 2.6f, Vector3.zero, Color.white), _exitMat);
            _exit.gameObject.SetActive(false);
            UpdateThread(0f);
        }

        Transform MeshObj(string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.layer = Layers.Sky;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go.transform;
        }

        static List<Vector3> Densify(List<Vector3> pts, int per)
        {
            var o = new List<Vector3>();
            for (int i = 0; i < pts.Count - 1; i++)
                for (int k = 0; k < per; k++) o.Add(Vector3.Slerp(pts[i], pts[i + 1], k / (float)per));
            o.Add(pts[pts.Count - 1]);
            return o;
        }

        public static Vector3 NodeDir(int node) =>
            node <= 0 ? Vector3.up : Dir(RoadAltitudes[Mathf.Clamp(node, 0, RoadAltitudes.Length - 1)], RoadAzimuth + (node > 0 && node < SkyMapModel.TamboNode ? (node % 2 == 0 ? 1.2f : -1.2f) : 0f));

        /// <summary>The cord in Manco's hand at the bottom of the frame (§8 step 1).</summary>
        public void AttachCord(Camera cam)
        {
            if (_cord != null) return;
            _cordMat = MaterialKit.Glow(new Color(1f, 0.85f, 0.5f), 0f);
            var pts = new List<Vector3>();
            for (int i = 0; i <= 12; i++) pts.Add(new Vector3(-0.09f + i * 0.015f, -0.215f - Mathf.Sin(i * 0.6f) * 0.008f, 0.5f));
            var go = new GameObject("Second cord");
            go.transform.SetParent(cam.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = MeshKit.Ribbon(pts, _ => 0.006f, new Vector3(0, 0, -1f), Color.white);
            go.AddComponent<MeshRenderer>().sharedMaterial = _cordMat;
            _cord = go.transform;
        }

        public void DetachCord()
        {
            if (_cord != null) Destroy(_cord.gameObject);
            _cord = null;
        }

        // ------------------------------------------------------------------ per frame

        public void Sync(SkyMapModel m, Camera cam, Vector3 cursorDir, Overlay overlay, float dt)
        {
            transform.position = cam.transform.position;
            float target = m.ArmySharp ? 0f : 1f;
            _smudgeAmount = Mathf.MoveTowards(_smudgeAmount, target, dt * 0.8f);
            for (int i = 0; i < _smudge.Count; i++)
            {
                float a = (i + 1) / 6f * Mathf.PI * 2f + Time.time * 0.3f;
                _smudge[i].localPosition = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0) * (4.5f * _smudgeAmount) + Vector3.back * 0.5f;
                _smudge[i].gameObject.SetActive(_smudgeAmount > 0.02f);
            }
            _llamaRimMat.SetFloat("_Intensity", Mathf.Lerp(0.9f, 0.05f, _smudgeAmount));

            // Army position along the road while moving.
            Vector3 from = NodeDir(m.ArmyNode), to = NodeDir(Mathf.Min(m.ArmyNode + 1, SkyMapModel.TamboNode));
            _llamaDir = Vector3.Slerp(from, to, m.Step == SkyStep.Moving ? Mathf.SmoothStep(0, 1, m.ArmyProgress) : 0f);
            _llama.localPosition = _llamaDir * (R - 4f);
            _llama.localRotation = Quaternion.LookRotation(-_llamaDir, Vector3.Cross(_llamaDir, Vector3.right));

            _threadTaut = Mathf.MoveTowards(_threadTaut, m.ThreadTaken ? 1f : 0f, dt * 1.5f);
            UpdateThread(_threadTaut);
            _pathReveal = Mathf.MoveTowards(_pathReveal, m.OrderGiven ? 1f : 0f, dt * 0.6f);
            _pathMat.SetFloat("_Reveal", _pathReveal);
            _tamboMat.SetFloat("_Intensity", m.TamboLit ? 3.2f + Mathf.Sin(Time.time * 2f) * 0.3f : 1.4f);
            _tupaStar.gameObject.SetActive(m.TupaStarLit);
            _fadingStar.gameObject.SetActive(!m.CuscoStarOut);
            _exit.gameObject.SetActive(m.Step == SkyStep.ExitOffered || m.Step == SkyStep.Done);

            if (_cordMat != null)
            {
                bool glowing = m.Step == SkyStep.CordGlowing;
                _cordMat.SetFloat("_Intensity", glowing ? 1.4f + Mathf.Sin(Time.time * 4f) * 0.6f : m.ArmySharp ? 0.15f : 0.3f);
            }

            Hovered = HitTest(m, cursorDir);
            overlay.Markers.Clear();
            if (_smudgeAmount > 0.05f)
            {
                // The faint "?" and an old-knot icon on the smudge (§8 "Confidence").
                var sp = cam.WorldToScreenPoint(cam.transform.position + _llamaDir * R);
                overlay.Markers.Add(new Overlay.Marker { Screen = sp + new Vector3(Screen.height * 0.05f, Screen.height * 0.03f, 0), Text = "?", Size = 1.2f * _smudgeAmount });
                overlay.Markers.Add(new Overlay.Marker { Screen = sp + new Vector3(Screen.height * 0.075f, -Screen.height * 0.02f, 0), Text = "∞", Size = 0.6f * _smudgeAmount });
            }
        }

        SkyTarget HitTest(SkyMapModel m, Vector3 cursor)
        {
            // Smallest angular distance relative to each target's hover radius wins.
            float bestScore = 1f;
            var hit = SkyTarget.None;
            void Try(Vector3 dir, float radius, SkyTarget t)
            {
                float score = Vector3.Angle(cursor, dir) / radius;
                if (score < bestScore) { bestScore = score; hit = t; }
            }
            Try(_llamaDir, 6.5f, SkyTarget.Llama);
            Try(NodeDir(SkyMapModel.TamboNode), 3.5f, SkyTarget.Tambo);
            for (int n = 1; n <= SkyMapModel.FaintRoadStars; n++) Try(NodeDir(n), 2.6f, SkyTarget.RoadStar);
            Try(Vector3.up, 3.5f, SkyTarget.Cusco);
            if (m.Step == SkyStep.ExitOffered)
                for (int i = 0; i <= 12; i++) Try(Dir(Mathf.Lerp(88f, 10f, i / 12f), ExitAzimuth), 4f, SkyTarget.ExitLine);
            return hit;
        }

        /// <summary>Where the exit line is, for drawing the cursor toward it (§8 Exit: "The cursor is drawn to it").</summary>
        public Vector3 ExitDirection => Dir(60f, ExitAzimuth);

        void UpdateThread(float taut)
        {
            // Origin moves from the dead centre (Cusco) to the roof under Manco's feet (bottom of the frame).
            Vector3 origin = Vector3.Slerp(Vector3.up, Dir(36f, 0f), taut);
            Vector3 end = _llamaDir;
            var pts = new List<Vector3>();
            for (int i = 0; i <= 24; i++)
            {
                float t = i / 24f;
                Vector3 d = Vector3.Slerp(origin, end, t);
                // Slack: sags (toward the horizon) in the middle until it pulls taut.
                d = Vector3.Slerp(d, Dir(60f, 25f), Mathf.Sin(t * Mathf.PI) * 0.12f * (1f - taut));
                pts.Add(d * (R - 1f));
            }
            MeshKit.Ribbon(pts, _ => Mathf.Lerp(1.2f, 2.2f, taut), Vector3.zero, Color.white, _threadMesh);
            _threadMat.SetFloat("_Intensity", Mathf.Lerp(0.35f, 2.4f, taut));
            _threadMat.SetFloat("_PulseSpeed", taut > 0.5f ? 0.8f : 0f);
            _threadMat.SetFloat("_PulseAmount", taut > 0.5f ? 0.5f : 0f);
        }
    }
}
