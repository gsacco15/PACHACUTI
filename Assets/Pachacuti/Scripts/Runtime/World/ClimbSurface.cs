using System.Collections.Generic;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>
    /// A gripable rectangle on a wall face: polygonal masonry or roof beams (§5 "Climb:
    /// only on gripable surfaces"). Manco moves in the face's (u, v) plane while on it.
    /// Ledges are authored rest heights; "on a wall [at 0 breath] he falls to the last ledge".
    /// </summary>
    public sealed class ClimbSurface : MonoBehaviour
    {
        public static readonly List<ClimbSurface> All = new List<ClimbSurface>();

        /// <summary>Bottom-left corner of the gripable rectangle, on the face plane (world).</summary>
        public Vector3 Origin;
        /// <summary>Outward normal (horizontal).</summary>
        public Vector3 Normal;
        /// <summary>Horizontal tangent along the face; u grows along it.</summary>
        public Vector3 Right;
        public float Width;
        public float Height;
        /// <summary>Rest heights, metres above Origin.y.</summary>
        public float[] Ledges = new float[0];
        /// <summary>Grip difficulty: multiplies the climb breath drain (Tuning.ZoneAWallDrainMultiplier).</summary>
        public float DrainMultiplier = 1f;
        /// <summary>True if the top edge leads onto a walkable surface.</summary>
        public bool CanMantle = true;
        public string Label = "";

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public Vector3 PointAt(float u, float v) => Origin + Right * u + Vector3.up * v;

        /// <summary>Project a world point onto the face. Returns false if the point is behind the face.</summary>
        public bool Project(Vector3 world, out float u, out float v, out float distance)
        {
            Vector3 d = world - Origin;
            u = Vector3.Dot(d, Right);
            v = d.y;
            distance = Vector3.Dot(d, Normal);
            return distance > -0.2f;
        }

        public bool Contains(float u, float v, float margin = 0f) =>
            u >= -margin && u <= Width + margin && v >= -margin && v <= Height + margin;

        /// <summary>Highest ledge at or below v (the one Manco falls back to), or 0 for the foot of the wall.</summary>
        public float LedgeBelow(float v)
        {
            float best = 0f;
            foreach (var l in Ledges) if (l <= v + 0.05f && l > best) best = l;
            return best;
        }

        public bool NearLedge(float v, out float ledge)
        {
            foreach (var l in Ledges)
                if (Mathf.Abs(v - l) < 0.35f) { ledge = l; return true; }
            ledge = 0f;
            return false;
        }

        /// <summary>Find the climbable face Manco could grab from here: in front of him, within reach.</summary>
        public static ClimbSurface FindGrab(Vector3 feet, Vector3 forward, float reach, out float u, out float v)
        {
            ClimbSurface best = null;
            float bestDist = float.MaxValue;
            u = v = 0f;
            foreach (var s in All)
            {
                if (Vector3.Dot(forward, -s.Normal) < 0.55f) continue; // must be facing the wall
                if (!s.Project(feet + Vector3.up * 0.3f, out var pu, out var pv, out var dist)) continue;
                if (dist > reach || dist < -0.1f) continue;
                if (!s.Contains(pu, pv, 0.3f)) continue;
                if (dist < bestDist) { bestDist = dist; best = s; u = Mathf.Clamp(pu, 0.3f, s.Width - 0.3f); v = Mathf.Clamp(pv, 0f, s.Height); }
            }
            return best;
        }
    }
}
