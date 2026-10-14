using System;

namespace Pachacuti.Core
{
    /// <summary>The Qenqo vision shot list (pitch slice §9), 90 s.</summary>
    public enum VisionShot
    {
        Touch,    // 0:00 hand on the stone; one beat of nothing; breath stops
        Cut,      // 0:02 THE CUT: fog gone, sky deep indigo, Milky Way ×3 and flowing
        Channel,  // 0:05 liquid light runs the zigzag channel in 3 s, splitting at the fork
        Lines,    // 0:10 41 golden ceque threads snap taut from the Coricancha
        Llama,    // 0:20 the dark Yacana stands up out of the Milky Way and walks
        Stones,   // 0:35 Qenqo splits along its fracture planes; blocks rise and hang
        Pacha,    // 0:50 time folds: noon, dawn, snow, rain, cycling to a strobe; condor above, serpent below
        Square,   // 1:05 hard cut: Cajamarca full of people — then, in one frame, only bodies; the gold room
        Return,   // 1:20 black; breathing; fog; Qenqo dry; hand still on the stone
        Title,    // 1:30 PACHACUTI. Cusco, 1532. He was sixteen.
    }

    public static class VisionTimeline
    {
        static readonly float[] Starts = { 0f, 2f, 5f, 10f, 20f, 35f, 50f, 65f, 80f, 90f };

        public const float ChannelRunSeconds = 3f;
        public const float SquareEmptiesAt = 72f;   // "then, in one frame, empty except for bodies"
        public const float GoldRoomAt = 74f;
        public const float HarquebusAt = 71.9f;     // "Silence, then a single harquebus crack" lands on the swap
        public const float StrobeMinHz = 0.6f;
        public const int SnapshotCount = 5;         // noon, dawn, snow, rain, night

        public static float Start(VisionShot shot) => Starts[(int)shot];

        public static float End(VisionShot shot) => (int)shot + 1 < Starts.Length ? Starts[(int)shot + 1] : float.PositiveInfinity;

        public static VisionShot ShotAt(float t)
        {
            for (int i = Starts.Length - 1; i >= 0; i--)
                if (t >= Starts[i]) return (VisionShot)i;
            return VisionShot.Touch;
        }

        public static float LocalTime(float t) => t - Start(ShotAt(t));

        /// <summary>
        /// Strobe frequency during the Pacha shot: rising exponentially from
        /// <see cref="StrobeMinHz"/> to the cap (12 Hz, or 3 Hz with reduced flash).
        /// </summary>
        public static float StrobeHz(float t, bool reducedFlash)
        {
            if (ShotAt(t) != VisionShot.Pacha) return 0f;
            float max = reducedFlash ? Tuning.StrobeReducedFlashMaxHz : Tuning.StrobeMaxHz;
            float u = LocalTime(t) / (End(VisionShot.Pacha) - Start(VisionShot.Pacha));
            return (float)(StrobeMinHz * Math.Pow(max / StrobeMinHz, u));
        }

        /// <summary>
        /// Which pre-lit snapshot (0..4) shows at time t in the Pacha shot. Integrates
        /// the frequency curve so swaps never exceed the cap.
        /// </summary>
        public static int StrobeSnapshot(float t, bool reducedFlash)
        {
            if (ShotAt(t) != VisionShot.Pacha) return -1;
            float max = reducedFlash ? Tuning.StrobeReducedFlashMaxHz : Tuning.StrobeMaxHz;
            double T = End(VisionShot.Pacha) - Start(VisionShot.Pacha);
            double r = max / StrobeMinHz;
            double s = LocalTime(t);
            double cycles = StrobeMinHz * T / Math.Log(r) * (Math.Pow(r, s / T) - 1.0);
            return (int)Math.Floor(cycles) % SnapshotCount;
        }
    }
}
