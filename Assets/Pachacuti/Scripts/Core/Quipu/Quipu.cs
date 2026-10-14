using System;
using System.Collections.Generic;

namespace Pachacuti.Core
{
    public enum KnotType
    {
        None,         // a gap: zero
        Single,       // overhand knot; tens/hundreds/thousands use one per unit of the digit
        Long,         // units digits 2–9: one long knot with that many turns
        FigureEight,  // units digit 1
    }

    /// <summary>
    /// One digit position on a pendant cord. Position 0 is the units, nearest the
    /// bottom; tens above it, then hundreds, then thousands (pitch slice §7,
    /// "Historical basis").
    /// </summary>
    public readonly struct KnotCluster
    {
        public readonly int Position;
        public readonly int Digit;

        public KnotCluster(int position, int digit)
        {
            if (position < 0) throw new ArgumentOutOfRangeException(nameof(position));
            if (digit < 0 || digit > 9) throw new ArgumentOutOfRangeException(nameof(digit));
            Position = position;
            Digit = digit;
        }

        public bool IsGap => Digit == 0;

        public KnotType Type
        {
            get
            {
                if (Digit == 0) return KnotType.None;
                if (Position > 0) return KnotType.Single;
                return Digit == 1 ? KnotType.FigureEight : KnotType.Long;
            }
        }

        /// <summary>Physical knots tied in this cluster.</summary>
        public int KnotCount => Digit == 0 ? 0 : (Position == 0 ? 1 : Digit);

        /// <summary>Turns in the long knot (units 2–9 only).</summary>
        public int Turns => Type == KnotType.Long ? Digit : 0;

        /// <summary>Read a digit back from what is physically on the cord.</summary>
        public static int ReadDigit(KnotType type, int knotCount, int turns)
        {
            switch (type)
            {
                case KnotType.None: return 0;
                case KnotType.FigureEight: return 1;
                case KnotType.Long: return turns;
                case KnotType.Single: return knotCount;
                default: throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }

    /// <summary>A pendant cord holding one number (pitch slice §7).</summary>
    public sealed class Pendant
    {
        readonly KnotCluster[] _clusters;

        Pendant(int value, KnotCluster[] clusters)
        {
            Value = value;
            _clusters = clusters;
        }

        public int Value { get; }

        /// <summary>Clusters by position, index 0 = units (bottom).</summary>
        public IReadOnlyList<KnotCluster> Clusters => _clusters;
        public int PositionCount => _clusters.Length;

        /// <param name="minPositions">Show at least this many positions (gaps included), so a cord can visibly hold zeros.</param>
        public static Pendant FromValue(int value, int minPositions = 1)
        {
            if (value < 0 || value > 9999) throw new ArgumentOutOfRangeException(nameof(value), "slice cords hold 0..9999");
            int positions = Math.Max(minPositions, Math.Max(1, value.ToString().Length));
            var c = new KnotCluster[positions];
            int v = value;
            for (int p = 0; p < positions; p++)
            {
                c[p] = new KnotCluster(p, v % 10);
                v /= 10;
            }
            return new Pendant(value, c);
        }

        /// <summary>Decode a number from clusters exactly as a reader would: by position and knot type.</summary>
        public static int Read(IReadOnlyList<KnotCluster> clusters)
        {
            int value = 0, scale = 1;
            for (int p = 0; p < clusters.Count; p++)
            {
                var k = clusters[p];
                value += KnotCluster.ReadDigit(k.Type, k.KnotCount, k.Turns) * scale;
                scale *= 10;
            }
            return value;
        }

        public static string PositionName(int position)
        {
            switch (position)
            {
                case 0: return "units";
                case 1: return "tens";
                case 2: return "hundreds";
                case 3: return "thousands";
                default: return "10^" + position;
            }
        }
    }
}
