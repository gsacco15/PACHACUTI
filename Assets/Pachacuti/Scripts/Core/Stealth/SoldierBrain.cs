namespace Pachacuti.Core
{
    public enum SoldierState
    {
        Idle,         // stand, talk, scan, or walk a patrol route
        Glance,       // head turns 1 s, returns
        Stare,        // faces Manco 3 s
        Investigate,  // turned toward a noise, holding 4 s
        Approach,     // walks to Manco, torch raised
        Challenge,    // grabs the load; 2 s to react
        Alert,        // shouting / converging / chasing
        Scripted,     // driven by a scene timeline (e.g. the kinsman beat)
        Dead,
    }

    /// <summary>Beat 1 soldiers cannot reach the roofs and react to sight immediately; Beat 2 uses the §6 suspicion ladder.</summary>
    public enum SoldierRules { Rooftop, Street }

    /// <summary>What a soldier perceives this tick. Filled in by the runtime from the scene.</summary>
    public struct SoldierSenses
    {
        public float Dt;
        public bool SeesManco;
        public float DistanceToManco;
        public bool MancoStopped;
        /// <summary>Walking in the porter lane at the line's pace.</summary>
        public bool MancoWithLine;
        /// <summary>Carrying the load and walking normally (tumpline, head down).</summary>
        public bool MancoHeadDown;
        public bool MancoCarrying;
        public bool MancoLit;
        /// <summary>False on rooftops: soldiers cannot climb (§5 "capture in Beat 1 is impossible").</summary>
        public bool MancoReachable;
        public bool HeardNoise;
        /// <summary>Another soldier within 20 m shouted this tick.</summary>
        public bool AlertCalled;
        public bool SeesBody;
    }

    public struct SoldierTickResult
    {
        public bool Shout;
        public bool Capture;
        public bool InspectedLoad;
        public bool StateChanged;
    }

    /// <summary>
    /// Per-soldier suspicion state machine from pitch slice §6 ("Soldier AI states").
    /// Suspicion is local (each soldier has his own 0–100) and visible only as
    /// behaviour: glance → stare → step toward → shout.
    /// </summary>
    public sealed class SoldierBrain
    {
        float _glanceCooldown;
        float _stareCooldown;
        float _timeSinceSeen = 999f;
        float _chaseTime;
        float _reAlertCooldown;

        public SoldierBrain(SoldierRules rules) { Rules = rules; }

        public SoldierRules Rules { get; set; }
        public SoldierState State { get; private set; } = SoldierState.Idle;
        public float StateTime { get; private set; }
        public float Suspicion { get; private set; }
        public bool IsDead => State == SoldierState.Dead;
        public bool IsChasing => State == SoldierState.Alert;

        public void AddSuspicion(float amount) => Suspicion = Clamp(Suspicion + amount);
        public void SetSuspicion(float value) => Suspicion = Clamp(value);

        /// <summary>Hand control to a scene timeline. The brain ignores senses until Release().</summary>
        public void BeginScripted() => Enter(SoldierState.Scripted);
        public void Release() { if (State == SoldierState.Scripted) Enter(SoldierState.Idle); }
        public void Kill() => Enter(SoldierState.Dead);

        /// <summary>Back to post with a clean slate (street reset after capture).</summary>
        public void ResetToPost()
        {
            Suspicion = 0f;
            _glanceCooldown = _stareCooldown = 0f;
            _timeSinceSeen = 999f;
            _chaseTime = 0f;
            _reAlertCooldown = 0f;
            Enter(SoldierState.Idle);
        }

        public SoldierTickResult Tick(in SoldierSenses s)
        {
            var r = new SoldierTickResult();
            if (State == SoldierState.Dead || State == SoldierState.Scripted) return r;

            var startState = State;
            float dt = s.Dt;
            StateTime += dt;
            _glanceCooldown -= dt;
            _stareCooldown -= dt;
            _reAlertCooldown -= dt;
            _timeSinceSeen = s.SeesManco ? 0f : _timeSinceSeen + dt;

            if (State != SoldierState.Alert)
            {
                if (s.SeesBody) { EnterAlert(); r.Shout = true; }
                else if (s.AlertCalled) EnterAlert();
                else if (Rules == SoldierRules.Rooftop && s.SeesManco && _reAlertCooldown <= 0f) { EnterAlert(); r.Shout = true; }
            }

            if (State == SoldierState.Alert)
            {
                TickAlert(s, ref r);
                r.StateChanged = State != startState;
                return r;
            }

            // Noise pulls attention from low-attention states.
            if (s.HeardNoise && (State == SoldierState.Idle || State == SoldierState.Glance || State == SoldierState.Investigate))
                Enter(SoldierState.Investigate);

            if (Rules == SoldierRules.Street)
                TickStreet(s, ref r);
            else
                TickRooftop();

            // §6: "Suspicion decays 5/s when Manco walks with the line, head down, out of torchlight."
            // Applied after the state logic so a threshold crossed this tick still escalates.
            bool lowAttention = State == SoldierState.Idle || State == SoldierState.Glance || State == SoldierState.Investigate;
            if (lowAttention && s.MancoWithLine && s.MancoHeadDown && !s.MancoLit)
                Suspicion = Clamp(Suspicion - Tuning.SuspicionDecayPerSecond * dt);

            r.StateChanged = State != startState;
            return r;
        }

        void TickRooftop()
        {
            if (State == SoldierState.Investigate && StateTime >= Tuning.NoiseHoldSeconds) Enter(SoldierState.Idle);
        }

        void TickStreet(in SoldierSenses s, ref SoldierTickResult r)
        {
            // Threshold escalation (§6 Approach ≥ 50, Challenge ≥ 80).
            if (State != SoldierState.Challenge && State != SoldierState.Approach
                && Suspicion >= Tuning.ApproachThreshold && s.SeesManco)
                Enter(SoldierState.Approach);

            switch (State)
            {
                case SoldierState.Idle:
                case SoldierState.Investigate:
                    if (State == SoldierState.Investigate && StateTime >= Tuning.NoiseHoldSeconds) Enter(SoldierState.Idle);
                    TryStareOrGlance(s);
                    break;

                case SoldierState.Glance:
                    if (StateTime >= Tuning.GlanceSeconds)
                    {
                        Enter(SoldierState.Idle);
                        _glanceCooldown = Tuning.GlanceCooldown;
                    }
                    // A glance can turn into a stare if he stops or steps out of line.
                    if (State == SoldierState.Glance) TryStare(s);
                    break;

                case SoldierState.Stare:
                    if (StateTime >= Tuning.StareSeconds)
                    {
                        Enter(SoldierState.Idle);
                        _stareCooldown = Tuning.StareCooldown;
                    }
                    break;

                case SoldierState.Approach:
                    if (s.SeesManco && s.DistanceToManco <= Tuning.ApproachCloseRange)
                        Suspicion = Clamp(Suspicion + Tuning.ApproachSuspicionPerSecond * s.Dt);
                    if (Suspicion >= Tuning.ChallengeThreshold && s.DistanceToManco <= Tuning.ApproachCloseRange && s.MancoReachable)
                        Enter(SoldierState.Challenge);
                    else if (_timeSinceSeen >= Tuning.ApproachGiveUpSeconds)
                        Enter(SoldierState.Idle);
                    break;

                case SoldierState.Challenge:
                    if (s.DistanceToManco > Tuning.ApproachCloseRange + 1.5f)
                    {
                        // Ran from a challenge: shout.
                        EnterAlert();
                        r.Shout = true;
                    }
                    else if (StateTime >= Tuning.ChallengeReactSeconds)
                    {
                        if (s.MancoCarrying)
                        {
                            Suspicion = Tuning.ChallengePassSuspicion;
                            r.InspectedLoad = true;
                            Enter(SoldierState.Idle);
                            _stareCooldown = Tuning.StareCooldown;
                            _glanceCooldown = Tuning.GlanceCooldown;
                        }
                        else
                        {
                            r.Capture = true;
                            EnterAlert();
                        }
                    }
                    break;
            }
        }

        void TryStareOrGlance(in SoldierSenses s)
        {
            if (TryStare(s)) return;
            if (s.SeesManco && s.DistanceToManco <= Tuning.GlanceRange && _glanceCooldown <= 0f)
            {
                Enter(SoldierState.Glance);
                if (!s.MancoHeadDown) Suspicion = Clamp(Suspicion + Tuning.GlanceSuspicion);
            }
        }

        bool TryStare(in SoldierSenses s)
        {
            if (!s.SeesManco || _stareCooldown > 0f) return false;
            bool stoppedClose = s.MancoStopped && s.DistanceToManco <= Tuning.StareRange;
            bool brokeLine = !s.MancoWithLine && s.DistanceToManco <= Tuning.GlanceRange;
            if (!stoppedClose && !brokeLine) return false;
            Enter(SoldierState.Stare);
            Suspicion = Clamp(Suspicion + Tuning.StareSuspicion);
            return true;
        }

        void TickAlert(in SoldierSenses s, ref SoldierTickResult r)
        {
            _chaseTime += s.Dt;
            if (Rules == SoldierRules.Street && s.MancoReachable && s.SeesManco && s.DistanceToManco <= Tuning.CaptureTouchRange)
            {
                r.Capture = true;
                return;
            }
            bool lost = _timeSinceSeen >= Tuning.LoseSightSeconds;
            bool timedOut = Rules == SoldierRules.Rooftop && _chaseTime >= Tuning.ChaseSeconds;
            if (lost || timedOut)
            {
                Suspicion = Rules == SoldierRules.Street ? Tuning.ApproachThreshold - 1f : 0f;
                Enter(SoldierState.Idle);
                _glanceCooldown = Tuning.GlanceCooldown;
                // A rooftop chase that times out gives up for a while even if Manco is still
                // in view; otherwise a soldier who cannot reach him would chase forever.
                if (Rules == SoldierRules.Rooftop) _reAlertCooldown = Tuning.LoseSightSeconds;
            }
        }

        void EnterAlert()
        {
            Suspicion = 100f;
            _chaseTime = 0f;
            _timeSinceSeen = 0f; // a shout tells everyone where he is; the 5 s line-of-sight clock starts now
            Enter(SoldierState.Alert);
        }

        void Enter(SoldierState s)
        {
            State = s;
            StateTime = 0f;
        }

        static float Clamp(float v) => v < 0f ? 0f : (v > 100f ? 100f : v);
    }
}
