using System;
using System.Linq;
using NUnit.Framework;

namespace Pachacuti.Core.Tests
{
    public class SkyMapModelTests
    {
        static SkyEvent Advance(SkyMapModel m, float seconds)
        {
            var ev = SkyEvent.None;
            for (int i = 0; i < (int)Math.Round(seconds / 0.05f); i++) ev |= m.Tick(0.05f);
            return ev;
        }

        static SkyMapModel AtCordGlow(KinsmanOutcome tupa = KinsmanOutcome.Hidden)
        {
            var m = new SkyMapModel(tupa);
            Advance(m, Tuning.SkyTiltSeconds + 0.05f);
            Advance(m, Tuning.SkyCordArrivesAfterSeconds + 0.05f);
            return m;
        }

        [Test]
        public void TiltThenCordArrivesAfterTenSeconds()
        {
            var m = new SkyMapModel(KinsmanOutcome.None);
            Assert.That(Advance(m, 3.05f) & SkyEvent.TiltFinished, Is.EqualTo(SkyEvent.TiltFinished));
            Assert.That(Advance(m, 9.9f) & SkyEvent.CordGlows, Is.EqualTo(SkyEvent.None));
            Assert.That(Advance(m, 0.2f) & SkyEvent.CordGlows, Is.EqualTo(SkyEvent.CordGlows));
        }

        [Test]
        public void ReadingTheCordSharpensTheSmudge()
        {
            var m = AtCordGlow();
            Assert.That(m.ArmySharp, Is.False);
            var ev = m.Interact();
            Assert.That(ev & SkyEvent.CordRead, Is.EqualTo(SkyEvent.CordRead));
            Assert.That(m.ArmySharp, Is.True);
            Assert.That(m.Lines.Dequeue().Text, Is.EqualTo("Fresh. They are at the bridge."));
        }

        [Test]
        public void TheSkyReadsTheCordIfThePlayerNeverDoes()
        {
            var m = AtCordGlow();
            Advance(m, SkyMapModel.CordAutoReadSeconds + 0.1f);
            Assert.That(m.ArmySharp, Is.True);
        }

        [Test]
        public void HoveringTheLlamaBeforeTheCordIsLogged()
        {
            var m = new SkyMapModel(KinsmanOutcome.None);
            Advance(m, 3.1f);
            var ev = m.Hover(SkyTarget.Llama);
            Assert.That(ev & SkyEvent.LlamaHoveredBeforeCord, Is.EqualTo(SkyEvent.LlamaHoveredBeforeCord));
            Assert.That(ev & SkyEvent.FirstInteraction, Is.EqualTo(SkyEvent.FirstInteraction));
            Assert.That(m.ThreadTaken, Is.False);
        }

        [Test]
        public void TheArmyCannotBeOrderedWhileItIsASmudge()
        {
            var m = new SkyMapModel(KinsmanOutcome.None);
            Advance(m, 3.1f);
            m.Select(SkyTarget.Llama);
            Assert.That(m.Step, Is.EqualTo(SkyStep.Looking));
        }

        [Test]
        public void FullSequenceWithTupaSpared()
        {
            var m = AtCordGlow(KinsmanOutcome.Hidden);
            m.Interact();
            Assert.That(m.Hover(SkyTarget.Llama) & SkyEvent.ThreadTaken, Is.EqualTo(SkyEvent.ThreadTaken));
            Assert.That(m.Select(SkyTarget.Llama) & SkyEvent.ArmySelected, Is.EqualTo(SkyEvent.ArmySelected));
            Assert.That(m.Select(SkyTarget.Tambo) & SkyEvent.OrderGiven, Is.EqualTo(SkyEvent.OrderGiven));
            var ev = Advance(m, Tuning.SkyArmyMoveSeconds + 0.05f);
            Assert.That(ev & SkyEvent.TamboBrightens, Is.EqualTo(SkyEvent.TamboBrightens));
            Assert.That(m.ArmyNode, Is.EqualTo(SkyMapModel.ArmyStartNode + 1), "the llama slides one node toward the tambo");
            ev = Advance(m, 3f);
            Assert.That(ev & SkyEvent.TupaStarLit, Is.EqualTo(SkyEvent.TupaStarLit));
            Assert.That(m.CuscoStarOut, Is.False);
            Assert.That(m.Lines.Select(l => l.Text), Does.Contain("Tupa."));
            ev = Advance(m, 3f);
            Assert.That(ev & SkyEvent.ExitOffered, Is.EqualTo(SkyEvent.ExitOffered));
            Assert.That(m.Select(SkyTarget.ExitLine) & SkyEvent.Exit, Is.EqualTo(SkyEvent.Exit));
            Assert.That(m.Step, Is.EqualTo(SkyStep.Done));
        }

