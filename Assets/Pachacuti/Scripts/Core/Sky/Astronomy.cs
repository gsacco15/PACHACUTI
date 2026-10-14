using System;

namespace Pachacuti.Core
{
    /// <summary>Altitude above the horizon and azimuth from north toward east, in degrees.</summary>
    public readonly struct Horizontal
    {
        public readonly double AltitudeDeg;
        public readonly double AzimuthDeg;

        public Horizontal(double altitudeDeg, double azimuthDeg)
        {
            AltitudeDeg = altitudeDeg;
            AzimuthDeg = azimuthDeg;
        }

        /// <summary>Unit vector in a local frame: x = east, y = up, z = north (Unity's axes with +Z as north).</summary>
        public void ToLocal(out double east, out double up, out double north)
        {
            double alt = Astronomy.Rad(AltitudeDeg), az = Astronomy.Rad(AzimuthDeg);
            east = Math.Cos(alt) * Math.Sin(az);
            north = Math.Cos(alt) * Math.Cos(az);
            up = Math.Sin(alt);
        }
    }

    /// <summary>Where and when the sky is observed.</summary>
    public readonly struct ObservationSite
    {
        public readonly double LatitudeDeg;
        public readonly double LongitudeEastDeg;
        public readonly double JulianDayUT;

        public ObservationSite(double latitudeDeg, double longitudeEastDeg, double julianDayUT)
        {
            LatitudeDeg = latitudeDeg;
            LongitudeEastDeg = longitudeEastDeg;
            JulianDayUT = julianDayUT;
        }

        /// <summary>
        /// Pitch slice §12: "Cusco, 16 Nov 1532, 22:00". 1532 predates the Gregorian
        /// reform, so the date is Julian-calendar. 22:00 is taken as local mean time
        /// at the Coricancha (13.5199 S, 71.9752 W): UT = 22:00 + 4h47m54s.
        /// </summary>
        public static ObservationSite CuscoSliceNight
        {
            get
            {
                const double lat = -13.5199, lon = -71.9752;
                double utHours = 22.0 - lon / 15.0;
                return new ObservationSite(lat, lon, Astronomy.JulianDayJulianCalendar(1532, 11, 16 + utHours / 24.0));
            }
        }
    }

    /// <summary>
    /// The small amount of positional astronomy the slice needs (Meeus,
    /// "Astronomical Algorithms", 2nd ed., ch. 7, 12, 13, 21). Good to well under
    /// a tenth of a degree over the 468 years back to 1532 — far finer than a
    /// star sprite.
    /// </summary>
    public static class Astronomy
    {
        public const double J2000 = 2451545.0;

        public static double Rad(double deg) => deg * Math.PI / 180.0;
        public static double Deg(double rad) => rad * 180.0 / Math.PI;

        public static double Normalize360(double deg)
        {
            deg %= 360.0;
            return deg < 0 ? deg + 360.0 : deg;
        }

        /// <summary>Julian Day for a date in the Gregorian calendar (Meeus 7.1).</summary>
        public static double JulianDayGregorian(int year, int month, double day) => JulianDay(year, month, day, true);

        /// <summary>Julian Day for a date in the Julian calendar (Meeus 7.1 with B = 0).</summary>
        public static double JulianDayJulianCalendar(int year, int month, double day) => JulianDay(year, month, day, false);

        static double JulianDay(int y, int m, double d, bool gregorian)
        {
            if (m <= 2) { y -= 1; m += 12; }
            int b = 0;
            if (gregorian)
            {
                int a = (int)Math.Floor(y / 100.0);
                b = 2 - a + (int)Math.Floor(a / 4.0);
            }
            return Math.Floor(365.25 * (y + 4716)) + Math.Floor(30.6001 * (m + 1)) + d + b - 1524.5;
        }

        /// <summary>Greenwich mean sidereal time in degrees (Meeus 12.4).</summary>
        public static double GreenwichMeanSiderealTimeDeg(double jdUT)
        {
            double t = (jdUT - J2000) / 36525.0;
            double theta = 280.46061837 + 360.98564736629 * (jdUT - J2000) + 0.000387933 * t * t - t * t * t / 38710000.0;
            return Normalize360(theta);
        }

        /// <summary>Rigorous precession between two epochs (Meeus 21.2–21.4). Angles in degrees.</summary>
        public static void Precess(double raDeg, double decDeg, double jdFrom, double jdTo, out double raOut, out double decOut)
        {
            double T = (jdFrom - J2000) / 36525.0;
            double t = (jdTo - jdFrom) / 36525.0;
            double c1 = 2306.2181 + 1.39656 * T - 0.000139 * T * T;
            double zeta = c1 * t + (0.30188 - 0.000344 * T) * t * t + 0.017998 * t * t * t;
            double z = c1 * t + (1.09468 + 0.000066 * T) * t * t + 0.018203 * t * t * t;
            double theta = (2004.3109 - 0.85330 * T - 0.000217 * T * T) * t - (0.42665 + 0.000217 * T) * t * t - 0.041833 * t * t * t;
            zeta = Rad(zeta / 3600.0);
            z = Rad(z / 3600.0);
            theta = Rad(theta / 3600.0);

            double a0 = Rad(raDeg), d0 = Rad(decDeg);
            double A = Math.Cos(d0) * Math.Sin(a0 + zeta);
            double B = Math.Cos(theta) * Math.Cos(d0) * Math.Cos(a0 + zeta) - Math.Sin(theta) * Math.Sin(d0);
            double C = Math.Sin(theta) * Math.Cos(d0) * Math.Cos(a0 + zeta) + Math.Cos(theta) * Math.Sin(d0);
            raOut = Normalize360(Deg(Math.Atan2(A, B) + z));
            decOut = Deg(Math.Asin(Math.Max(-1.0, Math.Min(1.0, C))));
        }

