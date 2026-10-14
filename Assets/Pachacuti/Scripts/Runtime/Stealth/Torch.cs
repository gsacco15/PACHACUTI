using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>
    /// A flame: a point light, a glow sprite and the 6 m "lit = seen" radius (§5).
    /// "Torchlight is the only light besides the sky. Soldiers carry torches; Manco does not."
    /// The budget keeps at most 8 real-time point lights on (§13), nearest the camera first;
    /// the stealth radius applies whether or not the light itself is currently rendered.
    /// </summary>
    public sealed class Torch : MonoBehaviour
    {
        public static readonly List<Torch> All = new List<Torch>();
        const int LightBudget = 8;

        public float Radius = Tuning.TorchLightRadius;
        public bool Lit = true;
        Light _light;
        Transform _flame;
        float _baseIntensity;
        float _seed;

        public static Torch Create(Transform parent, Vector3 localPos, float intensity = 2.4f, float range = 9f, bool flame = true, Color? color = null)
        {
            var go = new GameObject("Torch");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var t = go.AddComponent<Torch>();
            t._light = go.AddComponent<Light>();
            t._light.type = LightType.Point;
            t._light.range = range;
            t._light.color = color ?? new Color(1f, 0.62f, 0.3f);
            t._light.intensity = intensity;
            t._light.shadows = LightShadows.None; // §13: torches no shadows
            t._baseIntensity = intensity;
            t._seed = Random.value * 100f;
            if (flame)
            {
                var f = new GameObject("Flame");
                f.transform.SetParent(go.transform, false);
                f.AddComponent<MeshFilter>().sharedMesh = MeshKit.Quad(0.35f, 0.55f, new Color(1f, 0.55f, 0.18f));
                f.AddComponent<MeshRenderer>().sharedMaterial = MaterialKit.Glow(new Color(1f, 0.6f, 0.25f), 2.2f);
                f.AddComponent<Billboard>();
                t._flame = f.transform;
            }
            return t;
        }

        void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        void OnDisable() => All.Remove(this);

        public Vector3 Position => transform.position;

        void Update()
        {
            if (_light == null) return;
            float flicker = 0.85f + 0.1f * Mathf.PerlinNoise(_seed, Time.time * 7f) + 0.05f * Mathf.Sin(Time.time * 23f + _seed);
            _light.intensity = Lit ? _baseIntensity * flicker : 0f;
            if (_flame != null)
            {
                _flame.gameObject.SetActive(Lit);
                _flame.localScale = new Vector3(1f, 0.85f + 0.3f * flicker, 1f);
            }
        }

        /// <summary>Distance from a point to the nearest burning torch.</summary>
        public static float NearestDistance(Vector3 p)
        {
            float best = float.MaxValue;
            foreach (var t in All)
            {
                if (!t.Lit || !t.isActiveAndEnabled) continue;
                float d = Vector3.Distance(t.Position, p);
                if (d < best) best = d;
            }
            return best;
        }

        public static bool IsLit(Vector3 p)
        {
            foreach (var t in All)
                if (t.Lit && t.isActiveAndEnabled && Vector3.Distance(t.Position, p) <= t.Radius) return true;
            return false;
        }

        static readonly List<Torch> Sorted = new List<Torch>();

        /// <summary>Enable only the nearest <see cref="LightBudget"/> lights to the viewer.</summary>
        public static void ApplyBudget(Vector3 viewer)
        {
            Sorted.Clear();
            foreach (var t in All) if (t._light != null && t.isActiveAndEnabled) Sorted.Add(t);
            Sorted.Sort((a, b) => (a.Position - viewer).sqrMagnitude.CompareTo((b.Position - viewer).sqrMagnitude));
            for (int i = 0; i < Sorted.Count; i++) Sorted[i]._light.enabled = i < LightBudget;
        }
    }

    public struct NoiseEvent
    {
        public Vector3 Position;
        public float Radius;
        public NoiseKind Kind;
        public float Time;
    }

    /// <summary>
    /// Manco's noises (§5): "run and drop emit a noise radius (run 15 m, drop 8–20 m by
    /// height). A soldier within the radius turns toward it and holds for 4 s."
    /// Soldiers read the frame's events in their own Update.
    /// </summary>
    public static class NoiseBus
    {
        public static readonly List<NoiseEvent> Recent = new List<NoiseEvent>();

        public static void Emit(Vector3 position, NoiseKind kind, float dropHeight = 0f)
        {
            float r = StealthRules.NoiseRadius(kind, dropHeight);
            if (r <= 0f) return;
            Recent.Add(new NoiseEvent { Position = position, Radius = r, Kind = kind, Time = UnityEngine.Time.time });
        }

        /// <summary>Most recent noise this soldier can hear within the last frame window.</summary>
        public static bool Heard(Vector3 listener, float window, out NoiseEvent heard)
        {
            heard = default;
            bool any = false;
            float now = UnityEngine.Time.time;
            foreach (var e in Recent)
            {
                if (now - e.Time > window) continue;
                if (StealthRules.Hears(Vector3.Distance(listener, e.Position), e.Radius)) { heard = e; any = true; }
            }
            return any;
        }

        public static void Prune(float maxAge)
        {
            float now = UnityEngine.Time.time;
            Recent.RemoveAll(e => now - e.Time > maxAge);
        }
    }
}
