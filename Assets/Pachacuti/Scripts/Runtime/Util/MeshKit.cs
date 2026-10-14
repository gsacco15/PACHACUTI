using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Pachacuti
{
    /// <summary>Procedural meshes for the grey-box: boxes with world-scaled UVs, ribbons, polygons.</summary>
    public static class MeshKit
    {
        static readonly Dictionary<long, Mesh> BoxCache = new Dictionary<long, Mesh>();

        /// <summary>
        /// A box centred on the origin whose UVs are in metres / <paramref name="tile"/>, so
        /// surface textures (masonry joints, thatch) keep their real-world scale on any size.
        /// Shared per size.
        /// </summary>
        public static Mesh Box(Vector3 size, float tile = 1f)
        {
            long key = ((long)Mathf.RoundToInt(size.x * 100) * 73856093L) ^ ((long)Mathf.RoundToInt(size.y * 100) * 19349663L)
                       ^ ((long)Mathf.RoundToInt(size.z * 100) * 83492791L) ^ ((long)Mathf.RoundToInt(tile * 100) << 40);
            if (BoxCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var v = new List<Vector3>(24);
            var n = new List<Vector3>(24);
            var uv = new List<Vector2>(24);
            var tris = new List<int>(36);
            Vector3 h = size * 0.5f;
            void Face(Vector3 normal, Vector3 right, Vector3 up, float w, float hgt, float depth)
            {
                int i = v.Count;
                Vector3 c = normal * depth;
                v.Add(c - right * (w * 0.5f) - up * (hgt * 0.5f));
                v.Add(c + right * (w * 0.5f) - up * (hgt * 0.5f));
                v.Add(c + right * (w * 0.5f) + up * (hgt * 0.5f));
                v.Add(c - right * (w * 0.5f) + up * (hgt * 0.5f));
                for (int k = 0; k < 4; k++) n.Add(normal);
                uv.Add(new Vector2(0, 0));
                uv.Add(new Vector2(w / tile, 0));
                uv.Add(new Vector2(w / tile, hgt / tile));
                uv.Add(new Vector2(0, hgt / tile));
                tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
                tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
            }
            Face(Vector3.forward, Vector3.left, Vector3.up, size.x, size.y, h.z);
            Face(Vector3.back, Vector3.right, Vector3.up, size.x, size.y, h.z);
            Face(Vector3.right, Vector3.forward, Vector3.up, size.z, size.y, h.x);
            Face(Vector3.left, Vector3.back, Vector3.up, size.z, size.y, h.x);
            Face(Vector3.up, Vector3.right, Vector3.forward, size.x, size.z, h.y);
            Face(Vector3.down, Vector3.right, Vector3.back, size.x, size.z, h.y);

            var m = new Mesh { name = $"Box {size.x:0.##}x{size.y:0.##}x{size.z:0.##}" };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            m.RecalculateTangents();
            BoxCache[key] = m;
            return m;
        }

        /// <summary>
        /// A flat ribbon along a polyline. Each segment is widened perpendicular to both
        /// the tangent and <paramref name="faceToward"/>'s direction from the point (so it
        /// faces a viewer). UV.x runs 0..1 along the length, UV.y 0..1 across.
        /// </summary>
        public static Mesh Ribbon(IReadOnlyList<Vector3> points, System.Func<Vector3, float> widthAt, Vector3 faceToward, Color color, Mesh reuse = null)
        {
            var m = reuse != null ? reuse : new Mesh { name = "Ribbon" };
            int count = points.Count;
            var v = new List<Vector3>(count * 2);
            var uv = new List<Vector2>(count * 2);
            var col = new List<Color>(count * 2);
            var tris = new List<int>((count - 1) * 6);
            float total = 0f;
            var cum = new float[count];
            for (int i = 1; i < count; i++) { total += Vector3.Distance(points[i - 1], points[i]); cum[i] = total; }
            if (total <= 0f) total = 1f;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = points[i];
                Vector3 tangent = (points[Mathf.Min(i + 1, count - 1)] - points[Mathf.Max(i - 1, 0)]).normalized;
                Vector3 view = (faceToward - p).normalized;
                Vector3 side = Vector3.Cross(tangent, view);
                if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(tangent, Vector3.up);
                side = side.normalized * (widthAt(p) * 0.5f);
                v.Add(p - side);
                v.Add(p + side);
                float u = cum[i] / total;
                uv.Add(new Vector2(u, 0));
                uv.Add(new Vector2(u, 1));
                col.Add(color);
                col.Add(color);
                if (i < count - 1)
                {
                    int a = i * 2;
                    tris.Add(a); tris.Add(a + 2); tris.Add(a + 1);
                    tris.Add(a + 1); tris.Add(a + 2); tris.Add(a + 3);
                }
            }
            m.Clear();
            if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetColors(col);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return m;
        }

        /// <summary>A flat polygon in the XY plane from a 2D outline (ear-clipped in Core).</summary>
        public static Mesh Polygon(IReadOnlyList<P2> outline, Color color, Vector2 scale, Vector2 pivot)
        {
            var tris = Polygon2D.Triangulate(outline);
            var v = new Vector3[outline.Count];
            var c = new Color[outline.Count];
            var uv = new Vector2[outline.Count];
            for (int i = 0; i < outline.Count; i++)
            {
                v[i] = new Vector3((outline[i].X - pivot.x) * scale.x, (outline[i].Y - pivot.y) * scale.y, 0f);
                c[i] = color;
                uv[i] = new Vector2(outline[i].X, outline[i].Y);
            }
            var m = new Mesh { name = "Polygon" };
            m.vertices = v;
            m.colors = c;
            m.uv = uv;
            m.triangles = tris;
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>Camera-facing-agnostic quad (XY plane), centred, for billboards and cards.</summary>
        public static Mesh Quad(float width, float height, Color color)
        {
            var m = new Mesh { name = "Quad" };
            float w = width * 0.5f, h = height * 0.5f;
            m.vertices = new[] { new Vector3(-w, -h, 0), new Vector3(w, -h, 0), new Vector3(w, h, 0), new Vector3(-w, h, 0) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            m.colors = new[] { color, color, color, color };
            m.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        public static Mesh Combine(List<CombineInstance> parts, string name)
        {
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.CombineMeshes(parts.ToArray(), true, true);
            m.RecalculateBounds();
            return m;
        }
    }
}
