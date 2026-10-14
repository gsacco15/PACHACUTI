namespace Pachacuti.Core
{
    /// <summary>
    /// The one plain object that drives everything the beats need to remember
    /// (pitch slice §13: state.ts). It is the miniature of the full game's
    /// "world state = the cord" idea (GDD §18), so it stays a flat, serializable
    /// record with no references to scene objects.
    /// </summary>
    public sealed class WorldState
    {
        public Beat Beat = Beat.Boot;

        // Beat 1–2
        public bool CarryingLoad;
        public int StreetResets;
        public KinsmanOutcome Tupa = KinsmanOutcome.None;

        // Beat 3
        /// <summary>Pendant cords read (0..3), whether by the player or told by Ninan after 3 tries.</summary>
        public int CordsRead;
        public readonly int[] QuipuTries = new int[3];
        public readonly bool[] QuipuSolvedByPlayer = new bool[3];
        public bool HasSecondCord;

        // Beat 4
        public bool SecondCordRead;
        public bool ArmyConfidenceSharp;
        public bool ThreadTaken;
        public int ArmyNode = SkyMapModel.ArmyStartNode;
        public bool OrderSent;

        // Beat 5
        public bool VisionSeen;
        public bool ReducedFlash;

        public bool TupaFled => Tupa == KinsmanOutcome.Fled;

        public string ToJson()
        {
            var w = new JsonWriter();
            w.BeginObject();
            w.Field("beat", Beat.ToString());
            w.Field("carryingLoad", CarryingLoad);
            w.Field("streetResets", StreetResets);
            w.Field("tupa", Tupa.ToString());
            w.Field("cordsRead", CordsRead);
            w.Key("quipuTries"); w.Array(QuipuTries);
            w.Key("quipuSolvedByPlayer"); w.Array(QuipuSolvedByPlayer);
            w.Field("hasSecondCord", HasSecondCord);
            w.Field("secondCordRead", SecondCordRead);
            w.Field("armyConfidenceSharp", ArmyConfidenceSharp);
            w.Field("threadTaken", ThreadTaken);
            w.Field("armyNode", ArmyNode);
            w.Field("orderSent", OrderSent);
            w.Field("visionSeen", VisionSeen);
            w.Field("reducedFlash", ReducedFlash);
            w.EndObject();
            return w.ToString();
        }
    }
}
