using NUnit.Framework;

namespace Pachacuti.Core.Tests
{
    public class BreathModelTests
    {
        static void Run(BreathModel b, Exertion e, float seconds, float mult = 1f)
        {
            for (int i = 0; i < (int)(seconds * 100); i++) b.Tick(e, 0.01f, mult);
        }

        [Test]
        public void StartsFullAndCalm()
        {
            var b = new BreathModel();
            Assert.That(b.Value, Is.EqualTo(100f));
            Assert.That(b.Band, Is.EqualTo(BreathBand.Calm));
            Assert.That(b.CanMove, Is.True);
        }

        [Test]
        public void RatesMatchSection5()
        {
            Assert.That(BreathModel.RatePerSecond(Exertion.Run), Is.EqualTo(-8f));
            Assert.That(BreathModel.RatePerSecond(Exertion.Climb), Is.EqualTo(-10f));
            Assert.That(BreathModel.RatePerSecond(Exertion.Idle), Is.EqualTo(15f));
            Assert.That(BreathModel.RatePerSecond(Exertion.CrouchWalk), Is.EqualTo(8f));
            Assert.That(BreathModel.RatePerSecond(Exertion.LedgeRest), Is.EqualTo(15f));
        }

        [Test]
        public void RunningDrainsAndIdleRecoversToCap()
        {
            var b = new BreathModel();
            Run(b, Exertion.Run, 2f);
            Assert.That(b.Value, Is.EqualTo(84f).Within(0.01f));
            Run(b, Exertion.Idle, 5f);
            Assert.That(b.Value, Is.EqualTo(100f));
        }

        [Test]
        public void ZoneAWallCostsAbout75Breath()
        {
            // 6 m at 1.2 m/s with the Zone A grip multiplier (§5 "tuned to need ~75 breath").
            var b = new BreathModel();
            float climbSeconds = 6f / Tuning.ClimbSpeed;
            Run(b, Exertion.Climb, climbSeconds, Tuning.ZoneAWallDrainMultiplier);
            Assert.That(100f - b.Value, Is.EqualTo(75f).Within(0.5f));
        }

        [Test]
        public void RunningAcrossTheRoofFirstForcesALedgeStop()
        {
            // §5: "a player who runs across the roof first will have to stop halfway on a ledge".
            var b = new BreathModel();
            Run(b, Exertion.Run, 4f);
            float height = 0f;
            bool exhausted = false;
            for (int i = 0; i < 1000 && !exhausted; i++)
            {
                height += Tuning.ClimbSpeed * b.ClimbSpeedMultiplier * 0.01f;
                exhausted = (b.Tick(Exertion.Climb, 0.01f, Tuning.ZoneAWallDrainMultiplier) & BreathEvent.Exhausted) != 0;
            }
            Assert.That(exhausted, Is.True);
            Assert.That(height, Is.LessThan(6f), "should not reach the top of the 6 m wall in one breath");
            Assert.That(height, Is.GreaterThan(3f), "should get past the mid-wall ledge");
        }

        [Test]
        public void ThresholdEventsFireOnceInOrder()
        {
            var b = new BreathModel();
            var seen = BreathEvent.None;
            for (int i = 0; i < 2000; i++)
            {
                var ev = b.Tick(Exertion.Climb, 0.01f);
                seen |= ev;
                if ((ev & BreathEvent.Exhausted) != 0) break;
            }
            Assert.That(seen & BreathEvent.BecameAudible, Is.EqualTo(BreathEvent.BecameAudible));
            Assert.That(seen & BreathEvent.BecameShaking, Is.EqualTo(BreathEvent.BecameShaking));
            Assert.That(seen & BreathEvent.Exhausted, Is.EqualTo(BreathEvent.Exhausted));
            Assert.That(b.IsGasping, Is.True);
            Assert.That(b.CanMove, Is.False);
        }

        [Test]
        public void GaspLastsTwoSecondsThenRecovers()
        {
            var b = new BreathModel();
            b.Spend(100f);
            Assert.That(b.IsGasping, Is.True);
            var ev = BreathEvent.None;
            for (int i = 0; i < 199; i++) ev |= b.Tick(Exertion.Run, 0.01f); // input ignored while gasping
            Assert.That(b.IsGasping, Is.True);
            ev |= b.Tick(Exertion.Run, 0.02f);
            Assert.That(b.IsGasping, Is.False);
            Assert.That(ev & BreathEvent.Recovered, Is.EqualTo(BreathEvent.Recovered));
            Assert.That(b.Value, Is.EqualTo(30f).Within(0.5f), "recovers at the idle rate while gasping");
        }

        [Test]
        public void ClimbSpeedHalvesBelowTen()
        {
            var b = new BreathModel();
            b.Reset(10.5f);
            Assert.That(b.ClimbSpeedMultiplier, Is.EqualTo(1f));
            b.Reset(9.5f);
            Assert.That(b.ClimbSpeedMultiplier, Is.EqualTo(0.5f));
        }

        [Test]
        public void FourMetreDropCostsAFullBreath()
        {
            Assert.That(BreathModel.DropCost(3.9f), Is.EqualTo(0f));
            Assert.That(BreathModel.DropCost(4.0f), Is.EqualTo(100f));
        }
    }

    public class StealthRulesTests
    {
        [Test]
        public void TorchlightRadiusIsSixMetres()
        {
            Assert.That(StealthRules.IsLit(5.9f), Is.True);
            Assert.That(StealthRules.IsLit(6.1f), Is.False);
        }

        [Test]
        public void UnlitMancoIsOnlySeenWithinThreeMetres()
        {
            Assert.That(StealthRules.CanSee(2.5f, 0f, 0f, lit: false, lineOfSightClear: true), Is.True);
            Assert.That(StealthRules.CanSee(4f, 0f, 0f, lit: false, lineOfSightClear: true), Is.False);
            Assert.That(StealthRules.CanSee(11f, 0f, 0f, lit: true, lineOfSightClear: true), Is.True);
            Assert.That(StealthRules.CanSee(13f, 0f, 0f, lit: true, lineOfSightClear: true), Is.False);
        }

        [Test]
        public void SightConeIsSeventyDegrees()
        {
            Assert.That(StealthRules.CanSee(8f, 34f, 0f, true, true), Is.True);
            Assert.That(StealthRules.CanSee(8f, 36f, 0f, true, true), Is.False);
            Assert.That(StealthRules.CanSee(8f, 0f, 0f, true, false), Is.False, "walls block sight");
        }

        [Test]
        public void DropNoiseScalesFromEightToTwenty()
        {
            Assert.That(StealthRules.DropNoiseRadius(0.5f), Is.EqualTo(0f));
            Assert.That(StealthRules.DropNoiseRadius(Tuning.NoiseDropSilentBelow), Is.EqualTo(8f).Within(0.01f));
            Assert.That(StealthRules.DropNoiseRadius(4f), Is.EqualTo(20f).Within(0.01f));
            Assert.That(StealthRules.DropNoiseRadius(6f), Is.EqualTo(20f).Within(0.01f));
            Assert.That(StealthRules.NoiseRadius(NoiseKind.Run), Is.EqualTo(15f));
        }
    }
}
