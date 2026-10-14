using NUnit.Framework;

namespace Pachacuti.Core.Tests
{
    public class QuipuTests
    {
        [Test]
        public void LessonCord340()
        {
            var p = Pendant.FromValue(340);
            Assert.That(p.PositionCount, Is.EqualTo(3));
            Assert.That(p.Clusters[0].IsGap, Is.True, "units: a gap");
            Assert.That(p.Clusters[1].Type, Is.EqualTo(KnotType.Single));
            Assert.That(p.Clusters[1].KnotCount, Is.EqualTo(4));
            Assert.That(p.Clusters[2].Type, Is.EqualTo(KnotType.Single));
            Assert.That(p.Clusters[2].KnotCount, Is.EqualTo(3));
        }

        [Test]
        public void TestCord1200HasAThousandsCluster()
        {
            var p = Pendant.FromValue(1200);
            Assert.That(p.PositionCount, Is.EqualTo(4));
            Assert.That(p.Clusters[0].IsGap && p.Clusters[1].IsGap, Is.True);
            Assert.That(p.Clusters[2].KnotCount, Is.EqualTo(2));
            Assert.That(p.Clusters[3].KnotCount, Is.EqualTo(1));
        }

        [Test]
        public void EmptyCordIsZero()
        {
            var p = Pendant.FromValue(0);
            Assert.That(p.PositionCount, Is.EqualTo(1));
            Assert.That(p.Clusters[0].IsGap, Is.True);
            Assert.That(Pendant.Read(p.Clusters), Is.EqualTo(0));
        }

        [Test]
        public void UnitsUseLongKnotsAndAFigureEightForOne()
        {
            Assert.That(Pendant.FromValue(1).Clusters[0].Type, Is.EqualTo(KnotType.FigureEight));
            var seven = Pendant.FromValue(7).Clusters[0];
            Assert.That(seven.Type, Is.EqualTo(KnotType.Long));
            Assert.That(seven.Turns, Is.EqualTo(7));
            Assert.That(seven.KnotCount, Is.EqualTo(1));
            Assert.That(Pendant.FromValue(10).Clusters[1].Type, Is.EqualTo(KnotType.Single), "tens use singles even for 1");
        }

        [Test]
        public void EveryValueRoundTripsThroughTheKnots()
        {
            for (int v = 0; v <= 9999; v++)
                Assert.That(Pendant.Read(Pendant.FromValue(v).Clusters), Is.EqualTo(v));
        }

        [Test]
        public void LessonFlowCorrectFirstTry()
        {
            var l = new QuipuLesson();
            l.SetGuess(2, 3);
            l.SetGuess(1, 4);
            var r = l.Confirm();
            Assert.That(r.Verdict, Is.EqualTo(QuipuVerdict.Correct), "uncommitted units read as a gap");
            Assert.That(r.Tries, Is.EqualTo(1));
        }

        [Test]
        public void WrongAnswerPointsAtTheLowestMiscountedKnot()
        {
            var l = new QuipuLesson();
            l.SetGuess(2, 3);
            l.SetGuess(1, 5);
            l.SetGuess(0, 0);
            var r = l.Confirm();
            Assert.That(r.Verdict, Is.EqualTo(QuipuVerdict.Wrong));
            Assert.That(r.WrongPosition, Is.EqualTo(1));
        }

        [Test]
        public void ThirdWrongTryRevealsAndMovesOn()
        {
            var l = new QuipuLesson();
            l.SetGuess(2, 9);
            Assert.That(l.Confirm().Verdict, Is.EqualTo(QuipuVerdict.Wrong));
            Assert.That(l.Confirm().Verdict, Is.EqualTo(QuipuVerdict.Wrong));
            Assert.That(l.Confirm().Verdict, Is.EqualTo(QuipuVerdict.Revealed));
            Assert.That(l.NextPendant(), Is.True);
            Assert.That(l.Current.Value, Is.EqualTo(1200));
            Assert.That(l.Tries, Is.EqualTo(0));
        }

        [Test]
        public void HoverHintsOnlyOnTheLessonCordAndOnlyOnce()
        {
            var l = new QuipuLesson();
            Assert.That(l.TryHoverHint(1, out var line), Is.True);
            Assert.That(line.Text, Is.EqualTo("Tens."));
            Assert.That(l.TryHoverHint(1, out _), Is.False);
            l.NextPendant();
            Assert.That(l.TryHoverHint(0, out _), Is.False, "cord 2 is read with no help");
        }

        [Test]
        public void TheEmptyCordNeverFails()
        {
            var l = new QuipuLesson();
            l.NextPendant();
            l.NextPendant();
            Assert.That(l.Current.Value, Is.EqualTo(0));
            l.SetGuess(0, 4);
            Assert.That(l.Confirm().Verdict, Is.EqualTo(QuipuVerdict.Revealed));
            Assert.That(l.NextPendant(), Is.False);
            Assert.That(l.IsComplete, Is.True);
        }
    }
}
