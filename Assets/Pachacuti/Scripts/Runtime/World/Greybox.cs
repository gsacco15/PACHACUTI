using System.Collections.Generic;
using UnityEngine;

namespace Pachacuti
{
    public enum Face { North, South, East, West }

    /// <summary>
    /// Grey-box construction helpers. Pitch slice §13 rule: "no system is built before
    /// its beat is blocked out with primitives. Grey-box the whole 20 minutes in week 1."
    /// Coordinates: +Z is north, +X is east, metres, ground at y = 0.
    /// </summary>
    public static class Greybox
    {
        public static class Palette
        {
            public static readonly Color Masonry = new Color(0.56f, 0.53f, 0.49f);
            public static readonly Color MasonryDark = new Color(0.40f, 0.38f, 0.36f);
            public static readonly Color Adobe = new Color(0.55f, 0.43f, 0.33f);
            public static readonly Color Thatch = new Color(0.60f, 0.51f, 0.32f);
            public static readonly Color Earth = new Color(0.30f, 0.27f, 0.23f);
            public static readonly Color Paving = new Color(0.38f, 0.36f, 0.33f);
            public static readonly Color Wood = new Color(0.33f, 0.24f, 0.17f);
            public static readonly Color IncaRed = new Color(0.55f, 0.12f, 0.08f);
            public static readonly Color Gold = new Color(1f, 0.74f, 0.3f);
            public static readonly Color Limestone = new Color(0.66f, 0.63f, 0.57f);
            public static readonly Color Cloth = new Color(0.45f, 0.36f, 0.27f);
            public static readonly Color Night = new Color(0.07f, 0.085f, 0.115f);       // fog and sky at the horizon
            public static readonly Color NightAmbient = new Color(0.30f, 0.32f, 0.40f);  // fill light so roofs and people read
        }

        public static Color ColorFor(Surface s)
        {
            switch (s)
            {
                case Surface.Masonry: return Palette.Masonry;
                case Surface.Adobe: return Palette.Adobe;
                case Surface.Thatch: return Palette.Thatch;
                case Surface.Earth: return Palette.Earth;
                default: return Palette.Masonry;
            }
        }

        /// <summary>An axis-aligned block from min/max corners. Masonry goes on the Climbable layer.</summary>
        public static GameObject Block(Transform parent, string name, Vector3 min, Vector3 max, Surface surface, Color? color = null, bool collider = true)
        {
            Vector3 size = max - min;
            Vector3 center = (min + max) * 0.5f;
            var mat = MaterialKit.Lit(color ?? ColorFor(surface), surface, surface == Surface.Masonry ? 0.15f : 0.05f);
            int layer = surface == Surface.Masonry ? Layers.Climbable : Layers.Default;
            return Box(parent, name, center, size, mat, layer, collider);
        }

        public static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Material mat, int layer = Layers.Default, bool collider = true, Quaternion? rotation = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            if (rotation.HasValue) go.transform.rotation = rotation.Value;
            go.layer = layer;
            go.AddComponent<MeshFilter>().sharedMesh = MeshKit.Box(size);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            if (collider)
            {
                var c = go.AddComponent<BoxCollider>();
                c.size = size;
            }
            return go;
        }

        /// <summary>Invisible collider (play-area bounds, blockers).</summary>
        public static GameObject Blocker(Transform parent, string name, Vector3 min, Vector3 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = (min + max) * 0.5f;
            var c = go.AddComponent<BoxCollider>();
            c.size = max - min;
            return go;
        }

        /// <summary>
        /// Make one face of a block climbable. The gripable rectangle can be limited in
        /// height (from/to, metres above the block's base) and inset at the sides.
        /// </summary>
        public static ClimbSurface ClimbFace(GameObject block, Vector3 min, Vector3 max, Face face, float fromY, float toY,
                                             float[] ledges = null, float drainMultiplier = 1f, bool canMantle = true, float inset = 0.4f, string label = "")
        {
            var s = block.AddComponent<ClimbSurface>();
            switch (face)
            {
                case Face.North: s.Normal = Vector3.forward; s.Right = Vector3.left; s.Origin = new Vector3(max.x - inset, fromY, max.z); s.Width = max.x - min.x - 2 * inset; break;
                case Face.South: s.Normal = Vector3.back; s.Right = Vector3.right; s.Origin = new Vector3(min.x + inset, fromY, min.z); s.Width = max.x - min.x - 2 * inset; break;
                case Face.East: s.Normal = Vector3.right; s.Right = Vector3.forward; s.Origin = new Vector3(max.x, fromY, min.z + inset); s.Width = max.z - min.z - 2 * inset; break;
                default: s.Normal = Vector3.left; s.Right = Vector3.back; s.Origin = new Vector3(min.x, fromY, max.z - inset); s.Width = max.z - min.z - 2 * inset; break;
            }
            s.Height = toY - fromY;
            s.Ledges = ledges ?? new float[0];
            s.DrainMultiplier = drainMultiplier;
            s.CanMantle = canMantle;
            s.Label = label;
            // Visible stone ledges: §10 "every surface tells you whether it climbs".
            foreach (float l in s.Ledges)
            {
                Vector3 a = s.PointAt(0, l) + s.Normal * 0.15f + Vector3.down * 0.08f;
                Vector3 b = s.PointAt(s.Width, l) + s.Normal * 0.15f + Vector3.up * 0.08f;
                var lmin = Vector3.Min(a, b) - new Vector3(face == Face.East || face == Face.West ? 0.15f : 0f, 0, face == Face.North || face == Face.South ? 0.15f : 0f);
                var lmax = Vector3.Max(a, b) + new Vector3(face == Face.East || face == Face.West ? 0.15f : 0f, 0, face == Face.North || face == Face.South ? 0.15f : 0f);
                Block(block.transform, "Ledge", lmin, lmax, Surface.Masonry, Palette.MasonryDark, collider: false);
            }
            return s;
        }