        [Test]
        public void KillingTupaPutsAStarOut()
        {
            var m = AtCordGlow(KinsmanOutcome.Killed);
            m.Interact();
            m.Select(SkyTarget.Llama);
            m.Select(SkyTarget.Tambo);
            Advance(m, 8f);
            Assert.That(m.CuscoStarOut, Is.True);
            Assert.That(m.TupaStarLit, Is.False);
        }

        [Test]
        public void HoverLinesDoNotRepeatInsideTheCooldown()
        {
            var m = new SkyMapModel(KinsmanOutcome.None);
            Advance(m, 3.1f);
            m.Hover(SkyTarget.Tambo);
            m.Hover(SkyTarget.Tambo);
            Assert.That(m.Lines.Count, Is.EqualTo(1));
            Assert.That(m.Lines.Peek().Text, Is.EqualTo("The tambo at Quispicanchi."));
        }
    }

    public class AstronomyTests
    {
        [Test]
        public void JulianDays()
        {
            Assert.That(Astronomy.JulianDayGregorian(2000, 1, 1.5), Is.EqualTo(2451545.0).Within(1e-9));
            Assert.That(Astronomy.JulianDayGregorian(1957, 10, 4.81), Is.EqualTo(2436116.31).Within(1e-6));   // Meeus 7.a
            Assert.That(Astronomy.JulianDayJulianCalendar(333, 1, 27.5), Is.EqualTo(1842713.0).Within(1e-6)); // Meeus 7.b
        }

        [Test]
        public void SiderealTimeMeeus12a()
        {
            Assert.That(Astronomy.GreenwichMeanSiderealTimeDeg(2446895.5), Is.EqualTo(197.693195).Within(1e-5));
        }

        [Test]
        public void PrecessionAndProperMotionMeeus21b()
        {
            // θ Persei, J2000 → 2028 Nov 13.19 TD.
            Astronomy.ApplyProperMotion(41.049942, 49.228467, 335.53, -89.5, 28.86705, out var ra, out var dec);
            Astronomy.Precess(ra, dec, Astronomy.J2000, 2462088.69, out ra, out dec);
            Assert.That(ra, Is.EqualTo(41.547214).Within(2e-5));
            Assert.That(dec, Is.EqualTo(49.348483).Within(2e-5));
        }

        [Test]
        public void GalacticFrame()
        {
            Astronomy.GalacticToEquatorialJ2000(0, 0, out var ra, out var dec);
            Assert.That(ra, Is.EqualTo(266.405).Within(0.01));
            Assert.That(dec, Is.EqualTo(-28.936).Within(0.01));
            Astronomy.GalacticToEquatorialJ2000(0, 90, out ra, out dec);
            Assert.That(ra, Is.EqualTo(192.859).Within(0.01));
            Assert.That(dec, Is.EqualTo(27.128).Within(0.01));
            Astronomy.EquatorialJ2000ToGalactic(266.405, -28.936, out var l, out var b);
            Assert.That(Math.Min(l, 360 - l), Is.LessThan(0.02));
            Assert.That(b, Is.EqualTo(0).Within(0.02));
        }

        [Test]
        public void HorizontalRoundTrip()
        {
            var site = ObservationSite.CuscoSliceNight;
            foreach (var (ra, dec) in new[] { (10.0, 20.0), (200.0, -60.0), (75.0, -13.5), (300.0, 5.0) })
            {
                var h = Astronomy.ToHorizontal(ra, dec, site);
                h.ToLocal(out var e, out var u, out var n);
                Astronomy.LocalToEquatorial(e, u, n, site, out var ra2, out var dec2);
                Assert.That(Astronomy.Separation(ra, dec, ra2, dec2), Is.LessThan(1e-6));
            }
        }

