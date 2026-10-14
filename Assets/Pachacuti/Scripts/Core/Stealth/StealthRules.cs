using System;

namespace Pachacuti.Core
{
    public enum NoiseKind
    {
        Run,
        Drop,
        LoadDrop,
        Scrape,   // sliding off adobe/plaster (§5 "slides off with an audible scrape")
        Strike,
        Shout,
    }

    /// <summary>
    /// Light, sound and sight rules shared by Beat 1 and Beat 2 (pitch slice §5).
    /// Pure functions; the runtime supplies distances and angles from the scene.
    /// </summary>
    public static class StealthRules
    {
        /// <summary>Inside any torch's 6 m radius Manco is lit (§4 "Lit = seen").</summary>
        public static bool IsLit(float distanceToNearestTorch) => distanceToNearestTorch <= Tuning.TorchLightRadius;

        /// <summary>How far a soldier can see Manco given his lighting: 12 m in fog if lit, else 3 m.</summary>
        public static float VisibleRange(bool lit) => lit ? Tuning.SightRangeInFog : Tuning.UnlitVisibleRange;

        /// <param name="horizontalAngleDeg">Angle between the soldier's facing and the direction to Manco, on the ground plane.</param>
        /// <param name="verticalAngleDeg">Elevation of Manco relative to the soldier's eye line.</param>
        public static bool InSightCone(float horizontalAngleDeg, float verticalAngleDeg)
        {
            return Math.Abs(horizontalAngleDeg) <= Tuning.SightConeDegrees * 0.5f
                && Math.Abs(verticalAngleDeg) <= Tuning.SightConeVerticalDegrees * 0.5f;
        }

        /// <summary>
        /// Full sight test. Manco right next to a soldier (within the unlit range)
        /// is noticed even outside the cone: you cannot stand behind someone's
        /// shoulder in a 4 m street unnoticed.
        /// </summary>
        public static bool CanSee(float distance, float horizontalAngleDeg, float verticalAngleDeg, bool lit, bool lineOfSightClear)
        {
            if (!lineOfSightClear) return false;
            if (distance > VisibleRange(lit)) return false;
            if (distance <= 1.2f) return true;
            return InSightCone(horizontalAngleDeg, verticalAngleDeg);
        }

        /// <summary>Noise radius in metres for an event, or 0 if silent.</summary>
        public static float NoiseRadius(NoiseKind kind, float dropHeight = 0f)
        {
            switch (kind)
            {
                case NoiseKind.Run: return Tuning.NoiseRunRadius;
                case NoiseKind.Drop: return DropNoiseRadius(dropHeight);
                case NoiseKind.LoadDrop: return Tuning.NoiseLoadDropRadius;
                case NoiseKind.Scrape: return 8f;
                case NoiseKind.Strike: return 4f;
                case NoiseKind.Shout: return Tuning.AlertConvergeRadius;
                default: return 0f;
            }
        }

        /// <summary>§5 "drop 8–20 m by height" for drops up to 4 m; louder above.</summary>
        public static float DropNoiseRadius(float height)
        {
            if (height < Tuning.NoiseDropSilentBelow) return 0f;
            float t = (height - Tuning.NoiseDropSilentBelow) / (Tuning.DropMaxHeight - Tuning.NoiseDropSilentBelow);
            if (t > 1f) t = 1f;
            return Tuning.NoiseDropMinRadius + (Tuning.NoiseDropMaxRadius - Tuning.NoiseDropMinRadius) * t;
        }

        public static bool Hears(float distanceToNoise, float noiseRadius) => noiseRadius > 0f && distanceToNoise <= noiseRadius;
    }
}
