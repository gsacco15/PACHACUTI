using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>
    /// The real sky over Cusco on the slice night (pitch slice §12 Sky: brightest
    /// ~2,000 stars positioned for Cusco, 16 Nov 1532, 22:00; the Milky Way as a
    /// dome). Fog is a ground layer, not a sky layer (§4): the sky is clear above
    /// a horizon band, so the Pleiades and the Milky Way are visible from the start.
    /// The dome follows the camera so it reads as infinitely far.
    /// </summary>
    public sealed class SkyDome : MonoBehaviour
    {
        public const float Radius = 800f;
        Material _dome, _stars, _river;
        GameObject _riverGo;
        public ObservationSite Site { get; private set; }
        public List<SkyStar> Stars { get; private set; }
        public Camera Follow;

        public static SkyDome Create(Transform parent, ObservationSite site)
        {
            var go = new GameObject("Sky");
            go.transform.SetParent(parent, false);
            go.layer = Layers.Sky;
            var sky = go.AddComponent<SkyDome>();
            sky.Site = site;
            sky.BuildDome();
            sky.BuildStars();
            sky.BuildRiver();
            sky.SetNight();
            return sky;
        }

        /// <summary>World-space unit direction for an altitude/azimuth (x east, y up, z north).</summary>
        public static Vector3 Direction(double altDeg, double azDeg)
        {
            new Horizontal(altDeg, azDeg).ToLocal(out var e, out var u, out var n);
            return new Vector3((float)e, (float)u, (float)n);
        }

        void BuildDome()
        {
            const int lon = 96, lat = 48;
            var verts = new List<Vector3>();
            var cols = new List<Color>();
            var tris = new List<int>();
            Color zenith = new Color(0.004f, 0.006f, 0.013f);
            Color horizon = new Color(0.03f, 0.038f, 0.052f);
            Color fog = Greybox.Palette.Night;
            for (int j = 0; j <= lat; j++)
            {
                float alt = Mathf.Lerp(-30f, 90f, j / (float)lat);
                for (int i = 0; i <= lon; i++)
                {
                    float az = i / (float)lon * 360f;
                    Vector3 dir = Direction(alt, az);
                    verts.Add(dir * Radius);
                    Astronomy.LocalToEquatorial(dir.x, dir.y, dir.z, Site, out var raDate, out var decDate);
                    Astronomy.Precess(raDate, decDate, Site.JulianDayUT, Astronomy.J2000, out var ra, out var dec);
                    Astronomy.EquatorialJ2000ToGalactic(ra, dec, out var l, out var b);
                    float mw = (float)MilkyWay.Brightness(l, b);
                    float h = Mathf.Clamp01(alt / 60f);
                    Color c = Color.Lerp(horizon, zenith, Mathf.Sqrt(h));
                    // Ground fog band: below ~8° the sky dissolves into the fog colour.
                    float band = Mathf.Clamp01((8f - alt) / 10f);
                    c = Color.Lerp(c, fog, band);
                    mw *= Mathf.Clamp01((alt + 2f) / 10f);
                    c.a = mw * 0.09f;
                    cols.Add(c);
                }
            }
            for (int j = 0; j < lat; j++)
                for (int i = 0; i < lon; i++)
                {
                    int a = j * (lon + 1) + i;
                    tris.Add(a); tris.Add(a + 1); tris.Add(a + lon + 1);
                    tris.Add(a + 1); tris.Add(a + lon + 2); tris.Add(a + lon + 1);
                }
            var mesh = new Mesh { name = "Sky dome" };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetTriangles(tris, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * Radius * 2.2f);
            var go = new GameObject("Dome");
            go.transform.SetParent(transform, false);
            go.layer = Layers.Sky;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var r = go.AddComponent<MeshRenderer>();
            _dome = MaterialKit.Sky();
            r.sharedMaterial = _dome;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
        }

        void BuildStars()
        {
            var asset = Resources.Load<TextAsset>("Pachacuti/Sky/bright_stars");
            var catalog = asset != null ? StarCatalog.Parse(asset.text) : new List<CatalogStar>();
            if (asset == null) Debug.LogError("[PACHACUTI] Resources/Pachacuti/Sky/bright_stars.csv missing.");
            Stars = StarCatalog.Place(catalog, Site, aboveHorizonOnly: true);

            var verts = new List<Vector3>(Stars.Count * 4);
            var cols = new List<Color>(Stars.Count * 4);
            var uv0 = new List<Vector2>(Stars.Count * 4);
            var uv1 = new List<Vector2>(Stars.Count * 4);
            var tris = new List<int>(Stars.Count * 6);
            var rng = new System.Random(3);
            foreach (var s in Stars)
            {
                Vector3 dir = Direction(s.Position.AltitudeDeg, s.Position.AzimuthDeg);
                StarColor.FromBV(s.ColorIndexBV, out var r, out var g, out var b);
                float flux = StarColor.Flux(s.Magnitude);
                float brightness = Mathf.Clamp(Mathf.Pow(flux, 0.42f) * 2.2f, 0.06f, 3.5f);
                float size = Mathf.Lerp(0.0016f, 0.0058f, Mathf.InverseLerp(5.2f, -1.5f, s.Magnitude));
                // Extinction: stars dim and redden near the horizon.
                float ext = Mathf.Clamp01((float)s.Position.AltitudeDeg / 12f);
                var col = new Color(r, g * Mathf.Lerp(0.85f, 1f, ext), b * Mathf.Lerp(0.7f, 1f, ext), brightness * Mathf.Lerp(0.1f, 1f, ext));
                float seed = (float)rng.NextDouble();
                int i0 = verts.Count;
                for (int k = 0; k < 4; k++)
                {
                    verts.Add(dir * (Radius - 5f));
                    cols.Add(col);
                    uv1.Add(new Vector2(size, seed));
                }
                uv0.Add(new Vector2(-1, -1)); uv0.Add(new Vector2(1, -1)); uv0.Add(new Vector2(1, 1)); uv0.Add(new Vector2(-1, 1));
                tris.Add(i0); tris.Add(i0 + 1); tris.Add(i0 + 2);
                tris.Add(i0); tris.Add(i0 + 2); tris.Add(i0 + 3);
            }
            var mesh = new Mesh { name = "Stars", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.SetVertices(verts);
            mesh.SetColors(cols);
            mesh.SetUVs(0, uv0);
            mesh.SetUVs(1, uv1);
            mesh.SetTriangles(tris, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * Radius * 2.2f);
            var go = new GameObject("Stars");
            go.transform.SetParent(transform, false);
            go.layer = Layers.Sky;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            _stars = MaterialKit.Stars();
            mr.sharedMaterial = _stars;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>For the vision: the Mayu as a river with current (§9 0:02).</summary>
        void BuildRiver()
        {
            var pts = new List<Vector3>();
            for (int i = 0; i <= 180; i++)
            {
                double l = i * 2.0;
                Astronomy.GalacticToEquatorialJ2000(l, 0, out var ra, out var dec);
                Astronomy.Precess(ra, dec, Astronomy.J2000, Site.JulianDayUT, out ra, out dec);
                var h = Astronomy.ToHorizontal(ra, dec, Site);
                if (h.AltitudeDeg < -4) { if (pts.Count > 1) break; pts.Clear(); continue; }
                pts.Add(Direction(h.AltitudeDeg, h.AzimuthDeg) * (Radius - 12f));
            }
            if (pts.Count < 2) return;
            _riverGo = new GameObject("Mayu river");
            _riverGo.transform.SetParent(transform, false);
            _riverGo.layer = Layers.Sky;
            _riverGo.AddComponent<MeshFilter>().sharedMesh = MeshKit.Ribbon(pts, _ => 95f, Vector3.zero, new Color(0.55f, 0.6f, 1f, 1f));
            _river = MaterialKit.Glow(new Color(0.55f, 0.62f, 1f), 0.16f);
            _river.SetFloat("_PulseSpeed", 0.035f);
            _river.SetFloat("_PulseAmount", 0.6f);
            _river.SetFloat("_PulseWidth", 0.08f);
            _riverGo.AddComponent<MeshRenderer>().sharedMaterial = _river;
            _riverGo.SetActive(false);
        }

        public void SetNight()
        {
            _dome.SetColor("_SkyTint", Color.white);
            _dome.SetColor("_MilkyWayColor", new Color(0.55f, 0.6f, 0.72f));
            _dome.SetFloat("_MilkyWayGain", 1f);
            _dome.SetFloat("_Exposure", 1f);
            _stars.SetFloat("_Brightness", 1f);
            _stars.SetFloat("_SizeScale", 1f);
            _stars.SetFloat("_Twinkle", 0.25f);
            if (_riverGo != null) _riverGo.SetActive(false);
        }

        /// <summary>The cut (§9 0:02): sky saturated deep indigo; the Milky Way three times brighter and visibly a river.</summary>
        public void SetVision()
        {
            _dome.SetColor("_SkyTint", new Color(2.2f, 1.4f, 5.5f));
            _dome.SetColor("_MilkyWayColor", new Color(0.7f, 0.68f, 1f));
            _dome.SetFloat("_MilkyWayGain", 3f);
            _dome.SetFloat("_Exposure", 1.2f);
            _stars.SetFloat("_Brightness", 1.8f);
            _stars.SetFloat("_SizeScale", 1.25f);
            _stars.SetFloat("_Twinkle", 0.05f);
            if (_riverGo != null) _riverGo.SetActive(true);
        }

        /// <summary>Daylight snapshots in the time-fold and the Cajamarca square.</summary>
        public void SetDay(Color tint, float exposure, float starBrightness)
        {
            _dome.SetColor("_SkyTint", tint);
            _dome.SetFloat("_MilkyWayGain", 0f);
            _dome.SetFloat("_Exposure", exposure);
            _stars.SetFloat("_Brightness", starBrightness);
            if (_riverGo != null) _riverGo.SetActive(false);
        }

        public bool TryGetStar(string name, out SkyStar star)
        {
            int i = StarCatalog.IndexOf(Stars, name);
            star = i >= 0 ? Stars[i] : default;
            return i >= 0;
        }

        void LateUpdate()
        {
            var cam = Follow != null ? Follow : Camera.main;
            if (cam != null) transform.position = cam.transform.position;
        }
    }
}
