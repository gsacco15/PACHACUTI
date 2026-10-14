using System;

namespace Pachacuti.Core
{
    /// <summary>
    /// Procedural Milky Way brightness in galactic coordinates, standing in for
    /// the painted dome texture of pitch slice §12 until an artist paints one.
    /// Includes the dark clouds the Inca read as constellations (GDD §2: "the
    /// Mayu (Milky Way as river), the dark-cloud constellations"): the Coalsack,
    /// the lanes through Centaurus–Scorpius where Yacana the llama lies, the
    /// Pipe/Ophiuchus clouds, and the Great Rift.
    /// </summary>
    public static class MilkyWay
    {
        struct Cloud
        {
            public double L, B, RadiusL, RadiusB, Depth;
            public Cloud(double l, double b, double rl, double rb, double depth) { L = l; B = b; RadiusL = rl; RadiusB = rb; Depth = depth; }
        }

        static readonly Cloud[] Clouds =
        {
            new Cloud(301.0, -1.0, 3.2, 3.0, 0.85),  // Coalsack (Yutu, the tinamou)
            new Cloud(316.0, 1.5, 9.0, 2.2, 0.55),   // Yacana's head and neck, by α/β Centauri
            new Cloud(333.0, 3.0, 11.0, 2.6, 0.55),  // Yacana's body (Lupus–Norma lanes)
            new Cloud(353.0, 5.5, 8.0, 3.0, 0.60),   // Pipe nebula / Ophiuchus (Yacana's rump; the baby llama below)
            new Cloud(5.0, 1.5, 6.0, 2.0, 0.45),
            new Cloud(25.0, 2.0, 14.0, 2.4, 0.55),   // Great Rift, Aquila → Cygnus
            new Cloud(50.0, 1.0, 14.0, 2.2, 0.50),
            new Cloud(75.0, 1.0, 9.0, 2.0, 0.40),
            new Cloud(240.0, -1.5, 18.0, 2.5, 0.25), // Puppis–Canis Major dust (Mach'acuay, the serpent, runs here)
        };

        /// <summary>Galactic longitude of α Centauri, the llama's eye (b ≈ −0.7°).</summary>
        public const double YacanaEyeL = 315.73;
        public const double YacanaEyeB = -0.68;

        /// <summary>Brightness 0..~1.2 at galactic (l, b) in degrees.</summary>
        public static double Brightness(double lDeg, double bDeg)
        {
            double l = WrapSigned(lDeg);                        // −180..180, 0 = galactic centre
            double centre = 0.5 + 0.5 * Math.Cos(Astronomy.Rad(l));
            double width = 5.0 + 4.0 * centre;                   // thicker toward Sagittarius
            double band = Math.Exp(-(bDeg * bDeg) / (2 * width * width));
            double along = 0.28 + 0.72 * Math.Pow(centre, 1.6);
            double bulge = 0.75 * Math.Exp(-(l * l) / (2 * 11.0 * 11.0) - (bDeg * bDeg) / (2 * 7.0 * 7.0));
            double glow = band * along + bulge;

            double dust = 0.0;
            foreach (var c in Clouds)
            {
                double dl = WrapSigned(lDeg - c.L) / c.RadiusL;
                double db = (bDeg - c.B) / c.RadiusB;
                dust = Math.Max(dust, c.Depth * Math.Exp(-(dl * dl + db * db)));
            }

            double mottle = 0.75 + 0.5 * Noise(lDeg * 0.35, bDeg * 0.35);
            double v = glow * (1.0 - dust) * mottle;
            return v < 0 ? 0 : v;
        }

        static double WrapSigned(double deg)
        {
            deg %= 360.0;
            if (deg > 180.0) deg -= 360.0;
            if (deg < -180.0) deg += 360.0;
            return deg;
        }

        // Smooth value noise (2 octaves), deterministic, wraps in l every 360/0.35 degrees.
        static double Noise(double x, double y)
        {
            return 0.65 * ValueNoise(x, y) + 0.35 * ValueNoise(x * 2.3 + 17.1, y * 2.3 - 4.2);
        }

        static double ValueNoise(double x, double y)
        {
            int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
            double xf = x - xi, yf = y - yi;
            double u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf);
            double a = Hash(xi, yi), b = Hash(xi + 1, yi), c = Hash(xi, yi + 1), d = Hash(xi + 1, yi + 1);
            return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
        }

        static double Hash(int x, int y)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (double)0xFFFFFF;
            }
        }
    }
}
