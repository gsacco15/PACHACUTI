using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;
using P = Pachacuti.Greybox.Palette;

namespace Pachacuti
{
    /// <summary>References the Qenqo beat and the vision need.</summary>
    public sealed class QenqoScene
    {
        public Transform Root;
        public Vector3 Origin;
        public Vector3 Axis;              // along the zigzag channel, toward the Pleiades
        public Vector3 Across;
        public readonly List<Vector3> WalkPath = new List<Vector3>();
        public Vector3 ChannelHead;       // where Manco stands to touch the stone
        public Interactable TouchPoint;
        public Transform Pulse;           // faint pulse in the stone that invites the touch
        public readonly List<Transform> Blocks = new List<Transform>();
        public readonly List<Vector3> BlockRest = new List<Vector3>();
        public Material ChannelLight;     // _Reveal 0..1 runs the light down the channel
        public Material ForkLight;
        public Light CaveLight;
        public Transform Interior;        // carved interior revealed when the blocks rise
        public Vector3 CoricanchaPoint;   // the Coricancha seen across the valley as a point of gold
        public Transform CoricanchaGlow;
        public Transform Terrain;
        public System.Func<float, float, float> HeightAt; // world x,z → terrain height (y)
        public Transform Serpent;
        public Transform Snow, Rain;

        // Cajamarca (vision shot 1:05)
        public Transform Cajamarca, Crowd, Bodies;
        public Vector3 CajamarcaCamera, CajamarcaLook;
        // The room filling with gold
        public Transform GoldRoom, GoldPlane;
        public Material GoldRunMaterial;
        public Vector3 GoldRoomCamera, GoldRoomLook;
    }

    /// <summary>
    /// Zone G: Qenqo as a detached 60 × 60 m scene (§4), its valley, and the two
    /// places the vision cuts to. Pre-split rock (~12 pieces) per §9 VFX spec.
    /// </summary>
    public static class QenqoBuilder
    {
        public static QenqoScene Build(Transform parent)
        {
            var q = new QenqoScene { Root = new GameObject("G Qenqo").transform, Origin = Layout.QenqoOrigin };
            q.Root.SetParent(parent, false);
            q.Axis = Layout.Dir(Layout.QenqoChannelAzimuth);
            q.Across = new Vector3(q.Axis.z, 0f, -q.Axis.x);

            Terrain(q);
            Rock(q);
            Valley(q);
            Weather(q);
            Cajamarca(q, parent);
            GoldRoom(q, parent);
            return q;
        }

        // ------------------------------------------------------------------ terrain

