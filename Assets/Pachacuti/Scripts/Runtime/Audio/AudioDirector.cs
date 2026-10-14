using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    public enum Ambience
    {
        Silence,
        District,   // wind (3 layers), fog hush, distant city
        Tunnel,     // close, dripping
        Sky,        // ground audio fades; a low drone enters (§8)
        Vision,
    }

    /// <summary>
    /// Owns every AudioSource. Ground: wind and breath are the loudest sounds on the
    /// roof; no music (§5). Sky-map: ground ducks, drone enters (§8). Vision: the
    /// shot list's audio column (§9) is driven by VisionDirector through Play/Loop.
    /// </summary>
    public sealed class AudioDirector : MonoBehaviour
    {
        readonly Dictionary<string, AudioClip> _clips = new Dictionary<string, AudioClip>();
        AudioSource _windLow, _windHigh, _windGust, _breath, _drone, _pad, _loopA, _loopB;
        readonly List<AudioSource> _oneShots = new List<AudioSource>();
        readonly List<AudioSource> _spatial = new List<AudioSource>();
        Ambience _ambience = Ambience.Silence;
        float _groundTarget, _droneTarget, _padTarget;
        float _nextBreath;
        int _breathSeed;
        public bool BreathMuted;

        public static AudioDirector Create(Transform parent)
        {
            var go = new GameObject("Audio");
            go.transform.SetParent(parent, false);
            var a = go.AddComponent<AudioDirector>();
            a.Build();
            return a;
        }

        void Build()
        {
            Add("wind_low", Synth.Wind(9f, 1, 0.1f));
            Add("wind_high", Synth.Wind(7f, 2, 0.8f));
            Add("wind_gust", Synth.Wind(11f, 3, 0.45f));
            Add("breath_calm", Synth.Breath(0f, 11));
            Add("breath_audible", Synth.Breath(0.55f, 12));
            Add("breath_shaking", Synth.Breath(1f, 13));
            Add("gasp", Synth.Gasp(14));
            Add("step", Synth.Thud(0.05f, 21));
            Add("land_small", Synth.Thud(0.35f, 22));
            Add("land_mid", Synth.Thud(0.65f, 23));
            Add("land_big", Synth.Thud(1f, 24));
            Add("load_drop", Synth.Thud(0.8f, 25));
            Add("scrape", Synth.Scrape(26));
            Add("shout", Synth.Shout(27, 180f));
            Add("shout_low", Synth.Shout(28, 130f));
            Add("torch", Synth.Fire(4f, 31, 6f));
            Add("fire", Synth.Fire(5f, 32, 14f));
            Add("water", Synth.Water(4f, 33));
            Add("pututu", Synth.Pututu(3.6f, 233f, 41, 0.15f));
            Add("pututu_far", Synth.Pututu(3.2f, 220f, 42, 0.9f));
            Add("drone", Synth.Drone(6f, 110f));
            Add("choir", Synth.ChoirPad(8f));
            Add("quena", Synth.Quena(6f, 587.33f, 51));
            Add("subhit", Synth.SubHit(4f));
            Add("grind", Synth.Grind(6f, 52));
            Add("hum", Synth.Hum(4f));
            Add("harquebus", Synth.Harquebus(53));

            _windLow = Source("WindLow", "wind_low", true);
            _windHigh = Source("WindHigh", "wind_high", true);
            _windGust = Source("WindGust", "wind_gust", true);
            _breath = Source("Breath", null, false);
            _drone = Source("Drone", "drone", true);
            _pad = Source("Pad", "choir", true);
            _loopA = Source("LoopA", null, true);
            _loopB = Source("LoopB", null, true);
            for (int i = 0; i < 6; i++) _oneShots.Add(Source("OneShot" + i, null, false));
            for (int i = 0; i < 6; i++)
            {
                var s = Source("Spatial" + i, null, false);
                s.spatialBlend = 1f;
                s.rolloffMode = AudioRolloffMode.Linear;
                s.minDistance = 2f;
                s.maxDistance = 45f;
                _spatial.Add(s);
            }
            _windLow.volume = _windHigh.volume = _windGust.volume = 0f;
            _drone.volume = _pad.volume = 0f;
        }

        void Add(string name, float[] data) => _clips[name] = Synth.Clip(name, data);

        public AudioClip Get(string name) => _clips.TryGetValue(name, out var c) ? c : null;

        AudioSource Source(string name, string clip, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var s = go.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = loop;
            s.spatialBlend = 0f;
            if (clip != null) s.clip = Get(clip);
            return s;
        }

        public void SetAmbience(Ambience a)
        {
            _ambience = a;
            switch (a)
            {
                case Ambience.District: _groundTarget = 1f; _droneTarget = 0f; _padTarget = 0f; break;
                case Ambience.Tunnel: _groundTarget = 0.25f; _droneTarget = 0f; _padTarget = 0f; break;
                case Ambience.Sky: _groundTarget = 0.15f; _droneTarget = 0.55f; _padTarget = 0f; break;
                case Ambience.Vision: _groundTarget = 0f; _droneTarget = 0.6f; _padTarget = 0f; break;
                default: _groundTarget = 0f; _droneTarget = 0f; _padTarget = 0f; break;
            }
            foreach (var s in new[] { _windLow, _windHigh, _windGust, _drone, _pad })
                if (!s.isPlaying && s.clip != null) s.Play();
        }

        public Ambience Ambience => _ambience;

        /// <summary>Hard-set the drone (for the vision's octave drop) and choir.</summary>
        public void SetDrone(float volume, float pitch) { _droneTarget = volume; _drone.pitch = pitch; }
        public void SetChoir(float volume) => _padTarget = volume;

        /// <summary>Kill every sound this frame (the vision's "Silence").</summary>
        public void HardSilence()
        {
            foreach (var s in new[] { _windLow, _windHigh, _windGust, _drone, _pad, _loopA, _loopB, _breath }) s.volume = 0f;
            foreach (var s in _oneShots) s.Stop();
            _groundTarget = _droneTarget = _padTarget = 0f;
        }

        public void Play(string clip, float volume = 1f, float pitch = 1f)
        {
            var c = Get(clip);
            if (c == null) return;
            var s = Free(_oneShots);
            s.pitch = pitch;
            s.volume = volume;
            s.clip = c;
            s.Play();
        }

        public void PlayAt(string clip, Vector3 position, float volume = 1f, float pitch = 1f, float maxDistance = 45f)
        {
            var c = Get(clip);
            if (c == null) return;
            var s = Free(_spatial);
            s.transform.position = position;
            s.maxDistance = maxDistance;
            s.pitch = pitch;
            s.volume = volume;
            s.clip = c;
            s.Play();
        }

        public void Loop(int channel, string clip, float volume, float pitch = 1f)
        {
            var s = channel == 0 ? _loopA : _loopB;
            var c = Get(clip);
            if (c == null) { s.Stop(); return; }
            if (s.clip != c) { s.clip = c; s.Play(); }
            else if (!s.isPlaying) s.Play();
            s.volume = volume;
            s.pitch = pitch;
        }

        public void StopLoop(int channel) => (channel == 0 ? _loopA : _loopB).Stop();

        /// <summary>Attach a looping 3D source (torch crackle, Ninan's fire) to a transform.</summary>
        public AudioSource Attach(Transform target, string clip, float volume, float maxDistance)
        {
            var s = target.gameObject.AddComponent<AudioSource>();
            s.clip = Get(clip);
            s.loop = true;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = 1f;
            s.maxDistance = maxDistance;
            s.volume = volume;
            s.time = Random.Range(0f, Mathf.Max(0.01f, s.clip.length - 0.01f));
            s.Play();
            return s;
        }

        static AudioSource Free(List<AudioSource> pool)
        {
            foreach (var s in pool) if (!s.isPlaying) return s;
            return pool[0];
        }

        /// <summary>
        /// Breath is the meter (§5): calm and quiet when full; audible and fast below 30;
        /// rough and loud below 10; a gasp at 0.
        /// </summary>
        public void TickBreath(BreathModel breath, float dt)
        {
            if (BreathMuted) return;
            _nextBreath -= dt;
            if (_nextBreath > 0f) return;
            string clip;
            float vol, gap;
            switch (breath.Band)
            {
                case BreathBand.Empty: clip = "gasp"; vol = 0.95f; gap = 0.55f; break;
                case BreathBand.Shaking: clip = "breath_shaking"; vol = 0.9f; gap = 0.95f; break;
                case BreathBand.Audible: clip = "breath_audible"; vol = Mathf.Lerp(0.45f, 0.8f, breath.Strain); gap = 1.25f; break;
                default: clip = "breath_calm"; vol = 0.16f; gap = 3.4f; break;
            }
            _breath.clip = Get(clip);
            _breath.volume = vol;
            _breath.pitch = 0.95f + 0.1f * ((_breathSeed++ * 7919) % 100) / 100f;
            _breath.Play();
            _nextBreath = gap;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            float k = 1f - Mathf.Exp(-dt * 1.5f);
            float gust = 0.75f + 0.25f * Mathf.Sin(Time.time * 0.21f);
            _windLow.volume = Mathf.Lerp(_windLow.volume, 0.55f * _groundTarget, k);
            _windHigh.volume = Mathf.Lerp(_windHigh.volume, 0.22f * _groundTarget, k);
            _windGust.volume = Mathf.Lerp(_windGust.volume, 0.3f * _groundTarget * gust, k);
            _drone.volume = Mathf.Lerp(_drone.volume, _droneTarget, k);
            _pad.volume = Mathf.Lerp(_pad.volume, _padTarget, k);
        }
    }
}
