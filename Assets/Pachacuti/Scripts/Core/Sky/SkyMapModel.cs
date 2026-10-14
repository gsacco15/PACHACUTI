using System.Collections.Generic;

namespace Pachacuti.Core
{
    public enum SkyTarget
    {
        None,
        Cusco,      // tight bright cluster at the zenith, over the Coricancha
        RoadStar,   // one of the five faint stars of the north road
        Tambo,      // slightly brighter star at the road's end
        Llama,      // the army: Huáscar's last loyal levy, 1,200 men
        ExitLine,   // the gold line out of the sky's edge toward the north-east (Qenqo)
    }

    public enum SkyStep
    {
        Tilting,      // camera tilts to the zenith over 3 s
        Looking,      // free to look; the cord arrives after 10 s
        CordGlowing,  // the second cord glows in Manco's hand
        CordRead,     // the smudge has sharpened; the army can be ordered
        ArmySelected,
        Moving,       // the llama slides one node toward the tambo over 4 s
        Consequence,  // tambo brightens; Tupa's star lights, or a star goes out
        ExitOffered,  // the north-east gold line pulses
        Done,
    }

    [System.Flags]
    public enum SkyEvent
    {
        None = 0,
        TiltFinished = 1 << 0,
        CordGlows = 1 << 1,
        CordRead = 1 << 2,         // smudge sharpens into a star; "?" fades
        ThreadTaken = 1 << 3,      // gold line now runs from the roof under Manco's feet
        ArmySelected = 1 << 4,
        OrderGiven = 1 << 5,       // gold path lights along the road; distant pututu
        ArmyArrived = 1 << 6,
        TamboBrightens = 1 << 7,
        TupaStarLit = 1 << 8,
        CuscoStarOut = 1 << 9,
        ExitOffered = 1 << 10,
        Exit = 1 << 11,
        FirstInteraction = 1 << 12,
        LlamaHoveredBeforeCord = 1 << 13,
    }

    /// <summary>
    /// Beat 4's sky-map (pitch slice §8): the sky is the empire, a blurred star is
    /// uncertainty, a line of light is an order. Scripted in four steps, but the
    /// player drives every step. Nothing is labelled with text; hovering plays a
    /// short line from Ninan naming the place (queued in <see cref="Lines"/>).
    /// </summary>
    public sealed class SkyMapModel
    {
        public const int CuscoNode = 0;
        public const int FaintRoadStars = 5;
        public const int TamboNode = FaintRoadStars + 1;   // 6
        public const int ArmyStartNode = 2;                // "sitting on the road's second node"
        /// <summary>ASSUMPTION: "the sky will read it before you can" — if the player never presses interact, the cord reads itself.</summary>
        public const float CordAutoReadSeconds = 20f;
        const float ConsequenceTupaDelay = 2.5f;
        const float ConsequenceExitDelay = 5f;

        readonly KinsmanOutcome _tupa;
        readonly float[] _hoverCooldown = new float[(int)SkyTarget.ExitLine + 1];
        readonly Queue<ScriptLine> _lines = new Queue<ScriptLine>();
        bool _anyInteraction;
        bool _tupaConsequenceDone;

        public SkyMapModel(KinsmanOutcome tupaOutcome)
        {
            _tupa = tupaOutcome;
        }

        public SkyStep Step { get; private set; } = SkyStep.Tilting;
        public float StepTime { get; private set; }
        public int ArmyNode { get; private set; } = ArmyStartNode;
        /// <summary>0..1 along the edge from ArmyNode toward the tambo while moving.</summary>
        public float ArmyProgress { get; private set; }
        public bool ArmySharp { get; private set; }
        public bool ThreadTaken { get; private set; }
        public bool OrderGiven { get; private set; }
        public bool TamboLit { get; private set; }
        public bool TupaStarLit { get; private set; }
        public bool CuscoStarOut { get; private set; }
        public Queue<ScriptLine> Lines => _lines;

