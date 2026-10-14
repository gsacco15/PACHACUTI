namespace Pachacuti.Core
{
    /// <summary>
    /// Every tuning number from the Pitch Slice Design Document, in one place,
    /// with the section it comes from. Values marked ASSUMPTION are not in the
    /// doc and were chosen to make the spec'd numbers work together; they are
    /// listed in docs/SLICE_IMPLEMENTATION_NOTES.md for the designer to confirm.
    /// </summary>
    public static class Tuning
    {
        // ---------------------------------------------------------------- §5 Movement
        public const float CrouchWalkSpeed = 1.6f;
        public const float WalkSpeed = 2.4f;
        public const float RunSpeed = 5.0f;
        public const float ClimbSpeed = 1.2f;            // vertical m/s
        public const float ClimbLateralSpeed = 0.7f;     // ASSUMPTION: sideways shuffle on a wall
        public const float JumpMaxGap = 2.5f;
        public const float JumpMaxRise = 1.0f;
        public const float DropMaxHeight = 4.0f;
        public const float CrawlSpeed = 0.8f;            // ASSUMPTION: tunnel low crawl (§4 zone E)
        public const float LoadDropSeconds = 0.5f;       // §6 "Dropping the load takes 0.5 s and is loud"

        // ---------------------------------------------------------------- §5 Breath
        public const float BreathMax = 100f;
        public const float BreathRunDrain = 8f;          // per second
        public const float BreathClimbDrain = 10f;       // per second, moving or holding on a wall
        public const float BreathIdleRecover = 15f;      // per second (also ledge rest)
        public const float BreathCrouchWalkRecover = 8f; // per second
        public const float BreathWalkRecover = 8f;       // ASSUMPTION: porter walk recovers like crouch-walk
        public const float BreathAudibleBelow = 30f;     // breathing audible and fast; 0.5 Hz camera sway
        public const float BreathShakeBelow = 10f;       // climb speed halves; hands shake
        public const float BreathGaspSeconds = 2f;       // at 0: stops and gasps, cannot move
        public const float BreathSwayHz = 0.5f;
        public const float BreathDropFullCostHeight = 4f; // "4 m+ costs a full breath" (meter to 0)

        /// <summary>
        /// §5: the 6 m climb in Zone A "is tuned to need ~75 breath". At the spec'd
        /// 1.2 m/s and 10/s drain a straight 6 m climb costs 50, so the Zone A wall
        /// carries a grip-difficulty multiplier: 6 / 1.2 * 10 * 1.5 = 75.
        /// </summary>
        public const float ZoneAWallDrainMultiplier = 1.5f;

        // ---------------------------------------------------------------- §5 Stealth (shared Beat 1/2)
        public const float TorchLightRadius = 6f;        // inside: Manco visible
        public const float UnlitVisibleRange = 3f;       // outside torchlight: only within 3 m
        public const float SightConeDegrees = 70f;       // horizontal, full angle
        public const float SightConeVerticalDegrees = 70f; // ASSUMPTION: soldiers do not look straight up
        public const float SightRangeInFog = 12f;
        public const float NoiseRunRadius = 15f;
        public const float NoiseDropMinRadius = 8f;
        public const float NoiseDropMaxRadius = 20f;
        public const float NoiseDropSilentBelow = 0.75f; // ASSUMPTION: stepping down a curb is silent
        public const float NoiseLoadDropRadius = 10f;    // ASSUMPTION: "is loud"
        public const float NoiseHoldSeconds = 4f;        // soldier turns toward a noise and holds
        public const float ChaseSeconds = 10f;           // Beat 1: shout + 10 s chase
        public const float LoseSightSeconds = 5f;        // break line of sight 5 s to end a chase

        // ---------------------------------------------------------------- §6 Suspicion
        public const float GlanceRange = 8f;
        public const float GlanceSeconds = 1f;
        public const float GlanceSuspicion = 10f;        // if Manco is not walking with head down
        public const float GlanceCooldown = 3f;          // ASSUMPTION
        public const float StareRange = 6f;
        public const float StareSeconds = 3f;
        public const float StareSuspicion = 25f;
        public const float StareCooldown = 2f;           // ASSUMPTION
        public const float ApproachThreshold = 50f;
        public const float ApproachCloseRange = 3f;
        public const float ApproachSuspicionPerSecond = 15f;
        public const float ApproachGiveUpSeconds = 4f;   // ASSUMPTION: lost sight while approaching
        public const float ChallengeThreshold = 80f;
        public const float ChallengeReactSeconds = 2f;
        public const float ChallengePassSuspicion = 40f; // "load inspected, suspicion resets to 40"
        public const float SuspicionDecayPerSecond = 5f; // walking with the line, head down, unlit
        public const float AlertConvergeRadius = 20f;
        public const float CaptureTouchRange = 1.0f;     // ASSUMPTION: "capture on touch"
        public const float SoldierWalkSpeed = 1.5f;      // ASSUMPTION
        public const float SoldierChaseSpeed = 5.4f;     // ASSUMPTION: slightly faster than Manco's run
        public const float SoldierKillLoadKickSeconds = 4f; // §6 hide branch: "he kicks it, 4 s"

        // ---------------------------------------------------------------- §6 The kinsman
        public const float KinsmanTriggerRange = 2f;
        public const float KinsmanWindowSeconds = 3f;
        public const float KinsmanStrikeHoldSeconds = 1f;
        public const float KinsmanTorchSoldierDistance = 8f;
        public const float HideBranchSuspicionAfterPickup = 60f;

        // ---------------------------------------------------------------- §6 Porter line
        public const int PorterCount = 6;
        public const float PorterLineSpeed = 1.2f;       // ASSUMPTION: the line sets Manco's pace
        public const float PorterSpacing = 2.6f;         // ASSUMPTION
        public const float PorterLaneHalfWidth = 1.0f;   // ASSUMPTION: "with the line" lateral tolerance

        // ---------------------------------------------------------------- §7 Quipu
        public const int QuipuMaxTries = 3;

        // ---------------------------------------------------------------- §8 Sky-map
        public const float SkyTiltSeconds = 3f;
        public const float SkyCordArrivesAfterSeconds = 10f;
        public const float SkyCameraPanLimitDegrees = 40f;
        public const float SkyArmyMoveSeconds = 4f;
        public const float SkyHoverLineCooldown = 6f;    // ASSUMPTION: hover lines do not spam
        public const float SkyLookUpPitchDegrees = 35f;  // ASSUMPTION: "look up" prompt satisfied

        // ---------------------------------------------------------------- §9 Vision
        public const float VisionSeconds = 90f;
        public const float QenqoWalkSeconds = 30f;
        public const float VisionFreeLookDegrees = 30f;  // §10 "rail with free look (±30°)"
        public const float StrobeMaxHz = 12f;            // photosensitivity cap
        public const float StrobeReducedFlashMaxHz = 3f; // ASSUMPTION: reduced-flash option
        public const int CequeLineCount = 41;
        public const float CequeLineLength = 2000f;

        // ---------------------------------------------------------------- §10 Camera
        public const float CameraCloseDistance = 1.2f;
        public const float CameraCloseShoulderHeight = 0.3f;
        public const float CameraMediumDistance = 2.5f;
        public const float CameraWideDistance = 4.5f;    // ASSUMPTION: "wide on the Coricancha roof"

        // ---------------------------------------------------------------- §4 World
        public const float FogDrawDistance = 60f;

        // ---------------------------------------------------------------- §13 Tech
        public const int RenderMaxHeight = 1080;         // §5/§13 "60 fps on an integrated GPU at 1080p"
    }
}
