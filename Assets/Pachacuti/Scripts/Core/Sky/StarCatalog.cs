using System;
using System.Collections.Generic;
using System.Globalization;

namespace Pachacuti.Core
{
    /// <summary>A catalog entry at J2000 (HYG v4.1 extract, see tools/build_star_catalog.py).</summary>
    public struct CatalogStar
    {
        public double RaHours;
        public double DecDeg;
        public double PmRaMasYr;   // μα·cos δ
        public double PmDecMasYr;
        public float Magnitude;
        public float ColorIndexBV;
        public string Name;
    }

    /// <summary>A star placed in the local sky for a site and moment.</summary>
    public struct SkyStar
    {
        public Horizontal Position;
        public double RaDeg;        // of date
        public double DecDeg;       // of date
        public float Magnitude;
        public float ColorIndexBV;
        public string Name;
    }

    public static class StarCatalog
    {
        /// <summary>Parse the slice catalog CSV: ra_h,dec_deg,pmra,pmdec,mag,bv,name. Lines starting with '#' are comments.</summary>
        public static List<CatalogStar> Parse(string csv)
        {
            var list = new List<CatalogStar>(2048);
            if (string.IsNullOrEmpty(csv)) return list;
            var inv = CultureInfo.InvariantCulture;
            foreach (var raw in csv.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                var f = line.Split(',');
                if (f.Length < 6) continue;
                list.Add(new CatalogStar
                {
                    RaHours = double.Parse(f[0], inv),
                    DecDeg = double.Parse(f[1], inv),
                    PmRaMasYr = double.Parse(f[2], inv),
                    PmDecMasYr = double.Parse(f[3], inv),
                    Magnitude = float.Parse(f[4], inv),
                    ColorIndexBV = float.Parse(f[5], inv),
                    Name = f.Length > 6 ? f[6] : "",
                });
            }
            return list;
        }

        /// <summary>
        /// Place catalog stars in the sky for a site: proper motion back from 2000.0,
        /// precession from J2000 to the date, then to altitude/azimuth.
        /// </summary>
        public static List<SkyStar> Place(IReadOnlyList<CatalogStar> stars, in ObservationSite site, bool aboveHorizonOnly = false)
        {
            var result = new List<SkyStar>(stars.Count);
            double years = (site.JulianDayUT - Astronomy.J2000) / 365.25;
            for (int i = 0; i < stars.Count; i++)
            {
                var s = stars[i];
                Astronomy.ApplyProperMotion(s.RaHours * 15.0, s.DecDeg, s.PmRaMasYr, s.PmDecMasYr, years, out var ra, out var dec);
                Astronomy.Precess(ra, dec, Astronomy.J2000, site.JulianDayUT, out ra, out dec);
                var h = Astronomy.ToHorizontal(ra, dec, site);
                if (aboveHorizonOnly && h.AltitudeDeg < -2.0) continue;
                result.Add(new SkyStar
                {
                    Position = h,
                    RaDeg = ra,
                    DecDeg = dec,
                    Magnitude = s.Magnitude,
                    ColorIndexBV = s.ColorIndexBV,
                    Name = s.Name,
                });
            }
            return result;
        }

        public static int IndexOf(IReadOnlyList<SkyStar> stars, string name)
        {
            for (int i = 0; i < stars.Count; i++)
                if (string.Equals(stars[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }
    }

    public static class StarColor
    {
        /// <summary>B−V colour index to an approximate linear RGB (0..1) via blackbody temperature.</summary>
        public static void FromBV(float bv, out float r, out float g, out float b)
        {
            if (bv < -0.4f) bv = -0.4f;
            if (bv > 2.0f) bv = 2.0f;
            // Ballesteros (2012) B−V → effective temperature.
            double t = 4600.0 * (1.0 / (0.92 * bv + 1.7) + 1.0 / (0.92 * bv + 0.62));
            // Tanner Helland's blackbody fit, in hundreds of kelvin.
            double k = t / 100.0;
            double rr, gg, bb;
            if (k <= 66) rr = 255;
            else rr = 329.698727446 * Math.Pow(k - 60, -0.1332047592);
            if (k <= 66) gg = 99.4708025861 * Math.Log(k) - 161.1195681661;
            else gg = 288.1221695283 * Math.Pow(k - 60, -0.0755148492);
            if (k >= 66) bb = 255;
            else if (k <= 19) bb = 0;
            else bb = 138.5177312231 * Math.Log(k - 10) - 305.0447927307;
            r = (float)(Clamp255(rr) / 255.0);
            g = (float)(Clamp255(gg) / 255.0);
            b = (float)(Clamp255(bb) / 255.0);
        }

        static double Clamp255(double v) => v < 0 ? 0 : (v > 255 ? 255 : v);

        /// <summary>Relative flux of a star versus magnitude 0.</summary>
        public static float Flux(float magnitude) => (float)Math.Pow(10.0, -0.4 * magnitude);
    }
}
