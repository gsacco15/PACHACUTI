using System;
using NUnit.Framework;

namespace Pachacuti.Core.Tests
{
    public class KinsmanEncounterTests
    {
        static InteractInput Press() => new InteractInput { Pressed = true, Held = true, HeldSeconds = 0f };
        static InteractInput Hold(float t) => new InteractInput { Held = true, HeldSeconds = t };
        static InteractInput Release(float t) => new InteractInput { Released = true, HeldSeconds = t };

        [Test]
        public void StepsOutWithinTwoMetres()
        {
            var k = new KinsmanEncounter();
            Assert.That(k.Tick(0.1f, 2.5f, InteractInput.None), Is.EqualTo(KinsmanEvent.None));
            Assert.That(k.Tick(0.1f, 1.9f, InteractInput.None), Is.EqualTo(KinsmanEvent.SteppedOut));
            Assert.That(k.Phase, Is.EqualTo(KinsmanPhase.Window));
        }

        [Test]
        public void HoldingOneSecondStrikes()
        {
            var k = new KinsmanEncounter();
            k.Tick(0.1f, 1f, InteractInput.None);
            k.Tick(0.1f, 1f, Press());
            Assert.That(k.Tick(0.5f, 1f, Hold(0.5f)), Is.EqualTo(KinsmanEvent.None));
            Assert.That(k.Tick(0.5f, 1f, Hold(1.0f)), Is.EqualTo(KinsmanEvent.Strike));
            Assert.That(k.Outcome, Is.EqualTo(KinsmanOutcome.Killed));
        }

        [Test]
        public void TappingHides()
        {
            var k = new KinsmanEncounter();
            k.Tick(0.1f, 1f, InteractInput.None);
            k.Tick(0.1f, 1f, Press());
            Assert.That(k.Tick(0.1f, 1f, Release(0.15f)), Is.EqualTo(KinsmanEvent.Hide));
            Assert.That(k.Outcome, Is.EqualTo(KinsmanOutcome.Hidden));
        }

        [Test]
        public void DoingNothingForThreeSecondsLetsHimSayTheName()
        {
            var k = new KinsmanEncounter();
            k.Tick(0.1f, 1f, InteractInput.None);
            var ev = KinsmanEvent.None;
            for (int i = 0; i < 29; i++) ev = k.Tick(0.1f, 1f, InteractInput.None);
            Assert.That(ev, Is.EqualTo(KinsmanEvent.None));
            ev = k.Tick(0.11f, 1f, InteractInput.None);
            Assert.That(ev, Is.EqualTo(KinsmanEvent.NameSpoken));
            Assert.That(k.Outcome, Is.EqualTo(KinsmanOutcome.Fled));
        }

        [Test]
        public void APressBegunInTheWindowResolvesAfterIt()
        {
            var k = new KinsmanEncounter();
            k.Tick(0.1f, 1f, InteractInput.None);
            k.Tick(2.8f, 1f, InteractInput.None);
            k.Tick(0.1f, 1f, Press());
            Assert.That(k.Tick(0.4f, 1f, Hold(0.4f)), Is.EqualTo(KinsmanEvent.None), "window passed but the press is honoured");
            Assert.That(k.Tick(0.6f, 1f, Hold(1.0f)), Is.EqualTo(KinsmanEvent.Strike));
        }

        [Test]
        public void HeIsGoneOnTheRetryAfterFleeing()
        {
            var k = new KinsmanEncounter(fledOnEarlierAttempt: true);
            Assert.That(k.Phase, Is.EqualTo(KinsmanPhase.Absent));
            Assert.That(k.Tick(0.1f, 0.5f, InteractInput.None), Is.EqualTo(KinsmanEvent.None));
        }
    }

    public class BeatMachineTests
    {
        [Test]
        public void BeatsRunInSpecOrder()
        {
            var expected = new[] { Beat.ColdOpen, Beat.Rooftops, Beat.Compound, Beat.Street, Beat.Yard, Beat.Tunnel, Beat.Roof, Beat.Qenqo, Beat.Title, Beat.Ended };
            var m = new BeatMachine();
            foreach (var b in expected) m.Advance(b);
            Assert.That(m.Current, Is.EqualTo(Beat.Ended));
        }

