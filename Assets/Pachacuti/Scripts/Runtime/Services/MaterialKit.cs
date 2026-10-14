using System.Collections.Generic;
using UnityEngine;

namespace Pachacuti
{
    public enum Surface
    {
        Masonry,   // polygonal Inca stone: gripable, joints visible
        Adobe,     // plaster / adobe: smooth, not climbable
        Thatch,    // ichu grass roofs: walkable, not climbable
        Earth,     // packed ground, street paving
        Plain,     // untextured (props, people)
    }

    /// <summary>
    /// Creates and caches grey-box materials. Everything loads from Resources so the
    /// shaders are guaranteed to ship in builds (Shader.Find alone gets stripped).
    /// Surface textures are generated at startup: §10 "every surface tells you
    /// whether it climbs before you touch it (masonry joints are visible; plaster is smooth)".
    /// </summary>
    public static class MaterialKit
    {
        const string Root = "Pachacuti/";
        static Material _litTemplate;
        static Shader _glow, _alpha, _silhouette, _sky, _stars;
        static readonly Dictionary<(Color32, Surface), Material> LitCache = new Dictionary<(Color32, Surface), Material>();
        static readonly Dictionary<Color32, Material> SilhouetteCache = new Dictionary<Color32, Material>();
        static Texture2D _masonry, _thatch, _adobe, _earth;

        public static void Init()
        {
            if (_litTemplate != null) return;
            _litTemplate = Resources.Load<Material>(Root + "Materials/GreyboxLit");
            if (_litTemplate == null)
            {
                var lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                _litTemplate = new Material(lit);
                Debug.LogWarning("[PACHACUTI] GreyboxLit material missing from Resources; using Shader.Find fallback (may be stripped in builds).");
            }
            _glow = LoadShader("Glow");
            _alpha = LoadShader("AlphaBlend");
            _silhouette = LoadShader("Silhouette");
            _sky = LoadShader("SkyDome");
            _stars = LoadShader("Stars");
            _masonry = SurfaceTextures.Masonry(256, 7);
            _thatch = SurfaceTextures.Thatch(256, 3);
            _adobe = SurfaceTextures.Adobe(128, 5);
            _earth = SurfaceTextures.Earth(128, 11);
        }

        static Shader LoadShader(string name)
        {
            var s = Shader.Find("Pachacuti/" + name);
            if (s == null) Debug.LogError("[PACHACUTI] Shader Pachacuti/" + name + " not found. Is Assets/Pachacuti/Resources/Pachacuti/Shaders imported?");
            return s;
        }

        public static Material Lit(Color color, Surface surface = Surface.Plain, float smoothness = 0.1f)
        {
            Init();
            var key = ((Color32)color, surface);
            if (LitCache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(_litTemplate) { name = $"Lit {surface} {ColorUtility.ToHtmlStringRGB(color)}" };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", smoothness);
            var tex = TextureFor(surface);
            if (tex != null)
            {
                m.SetTexture("_BaseMap", tex);
                m.SetTexture("_MainTex", tex);
            }
            LitCache[key] = m;
            return m;
        }

        static Texture2D TextureFor(Surface s)
        {
            switch (s)
            {
                case Surface.Masonry: return _masonry;
                case Surface.Thatch: return _thatch;
                case Surface.Adobe: return _adobe;
                case Surface.Earth: return _earth;
                default: return null;
            }
        }

        /// <summary>Additive emissive (never cached: callers animate their own instance).</summary>
        public static Material Glow(Color color, float intensity = 1f)
        {
            Init();
            var m = new Material(_glow) { name = "Glow" };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Intensity", intensity);
            return m;
        }

        public static Material Alpha(Color color, bool fog = false)
        {
            Init();
            var m = new Material(_alpha) { name = "Alpha" };
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Fog", fog ? 1f : 0f);
            return m;
        }

        public static Material Silhouette(Color color)
        {
            Init();
            var key = (Color32)color;
            if (SilhouetteCache.TryGetValue(key, out var m) && m != null) return m;
            m = new Material(_silhouette) { name = "Silhouette" };
            m.SetColor("_BaseColor", color);
            SilhouetteCache[key] = m;
            return m;
        }

        public static Material Sky() { Init(); return new Material(_sky) { name = "SkyDome" }; }
        public static Material Stars() { Init(); return new Material(_stars) { name = "Stars" }; }
    }

