using System.Collections.Generic;

namespace Pachacuti.Core
{
    public enum Speaker
    {
        None,       // on-screen subtitle with no speaker (cold open, title card)
        Woman,
        Tupa,
        Ninan,
        Soldier,
    }

    public readonly struct ScriptLine
    {
        public readonly string Id;
        public readonly Speaker Speaker;
        public readonly string Text;

        public ScriptLine(string id, Speaker speaker, string text)
        {
            Id = id;
            Speaker = speaker;
            Text = text;
        }

        /// <summary>How long the subtitle stays up: ~0.33 s per word, 1.6 s minimum, plus a beat.</summary>
        public float Duration
        {
            get
            {
                int words = 1;
                foreach (char c in Text) if (c == ' ') words++;
                float d = 0.9f + words * 0.33f;
                return d < 1.6f ? 1.6f : d;
            }
        }
    }

    /// <summary>
    /// Every line in the slice (pitch slice §11). English placeholder; Quechua VO
    /// is recorded before the publisher cut. Lines are looked up by id so audio
    /// can later be bound by the same ids.
    /// </summary>
    public static class SliceScript
    {
        public static readonly ScriptLine ColdOpen = new ScriptLine("cold_open", Speaker.None, "Quizquiz's men are in the city.");
        public static readonly ScriptLine Woman = new ScriptLine("woman_load", Speaker.Woman, "They are killing everyone of your father's house. Walk with the porters. Do not look up.");

        public static readonly ScriptLine[] SoldierBarks =
        {
            new ScriptLine("bark_load", Speaker.Soldier, "Load."),
            new ScriptLine("bark_keep_moving", Speaker.Soldier, "Keep moving."),
            new ScriptLine("bark_you_stop", Speaker.Soldier, "You. Stop."),
            new ScriptLine("bark_eyes_down", Speaker.Soldier, "Eyes down."),
            new ScriptLine("bark_whos_that", Speaker.Soldier, "Who's that?"),
            new ScriptLine("bark_here", Speaker.Soldier, "HERE! He's here!"),
        };
        public static ScriptLine BarkLoad => SoldierBarks[0];
        public static ScriptLine BarkKeepMoving => SoldierBarks[1];
        public static ScriptLine BarkStop => SoldierBarks[2];
        public static ScriptLine BarkEyesDown => SoldierBarks[3];
        public static ScriptLine BarkWho => SoldierBarks[4];
        public static ScriptLine BarkHere => SoldierBarks[5];

        public static readonly ScriptLine TupaCut = new ScriptLine("tupa_cut", Speaker.Tupa, "Manc—");
        public static readonly ScriptLine TupaFull = new ScriptLine("tupa_full", Speaker.Tupa, "Manco! Cousin, they said you were—");

        public static readonly ScriptLine NinanReadIt = new ScriptLine("ninan_read_it", Speaker.Ninan, "Maize. The storehouse at the north gate. Read it.");
        public static readonly ScriptLine NinanCount = new ScriptLine("ninan_count", Speaker.Ninan, "Bottom is one. Above is ten. Above is a hundred. Count.");
        public static readonly ScriptLine NinanUnits = new ScriptLine("ninan_units", Speaker.Ninan, "Units.");
        public static readonly ScriptLine NinanTens = new ScriptLine("ninan_tens", Speaker.Ninan, "Tens.");
        public static readonly ScriptLine NinanHundreds = new ScriptLine("ninan_hundreds", Speaker.Ninan, "Hundreds.");
        public static readonly ScriptLine NinanThousands = new ScriptLine("ninan_thousands", Speaker.Ninan, "Thousands.");
        public static readonly ScriptLine NinanWrong = new ScriptLine("ninan_wrong", Speaker.Ninan, "That knot. Again.");
        public static readonly ScriptLine NinanCord1Right = new ScriptLine("ninan_cord1_right", Speaker.Ninan, "Three hundred and forty maize. Enough for the valley for a month. They will burn it by morning.");
        public static readonly ScriptLine NinanCord2 = new ScriptLine("ninan_cord2", Speaker.Ninan, "Now that one. No help.");
        public static readonly ScriptLine NinanCord2Right = new ScriptLine("ninan_cord2_right", Speaker.Ninan, "Twelve hundred. The last men who answered your brother.");
        public static readonly ScriptLine NinanCord3 = new ScriptLine("ninan_cord3", Speaker.Ninan, "And that one is the men of your father's house.");
        public static readonly ScriptLine NinanSecondCord = new ScriptLine("ninan_second_cord", Speaker.Ninan, "Take this to the roof. The sky will read it before you can.");

        public static readonly ScriptLine NinanCordReads = new ScriptLine("ninan_fresh", Speaker.Ninan, "Fresh. They are at the bridge.");
        public static readonly ScriptLine NinanNorthRoad = new ScriptLine("ninan_north_road", Speaker.Ninan, "The north road.");
        public static readonly ScriptLine NinanTambo = new ScriptLine("ninan_tambo", Speaker.Ninan, "The tambo at Quispicanchi.");
        public static readonly ScriptLine NinanTwelveHundred = new ScriptLine("ninan_twelve_hundred", Speaker.Ninan, "Twelve hundred.");
        public static readonly ScriptLine NinanTheyWillEat = new ScriptLine("ninan_eat", Speaker.Ninan, "They will eat tonight.");
        public static readonly ScriptLine NinanTupa = new ScriptLine("ninan_tupa", Speaker.Ninan, "Tupa.");

        public const string TitleWord = "PACHACUTI";
        public static readonly ScriptLine TitleLine = new ScriptLine("title", Speaker.None, "Cusco, 1532. He was sixteen.");

        /// <summary>
        /// Ninan's position-name line for a digit position (0 = units). Pendant 2
        /// has a thousands cluster; the doc only scripts units/tens/hundreds, and
        /// pendant 2 is read "no help", so thousands is never actually spoken in
        /// the scripted flow.
        /// </summary>
        public static ScriptLine PositionName(int position)
        {
            switch (position)
            {
                case 0: return NinanUnits;
                case 1: return NinanTens;
                case 2: return NinanHundreds;
                default: return NinanThousands;
            }
        }

        public static IEnumerable<ScriptLine> All()
        {
            yield return ColdOpen;
            yield return Woman;
            foreach (var b in SoldierBarks) yield return b;
            yield return TupaCut;
            yield return TupaFull;
            yield return NinanReadIt;
            yield return NinanCount;
            yield return NinanUnits;
            yield return NinanTens;
            yield return NinanHundreds;
            yield return NinanWrong;
            yield return NinanCord1Right;
            yield return NinanCord2;
            yield return NinanCord2Right;
            yield return NinanCord3;
            yield return NinanSecondCord;
            yield return NinanCordReads;
            yield return NinanNorthRoad;
            yield return NinanTambo;
            yield return NinanTwelveHundred;
            yield return NinanTheyWillEat;
            yield return NinanTupa;
            yield return TitleLine;
        }
    }
}
