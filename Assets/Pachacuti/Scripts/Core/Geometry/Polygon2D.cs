using System;
using System.Collections.Generic;

namespace Pachacuti.Core
{
    public readonly struct P2
    {
        public readonly float X, Y;
        public P2(float x, float y) { X = x; Y = y; }
        public static P2 operator -(P2 a, P2 b) => new P2(a.X - b.X, a.Y - b.Y);
        public override string ToString() => $"({X:0.###},{Y:0.###})";
    }

    public static class Polygon2D
    {
        public static float SignedArea(IReadOnlyList<P2> pts)
        {
            double a = 0;
            for (int i = 0, j = pts.Count - 1; i < pts.Count; j = i++)
                a += (double)pts[j].X * pts[i].Y - (double)pts[i].X * pts[j].Y;
            return (float)(a * 0.5);
        }

        /// <summary>
        /// Ear-clipping triangulation of a simple polygon (either winding). Returns
        /// index triples, counter-clockwise. n − 2 triangles for n vertices.
        /// </summary>
        public static int[] Triangulate(IReadOnlyList<P2> pts)
        {
            int n = pts.Count;
            if (n < 3) return new int[0];
            var idx = new List<int>(n);
            if (SignedArea(pts) > 0) for (int i = 0; i < n; i++) idx.Add(i);
            else for (int i = n - 1; i >= 0; i--) idx.Add(i);

            var tris = new List<int>((n - 2) * 3);
            int guard = n * n;
            int k = 0;
            while (idx.Count > 3 && guard-- > 0)
            {
                int c = idx.Count;
                int i0 = idx[(k + c - 1) % c], i1 = idx[k % c], i2 = idx[(k + 1) % c];
                if (IsEar(pts, idx, i0, i1, i2))
                {
                    tris.Add(i0); tris.Add(i1); tris.Add(i2);
                    idx.RemoveAt(k % c);
                    k = Math.Max(0, k - 1);
                }
                else k = (k + 1) % c;
            }
            if (idx.Count == 3) { tris.Add(idx[0]); tris.Add(idx[1]); tris.Add(idx[2]); }
            return tris.ToArray();
        }

        static bool IsEar(IReadOnlyList<P2> p, List<int> idx, int a, int b, int c)
        {
            if (Cross(p[b] - p[a], p[c] - p[b]) <= 1e-9f) return false; // reflex or degenerate
            foreach (int j in idx)
            {
                if (j == a || j == b || j == c) continue;
                if (InTriangle(p[j], p[a], p[b], p[c])) return false;
            }
            return true;
        }

        static float Cross(P2 u, P2 v) => u.X * v.Y - u.Y * v.X;

        static bool InTriangle(P2 q, P2 a, P2 b, P2 c)
        {
            float d1 = Cross(b - a, q - a), d2 = Cross(c - b, q - b), d3 = Cross(a - c, q - c);
            return d1 >= 0 && d2 >= 0 && d3 >= 0;
        }

        public static float TriangulatedArea(IReadOnlyList<P2> pts, int[] tris)
        {
            double a = 0;
            for (int t = 0; t < tris.Length; t += 3)
            {
                var p0 = pts[tris[t]]; var p1 = pts[tris[t + 1]]; var p2 = pts[tris[t + 2]];
                a += 0.5 * Cross(p1 - p0, p2 - p0);
            }
            return (float)a;
        }
    }

    /// <summary>
    /// Side-view silhouette of Yacana, the dark-cloud llama (head left). Used for
    /// the army marker on the sky-map and the llama that stands up and walks in
    /// the vision. Units: about 1 long, 1 tall; feet at y = 0.
    /// </summary>
    public static class LlamaShape
    {
        public static readonly P2[] Outline =
        {
            new P2(0.00f, 0.80f), new P2(0.02f, 0.74f), new P2(0.10f, 0.72f), new P2(0.14f, 0.62f),
            new P2(0.18f, 0.40f), new P2(0.17f, 0.30f), new P2(0.20f, 0.22f), new P2(0.19f, 0.00f),
            new P2(0.245f, 0.00f), new P2(0.26f, 0.18f), new P2(0.40f, 0.20f), new P2(0.68f, 0.20f),
            new P2(0.72f, 0.17f), new P2(0.72f, 0.00f), new P2(0.78f, 0.00f), new P2(0.82f, 0.20f),
            new P2(0.86f, 0.30f), new P2(0.92f, 0.38f), new P2(0.95f, 0.44f), new P2(0.90f, 0.47f),
            new P2(0.84f, 0.45f), new P2(0.55f, 0.46f), new P2(0.30f, 0.47f), new P2(0.26f, 0.55f),
            new P2(0.22f, 0.72f), new P2(0.20f, 0.82f), new P2(0.19f, 0.94f), new P2(0.17f, 0.97f),
            new P2(0.15f, 0.88f), new P2(0.08f, 0.86f),
        };

        /// <summary>The two eyes: α and β Centauri (pitch slice §9 "its two eyes stay lit").</summary>
        public static readonly P2 EyeAlpha = new P2(0.085f, 0.80f);
        public static readonly P2 EyeBeta = new P2(0.125f, 0.805f);

        /// <summary>Vertices below this height belong to legs (for the walk animation).</summary>
        public const float LegTop = 0.21f;
        public const float FrontLegMaxX = 0.5f;
    }
}