        static void Terrain(QenqoScene q)
        {
            Vector3 O = q.Origin;
            double bearing = CequeLayout.BearingCoricanchaToQenqo + 180.0;
            Vector3 toCor = Layout.Dir((float)bearing);
            Vector3 perp = new Vector3(toCor.z, 0, -toCor.x);
            float dist = (float)CequeLayout.DistanceCoricanchaToQenqoM;
            float drop = (float)CequeLayout.QenqoAboveCoricanchaM;
            q.CoricanchaPoint = O + toCor * dist + Vector3.down * drop;

            q.HeightAt = (x, z) =>
            {
                Vector3 p = new Vector3(x - O.x, 0, z - O.z);
                float u = Vector3.Dot(p, toCor), v = Mathf.Abs(Vector3.Dot(p, perp));
                float valley = -drop * Smooth(80f, dist, u) * (1f - 0.35f * Smooth(1200f, 2600f, v));
                float behind = 70f * Smooth(0f, 700f, -u);
                float flanks = 380f * Smooth(1800f, 3600f, v) + 250f * Smooth(dist + 600f, dist + 2800f, u);
                float noise = 9f * (Mathf.PerlinNoise(x * 0.004f, z * 0.004f) - 0.5f) + 3f * (Mathf.PerlinNoise(x * 0.03f, z * 0.03f) - 0.5f);
                float flat = Smooth(45f, 120f, p.magnitude);
                return (valley + behind + flanks + noise) * flat;
            };

            const int n = 90;
            const float size = 7000f;
            Vector3 c = O + toCor * (dist * 0.5f);
            var verts = new Vector3[(n + 1) * (n + 1)];
            var cols = new Color[verts.Length];
            var tris = new int[n * n * 6];
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    float x = c.x - size * 0.5f + size * i / n, z = c.z - size * 0.5f + size * j / n;
                    verts[j * (n + 1) + i] = new Vector3(x, q.HeightAt(x, z), z);
                    cols[j * (n + 1) + i] = Color.white;
                }
            int t = 0;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    int a = j * (n + 1) + i;
                    tris[t++] = a; tris[t++] = a + n + 1; tris[t++] = a + 1;
                    tris[t++] = a + 1; tris[t++] = a + n + 1; tris[t++] = a + n + 2;
                }
            var mesh = new Mesh { name = "Qenqo valley", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.vertices = verts;
            mesh.colors = cols;
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("Valley terrain");
            go.transform.SetParent(q.Root, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Lit(new Color(0.24f, 0.23f, 0.19f), Surface.Earth);
            q.Terrain = go.transform; // visual only: Manco walks Qenqo on a rail
        }

        static float Smooth(float a, float b, float x) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, x));

        // ------------------------------------------------------------------ the rock

        static void Rock(QenqoScene q)
        {
            Vector3 O = q.Origin, A = q.Axis, R = q.Across;
            var rot = Quaternion.LookRotation(A, Vector3.up);
            var lime = MaterialKit.Lit(P.Limestone, Surface.Masonry, 0.25f);
            var limeDark = MaterialKit.Lit(new Color(0.5f, 0.48f, 0.43f), Surface.Masonry, 0.2f);
            const float top = 4f;

            // 4 × 3 pre-split blocks; the centre row is level so Manco can stand on it.
            float[] sAt = { -6f, -2f, 2f, 6f };
            float[] wAt = { -3.3f, 0f, 3.3f };
            float[,] heights = { { 3.4f, 4f, 3.7f }, { 4.4f, 4f, 3.6f }, { 3.8f, 4f, 4.5f }, { 3.3f, 4f, 3.9f } };
            for (int i = 0; i < 4; i++)
                for (int k = 0; k < 3; k++)
                {
                    float h = heights[i, k];
                    Vector3 center = O + A * sAt[i] + R * wAt[k] + Vector3.up * (h * 0.5f);
                    var b = Greybox.Box(q.Root, $"Qenqo block {i}{k}", center, new Vector3(3.3f, h, 4f), (i + k) % 2 == 0 ? lime : limeDark, Layers.Climbable, true, rot);
                    q.Blocks.Add(b.transform);
                    q.BlockRest.Add(center);
                }

            // Steps up from the south end.
            for (int s = 0; s < 5; s++)
            {
                float sy = (s + 1) * (top / 5f);
                Vector3 c = O + A * (-9.2f - (4 - s) * 0.9f) + Vector3.up * (sy * 0.5f);
                Greybox.Box(q.Root, "Step", c, new Vector3(2.4f, sy, 0.9f), lime, Layers.Default, true, rot);
            }

            // The zigzag channel: a dark groove, and a light that will run down it (§9 0:05).
            var path = new List<Vector3>();
            for (int i = 0; i <= 10; i++)
            {
                float s = Mathf.Lerp(-7.4f, 4.6f, i / 10f);
                float w = (i % 2 == 0 ? -0.55f : 0.55f) * (i == 0 ? 0f : 1f);
                path.Add(O + A * s + R * w + Vector3.up * (top + 0.025f));
            }
            Vector3 fork = path[path.Count - 1];
            q.ChannelHead = O + A * -7.6f + Vector3.up * top;
            var groove = Strip(q.Root, "Channel groove", path, 0.32f, MaterialKit.Alpha(new Color(0.03f, 0.03f, 0.03f, 0.8f)));
            q.ChannelLight = MaterialKit.Glow(new Color(1f, 0.82f, 0.45f), 2.5f);
            q.ChannelLight.SetFloat("_Reveal", 0f);
            Strip(q.Root, "Channel light", path, 0.24f, q.ChannelLight).transform.position += Vector3.up * 0.01f;
            // The fork: the chakana-like split, left and right (libations were read by which way they ran).
            q.ForkLight = MaterialKit.Glow(new Color(1f, 0.82f, 0.45f), 2.5f);
            q.ForkLight.SetFloat("_Reveal", 0f);
            var left = new List<Vector3> { fork, fork + A * 1.2f - R * 0.9f, fork + A * 2.4f - R * 1.2f };
            var right = new List<Vector3> { fork, fork + A * 1.2f + R * 0.9f, fork + A * 2.4f + R * 1.2f };
            Strip(q.Root, "Fork groove L", left, 0.3f, groove.GetComponent<MeshRenderer>().sharedMaterial);
            Strip(q.Root, "Fork groove R", right, 0.3f, groove.GetComponent<MeshRenderer>().sharedMaterial);
            Strip(q.Root, "Fork light L", left, 0.22f, q.ForkLight).transform.position += Vector3.up * 0.01f;
            Strip(q.Root, "Fork light R", right, 0.22f, q.ForkLight).transform.position += Vector3.up * 0.01f;

            // The pulse at the channel head and the touch.
            var pulse = new GameObject("Stone pulse");
            pulse.transform.SetParent(q.Root, false);
            pulse.transform.position = q.ChannelHead + A * 0.6f + Vector3.up * 0.03f;
            pulse.transform.rotation = Quaternion.LookRotation(Vector3.down, A);
            pulse.AddComponent<MeshFilter>().sharedMesh = MeshKit.Quad(0.5f, 0.5f, Color.white);
            pulse.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Glow(new Color(1f, 0.85f, 0.5f), 0.4f);
            q.Pulse = pulse.transform;
            q.TouchPoint = Interactable.Add(pulse, 1.8f, null);

            // The seat.
            Greybox.Box(q.Root, "Seat", O + A * 1f + R * 5.4f + Vector3.up * 0.6f, new Vector3(1.4f, 1.2f, 1.2f), lime, Layers.Default, true, rot);
            Greybox.Box(q.Root, "Seat back", O + A * 1f + R * 6f + Vector3.up * 1.3f, new Vector3(0.4f, 2.6f, 1.2f), lime, Layers.Default, true, rot);

            // The cave below the north end: seen, not entered. An altar, a light for the vision, the serpent.
            Vector3 caveMouth = O + A * 8.2f;
            Greybox.Box(q.Root, "Cave mouth", caveMouth + Vector3.up * 0.9f, new Vector3(2.6f, 1.8f, 0.1f), MaterialKit.Lit(new Color(0.01f, 0.01f, 0.012f)), Layers.Default, false, rot);
            var interior = new GameObject("Carved interior").transform;
            interior.SetParent(q.Root, false);
            interior.position = O;
            Greybox.Box(interior, "Altar", O + A * 5.8f + Vector3.up * 0.5f, new Vector3(1.6f, 1f, 0.9f), MaterialKit.Lit(new Color(0.75f, 0.72f, 0.65f), Surface.Masonry, 0.4f), Layers.Default, false, rot);
            // Carved interior faces revealed when the blocks lift: tocapu-like grid of glowing incisions.
            var tocapu = MaterialKit.Glow(new Color(1f, 0.8f, 0.45f), 1.2f);
            for (int gx = -2; gx <= 2; gx++)
                for (int gy = 0; gy < 3; gy++)
                {
                    var cell = new GameObject("Tocapu");
                    cell.transform.SetParent(interior, false);
                    cell.transform.position = O + A * (gx * 2.6f) + Vector3.up * (0.7f + gy * 1.1f);
                    cell.transform.rotation = Quaternion.LookRotation(R, Vector3.up);
                    cell.AddComponent<MeshFilter>().sharedMesh = Tocapu(gx * 3 + gy);
                    cell.AddComponent<MeshRenderer>().sharedMaterial = tocapu;
                }
            interior.gameObject.SetActive(false);
            q.Interior = interior;
            var caveLightGo = new GameObject("Cave light");
            caveLightGo.transform.SetParent(q.Root, false);
            caveLightGo.transform.position = O + A * 6.2f + Vector3.up * 1.2f;
            q.CaveLight = caveLightGo.AddComponent<Light>();
            q.CaveLight.type = LightType.Point;
            q.CaveLight.range = 9f;
            q.CaveLight.intensity = 0f;
            q.CaveLight.color = new Color(1f, 0.75f, 0.4f);

            // The serpent (Ukhu Pacha) coiling in the cave: a sinuous dark-gold ribbon.
            var coil = new List<Vector3>();
            for (int i = 0; i <= 60; i++)
            {
                float t = i / 60f;
                float ang = t * Mathf.PI * 5f;
                coil.Add(O + A * 6.3f + new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang)) * (0.35f + t * 0.5f) + Vector3.up * (0.15f + t * 0.4f));
            }
            var serpent = Strip(q.Root, "Serpent", coil, 0.14f, MaterialKit.Glow(new Color(0.7f, 0.5f, 0.2f), 1.2f));
            serpent.SetActive(false);
            q.Serpent = serpent.transform;

            // The walk (compressed to 30 s on a fixed path).
            q.WalkPath.Add(O + A * -34f + R * -12f);
            q.WalkPath.Add(O + A * -24f + R * -6f);
            q.WalkPath.Add(O + A * -15f + R * -1.5f);
            q.WalkPath.Add(O + A * -13.3f);
            q.WalkPath.Add(O + A * -9.4f + Vector3.up * top);
            q.WalkPath.Add(q.ChannelHead);

            // Fog cards round the base: "the carved rock, fog at its base".
            var fogMat = MaterialKit.Alpha(new Color(0.12f, 0.14f, 0.17f, 0.35f), fog: true);
            fogMat.SetFloat("_SoftEdge", 0.8f);
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                var card = new GameObject("Fog card");
                card.transform.SetParent(q.Root, false);
                card.transform.position = O + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 11f + Vector3.up * 0.9f;
                card.AddComponent<MeshFilter>().sharedMesh = MeshKit.Quad(16f, 1.8f, Color.white);
                card.AddComponent<MeshRenderer>().sharedMaterial = fogMat;
                card.AddComponent<Billboard>().YawOnly = true;
            }
        }

        static GameObject Strip(Transform parent, string name, List<Vector3> pts, float width, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Vector3 above = pts[0] + Vector3.up * 1000f;
            var mesh = MeshKit.Ribbon(pts, _ => width, above, Color.white);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        /// <summary>A small stepped-cross / tocapu cell made of glowing incisions.</summary>
        static Mesh Tocapu(int seed)
        {
            var parts = new List<CombineInstance>();
            var rng = new System.Random(seed + 11);
            void Bar(float x, float y, float w, float h)
            {
                parts.Add(new CombineInstance { mesh = MeshKit.Quad(w, h, Color.white), transform = Matrix4x4.Translate(new Vector3(x, y, 0)) });
            }
            // Chakana (stepped cross) outline.
            Bar(0, 0, 0.9f, 0.06f); Bar(0, 0, 0.06f, 0.9f);
            Bar(0, 0.3f, 0.5f, 0.05f); Bar(0, -0.3f, 0.5f, 0.05f); Bar(0.3f, 0, 0.05f, 0.5f); Bar(-0.3f, 0, 0.05f, 0.5f);
            if (rng.NextDouble() > 0.5) { Bar(0.4f, 0.4f, 0.15f, 0.15f); Bar(-0.4f, -0.4f, 0.15f, 0.15f); }
            else { Bar(-0.4f, 0.4f, 0.15f, 0.15f); Bar(0.4f, -0.4f, 0.15f, 0.15f); }
            return MeshKit.Combine(parts, "Tocapu");
        }

        // ------------------------------------------------------------------ valley: the Coricancha seen as a point of gold

        static void Valley(QenqoScene q)
        {
            var g = new GameObject("Coricancha (distant)");
            g.transform.SetParent(q.Root, false);
            g.transform.position = q.CoricanchaPoint + Vector3.up * 6f;
            g.AddComponent<MeshFilter>().sharedMesh = MeshKit.Quad(30f, 30f, Color.white);
            var mat = MaterialKit.Glow(new Color(1f, 0.75f, 0.3f), 0.25f);
            g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            g.AddComponent<Billboard>();
            q.CoricanchaGlow = g.transform;
            // A scatter of very dim hearth-points where the city is.
            var embers = new List<CombineInstance>();
            var rng = new System.Random(5);
            for (int i = 0; i < 120; i++)
            {
                var p = q.CoricanchaPoint + new Vector3((float)rng.NextDouble() * 900f - 450f, 0f, (float)rng.NextDouble() * 900f - 450f);
                p.y = q.HeightAt(p.x, p.z) + 2f;
                embers.Add(new CombineInstance { mesh = MeshKit.Quad(3f, 3f, new Color(1f, 0.6f, 0.3f, 1f)), transform = Matrix4x4.TRS(p, Quaternion.LookRotation(p - q.Origin), Vector3.one) });
            }
            var city = new GameObject("City embers");
            city.transform.SetParent(q.Root, false);
            city.AddComponent<MeshFilter>().sharedMesh = MeshKit.Combine(embers, "Embers");
            city.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Glow(new Color(1f, 0.55f, 0.25f), 0.12f);
        }

        // ------------------------------------------------------------------ weather for the time-fold snapshots

        static void Weather(QenqoScene q)
        {
            q.Snow = Particles(q, "Snow", new Color(0.95f, 0.95f, 1f, 0.9f), 0.05f, 1.2f, 600, 0.6f);
            q.Rain = Particles(q, "Rain", new Color(0.7f, 0.75f, 0.85f, 0.6f), 0.025f, 12f, 1200, 1.4f);
        }

        static Transform Particles(QenqoScene q, string name, Color color, float size, float speed, int rate, float stretch)
        {
            var go = new GameObject(name);
            go.transform.SetParent(q.Root, false);
            go.transform.position = q.Origin + Vector3.up * 18f;
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.startLifetime = 20f / speed + 0.5f;
            main.startSpeed = speed;
            main.startSize = size;
            main.startColor = color;
            main.maxParticles = rate * 3;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            var em = ps.emission;
            em.rateOverTime = rate;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(60f, 60f, 1f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = MaterialKit.Alpha(Color.white);
            r.renderMode = stretch > 1f ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
            r.lengthScale = stretch;
            go.SetActive(false);
            return go.transform;
        }

        // ------------------------------------------------------------------ Cajamarca, 16 Nov 1532 (as the stones remember it)

        static void Cajamarca(QenqoScene q, Transform parent)
        {
            var root = new GameObject("Cajamarca (vision)").transform;
            root.SetParent(parent, false);
            Vector3 C = Layout.CajamarcaOrigin;
            Greybox.Block(root, "Square", C + new Vector3(-110f, -1f, -100f), C + new Vector3(110f, 0f, 100f), Surface.Earth, new Color(0.45f, 0.38f, 0.3f));
            // Long halls (kallanka) on three sides with trapezoidal doors.
            var adobe = Surface.Adobe;
            Greybox.Building(root, "Hall N", C.x - 100f, C.x + 100f, C.z + 100f, C.z + 112f, 7f, adobe);
            Greybox.Building(root, "Hall E", C.x + 100f, C.x + 112f, C.z - 100f, C.z + 100f, 7f, adobe);
            Greybox.Building(root, "Hall W", C.x - 112f, C.x - 100f, C.z - 100f, C.z + 100f, 7f, adobe);
            for (int i = 0; i < 18; i++)
            {
                Greybox.FakeDoor(root, new Vector3(C.x - 90f + i * 10.5f, 0, C.z + 100f), Face.South, 2.2f, 3.6f);
                Greybox.FakeDoor(root, new Vector3(C.x + 100f, 0, C.z - 90f + i * 10.5f), Face.West, 2.2f, 3.6f);
                Greybox.FakeDoor(root, new Vector3(C.x - 100f, 0, C.z - 90f + i * 10.5f), Face.East, 2.2f, 3.6f);
            }
            // The ushnu platform at the centre.
            Greybox.Block(root, "Ushnu", C + new Vector3(-6f, 0f, -6f), C + new Vector3(6f, 3f, 6f), Surface.Masonry);

            q.CajamarcaCamera = C + new Vector3(-95f, 26f, -95f);
            q.CajamarcaLook = C + new Vector3(10f, 0f, 10f);

            // Thousands in Inca dress — and nothing else. Then, in one frame, only bodies.
            var rng = new System.Random(1532);
            var dress = new[] { new Color(0.62f, 0.14f, 0.1f), new Color(0.75f, 0.6f, 0.3f), new Color(0.85f, 0.82f, 0.72f), new Color(0.12f, 0.1f, 0.1f), new Color(0.3f, 0.35f, 0.5f) };
            var crowd = new List<CombineInstance>();
            var bodies = new List<CombineInstance>();
            for (int i = 0; i < 3000; i++)
            {
                var p = C + new Vector3((float)rng.NextDouble() * 190f - 95f, 0f, (float)rng.NextDouble() * 180f - 90f);
                if (Mathf.Abs(p.x - C.x) < 8f && Mathf.Abs(p.z - C.z) < 8f) continue;
                var col = dress[rng.Next(dress.Length)];
                Quaternion face = Quaternion.LookRotation(new Vector3(p.x - q.CajamarcaCamera.x, 0f, p.z - q.CajamarcaCamera.z));
                crowd.Add(new CombineInstance { mesh = MeshKit.Quad(0.55f, 1.6f, col), transform = Matrix4x4.TRS(p + Vector3.up * 0.8f, face, Vector3.one) });
                crowd.Add(new CombineInstance { mesh = MeshKit.Quad(0.25f, 0.25f, new Color(0.2f, 0.14f, 0.1f)), transform = Matrix4x4.TRS(p + Vector3.up * 1.72f, face, Vector3.one) });
                if (i % 2 == 0)
                {
                    var lie = Quaternion.Euler(90f, (float)rng.NextDouble() * 360f, 0f);
                    bodies.Add(new CombineInstance { mesh = MeshKit.Quad(0.55f, 1.7f, Color.Lerp(col, new Color(0.25f, 0.06f, 0.04f), 0.55f)), transform = Matrix4x4.TRS(p + Vector3.up * 0.04f, lie, Vector3.one) });
                }
            }
            var unlit = MaterialKit.Silhouette(Color.white);
            q.Crowd = MeshObject(root, "Crowd", MeshKit.Combine(crowd, "Crowd"), unlit).transform;
            q.Bodies = MeshObject(root, "Bodies", MeshKit.Combine(bodies, "Bodies"), unlit).transform;
            q.Bodies.gameObject.SetActive(false);
            q.Cajamarca = root;
            root.gameObject.SetActive(false);
        }

        static void GoldRoom(QenqoScene q, Transform parent)
        {
            var root = new GameObject("Gold room (vision)").transform;
            root.SetParent(parent, false);
            Vector3 G = Layout.GoldRoomOrigin;
            const float w = 6.7f, d = 5.2f, h = 4.2f;
            var wall = Surface.Masonry;
            Greybox.Block(root, "Floor", G + new Vector3(-w / 2, -0.3f, -d / 2), G + new Vector3(w / 2, 0, d / 2), wall);
            Greybox.Block(root, "Wall N", G + new Vector3(-w / 2, 0, d / 2), G + new Vector3(w / 2, h, d / 2 + 0.5f), wall);
            Greybox.Block(root, "Wall S", G + new Vector3(-w / 2, 0, -d / 2 - 0.5f), G + new Vector3(w / 2, h, -d / 2), wall);
            Greybox.Block(root, "Wall E", G + new Vector3(w / 2, 0, -d / 2), G + new Vector3(w / 2 + 0.5f, h, d / 2), wall);
            Greybox.Block(root, "Wall W", G + new Vector3(-w / 2 - 0.5f, 0, -d / 2), G + new Vector3(-w / 2, h, d / 2), wall);
            // The line on the wall the room had to be filled to.
            Greybox.Block(root, "Line", G + new Vector3(-w / 2, 2.38f, d / 2 - 0.02f), G + new Vector3(w / 2, 2.42f, d / 2), Surface.Plain, new Color(0.1f, 0.05f, 0.03f));
            var plane = new GameObject("Gold");
            plane.transform.SetParent(root, false);
            plane.transform.position = G;
            plane.transform.rotation = Quaternion.Euler(90f, 0, 0);
            plane.AddComponent<MeshFilter>().sharedMesh = MeshKit.Quad(w, d, Color.white);
            plane.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Glow(new Color(1f, 0.7f, 0.25f), 1.4f);
            q.GoldPlane = plane.transform;
            // Metal running down the walls like the channel light.
            q.GoldRunMaterial = MaterialKit.Glow(new Color(1f, 0.78f, 0.35f), 2f);
            q.GoldRunMaterial.SetFloat("_Reveal", 0f);
            for (int i = 0; i < 7; i++)
            {
                float x = -w / 2 + 0.6f + i * (w - 1.2f) / 6f;
                var run = new List<Vector3> { G + new Vector3(x, h - 0.1f, d / 2 - 0.05f), G + new Vector3(x + 0.1f, h * 0.55f, d / 2 - 0.05f), G + new Vector3(x - 0.05f, 0.05f, d / 2 - 0.05f) };
                var go = new GameObject("Gold run");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = MeshKit.Ribbon(run, _ => 0.12f, G + new Vector3(0, 1.6f, -d), Color.white);
                go.AddComponent<MeshRenderer>().sharedMaterial = q.GoldRunMaterial;
            }
            var l = new GameObject("Gold light").AddComponent<Light>();
            l.transform.SetParent(root, false);
            l.transform.position = G + new Vector3(0, 1.5f, 0);
            l.type = LightType.Point;
            l.range = 12f;
            l.intensity = 2.5f;
            l.color = new Color(1f, 0.7f, 0.35f);
            q.GoldRoomCamera = G + new Vector3(0f, 1.6f, -d / 2 + 0.4f);
            q.GoldRoomLook = G + new Vector3(0f, 1.3f, d / 2);
            q.GoldRoom = root;
            root.gameObject.SetActive(false);
        }

        static GameObject MeshObject(Transform parent, string name, Mesh mesh, Material mat)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }
    }
}