        [Test]
        public void SkippingAheadIsIllegalButStreetCanRestart()
        {
            var m = new BeatMachine();
            m.Advance(Beat.ColdOpen);
            Assert.Throws<InvalidOperationException>(() => m.Advance(Beat.Street));
            m.Advance(Beat.Rooftops);
            m.Advance(Beat.Compound);
            m.Advance(Beat.Street);
            Assert.DoesNotThrow(() => m.Advance(Beat.Street));
            Assert.Throws<InvalidOperationException>(() => m.Advance(Beat.Compound));
        }

        [Test]
        public void DurationsAccumulateAcrossStreetRestarts()
        {
            var m = new BeatMachine();
            m.Advance(Beat.ColdOpen);
            m.Tick(30);
            m.Advance(Beat.Rooftops);
            m.Tick(150);
            m.Advance(Beat.Compound);
            m.Tick(60);
            m.Advance(Beat.Street);
            m.Tick(40);
            m.Advance(Beat.Street);
            m.Tick(100);
            m.Advance(Beat.Yard);
            Assert.That(m.Durations[Beat.ColdOpen], Is.EqualTo(30).Within(1e-9));
            Assert.That(m.Durations[Beat.Rooftops], Is.EqualTo(150).Within(1e-9));
            Assert.That(m.Durations[Beat.Street], Is.EqualTo(140).Within(1e-9));
            Assert.That(m.Clock, Is.EqualTo(380).Within(1e-9));
        }
    }

    public class TelemetryTests
    {
        [Test]
        public void JsonWriterProducesCompactValidJson()
        {
            var w = new JsonWriter();
            w.BeginObject();
            w.Field("a", 1);
            w.Field("b", "x\"y");
            w.Key("c"); w.Array(new[] { 1, 2 });
            w.Key("d"); w.BeginObject(); w.Field("e", true); w.EndObject();
            w.Field("f", 1.23456);
            w.EndObject();
            Assert.That(w.ToString(), Is.EqualTo("{\"a\":1,\"b\":\"x\\\"y\",\"c\":[1,2],\"d\":{\"e\":true},\"f\":1.235}"));
        }

        [Test]
        public void PlaytestLogHasEverySection15Field()
        {
            var beats = new BeatMachine();
            beats.Advance(Beat.ColdOpen);
            beats.Tick(12);
            beats.Advance(Beat.Rooftops);
            var world = new WorldState { Tupa = KinsmanOutcome.Hidden, StreetResets = 2 };
            world.QuipuTries[0] = 2;
            var log = new PlaytestLog { SessionId = "t" };
            log.MarkSkyTilt(100);
            log.MarkLlamaHoveredBeforeCord();
            log.MarkSkyInteraction(104.5);
            log.AddVisionYaw(12.5);
            string json = log.ToJson(beats, world);
            foreach (var key in new[] { "beatSeconds", "streetResets", "tupaOutcome", "quipuTriesPerCord", "skyFirstInteractionSeconds", "hoveredLlamaBeforeCord", "visionYawSamples", "totalSeconds" })
                StringAssert.Contains("\"" + key + "\"", json);
            StringAssert.Contains("\"tupaOutcome\":\"Hidden\"", json);
            StringAssert.Contains("\"skyFirstInteractionSeconds\":4.5", json);
            StringAssert.Contains("\"ColdOpen\":12", json);
        }

        [Test]
        public void WorldStateSerializes()
        {
            var w = new WorldState { Tupa = KinsmanOutcome.Killed, CordsRead = 3 };
            string json = w.ToJson();
            StringAssert.Contains("\"tupa\":\"Killed\"", json);
            StringAssert.Contains("\"cordsRead\":3", json);
            StringAssert.Contains("\"armyNode\":2", json);
        }

        [Test]
        public void EveryScriptLineHasTextAndAUniqueId()
        {
            var ids = new System.Collections.Generic.HashSet<string>();
            foreach (var line in SliceScript.All())
            {
                Assert.That(line.Text, Is.Not.Empty);
                Assert.That(ids.Add(line.Id), Is.True, "duplicate id " + line.Id);
                Assert.That(line.Duration, Is.GreaterThanOrEqualTo(1.6f));
            }
        }
    }
}