        /// <summary>
        /// Apply proper motion over <paramref name="years"/> (negative = into the past).
        /// <paramref name="pmRaMasYr"/> is μα·cos δ, as in Hipparcos/HYG.
        /// </summary>
        public static void ApplyProperMotion(double raDeg, double decDeg, double pmRaMasYr, double pmDecMasYr, double years,
                                             out double raOut, out double decOut)
        {
            double cosDec = Math.Cos(Rad(decDeg));
            decOut = decDeg + pmDecMasYr * years / 3.6e6;
            raOut = cosDec > 1e-9 ? Normalize360(raDeg + pmRaMasYr / cosDec * years / 3.6e6) : raDeg;
        }

        /// <summary>Equatorial (of date) to horizontal for the given site.</summary>
        public static Horizontal ToHorizontal(double raDeg, double decDeg, in ObservationSite site)
        {
            double lst = Normalize360(GreenwichMeanSiderealTimeDeg(site.JulianDayUT) + site.LongitudeEastDeg);
            double H = Rad(lst - raDeg);
            double d = Rad(decDeg), phi = Rad(site.LatitudeDeg);
            double east = -Math.Cos(d) * Math.Sin(H);
            double north = Math.Sin(d) * Math.Cos(phi) - Math.Cos(d) * Math.Cos(H) * Math.Sin(phi);
            double up = Math.Sin(d) * Math.Sin(phi) + Math.Cos(d) * Math.Cos(H) * Math.Cos(phi);
            return new Horizontal(Deg(Math.Asin(Math.Max(-1.0, Math.Min(1.0, up)))), Normalize360(Deg(Math.Atan2(east, north))));
        }

        /// <summary>Inverse of <see cref="ToHorizontal"/>: a local direction (east, up, north) to equatorial of date.</summary>
        public static void LocalToEquatorial(double east, double up, double north, in ObservationSite site, out double raDeg, out double decDeg)
        {
            double len = Math.Sqrt(east * east + up * up + north * north);
            east /= len; up /= len; north /= len;
            double phi = Rad(site.LatitudeDeg);
            double sinDec = north * Math.Cos(phi) + up * Math.Sin(phi);
            double H = Math.Atan2(-east, up * Math.Cos(phi) - north * Math.Sin(phi));
            double lst = Normalize360(GreenwichMeanSiderealTimeDeg(site.JulianDayUT) + site.LongitudeEastDeg);
            decDeg = Deg(Math.Asin(Math.Max(-1.0, Math.Min(1.0, sinDec))));
            raDeg = Normalize360(lst - Deg(H));
        }

        /// <summary>J2000 equatorial to galactic longitude/latitude (IAU 1958 system, J2000 rotation matrix).</summary>
        public static void EquatorialJ2000ToGalactic(double raDeg, double decDeg, out double lDeg, out double bDeg)
        {
            double a = Rad(raDeg), d = Rad(decDeg);
            double x = Math.Cos(d) * Math.Cos(a), y = Math.Cos(d) * Math.Sin(a), z = Math.Sin(d);
            double gx = -0.0548755604 * x - 0.8734370902 * y - 0.4838350155 * z;
            double gy = 0.4941094279 * x - 0.4448296300 * y + 0.7469822445 * z;
            double gz = -0.8676661490 * x - 0.1980763734 * y + 0.4559837762 * z;
            lDeg = Normalize360(Deg(Math.Atan2(gy, gx)));
            bDeg = Deg(Math.Asin(Math.Max(-1.0, Math.Min(1.0, gz))));
        }

        /// <summary>Galactic to J2000 equatorial (transpose of the matrix above).</summary>
        public static void GalacticToEquatorialJ2000(double lDeg, double bDeg, out double raDeg, out double decDeg)
        {
            double l = Rad(lDeg), b = Rad(bDeg);
            double gx = Math.Cos(b) * Math.Cos(l), gy = Math.Cos(b) * Math.Sin(l), gz = Math.Sin(b);
            double x = -0.0548755604 * gx + 0.4941094279 * gy - 0.8676661490 * gz;
            double y = -0.8734370902 * gx - 0.4448296300 * gy - 0.1980763734 * gz;
            double z = -0.4838350155 * gx + 0.7469822445 * gy + 0.4559837762 * gz;
            raDeg = Normalize360(Deg(Math.Atan2(y, x)));
            decDeg = Deg(Math.Asin(Math.Max(-1.0, Math.Min(1.0, z))));
        }

        /// <summary>Angular separation between two equatorial positions, degrees.</summary>
        public static double Separation(double ra1, double dec1, double ra2, double dec2)
        {
            double d1 = Rad(dec1), d2 = Rad(dec2), da = Rad(ra2 - ra1);
            double c = Math.Sin(d1) * Math.Sin(d2) + Math.Cos(d1) * Math.Cos(d2) * Math.Cos(da);
            return Deg(Math.Acos(Math.Max(-1.0, Math.Min(1.0, c))));
        }
    }
}