    /// <summary>Tiny procedural textures so surfaces read correctly before real art arrives.</summary>
    public static class SurfaceTextures
    {
        /// <summary>Polygonal masonry: jittered Voronoi cells with dark recessed joints (one tile ≈ 1 m).</summary>
        public static Texture2D Masonry(int size, int seed)
        {
            var rng = new System.Random(seed);
            const int cells = 3; // ~3 blocks per metre-tile, irregular
            var pts = new Vector2[cells * cells];
            for (int y = 0; y < cells; y++)
                for (int x = 0; x < cells; x++)
                    pts[y * cells + x] = new Vector2((x + 0.15f + 0.7f * (float)rng.NextDouble()) / cells, (y + 0.15f + 0.7f * (float)rng.NextDouble()) / cells);
            var shade = new float[pts.Length];
            for (int i = 0; i < shade.Length; i++) shade[i] = 0.82f + 0.18f * (float)rng.NextDouble();
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2((x + 0.5f) / size, (y + 0.5f) / size);
                    float d1 = float.MaxValue, d2 = float.MaxValue;
                    int nearest = 0;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        for (int oy = -1; oy <= 1; oy++)
                            for (int ox = -1; ox <= 1; ox++)
                            {
                                float d = (p - (pts[i] + new Vector2(ox, oy))).sqrMagnitude;
                                if (d < d1) { d2 = d1; d1 = d; nearest = i; }
                                else if (d < d2) d2 = d;
                            }
                    }
                    float edge = Mathf.Sqrt(d2) - Mathf.Sqrt(d1);
                    float joint = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((edge - 0.006f) / 0.02f));
                    float pillow = Mathf.Lerp(0.85f, 1f, Mathf.Clamp01(edge * 8f));
                    float g = (0.18f + 0.82f * joint) * shade[nearest] * pillow;
                    byte b = (byte)(Mathf.Clamp01(g) * 255);
                    px[y * size + x] = new Color32(b, b, b, 255);
                }
            }
            return Make(px, size, "Masonry");
        }

        /// <summary>Thatch: dense vertical straw strokes.</summary>
        public static Texture2D Thatch(int size, int seed)
        {
            var rng = new System.Random(seed);
            var col = new float[size];
            for (int x = 0; x < size; x++) col[x] = 0.75f + 0.25f * (float)rng.NextDouble();
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float band = 0.85f + 0.15f * Mathf.Sin(y * 0.35f + col[x] * 20f);
                    float g = col[x] * band * (0.9f + 0.1f * (float)rng.NextDouble());
                    byte b = (byte)(Mathf.Clamp01(g) * 255);
                    px[y * size + x] = new Color32(b, b, b, 255);
                }
            return Make(px, size, "Thatch");
        }

        /// <summary>Adobe/plaster: smooth, faint mottling only.</summary>
        public static Texture2D Adobe(int size, int seed) => Mottle(size, seed, 0.06f, "Adobe");

        public static Texture2D Earth(int size, int seed) => Mottle(size, seed, 0.18f, "Earth");

        static Texture2D Mottle(int size, int seed, float amount, string name)
        {
            var rng = new System.Random(seed);
            var px = new Color32[size * size];
            for (int i = 0; i < px.Length; i++)
            {
                float g = 1f - amount + amount * (float)rng.NextDouble();
                byte b = (byte)(Mathf.Clamp01(g) * 255);
                px[i] = new Color32(b, b, b, 255);
            }
            return Make(px, size, name);
        }

        static Texture2D Make(Color32[] px, int size, string name)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, true) { name = name, wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Trilinear, anisoLevel = 4 };
            t.SetPixels32(px);
            t.Apply(true, true);
            return t;
        }
    }
}