        [Test]
        public void SliceNightIsAJulianCalendarDateInCusco()
        {
            var site = ObservationSite.CuscoSliceNight;
            // 1532 Nov 16 (Julian) 22:00 LMT at 71.9752° W = Nov 17 02:47:54 UT.
            double expected = Astronomy.JulianDayJulianCalendar(1532, 11, 17) + (2 + 47.9 / 60.0) / 24.0;
            Assert.That(site.JulianDayUT, Is.EqualTo(expected).Within(1e-3));
            Assert.That(site.LatitudeDeg, Is.LessThan(0), "southern hemisphere");
        }
    }

    public class StarCatalogTests
    {
        static System.Collections.Generic.List<SkyStar> SliceSky()
        {
            string path = System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "bright_stars.csv");
            if (!System.IO.File.Exists(path))
                path = System.IO.Path.Combine("Assets", "Pachacuti", "Resources", "Pachacuti", "Sky", "bright_stars.csv");
            if (!System.IO.File.Exists(path)) Assert.Ignore("star catalog not found from " + Environment.CurrentDirectory);
            var cat = StarCatalog.Parse(System.IO.File.ReadAllText(path));
            Assert.That(cat.Count, Is.EqualTo(2000));
            return StarCatalog.Place(cat, ObservationSite.CuscoSliceNight);
        }

        [Test]
        public void PleiadesAreUpAndYacanasEyesAreDown()
        {
            // Documented finding (docs/SLICE_IMPLEMENTATION_NOTES.md): at 22:00 on 16 Nov 1532
            // the Pleiades stand ~51° up in the north-north-east, and α/β Centauri — the
            // llama's eyes — have not risen yet.
            var sky = SliceSky();
            var alcyone = sky[StarCatalog.IndexOf(sky, "Alcyone")].Position;
            Assert.That(alcyone.AltitudeDeg, Is.EqualTo(51.3).Within(1.0));
            Assert.That(alcyone.AzimuthDeg, Is.EqualTo(21.4).Within(2.0));
            Assert.That(sky[StarCatalog.IndexOf(sky, "Rigil Kentaurus")].Position.AltitudeDeg, Is.LessThan(0));
            Assert.That(sky[StarCatalog.IndexOf(sky, "Hadar")].Position.AltitudeDeg, Is.LessThan(0));
        }

        [Test]
        public void SiriusAndCanopusAreUpInTheEast()
        {
            var sky = SliceSky();
            var sirius = sky[StarCatalog.IndexOf(sky, "Sirius")].Position;
            Assert.That(sirius.AltitudeDeg, Is.GreaterThan(20));
            Assert.That(sirius.AzimuthDeg, Is.InRange(60.0, 140.0));
            Assert.That(sky[StarCatalog.IndexOf(sky, "Canopus")].Position.AltitudeDeg, Is.GreaterThan(20));
        }

        [Test]
        public void StarColours()
        {
            StarColor.FromBV(-0.2f, out var r, out _, out var b);
            Assert.That(b, Is.GreaterThan(r), "hot stars are blue-white");
            StarColor.FromBV(1.6f, out r, out _, out b);
            Assert.That(r, Is.GreaterThan(b), "cool stars are orange");
            Assert.That(StarColor.Flux(0f), Is.EqualTo(1f).Within(1e-6));
            Assert.That(StarColor.Flux(5f), Is.EqualTo(0.01f).Within(1e-6));
        }

