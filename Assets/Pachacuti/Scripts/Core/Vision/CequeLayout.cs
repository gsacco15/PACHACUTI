using System;
using System.Collections.Generic;

namespace Pachacuti.Core
{
    public enum Suyu
    {
        Chinchaysuyu, // north-west
        Antisuyu,     // north-east (Qenqo lies here)
        Collasuyu,    // south-east
        Cuntisuyu,    // south-west
    }

    public readonly struct CequeLine
    {
        public readonly int Index;
        public readonly Suyu Suyu;
        public readonly double BearingDeg;   // from north toward east, from the Coricancha
        public readonly double LengthM;
        public readonly bool ThroughQenqo;

        public CequeLine(int index, Suyu suyu, double bearingDeg, double lengthM, bool throughQenqo)
        {
            Index = index;
            Suyu = suyu;
            BearingDeg = bearingDeg;
            LengthM = lengthM;
            ThroughQenqo = throughQenqo;
        }
    }

    /// <summary>
    /// The 41 ceque lines radiating from the Coricancha (GDD §2, §12). Counts per
    /// quarter follow Bernabé Cobo's 1653 list: Chinchaysuyu 9, Antisuyu 9,
    /// Collasuyu 9, Cuntisuyu 14. Bearings within a quarter are evenly spread with
    /// a deterministic jitter (the real lines bent and were not evenly spaced; this
    /// is a stand-in until an archaeology consultant supplies surveyed bearings).
    /// One Antisuyu line runs exactly through Qenqo, "under Manco's feet" (§9).
    /// </summary>
    public static class CequeLayout
    {
        // Real positions (WGS84): Coricancha −13.5199, −71.9752; Qenqo −13.5100, −71.9633.
        public const double CoricanchaLat = -13.5199, CoricanchaLon = -71.9752;
        public const double QenqoLat = -13.5100, QenqoLon = -71.9633;

        /// <summary>Approximate elevation drop from Qenqo (~3,580 m) down to the Coricancha (~3,400 m).</summary>
        public const double QenqoAboveCoricanchaM = 180.0;

        public static double BearingCoricanchaToQenqo => Bearing(CoricanchaLat, CoricanchaLon, QenqoLat, QenqoLon);
        public static double DistanceCoricanchaToQenqoM => Distance(CoricanchaLat, CoricanchaLon, QenqoLat, QenqoLon);

        public static IReadOnlyList<CequeLine> Generate(double lengthM = Tuning.CequeLineLength)
        {
            var lines = new List<CequeLine>(Tuning.CequeLineCount);
            AddQuarter(lines, Suyu.Chinchaysuyu, 9, 278, 352, lengthM, -1);
            AddQuarter(lines, Suyu.Antisuyu, 9, 8, 82, lengthM, BearingCoricanchaToQenqo);
            AddQuarter(lines, Suyu.Collasuyu, 9, 98, 172, lengthM, -1);
            AddQuarter(lines, Suyu.Cuntisuyu, 14, 186, 266, lengthM, -1);
            return lines;
        }

        static void AddQuarter(List<CequeLine> lines, Suyu suyu, int count, double from, double to, double length, double forced)
        {
            int forcedSlot = -1;
            if (forced >= 0) forcedSlot = (int)Math.Round((forced - from) / (to - from) * (count - 1));
            for (int i = 0; i < count; i++)
            {
                double t = count == 1 ? 0.5 : i / (double)(count - 1);
                double bearing = from + (to - from) * t + Jitter(lines.Count) * (to - from) / count * 0.35;
                bool through = i == forcedSlot;
                if (through) bearing = forced;
                // Lines were of different lengths, ending at their last huaca.
                double len = length * (0.75 + 0.25 * (0.5 + 0.5 * Jitter(lines.Count + 101)));
                if (through) len = Math.Max(len, DistanceCoricanchaToQenqoM + 400.0);
                lines.Add(new CequeLine(lines.Count, suyu, Astronomy.Normalize360(bearing), len, through));
            }
        }

        static double Jitter(int i)
        {
            unchecked
            {
                uint h = (uint)(i * 2654435761u + 0x9E3779B9u);
                h ^= h >> 15; h *= 2246822519u; h ^= h >> 13;
                return (h & 0xFFFF) / 32767.5 - 1.0; // −1..1
            }
        }

        /// <summary>Initial great-circle bearing in degrees from north toward east.</summary>
        public static double Bearing(double lat1, double lon1, double lat2, double lon2)
        {
            double p1 = Astronomy.Rad(lat1), p2 = Astronomy.Rad(lat2), dl = Astronomy.Rad(lon2 - lon1);
            double y = Math.Sin(dl) * Math.Cos(p2);
            double x = Math.Cos(p1) * Math.Sin(p2) - Math.Sin(p1) * Math.Cos(p2) * Math.Cos(dl);
            return Astronomy.Normalize360(Astronomy.Deg(Math.Atan2(y, x)));
        }

        /// <summary>Haversine distance in metres.</summary>
        public static double Distance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000.0;
            double p1 = Astronomy.Rad(lat1), p2 = Astronomy.Rad(lat2);
            double dp = p2 - p1, dl = Astronomy.Rad(lon2 - lon1);
            double a = Math.Sin(dp / 2) * Math.Sin(dp / 2) + Math.Cos(p1) * Math.Cos(p2) * Math.Sin(dl / 2) * Math.Sin(dl / 2);
            return 2 * R * Math.Asin(Math.Sqrt(a));
        }
    }
}
