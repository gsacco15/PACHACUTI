using System;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>
    /// Placeholder sound design synthesized at startup (no audio files until the
    /// sound designer starts in week 7). The breath meter is hidden and audio-only
    /// (§5), so breath, wind and the cues the beats hang on must exist from week 1.
    /// All clips are mono, 22.05 kHz, non-streamed (procedural streaming is not
    /// supported on WebGL).
    /// </summary>
    public static class Synth
    {
        public const int Rate = 22050;
        const float TwoPi = Mathf.PI * 2f;

        public static AudioClip Clip(string name, float[] data)
        {
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static float[] Buffer(float seconds) => new float[Mathf.Max(1, (int)(seconds * Rate))];

        // ------------------------------------------------------------------ ambience

        /// <summary>Wind: brown noise, slow gusts, seamless loop.</summary>
        public static float[] Wind(float seconds, int seed, float brightness)
        {
            var rng = new System.Random(seed);
            var b = Buffer(seconds);
            float brown = 0f, lp = 0f;
            float a = Mathf.Lerp(0.02f, 0.12f, brightness);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                brown = (brown + 0.02f * Rand(rng)) / 1.02f;
                lp += a * (brown - lp);
                float gust = 0.55f + 0.45f * Mathf.Sin(TwoPi * (t / seconds) * 3f + Mathf.Sin(TwoPi * t / seconds) * 2f);
                b[i] = lp * 9f * gust;
            }
            return Loop(Normalize(b, 0.6f), 0.5f);
        }

        /// <summary>Fire / torch: crackle pops over a soft roar.</summary>
        public static float[] Fire(float seconds, int seed, float popRate)
        {
            var rng = new System.Random(seed);
            var b = Buffer(seconds);
            float lp = 0f, pop = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                lp += 0.05f * (Rand(rng) - lp);
                if (rng.NextDouble() < popRate / Rate) pop = 0.6f + 0.4f * (float)rng.NextDouble();
                pop *= 0.985f;
                b[i] = lp * 0.5f + pop * Rand(rng);
            }
            return Loop(Normalize(b, 0.5f), 0.3f);
        }

        /// <summary>Water on stone: trickle with small bubbles. Played pitched down in the vision.</summary>
        public static float[] Water(float seconds, int seed)
        {
            var rng = new System.Random(seed);
            var b = Buffer(seconds);
            float lp = 0f;
            float bubbleF = 0f, bubbleA = 0f, phase = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                lp += 0.25f * (Rand(rng) - lp);
                if (rng.NextDouble() < 18.0 / Rate) { bubbleF = 300f + 500f * (float)rng.NextDouble(); bubbleA = 0.5f; }
                bubbleF *= 1.00008f;
                bubbleA *= 0.9993f;
                phase += TwoPi * bubbleF / Rate;
                b[i] = lp * 0.35f + Mathf.Sin(phase) * bubbleA;
            }
            return Loop(Normalize(b, 0.5f), 0.3f);
        }

        // ------------------------------------------------------------------ body

        /// <summary>One breath cycle (inhale + exhale). Strain 0..1 makes it shorter, louder, rougher.</summary>
        public static float[] Breath(float strain, int seed)
        {
            var rng = new System.Random(seed);
            float inhale = Mathf.Lerp(1.1f, 0.35f, strain), exhale = Mathf.Lerp(1.4f, 0.45f, strain);
            var b = Buffer(inhale + exhale + 0.05f);
            float lp = 0f, hp = 0f;
            float lpA = Mathf.Lerp(0.12f, 0.35f, strain);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float env = t < inhale ? Mathf.Sin(Mathf.PI * t / inhale) * 0.7f
                          : Mathf.Sin(Mathf.PI * Mathf.Clamp01((t - inhale) / exhale)) * 1.0f;
                float n = Rand(rng);
                lp += lpA * (n - lp);
                hp = lp - hp * 0.92f;
                float rasp = strain > 0.5f ? 1f + 0.4f * Mathf.Sin(TwoPi * 90f * t) * (strain - 0.5f) : 1f;
                b[i] = hp * env * rasp;
            }
            return Normalize(b, Mathf.Lerp(0.35f, 0.8f, strain));
        }

        /// <summary>The gasp at 0 breath: a sharp ragged inhale.</summary>
        public static float[] Gasp(int seed)
        {
            var rng = new System.Random(seed);
            var b = Buffer(0.55f);
            float lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.05f) * Mathf.Exp(-t * 5f);
                lp += 0.4f * (Rand(rng) - lp);
                b[i] = lp * env * (1f + 0.5f * Mathf.Sin(TwoPi * 140f * t));
            }
            return Normalize(b, 0.9f);
        }

        /// <summary>Impact: footstep on stone (small), drop landing (big), load hitting the street.</summary>
        public static float[] Thud(float weight, int seed)
        {
            var rng = new System.Random(seed);
            float len = Mathf.Lerp(0.08f, 0.45f, weight);
            var b = Buffer(len);
            float lp = 0f, phase = 0f;
            float f0 = Mathf.Lerp(160f, 55f, weight);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Exp(-t * Mathf.Lerp(60f, 9f, weight));
                lp += 0.3f * (Rand(rng) - lp);
                phase += TwoPi * f0 * (1f - t * 0.6f) / Rate;
                b[i] = (Mathf.Sin(phase) * 0.8f + lp * 0.6f) * env;
            }
            return Normalize(b, Mathf.Lerp(0.3f, 0.95f, weight));
        }

        /// <summary>Sliding off adobe: a dry scrape (§5).</summary>
        public static float[] Scrape(int seed)
        {
            var rng = new System.Random(seed);
            var b = Buffer(0.6f);
            float lp = 0f, prev = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.03f) * Mathf.Exp(-t * 4f) * (0.7f + 0.3f * Mathf.Sin(TwoPi * 23f * t));
                float n = Rand(rng);
                lp += 0.6f * (n - lp);
                float hp = lp - prev;
                prev = lp;
                b[i] = hp * env * 3f;
            }
            return Normalize(b, 0.7f);
        }

        /// <summary>A shout: band-limited noise with a voiced buzz. Stand-in for soldier barks until VO.</summary>
        public static float[] Shout(int seed, float pitch)
        {
            var rng = new System.Random(seed);
            var b = Buffer(0.5f);
            float lp = 0f, phase = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.03f) * Mathf.Clamp01((0.5f - t) / 0.15f);
                phase += TwoPi * pitch * (1f + 0.15f * Mathf.Sin(TwoPi * 3f * t)) / Rate;
                float buzz = Mathf.Sin(phase) + 0.5f * Mathf.Sin(phase * 2f) + 0.3f * Mathf.Sin(phase * 3f);
                lp += 0.3f * (Rand(rng) - lp);
                b[i] = (buzz * 0.6f + lp * 0.5f) * env;
            }
            return Normalize(b, 0.6f);
        }

        // ------------------------------------------------------------------ instruments

        /// <summary>Pututu: the conch horn. Swell, breathy, slightly sharp on the attack.</summary>
        public static float[] Pututu(float seconds, float freq, int seed, float distance01)
        {
            var rng = new System.Random(seed);
            var b = Buffer(seconds);
            float phase = 0f, lp = 0f, noise = 0f;
            float lpA = Mathf.Lerp(0.5f, 0.06f, distance01);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.45f) * Mathf.Clamp01((seconds - t) / 0.7f);
                float bend = 1f + 0.03f * Mathf.Exp(-t * 3f);
                phase += TwoPi * freq * bend / Rate;
                float tone = Mathf.Sin(phase) + 0.55f * Mathf.Sin(phase * 2f) + 0.3f * Mathf.Sin(phase * 3f) + 0.12f * Mathf.Sin(phase * 4f);
                noise += 0.3f * (Rand(rng) - noise);
                float s = tone * (1f + 0.2f * Mathf.Sin(TwoPi * 4.3f * t)) + noise * 0.35f;
                lp += lpA * (s - lp);
                b[i] = lp * env;
            }
            return Normalize(b, Mathf.Lerp(0.8f, 0.35f, distance01));
        }

        /// <summary>Quena: a held flute note with breath and slow vibrato.</summary>
        public static float[] Quena(float seconds, float freq, int seed)
        {
            var rng = new System.Random(seed);
            var b = Buffer(seconds);
            float phase = 0f, noise = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t / 0.25f) * Mathf.Clamp01((seconds - t) / 1.2f);
                float vib = 1f + 0.006f * Mathf.Sin(TwoPi * 5.2f * t) * Mathf.Clamp01((t - 0.8f) / 1f);
                phase += TwoPi * freq * vib / Rate;
                noise += 0.5f * (Rand(rng) - noise);
                b[i] = (Mathf.Sin(phase) + 0.08f * Mathf.Sin(phase * 2f) + noise * 0.12f) * env;
            }
            return Normalize(b, 0.55f);
        }

        /// <summary>Siku-like drone: stacked fifths, slow beating. Looping.</summary>
        public static float[] Drone(float seconds, float root)
        {
            var b = Buffer(seconds);
            float[] ratios = { 0.5f, 1f, 1.5f, 1.003f, 2f };
            float[] amps = { 0.8f, 0.6f, 0.35f, 0.5f, 0.15f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int k = 0; k < ratios.Length; k++)
                    s += amps[k] * Mathf.Sin(TwoPi * Snap(root * ratios[k], seconds) * t);
                b[i] = s * (0.8f + 0.2f * Mathf.Sin(TwoPi * t / seconds * 2f));
            }
            return Normalize(b, 0.5f);
        }

        /// <summary>Choir pad (Quechua choir placeholder): soft harmonic stacks on an A-minor pentatonic chord. Looping.</summary>
        public static float[] ChoirPad(float seconds)
        {
            var b = Buffer(seconds);
            float[] notes = { 110f, 164.81f, 220f, 261.63f, 329.63f };
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float f = Snap(notes[n], seconds);
                    float sw = 0.6f + 0.4f * Mathf.Sin(TwoPi * (t / seconds) * (n + 1) + n);
                    for (int h = 1; h <= 5; h++) s += Mathf.Sin(TwoPi * f * h * t + n * h) / (h * h) * sw;
                }
                b[i] = s;
            }
            return Normalize(b, 0.45f);
        }

        /// <summary>Sub-bass hit for the cut: 42 Hz with a long tail.</summary>
        public static float[] SubHit(float seconds)
        {
            var b = Buffer(seconds);
            float phase = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                phase += TwoPi * (42f + 30f * Mathf.Exp(-t * 18f)) / Rate;
                b[i] = Mathf.Sin(phase) * Mathf.Exp(-t * 1.1f) * Mathf.Clamp01(t / 0.004f);
            }
            return Normalize(b, 1f);
        }

        /// <summary>Grinding stone: rough low rumble. Looping.</summary>
        public static float[] Grind(float seconds, int seed)
        {
            var rng = new System.Random(seed);
            var b = Buffer(seconds);
            float brown = 0f, lp = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                brown = (brown + 0.03f * Rand(rng)) / 1.03f;
                lp += 0.08f * (brown - lp);
                float grit = (float)rng.NextDouble() < 0.002 ? Rand(rng) : 0f;
                b[i] = lp * 8f * (0.6f + 0.4f * Mathf.Sin(TwoPi * 1.7f * t)) + Mathf.Sin(TwoPi * 31f * t) * 0.25f + grit * 0.3f;
            }
            return Loop(Normalize(b, 0.7f), 0.3f);
        }

        /// <summary>The ceque threads' hum. Looping.</summary>
        public static float[] Hum(float seconds)
        {
            var b = Buffer(seconds);
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                b[i] = Mathf.Sin(TwoPi * Snap(220f, seconds) * t) * 0.6f + Mathf.Sin(TwoPi * Snap(221.5f, seconds) * t) * 0.6f
                     + Mathf.Sin(TwoPi * Snap(330f, seconds) * t) * 0.25f;
            }
            return Normalize(b, 0.4f);
        }

        /// <summary>A single harquebus crack: the only gunshot in the slice (§12 audio).</summary>
        public static float[] Harquebus(int seed)
        {
            var rng = new System.Random(seed);
            var b = Buffer(2.2f);
            float lp = 0f, phase = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)Rate;
                float crack = Rand(rng) * Mathf.Exp(-t * 60f);
                lp += 0.05f * (Rand(rng) - lp);
                float tail = lp * 4f * Mathf.Exp(-t * 2.2f);
                phase += TwoPi * (70f * Mathf.Exp(-t * 4f) + 30f) / Rate;
                float thump = Mathf.Sin(phase) * Mathf.Exp(-t * 7f);
                b[i] = crack * 1.2f + tail + thump * 0.8f;
            }
            return Normalize(b, 1f);
        }

        // ------------------------------------------------------------------ helpers

        static float Rand(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

        /// <summary>Round a frequency so a whole number of cycles fits the loop (no click at the seam).</summary>
        static float Snap(float f, float seconds) => Mathf.Max(1f, Mathf.Round(f * seconds)) / seconds;

        static float[] Normalize(float[] b, float peak)
        {
            float m = 1e-6f;
            for (int i = 0; i < b.Length; i++) m = Math.Max(m, Math.Abs(b[i]));
            float k = peak / m;
            for (int i = 0; i < b.Length; i++) b[i] *= k;
            return b;
        }

        /// <summary>Crossfade the last <paramref name="fadeSeconds"/> into the start so the clip loops seamlessly.</summary>
        static float[] Loop(float[] b, float fadeSeconds)
        {
            int n = Mathf.Min(b.Length / 2, (int)(fadeSeconds * Rate));
            var outBuf = new float[b.Length - n];
            Array.Copy(b, outBuf, outBuf.Length);
            for (int i = 0; i < n; i++)
            {
                float w = i / (float)n;
                outBuf[i] = b[i] * w + b[b.Length - n + i] * (1f - w);
            }
            return outBuf;
        }
    }
}
