namespace Pachacuti.Core
{
    public enum KinsmanOutcome
    {
        None,
        Killed,  // strike: hold interact 1 s
        Hidden,  // hide: tap interact
        Fled,    // did nothing for 3 s: he said the name, alert, and he is gone on the retry
    }

    public enum KinsmanPhase
    {
        Waiting,   // pressed into his doorway
        Window,    // stepped out, "Manc—", 3 s to act
        Resolved,
        Absent,    // fled on an earlier attempt
    }

    public enum KinsmanEvent
    {
        None,
        SteppedOut,
        Strike,
        Hide,
        NameSpoken,
    }

    /// <summary>One frame of the interact key (E / X / Square).</summary>
    public struct InteractInput
    {
        public bool Pressed;      // went down this frame
        public bool Held;         // is down
        public bool Released;     // went up this frame
        public float HeldSeconds; // how long the current (or just-released) press lasted

        public static InteractInput None => default;
    }

    /// <summary>
    /// The kinsman beat (pitch slice §6). One context key: the player's instinct
    /// decides Tupa's fate. Holding for 1 s strikes; releasing earlier hides;
    /// doing nothing for 3 s lets him say the name.
    /// </summary>
    public sealed class KinsmanEncounter
    {
        bool _pressing;

        public KinsmanEncounter(bool fledOnEarlierAttempt = false)
        {
            Phase = fledOnEarlierAttempt ? KinsmanPhase.Absent : KinsmanPhase.Waiting;
        }

        public KinsmanPhase Phase { get; private set; }
        public KinsmanOutcome Outcome { get; private set; }
        public float WindowElapsed { get; private set; }
        public float WindowRemaining => Phase == KinsmanPhase.Window ? Tuning.KinsmanWindowSeconds - WindowElapsed : 0f;
        /// <summary>0..1 progress of a strike hold, for the runtime to animate the wind-up.</summary>
        public float StrikeWindUp { get; private set; }

        public KinsmanEvent Tick(float dt, float distanceToManco, in InteractInput input)
        {
            switch (Phase)
            {
                case KinsmanPhase.Waiting:
                    if (distanceToManco <= Tuning.KinsmanTriggerRange)
                    {
                        Phase = KinsmanPhase.Window;
                        WindowElapsed = 0f;
                        return KinsmanEvent.SteppedOut;
                    }
                    return KinsmanEvent.None;

                case KinsmanPhase.Window:
                    WindowElapsed += dt;
                    if (input.Pressed) _pressing = true;
                    if (_pressing)
                    {
                        StrikeWindUp = input.HeldSeconds / Tuning.KinsmanStrikeHoldSeconds;
                        if (StrikeWindUp > 1f) StrikeWindUp = 1f;
                        if (input.HeldSeconds >= Tuning.KinsmanStrikeHoldSeconds && (input.Held || input.Released))
                            return Resolve(KinsmanOutcome.Killed, KinsmanEvent.Strike);
                        if (input.Released)
                            return Resolve(KinsmanOutcome.Hidden, KinsmanEvent.Hide);
                        return KinsmanEvent.None; // a press begun inside the window resolves even if it runs past it
                    }
                    if (WindowElapsed >= Tuning.KinsmanWindowSeconds)
                        return Resolve(KinsmanOutcome.Fled, KinsmanEvent.NameSpoken);
                    return KinsmanEvent.None;

                default:
                    return KinsmanEvent.None;
            }
        }

        KinsmanEvent Resolve(KinsmanOutcome outcome, KinsmanEvent ev)
        {
            Outcome = outcome;
            Phase = KinsmanPhase.Resolved;
            _pressing = false;
            return ev;
        }
    }
}
