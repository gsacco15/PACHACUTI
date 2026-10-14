using System;

namespace Pachacuti.Core
{
    /// <summary>What Manco's body is doing this frame, as far as breath is concerned.</summary>
    public enum Exertion
    {
        Idle,
        CrouchWalk,
        Walk,
        Run,
        /// <summary>On a wall, moving or holding. Both drain (§5: "a held climb will drop him at 0").</summary>
        Climb,
        /// <summary>Standing or hanging on an authored ledge: recovers like idle.</summary>
        LedgeRest,
        /// <summary>Scripted moments (cutscenes, quipu screen): recovers like idle.</summary>
        Scripted,
    }

    public enum BreathBand
    {
        Calm,      // >= 30
        Audible,   // < 30: fast audible breathing, 0.5 Hz camera sway
        Shaking,   // < 10: climb speed halves, hands shake
        Empty,     // 0: gasp for 2 s, cannot move; on a wall, falls to the last ledge
    }

    [Flags]
    public enum BreathEvent
    {
        None = 0,
        BecameAudible = 1 << 0,
        BecameShaking = 1 << 1,
        Exhausted = 1 << 2,   // hit 0 this tick: gasp starts; climbing code drops to the last ledge
        Recovered = 1 << 3,   // gasp finished
        BecameCalm = 1 << 4,
    }

    /// <summary>
    /// The hidden altitude-breath meter (pitch slice §5). Shown to the player only
    /// through audio, posture and camera sway; never as a bar.
    /// </summary>
    public sealed class BreathModel
    {
        float _gasp;

        public float Value { get; private set; } = Tuning.BreathMax;
        public bool IsGasping => _gasp > 0f;
        public float GaspRemaining => _gasp;
        public bool CanMove => !IsGasping;

        public BreathBand Band => BandFor(Value, IsGasping);

        /// <summary>1 normally, 0.5 below the shake threshold (§5 "At 10: climb speed halves").</summary>
        public float ClimbSpeedMultiplier => Value < Tuning.BreathShakeBelow ? 0.5f : 1f;

        /// <summary>0..1 strength of the audible-breathing / camera-sway effects.</summary>
        public float Strain => Value >= Tuning.BreathAudibleBelow ? 0f : 1f - Value / Tuning.BreathAudibleBelow;

        public static BreathBand BandFor(float value, bool gasping)
        {
            if (gasping || value <= 0f) return BreathBand.Empty;
            if (value < Tuning.BreathShakeBelow) return BreathBand.Shaking;
            if (value < Tuning.BreathAudibleBelow) return BreathBand.Audible;
            return BreathBand.Calm;
        }

        public static float RatePerSecond(Exertion exertion, float drainMultiplier = 1f)
        {
            switch (exertion)
            {
                case Exertion.Run: return -Tuning.BreathRunDrain;
                case Exertion.Climb: return -Tuning.BreathClimbDrain * drainMultiplier;
                case Exertion.CrouchWalk: return Tuning.BreathCrouchWalkRecover;
                case Exertion.Walk: return Tuning.BreathWalkRecover;
                case Exertion.Idle:
                case Exertion.LedgeRest:
                case Exertion.Scripted:
                default: return Tuning.BreathIdleRecover;
            }
        }

        /// <param name="drainMultiplier">Per-surface grip difficulty for climbing (see Tuning.ZoneAWallDrainMultiplier).</param>
        public BreathEvent Tick(Exertion exertion, float dt, float drainMultiplier = 1f)
        {
            if (dt <= 0f) return BreathEvent.None;
            var before = Band;

            if (IsGasping)
            {
                // Gasping recovers at the idle rate whatever the input says.
                _gasp = Math.Max(0f, _gasp - dt);
                Value = Clamp(Value + Tuning.BreathIdleRecover * dt);
                var ev = BreathEvent.None;
                if (!IsGasping) ev |= BreathEvent.Recovered;
                return ev | Transition(before, Band);
            }

            Value = Clamp(Value + RatePerSecond(exertion, drainMultiplier) * dt);
            if (Value <= 0f)
            {
                Value = 0f;
                _gasp = Tuning.BreathGaspSeconds;
                return BreathEvent.Exhausted | Transition(before, BreathBand.Empty);
            }
            return Transition(before, Band);
        }

        /// <summary>Instant cost, e.g. a 4 m+ drop (§5 "4 m+ costs a full breath").</summary>
        public BreathEvent Spend(float amount)
        {
            if (amount <= 0f || IsGasping) return BreathEvent.None;
            var before = Band;
            Value = Clamp(Value - amount);
            if (Value <= 0f)
            {
                _gasp = Tuning.BreathGaspSeconds;
                return BreathEvent.Exhausted | Transition(before, BreathBand.Empty);
            }
            return Transition(before, Band);
        }

        /// <summary>Breath cost of landing from a fall or drop of the given height.</summary>
        public static float DropCost(float height) => height >= Tuning.BreathDropFullCostHeight ? Tuning.BreathMax : 0f;

        public void Reset(float value = Tuning.BreathMax)
        {
            Value = Clamp(value);
            _gasp = 0f;
        }

        static float Clamp(float v) => v < 0f ? 0f : (v > Tuning.BreathMax ? Tuning.BreathMax : v);

        static BreathEvent Transition(BreathBand from, BreathBand to)
        {
            if (from == to) return BreathEvent.None;
            switch (to)
            {
                case BreathBand.Audible: return from == BreathBand.Calm ? BreathEvent.BecameAudible : BreathEvent.None;
                case BreathBand.Shaking: return from == BreathBand.Empty ? BreathEvent.None : BreathEvent.BecameShaking;
                case BreathBand.Calm: return BreathEvent.BecameCalm;
                default: return BreathEvent.None;
            }
        }
    }
}
