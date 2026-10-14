using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Pachacuti.Core
{
    /// <summary>
    /// Minimal JSON writer so Core stays dependency-free (no UnityEngine.JsonUtility,
    /// no Newtonsoft). Handles commas and string escaping; nothing more.
    /// </summary>
    public sealed class JsonWriter
    {
        readonly StringBuilder _sb = new StringBuilder();
        readonly Stack<bool> _first = new Stack<bool>();
        bool _afterKey;

        public void BeginObject() { Separator(); _sb.Append('{'); _first.Push(true); }
        public void EndObject() { _first.Pop(); _sb.Append('}'); }
        public void BeginArray() { Separator(); _sb.Append('['); _first.Push(true); }
        public void EndArray() { _first.Pop(); _sb.Append(']'); }

        public void Key(string key)
        {
            Separator();
            WriteString(key);
            _sb.Append(':');
            _afterKey = true;
        }

        public void Value(string v) { Separator(); if (v == null) _sb.Append("null"); else WriteString(v); }
        public void Value(bool v) { Separator(); _sb.Append(v ? "true" : "false"); }
        public void Value(int v) { Separator(); _sb.Append(v.ToString(CultureInfo.InvariantCulture)); }
        public void Value(double v)
        {
            Separator();
            if (double.IsNaN(v) || double.IsInfinity(v)) _sb.Append("null");
            else _sb.Append(System.Math.Round(v, 3).ToString("0.###", CultureInfo.InvariantCulture));
        }

        public void Field(string key, string v) { Key(key); Value(v); }
        public void Field(string key, bool v) { Key(key); Value(v); }
        public void Field(string key, int v) { Key(key); Value(v); }
        public void Field(string key, double v) { Key(key); Value(v); }

        public void Array(IEnumerable<int> values) { BeginArray(); foreach (var v in values) Value(v); EndArray(); }
        public void Array(IEnumerable<bool> values) { BeginArray(); foreach (var v in values) Value(v); EndArray(); }
        public void Array(IEnumerable<double> values) { BeginArray(); foreach (var v in values) Value(v); EndArray(); }

        public override string ToString() => _sb.ToString();

        void Separator()
        {
            if (_afterKey) { _afterKey = false; return; }
            if (_first.Count == 0) return;
            if (_first.Peek()) { _first.Pop(); _first.Push(false); }
            else _sb.Append(',');
        }

        void WriteString(string s)
        {
            _sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': _sb.Append("\\\""); break;
                    case '\\': _sb.Append("\\\\"); break;
                    case '\n': _sb.Append("\\n"); break;
                    case '\r': _sb.Append("\\r"); break;
                    case '\t': _sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) _sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else _sb.Append(c);
                        break;
                }
            }
            _sb.Append('"');
        }
    }
}
