using System;
using System.Collections.Generic;

namespace Pachacuti.Core
{
    /// <summary>
    /// The slice's beats in play order (pitch slice §13: "cold → roofs → compound →
    /// street → yard → tunnel → roof → qenqo → title").
    /// </summary>
    public enum Beat
    {
        Boot,
        ColdOpen,
        Rooftops,   // Beat 1
        Compound,
        Street,     // Beat 2
        Yard,       // Beat 3
        Tunnel,
        Roof,       // Beat 4 (Coricancha climb + sky-map)
        Qenqo,      // Beat 5 (walk + vision)
        Title,
        Ended,
    }

    /// <summary>
    /// Owns beat transitions. Only forward moves are legal, plus restarting the
    /// Street after a capture. Each transition is timestamped for the playtest log.
    /// </summary>
    public sealed class BeatMachine
    {
        readonly Dictionary<Beat, double> _entered = new Dictionary<Beat, double>();
        readonly Dictionary<Beat, double> _durations = new Dictionary<Beat, double>();

        public Beat Current { get; private set; } = Beat.Boot;
        public double Clock { get; private set; }
        public double TimeInBeat => Clock - (_entered.TryGetValue(Current, out var t) ? t : 0.0);

        public event Action<Beat, Beat> Changed;

        public IReadOnlyDictionary<Beat, double> Durations => _durations;

        public void Tick(double dt) { if (dt > 0) Clock += dt; }

        public static bool IsLegal(Beat from, Beat to)
        {
            if (to == from) return from == Beat.Street; // restart after capture
            return (int)to == (int)from + 1;
        }

        public void Advance(Beat to)
        {
            if (!IsLegal(Current, to))
                throw new InvalidOperationException($"Illegal beat transition {Current} -> {to}");
            var from = Current;
            CloseCurrent();
            Current = to;
            _entered[to] = Clock;
            Changed?.Invoke(from, to);
        }

        /// <summary>Debug / playtest-facilitator skip: jump to any later beat, closing the ones in between.</summary>
        public void SkipTo(Beat to)
        {
            if ((int)to <= (int)Current) throw new InvalidOperationException($"Cannot skip backwards {Current} -> {to}");
            var from = Current;
            CloseCurrent();
            Current = to;
            _entered[to] = Clock;
            Changed?.Invoke(from, to);
        }

        void CloseCurrent()
        {
            if (Current == Beat.Boot || !_entered.TryGetValue(Current, out var start)) return;
            _durations.TryGetValue(Current, out var acc);
            _durations[Current] = acc + (Clock - start);
        }

        public static Beat Next(Beat b) => b >= Beat.Ended ? Beat.Ended : (Beat)((int)b + 1);
    }
}