        public static TraversalLink Link(Transform parent, string name, LinkKind kind, Vector3 fromMin, Vector3 fromMax, Vector3 landing, Vector3 direction)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<TraversalLink>();
            l.Kind = kind;
            var b = new Bounds();
            b.SetMinMax(fromMin, fromMax);
            l.From = b;
            l.Landing = landing;
            l.Direction = direction.normalized;
            return l;
        }

        /// <summary>A simple thatched roof slab on top of a block, overhanging a little.</summary>
        public static GameObject ThatchTop(Transform parent, Vector3 min, Vector3 max, float thickness = 0.25f, float overhang = 0.3f)
        {
            return Block(parent, "Thatch", new Vector3(min.x - overhang, max.y, min.z - overhang), new Vector3(max.x + overhang, max.y + thickness, max.z + overhang), Surface.Thatch);
        }

        /// <summary>A flat-roofed building: adobe or masonry walls with a thatch top whose surface sits at <paramref name="topY"/>.</summary>
        public static GameObject Building(Transform parent, string name, float x0, float x1, float z0, float z1, float topY, Surface walls)
        {
            var b = Block(parent, name, new Vector3(x0, 0, z0), new Vector3(x1, topY - 0.25f, z1), walls);
            ThatchTop(b.transform, new Vector3(x0, 0, z0), new Vector3(x1, topY - 0.25f, z1));
            return b;
        }

        /// <summary>A dark recess suggesting a doorway on a wall face (not enterable).</summary>
        public static void FakeDoor(Transform parent, Vector3 center, Face face, float width = 1.1f, float height = 2.1f)
        {
            Vector3 size = face == Face.North || face == Face.South ? new Vector3(width, height, 0.06f) : new Vector3(0.06f, height, width);
            Box(parent, "Doorway", center + Vector3.up * (height * 0.5f), size, MaterialKit.Lit(new Color(0.02f, 0.02f, 0.025f)), Layers.Default, false);
        }

        /// <summary>A ground slab covering a rectangle, minus rectangular holes (drain shafts, ramps).</summary>
        public static void GroundWithHoles(Transform parent, Rect area, float thickness, List<Rect> holes, Surface surface, Color color)
        {
            var pieces = new List<Rect> { area };
            foreach (var h in holes)
            {
                var next = new List<Rect>();
                foreach (var p in pieces)
                {
                    if (!p.Overlaps(h)) { next.Add(p); continue; }
                    float x0 = Mathf.Max(p.xMin, h.xMin), x1 = Mathf.Min(p.xMax, h.xMax);
                    if (h.yMin > p.yMin) next.Add(Rect.MinMaxRect(p.xMin, p.yMin, p.xMax, h.yMin));
                    if (h.yMax < p.yMax) next.Add(Rect.MinMaxRect(p.xMin, h.yMax, p.xMax, p.yMax));
                    float y0 = Mathf.Max(p.yMin, h.yMin), y1 = Mathf.Min(p.yMax, h.yMax);
                    if (x0 > p.xMin) next.Add(Rect.MinMaxRect(p.xMin, y0, x0, y1));
                    if (x1 < p.xMax) next.Add(Rect.MinMaxRect(x1, y0, p.xMax, y1));
                }
                pieces = next;
            }
            int i = 0;
            foreach (var p in pieces)
                Block(parent, "Ground" + i++, new Vector3(p.xMin, -thickness, p.yMin), new Vector3(p.xMax, 0f, p.yMax), surface, color);
        }

        /// <summary>A fog-free silhouette card (vertical polygon) for landmarks and ridgelines.</summary>
        public static GameObject Silhouette(Transform parent, string name, IReadOnlyList<Vector2> outline, Vector3 origin, Vector3 right, Color color)
        {
            var pts = new List<Core.P2>(outline.Count);
            foreach (var p in outline) pts.Add(new Core.P2(p.x, p.y));
            var mesh = MeshKit.Polygon(pts, Color.white, Vector2.one, Vector2.zero);
            // Map the XY polygon onto the requested plane (right, up).
            var v = mesh.vertices;
            for (int i = 0; i < v.Length; i++) v[i] = right * v[i].x + Vector3.up * v[i].y;
            mesh.vertices = v;
            mesh.RecalculateBounds();
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            go.layer = Layers.Sky;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Silhouette(color);
            return go;
        }

        public static GameObject Primitive(Transform parent, PrimitiveType type, string name, Vector3 position, Vector3 scale, Material mat, bool keepCollider = false)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            if (!keepCollider) Object.Destroy(go.GetComponent<Collider>());
            return go;
        }
    }
}
