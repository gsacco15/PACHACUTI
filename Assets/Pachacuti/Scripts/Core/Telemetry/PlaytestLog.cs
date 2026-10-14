using System.Collections.Generic;

namespace Pachacuti.Core
{
    /// <summary>
    /// What the build logs automatically (pitch slice §15): time per beat; resets
    /// in the street; Tupa outcome; quipu tries per cord; time from roof tilt to
    /// first sky interaction; whether the player hovered the llama before the cord
    /// arrived; where they looked during the vision (camera yaw samples); total time.
    /// Serialized to the console (and the browser console on WebGL) at the end.
    /// </summary>
    public sealed class PlaytestLog
    {
        public const float VisionYawSampleHz = 4f;

        readonly List<double> _visionYaw = new List<double>();
        double _skyTiltAt = -1;

        public string SessionId { get; set; } = "";
        public string BuildVersion { get; set; } = "";
        public double SkyFirstInteractionDelay { get; private set; } = -1;
        public bool HoveredLlamaBeforeCord { get; private set; }
        public int SkyHovers { get; private set; }
        public IReadOnlyList<double> VisionYawSamples => _visionYaw;

        public void MarkSkyTilt(double clock) { if (_skyTiltAt < 0) _skyTiltAt = clock; }

        public void MarkSkyInteraction(double clock)
        {
            SkyHovers++;
            if (SkyFirstInteractionDelay < 0 && _skyTiltAt >= 0) SkyFirstInteractionDelay = clock - _skyTiltAt;
        }

        public void MarkLlamaHoveredBeforeCord() => HoveredLlamaBeforeCord = true;

        public void AddVisionYaw(double yawDegrees) => _visionYaw.Add(yawDegrees);

        public string ToJson(BeatMachine beats, WorldState world)
        {
            var w = new JsonWriter();
            w.BeginObject();
            w.Field("session", SessionId);
            w.Field("build", BuildVersion);
            w.Key("beatSeconds");
            w.BeginObject();
            foreach (var kv in beats.Durations) w.Field(kv.Key.ToString(), kv.Value);
            w.EndObject();
            w.Field("totalSeconds", beats.Clock);
            w.Field("streetResets", world.StreetResets);
            w.Field("tupaOutcome", world.Tupa.ToString());
            w.Key("quipuTriesPerCord"); w.Array(world.QuipuTries);
            w.Key("quipuSolvedByPlayer"); w.Array(world.QuipuSolvedByPlayer);
            w.Field("skyFirstInteractionSeconds", SkyFirstInteractionDelay);
            w.Field("hoveredLlamaBeforeCord", HoveredLlamaBeforeCord);
            w.Field("armyOrdered", world.OrderSent);
            w.Field("reducedFlash", world.ReducedFlash);
            w.Field("visionYawSampleHz", VisionYawSampleHz);
            w.Key("visionYawSamples"); w.Array(_visionYaw);
            w.Key("world");
            w.BeginObject();
            w.Field("cordsRead", world.CordsRead);
            w.Field("secondCordRead", world.SecondCordRead);
            w.Field("threadTaken", world.ThreadTaken);
            w.Field("visionSeen", world.VisionSeen);
            w.EndObject();
            w.EndObject();
            return w.ToString();
        }
    }
}
