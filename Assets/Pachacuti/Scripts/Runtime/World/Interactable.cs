using System.Collections.Generic;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>Something Manco can use with the interact key. Tap and hold are separate verbs (§10).</summary>
    public sealed class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new List<Interactable>();

        public float Range = 1.6f;
        public float HoldSeconds;            // 0 = tap only
        public System.Func<bool> CanUse;
        public System.Action OnTap;
        public System.Action OnHold;
        public Vector3 Offset;

        public Vector3 Position => transform.position + Offset;

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public static Interactable Nearest(Vector3 from, Vector3 forward)
        {
            Interactable best = null;
            float bestScore = float.MaxValue;
            foreach (var i in All)
            {
                if (i.CanUse != null && !i.CanUse()) continue;
                Vector3 to = i.Position - from;
                to.y *= 0.5f;
                float d = to.magnitude;
                if (d > i.Range) continue;
                float facing = d < 0.6f ? 1f : Vector3.Dot(forward, new Vector3(to.x, 0, to.z).normalized);
                if (facing < 0.2f) continue;
                float score = d - facing;
                if (score < bestScore) { bestScore = score; best = i; }
            }
            return best;
        }

        public static Interactable Add(GameObject go, float range, System.Action onTap, System.Func<bool> canUse = null)
        {
            var i = go.AddComponent<Interactable>();
            i.Range = range;
            i.OnTap = onTap;
            i.CanUse = canUse;
            return i;
        }
    }
}
