namespace Pachacuti.Core
{
    public enum QuipuVerdict
    {
        Incomplete,  // nothing to judge (lesson finished)
        Correct,
        Wrong,       // Ninan taps the miscounted knot: "That knot. Again."
        Revealed,    // third wrong try: Ninan says the number and moves on — never a fail state
    }

    public readonly struct QuipuConfirmResult
    {
        public readonly QuipuVerdict Verdict;
        public readonly int WrongPosition;   // lowest miscounted position, or -1
        public readonly int Tries;

        public QuipuConfirmResult(QuipuVerdict verdict, int wrongPosition, int tries)
        {
            Verdict = verdict;
            WrongPosition = wrongPosition;
            Tries = tries;
        }
    }

    /// <summary>
    /// Beat 3 lesson flow (pitch slice §7): pendant 1 (340) with position hints,
    /// pendant 2 (1,200, the levy that becomes the army in Beat 4) with no help,
    /// pendant 3 with no knots at all (zero: the men of your father's house).
    /// No timer, no score. At most three tries per cord.
    /// </summary>
    public sealed class QuipuLesson
    {
        /// <summary>
        /// The slice's cords, by value. NOTE: the §7 cord table lists pendant 2 as
        /// "1 single over a thousands cluster of 1 / 2 singles / gap", which does not
        /// add up; the value 1,200 is authoritative (thousands 1, hundreds 2, tens 0, units 0).
        /// </summary>
        public static readonly int[] SliceValues = { 340, 1200, 0 };

        readonly int[] _values;
        readonly bool[] _hoverHintSpoken = new bool[4];
        int[] _guesses;

        public QuipuLesson() : this(SliceValues) { }

        public QuipuLesson(int[] values)
        {
            _values = values;
            Load(0);
        }

        public int PendantIndex { get; private set; }
        public int PendantCount => _values.Length;
        public Pendant Current { get; private set; }
        public int Tries { get; private set; }
        public bool IsComplete => PendantIndex >= _values.Length;
        /// <summary>Only the first (lesson) cord gets Ninan's position names on hover.</summary>
        public bool HintsEnabled => PendantIndex == 0;

        public int GetGuess(int position) => position >= 0 && position < _guesses.Length ? _guesses[position] : -1;
        public bool IsCommitted(int position) => GetGuess(position) >= 0;

        public void SetGuess(int position, int digit)
        {
            if (IsComplete || position < 0 || position >= _guesses.Length) return;
            if (digit < 0) digit = 0;
            if (digit > 9) digit = 9;
            _guesses[position] = digit;
        }

        public void ClearGuess(int position)
        {
            if (!IsComplete && position >= 0 && position < _guesses.Length) _guesses[position] = -1;
        }

        /// <summary>
        /// The player hovered a cluster. Returns the line Ninan says ("Units." /
        /// "Tens." / "Hundreds."), the first time only and only on the lesson cord.
        /// </summary>
        public bool TryHoverHint(int position, out ScriptLine line)
        {
            line = default;
            if (IsComplete || !HintsEnabled || position < 0 || position >= _hoverHintSpoken.Length) return false;
            if (_hoverHintSpoken[position]) return false;
            _hoverHintSpoken[position] = true;
            line = SliceScript.PositionName(position);
            return true;
        }

        /// <summary>The player's committed reading; uncommitted positions read as gaps (zero).</summary>
        public int GuessedValue()
        {
            int v = 0, scale = 1;
            for (int p = 0; p < _guesses.Length; p++)
            {
                v += (_guesses[p] < 0 ? 0 : _guesses[p]) * scale;
                scale *= 10;
            }
            return v;
        }

        public QuipuConfirmResult Confirm()
        {
            if (IsComplete) return new QuipuConfirmResult(QuipuVerdict.Incomplete, -1, Tries);
            Tries++;
            int wrong = -1;
            for (int p = 0; p < Current.PositionCount; p++)
            {
                int g = _guesses[p] < 0 ? 0 : _guesses[p];
                if (g != Current.Clusters[p].Digit) { wrong = p; break; }
            }
            if (wrong < 0) return new QuipuConfirmResult(QuipuVerdict.Correct, -1, Tries);
            // The empty cord is a gut-punch, not a test: one look and Ninan says it.
            bool lastChance = Tries >= Tuning.QuipuMaxTries || Current.Value == 0;
            return new QuipuConfirmResult(lastChance ? QuipuVerdict.Revealed : QuipuVerdict.Wrong, wrong, Tries);
        }

        /// <summary>Advance to the next pendant. Returns false when the lesson is over.</summary>
        public bool NextPendant()
        {
            Load(PendantIndex + 1);
            return !IsComplete;
        }

        void Load(int index)
        {
            PendantIndex = index;
            Tries = 0;
            if (index >= _values.Length)
            {
                Current = null;
                _guesses = new int[0];
                return;
            }
            Current = Pendant.FromValue(_values[index]);
            _guesses = new int[Current.PositionCount];
            for (int i = 0; i < _guesses.Length; i++) _guesses[i] = -1;
        }
    }
}