        [Test]
        public void MilkyWayIsBrightTowardSagittariusAndDarkInTheCoalsack()
        {
            Assert.That(MilkyWay.Brightness(0, 0), Is.GreaterThan(MilkyWay.Brightness(180, 0)));
            Assert.That(MilkyWay.Brightness(180, 0), Is.GreaterThan(MilkyWay.Brightness(0, 60)));
            Assert.That(MilkyWay.Brightness(301, -1), Is.LessThan(MilkyWay.Brightness(292, -1) * 0.5));
        }
    }

    public class VisionTests
    {
        [Test]
        public void ShotListTimes()
        {
            Assert.That(VisionTimeline.ShotAt(0f), Is.EqualTo(VisionShot.Touch));
            Assert.That(VisionTimeline.ShotAt(2f), Is.EqualTo(VisionShot.Cut));
            Assert.That(VisionTimeline.ShotAt(19.9f), Is.EqualTo(VisionShot.Lines));
            Assert.That(VisionTimeline.ShotAt(65f), Is.EqualTo(VisionShot.Square));
            Assert.That(VisionTimeline.ShotAt(89.9f), Is.EqualTo(VisionShot.Return));
            Assert.That(VisionTimeline.ShotAt(90f), Is.EqualTo(VisionShot.Title));
        }

        [Test]
        public void StrobeNeverExceedsTheCap([Values(false, true)] bool reduced)
        {
            float cap = reduced ? Tuning.StrobeReducedFlashMaxHz : Tuning.StrobeMaxHz;
            int swaps = 0, last = -1;
            float lastSwap = -1f, minGap = float.MaxValue;
            for (float t = 0f; t < 90f; t += 0.001f)
            {
                Assert.That(VisionTimeline.StrobeHz(t, reduced), Is.LessThanOrEqualTo(cap + 1e-3f));
                int s = VisionTimeline.StrobeSnapshot(t, reduced);
                if (s >= 0 && s != last)
                {
                    if (last >= 0) { swaps++; if (lastSwap >= 0) minGap = Math.Min(minGap, t - lastSwap); lastSwap = t; }
                    last = s;
                }
            }
            Assert.That(swaps, Is.GreaterThan(5));
            Assert.That(minGap, Is.GreaterThanOrEqualTo(1f / cap - 0.002f), "photosensitivity: no swaps faster than the cap");
            Assert.That(VisionTimeline.StrobeHz(30f, reduced), Is.EqualTo(0f));
        }

        [Test]
        public void FortyOneCequesInCobosQuarters()
        {
            var lines = CequeLayout.Generate();
            Assert.That(lines.Count, Is.EqualTo(41));
            Assert.That(lines.Count(l => l.Suyu == Suyu.Chinchaysuyu), Is.EqualTo(9));
            Assert.That(lines.Count(l => l.Suyu == Suyu.Antisuyu), Is.EqualTo(9));
            Assert.That(lines.Count(l => l.Suyu == Suyu.Collasuyu), Is.EqualTo(9));
            Assert.That(lines.Count(l => l.Suyu == Suyu.Cuntisuyu), Is.EqualTo(14));
            var through = lines.Where(l => l.ThroughQenqo).ToList();
            Assert.That(through.Count, Is.EqualTo(1));
            Assert.That(through[0].BearingDeg, Is.EqualTo(49.45).Within(0.5), "Qenqo is north-east of the Coricancha");
            Assert.That(through[0].LengthM, Is.GreaterThan(CequeLayout.DistanceCoricanchaToQenqoM));
            Assert.That(CequeLayout.DistanceCoricanchaToQenqoM, Is.EqualTo(1693).Within(50));
        }

        [Test]
        public void LlamaTriangulates()
        {
            var tris = Polygon2D.Triangulate(LlamaShape.Outline);
            Assert.That(tris.Length / 3, Is.EqualTo(LlamaShape.Outline.Length - 2));
            Assert.That(Polygon2D.TriangulatedArea(LlamaShape.Outline, tris), Is.EqualTo(Math.Abs(Polygon2D.SignedArea(LlamaShape.Outline))).Within(1e-4));
        }

        [Test]
        public void ConcaveAndClockwisePolygonsTriangulate()
        {
            var l = new[] { new P2(0, 0), new P2(0, 2), new P2(1, 2), new P2(1, 1), new P2(2, 1), new P2(2, 0) }; // clockwise L
            var tris = Polygon2D.Triangulate(l);
            Assert.That(tris.Length / 3, Is.EqualTo(4));
            Assert.That(Polygon2D.TriangulatedArea(l, tris), Is.EqualTo(3f).Within(1e-5));
        }
    }
}