        public SkyEvent Tick(float dt)
        {
            var ev = SkyEvent.None;
            StepTime += dt;
            for (int i = 0; i < _hoverCooldown.Length; i++) _hoverCooldown[i] -= dt;

            switch (Step)
            {
                case SkyStep.Tilting:
                    if (StepTime >= Tuning.SkyTiltSeconds) { Go(SkyStep.Looking); ev |= SkyEvent.TiltFinished; }
                    break;
                case SkyStep.Looking:
                    if (StepTime >= Tuning.SkyCordArrivesAfterSeconds) { Go(SkyStep.CordGlowing); ev |= SkyEvent.CordGlows; }
                    break;
                case SkyStep.CordGlowing:
                    if (StepTime >= CordAutoReadSeconds) ev |= ReadCord();
                    break;
                case SkyStep.Moving:
                    ArmyProgress = StepTime / Tuning.SkyArmyMoveSeconds;
                    if (ArmyProgress >= 1f)
                    {
                        ArmyProgress = 0f;
                        ArmyNode++;
                        TamboLit = true;
                        _lines.Enqueue(SliceScript.NinanTheyWillEat);
                        Go(SkyStep.Consequence);
                        ev |= SkyEvent.ArmyArrived | SkyEvent.TamboBrightens;
                    }
                    break;
                case SkyStep.Consequence:
                    if (!_tupaConsequenceDone && StepTime >= ConsequenceTupaDelay)
                    {
                        _tupaConsequenceDone = true;
                        if (_tupa == KinsmanOutcome.Hidden)
                        {
                            TupaStarLit = true;
                            _lines.Enqueue(SliceScript.NinanTupa);
                            ev |= SkyEvent.TupaStarLit;
                        }
                        else if (_tupa == KinsmanOutcome.Killed)
                        {
                            CuscoStarOut = true;
                            ev |= SkyEvent.CuscoStarOut;
                        }
                    }
                    if (StepTime >= ConsequenceExitDelay) { Go(SkyStep.ExitOffered); ev |= SkyEvent.ExitOffered; }
                    break;
            }
            return ev;
        }

        /// <summary>Interact while the cord glows: it "reads" and the smudge sharpens.</summary>
        public SkyEvent Interact()
        {
            return Step == SkyStep.CordGlowing ? ReadCord() | Touch() : SkyEvent.None;
        }

        public SkyEvent Hover(SkyTarget target)
        {
            if (target == SkyTarget.None || Step == SkyStep.Tilting) return SkyEvent.None;
            var ev = Touch();
            if (target == SkyTarget.Llama && !ArmySharp) ev |= SkyEvent.LlamaHoveredBeforeCord;
            if (target == SkyTarget.Llama && ArmySharp && !ThreadTaken)
            {
                ThreadTaken = true;
                ev |= SkyEvent.ThreadTaken;
            }
            if (CanSpeak(target))
            {
                var line = HoverLine(target);
                if (line.HasValue) _lines.Enqueue(line.Value);
            }
            return ev;
        }

        public SkyEvent Select(SkyTarget target)
        {
            if (Step == SkyStep.Tilting) return SkyEvent.None;
            var ev = Touch();
            switch (target)
            {
                case SkyTarget.Llama when ArmySharp && (Step == SkyStep.CordRead || Step == SkyStep.ArmySelected):
                    if (!ThreadTaken) { ThreadTaken = true; ev |= SkyEvent.ThreadTaken; }
                    Go(SkyStep.ArmySelected);
                    ev |= SkyEvent.ArmySelected;
                    break;
                case SkyTarget.Tambo when Step == SkyStep.ArmySelected:
                    OrderGiven = true;
                    Go(SkyStep.Moving);
                    ev |= SkyEvent.OrderGiven;
                    break;
                case SkyTarget.ExitLine when Step == SkyStep.ExitOffered:
                    Go(SkyStep.Done);
                    ev |= SkyEvent.Exit;
                    break;
            }
            return ev;
        }

        SkyEvent ReadCord()
        {
            ArmySharp = true;
            Go(SkyStep.CordRead);
            _lines.Enqueue(SliceScript.NinanCordReads);
            return SkyEvent.CordRead;
        }

        SkyEvent Touch()
        {
            if (_anyInteraction) return SkyEvent.None;
            _anyInteraction = true;
            return SkyEvent.FirstInteraction;
        }

        bool CanSpeak(SkyTarget t)
        {
            if (_hoverCooldown[(int)t] > 0f) return false;
            _hoverCooldown[(int)t] = Tuning.SkyHoverLineCooldown;
            return true;
        }

        static ScriptLine? HoverLine(SkyTarget t)
        {
            switch (t)
            {
                case SkyTarget.RoadStar: return SliceScript.NinanNorthRoad;
                case SkyTarget.Tambo: return SliceScript.NinanTambo;
                case SkyTarget.Llama: return SliceScript.NinanTwelveHundred;
                default: return null;
            }
        }

        void Go(SkyStep s)
        {
            Step = s;
            StepTime = 0f;
        }
    }
}
