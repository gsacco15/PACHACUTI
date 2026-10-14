using NUnit.Framework;

namespace Pachacuti.Core.Tests
{
    public class SoldierBrainTests
    {
        static SoldierSenses Porter(float distance, bool sees = true)
        {
            return new SoldierSenses
            {
                Dt = 0.05f,
                SeesManco = sees,
                DistanceToManco = distance,
                MancoWithLine = true,
                MancoHeadDown = true,
                MancoCarrying = true,
                MancoReachable = true,
            };
        }

        static SoldierTickResult TickFor(SoldierBrain b, SoldierSenses s, float seconds)
        {
            var acc = new SoldierTickResult();
            int n = (int)System.Math.Round(seconds / s.Dt);
            for (int i = 0; i < n; i++)
            {
                var r = b.Tick(s);
                acc.Shout |= r.Shout;
                acc.Capture |= r.Capture;
                acc.InspectedLoad |= r.InspectedLoad;
            }
            return acc;
        }

        [Test]
        public void GlanceAtAConformingPorterAddsNothing()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            b.Tick(Porter(7f));
            Assert.That(b.State, Is.EqualTo(SoldierState.Glance));
            Assert.That(b.Suspicion, Is.EqualTo(0f));
        }

        [Test]
        public void GlanceAtSomeoneNotHeadDownAddsTen()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            var s = Porter(7f);
            s.MancoHeadDown = false;
            b.Tick(s);
            Assert.That(b.State, Is.EqualTo(SoldierState.Glance));
            Assert.That(b.Suspicion, Is.EqualTo(10f));
        }

        [Test]
        public void GlanceLastsOneSecond()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            b.Tick(Porter(7f));
            TickFor(b, Porter(20f, sees: false), 1.1f);
            Assert.That(b.State, Is.EqualTo(SoldierState.Idle));
        }

        [Test]
        public void StoppingCloseTriggersAStare()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            var s = Porter(5f);
            s.MancoStopped = true;
            b.Tick(s);
            Assert.That(b.State, Is.EqualTo(SoldierState.Stare));
            Assert.That(b.Suspicion, Is.EqualTo(25f));
        }

        [Test]
        public void BreakingFromTheLineTriggersAStare()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            var s = Porter(7.5f);
            s.MancoWithLine = false;
            b.Tick(s);
            Assert.That(b.State, Is.EqualTo(SoldierState.Stare));
        }

        [Test]
        public void SuspicionDecaysFivePerSecondWithTheLineInTheDark()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            b.SetSuspicion(40f);
            TickFor(b, Porter(20f, sees: false), 2f);
            Assert.That(b.Suspicion, Is.EqualTo(30f).Within(0.3f));
        }

        [Test]
        public void NoDecayInTorchlight()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            b.SetSuspicion(40f);
            var s = Porter(20f, sees: false);
            s.MancoLit = true;
            TickFor(b, s, 2f);
            Assert.That(b.Suspicion, Is.EqualTo(40f));
        }

        [Test]
        public void FiftyMeansApproachAndCloseRangeClimbsFifteenPerSecond()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            b.SetSuspicion(50f);
            b.Tick(Porter(5f));
            Assert.That(b.State, Is.EqualTo(SoldierState.Approach));
            TickFor(b, Porter(2.5f), 1f);
            Assert.That(b.Suspicion, Is.EqualTo(65f).Within(1f));
        }

        [Test]
        public void ChallengeWhileCarryingPassesAndResetsToForty()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            b.SetSuspicion(79f);
            b.Tick(Porter(2f));
            var r = TickFor(b, Porter(2f), 0.2f);
            Assert.That(b.State, Is.EqualTo(SoldierState.Challenge));
            bool passed = false;
            for (int i = 0; i < 100 && !passed; i++)
            {
                r = b.Tick(Porter(2f));
                Assert.That(r.Capture, Is.False);
                passed = r.InspectedLoad;
            }
            Assert.That(passed, Is.True);
            Assert.That(b.Suspicion, Is.EqualTo(40f).Within(0.3f), "load inspected: suspicion resets to 40");
        }

        [Test]
        public void ChallengeWithoutTheLoadIsACapture()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            b.SetSuspicion(85f);
            var s = Porter(2f);
            s.MancoCarrying = false;
            s.MancoHeadDown = false;
            var r = TickFor(b, s, Tuning.ChallengeReactSeconds + 0.3f);
            Assert.That(r.Capture, Is.True);
        }

        [Test]
        public void RunningFromAChallengeRaisesTheAlarm()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            b.SetSuspicion(85f);
            TickFor(b, Porter(2f), 0.2f);
            Assert.That(b.State, Is.EqualTo(SoldierState.Challenge));
            var r = b.Tick(Porter(6f));
            Assert.That(r.Shout, Is.True);
            Assert.That(b.State, Is.EqualTo(SoldierState.Alert));
        }

        [Test]
        public void AFoundBodyRaisesTheAlarm()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            var s = Porter(15f, sees: false);
            s.SeesBody = true;
            var r = b.Tick(s);
            Assert.That(r.Shout, Is.True);
            Assert.That(b.State, Is.EqualTo(SoldierState.Alert));
        }

        [Test]
        public void AlertedSoldiersCaptureOnTouch()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            var s = Porter(10f);
            s.AlertCalled = true;
            b.Tick(s);
            Assert.That(b.State, Is.EqualTo(SoldierState.Alert));
            var r = b.Tick(Porter(0.8f));
            Assert.That(r.Capture, Is.True);
        }

        [Test]
        public void RooftopSightingShoutsAndChaseEndsAfterTenSeconds()
        {
            var b = new SoldierBrain(SoldierRules.Rooftop);
            var s = Porter(8f);
            s.MancoReachable = false;
            var r = b.Tick(s);
            Assert.That(r.Shout, Is.True);
            Assert.That(b.IsChasing, Is.True);
            r = TickFor(b, s, 9.5f);
            Assert.That(r.Capture, Is.False, "capture in Beat 1 is impossible");
            Assert.That(b.IsChasing, Is.True);
            TickFor(b, s, 0.6f);
            Assert.That(b.IsChasing, Is.False);
        }

        [Test]
        public void BreakingLineOfSightForFiveSecondsEndsTheChase()
        {
            var b = new SoldierBrain(SoldierRules.Rooftop);
            var s = Porter(8f);
            s.MancoReachable = false;
            b.Tick(s);
            var hidden = s;
            hidden.SeesManco = false;
            TickFor(b, hidden, 4.9f);
            Assert.That(b.IsChasing, Is.True);
            TickFor(b, hidden, 0.2f);
            Assert.That(b.IsChasing, Is.False);
        }

        [Test]
        public void NoiseMakesASoldierTurnAndHoldFourSeconds()
        {
            var b = new SoldierBrain(SoldierRules.Rooftop);
            var s = Porter(30f, sees: false);
            s.HeardNoise = true;
            b.Tick(s);
            Assert.That(b.State, Is.EqualTo(SoldierState.Investigate));
            TickFor(b, Porter(30f, sees: false), 3.9f);
            Assert.That(b.State, Is.EqualTo(SoldierState.Investigate));
            TickFor(b, Porter(30f, sees: false), 0.2f);
            Assert.That(b.State, Is.EqualTo(SoldierState.Idle));
        }

        [Test]
        public void ScriptedAndDeadSoldiersIgnoreSenses()
        {
            var b = new SoldierBrain(SoldierRules.Street);
            b.BeginScripted();
            var s = Porter(1f);
            s.AlertCalled = true;
            b.Tick(s);
            Assert.That(b.State, Is.EqualTo(SoldierState.Scripted));
            b.Kill();
            b.Tick(s);
            Assert.That(b.State, Is.EqualTo(SoldierState.Dead));
        }
    }
}
