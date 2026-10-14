using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>
    /// The line of six porters walking south down Hatunrumiyoc (§6), carrying the
    /// emptied palace's goods to the storehouse yard. Manco joins it. Walking in
    /// the lane, at the line's pace, head down, is what "with the line" means to
    /// the soldiers.
    /// </summary>
    public sealed class PorterLine : MonoBehaviour
    {
        readonly List<Humanoid> _porters = new List<Humanoid>();
        readonly List<float> _offsets = new List<float>();   // lateral jitter
        float _headZ;
        bool _moving;
        public float Speed = Tuning.PorterLineSpeed;
        public bool Moving => _moving;
        public float HeadZ => _headZ;
        public float TailZ => _headZ + (_porters.Count - 1 + 1) * Tuning.PorterSpacing; // one slot for Manco

        public static PorterLine Create(Transform parent)
        {
            var go = new GameObject("Porter line");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<PorterLine>();
            var colors = new[] { new Color(0.42f, 0.36f, 0.28f), new Color(0.35f, 0.3f, 0.25f), new Color(0.5f, 0.42f, 0.3f) };
            for (int i = 0; i < Tuning.PorterCount; i++)
            {
                var h = Humanoid.Build(go.transform, "Porter " + (i + 1), colors[i % colors.Length], new Color(0.4f, 0.28f, 0.2f), 1.55f + (i % 3) * 0.03f,
                                       i % 2 == 0 ? HeadGear.None : HeadGear.Llautu, HandProp.None, new Color(0.3f, 0.25f, 0.2f));
                h.SetLoad(true);
                h.HeadDown = 1f;
                Layers.SetRecursively(h.gameObject, Layers.Characters);
                line._porters.Add(h);
                line._offsets.Add(((i * 37) % 7 - 3) * 0.07f);
            }
            line.Place(Layout.StreetNorthZ - 3f);
            return line;
        }

        /// <summary>Slot index Manco fills: between porter 3 and 4.</summary>
        const int MancoSlot = 3;

        /// <summary>Put the line with its head at z, Manco's slot empty.</summary>
        public void Place(float headZ)
        {
            _headZ = headZ;
            _moving = false;
            UpdatePositions(0f);
        }

        public void Begin() => _moving = true;

        public Vector3 MancoSlotPosition => new Vector3(Layout.LaneX, 0f, _headZ + MancoSlot * Tuning.PorterSpacing);

        public void Tick(float dt)
        {
            if (_moving) _headZ -= Speed * dt;
            UpdatePositions(dt);
        }

        void UpdatePositions(float dt)
        {
            for (int i = 0; i < _porters.Count; i++)
            {
                int slot = i < MancoSlot ? i : i + 1;
                float z = _headZ + slot * Tuning.PorterSpacing;
                Vector3 pos;
                Vector3 facing = Vector3.back;
                if (z > Layout.StreetSouthZ + 2f)
                    pos = new Vector3(Layout.LaneX + _offsets[i], 0f, z);
                else
                {
                    // Into the yard and over to the storehouses.
                    float into = (Layout.StreetSouthZ + 2f) - z;
                    Vector3 start = new Vector3(Layout.LaneX, 0, Layout.StreetSouthZ + 2f);
                    Vector3 gather = Layout.PorterGather + new Vector3((i % 3) * 1.4f, 0, (i / 3) * 1.4f);
                    float len = Vector3.Distance(start, gather);
                    pos = Vector3.Lerp(start, gather, Mathf.Clamp01(into / len));
                    facing = (gather - start).normalized;
                }
                var t = _porters[i].transform;
                t.position = pos + Vector3.up * (Mathf.Abs(Mathf.Sin((Time.time + i * 0.37f) * 5.5f)) * 0.03f * (_moving ? 1f : 0f));
                t.rotation = Quaternion.LookRotation(facing, Vector3.up);
            }
        }

        /// <summary>
        /// Is Manco walking with the line? In the lane, within a slot's reach of a
        /// porter, and keeping pace (or standing still while the line stands).
        /// </summary>
        public bool IsWithLine(Manco m)
        {
            Vector3 p = m.transform.position;
            if (p.z < Layout.StreetSouthZ || p.z > Layout.StreetNorthZ + 1f) return false;
            if (Mathf.Abs(p.x - Layout.LaneX) > Tuning.PorterLaneHalfWidth) return false;
            float nearest = float.MaxValue;
            foreach (var h in _porters) nearest = Mathf.Min(nearest, Vector3.Distance(h.transform.position, p));
            if (nearest > Tuning.PorterSpacing * 1.4f) return false;
            if (!_moving) return true;
            Vector3 v = m.Velocity;
            return m.Speed > 0.4f && v.z < -0.2f && m.Speed < Tuning.WalkSpeed + 0.2f;
        }

        /// <summary>The pace cap for Manco: never walk up the back of the porter ahead.</summary>
        public float? PaceFor(Manco m)
        {
            Vector3 p = m.transform.position;
            if (Mathf.Abs(p.x - Layout.LaneX) > Tuning.PorterLaneHalfWidth + 0.3f) return null;
            float ahead = float.MaxValue;
            foreach (var h in _porters)
            {
                float dz = p.z - h.transform.position.z; // porter ahead (south) has smaller z
                if (dz > 0f && Mathf.Abs(h.transform.position.x - p.x) < 1f) ahead = Mathf.Min(ahead, dz);
            }
            if (ahead > Tuning.PorterSpacing * 1.2f) return null;
            return ahead < Tuning.PorterSpacing * 0.8f ? Speed * 0.5f : Speed;
        }

        public bool AllInYard => _headZ + (_porters.Count + 1) * Tuning.PorterSpacing < Layout.StreetSouthZ;
    }
}
