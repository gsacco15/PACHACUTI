using System.Collections.Generic;
using UnityEngine;

namespace Pachacuti
{
    public enum LinkKind
    {
        Jump,  // context jump across a gap: 2.5 m max, 1 m up max (§5)
        Drop,  // hang from an edge and let go: lands quieter and shorter than walking off
    }

    /// <summary>
    /// An authored context traversal from a roof edge. §5: "Context jump from a roof
    /// edge; no free jump." Standing in <see cref="From"/>, facing roughly along
    /// <see cref="Direction"/>, the context key performs it.
    /// </summary>
    public sealed class TraversalLink : MonoBehaviour
    {
        public static readonly List<TraversalLink> All = new List<TraversalLink>();

        public LinkKind Kind;
        public Bounds From;
        public Vector3 Landing;
        public Vector3 Direction;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public static TraversalLink Find(Vector3 feet, Vector3 forward)
        {
            foreach (var l in All)
            {
                if (!l.From.Contains(feet)) continue;
                if (Vector3.Dot(forward, l.Direction) < 0.45f) continue;
                return l;
            }
            return null;
        }
    }
}
