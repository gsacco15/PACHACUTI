using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    public enum ControlPrompt
    {
        None,
        Climb,   // 0:30 — the context key (climb / jump / drop)
        LookUp,  // 12:30 — on the Coricancha roof
    }

    /// <summary>
    /// Everything drawn on screen. §10: "No UI chrome ever. Subtitles bottom-center,
    /// 2 lines max, serif." §12 UI: subtitles, two control prompts (icon + word),
    /// the quipu dial, the sky cursor. Nothing else. IMGUI is plenty for that and
    /// costs one draw pass; swap for UI Toolkit when real UI art arrives.
    /// </summary>
    public sealed class Overlay : MonoBehaviour
    {
        struct Subtitle { public string Text; public float Until; }
        public struct Marker { public Vector3 Screen; public string Text; public bool Highlight; public float Size; }

        readonly Queue<Subtitle> _queue = new Queue<Subtitle>();
        Subtitle? _current;
        Font _serif, _serifItalic;
        GUIStyle _subStyle, _promptStyle, _keyStyle, _titleStyle, _titleLineStyle, _markerStyle, _debugStyle, _beginStyle;
        Texture2D _white, _dot;
        float _fade = 1f, _fadeFrom = 1f, _fadeTo = 1f, _fadeT, _fadeDuration;
        ControlPrompt _prompt, _shownPrompt;
        float _promptAlpha;
        bool _titleVisible;
        float _titleAlpha, _titleLineAlpha;

        public bool UsingGamepad;
        public bool ShowBeginScreen;
        public bool ShowDebug;
        public string DebugText = "";
        public readonly List<Marker> Markers = new List<Marker>();
        public Vector2? SkyCursor;           // normalized screen position (0..1, y up)
        public float SkyCursorPulse;

        public static Overlay Create(Transform parent)
        {
            var go = new GameObject("Overlay");
            go.transform.SetParent(parent, false);
            return go.AddComponent<Overlay>();
        }

        void Awake()
        {
            _serif = Resources.Load<Font>("Pachacuti/Fonts/Spectral-Regular");
            _serifItalic = Resources.Load<Font>("Pachacuti/Fonts/Spectral-Italic");
            _white = Texture2D.whiteTexture;
            _dot = SoftDot(64);
        }

        public float FadeAlpha => _fade;

        /// <summary>Fade the screen to <paramref name="alpha"/> (1 = black) over seconds; 0 s cuts.</summary>
        public void FadeTo(float alpha, float seconds)
        {
            _fadeFrom = _fade;
            _fadeTo = alpha;
            _fadeT = 0f;
            _fadeDuration = Mathf.Max(0f, seconds);
            if (_fadeDuration <= 0f) _fade = alpha;
        }

        public void Say(ScriptLine line) => Say(line.Text, line.Duration);

        public void Say(string text, float seconds)
        {
            _queue.Enqueue(new Subtitle { Text = text, Until = seconds });
        }

        public void ClearSubtitles()
        {
            _queue.Clear();
            _current = null;
        }

        public bool SubtitleShowing => _current.HasValue || _queue.Count > 0;

        public void ShowPrompt(ControlPrompt p)
        {
            _prompt = p;
            if (p != ControlPrompt.None) _shownPrompt = p; // keep drawing the last one while it fades out
        }
        public void HidePrompt() => _prompt = ControlPrompt.None;

        public void ShowTitle(bool show)
        {
            _titleVisible = show;
            if (!show) { _titleAlpha = 0f; _titleLineAlpha = 0f; }
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (_fadeDuration > 0f && _fadeT < _fadeDuration)
            {
                _fadeT += dt;
                _fade = Mathf.Lerp(_fadeFrom, _fadeTo, Mathf.SmoothStep(0f, 1f, _fadeT / _fadeDuration));
            }
            if (_current.HasValue)
            {
                var c = _current.Value;
                c.Until -= dt;
                _current = c.Until > 0f ? c : (Subtitle?)null;
            }
            if (!_current.HasValue && _queue.Count > 0) _current = _queue.Dequeue();
            _promptAlpha = Mathf.MoveTowards(_promptAlpha, _prompt == ControlPrompt.None ? 0f : 1f, dt * 2f);
            if (_titleVisible)
            {
                _titleAlpha = Mathf.MoveTowards(_titleAlpha, 1f, dt / 1.5f);
                if (_titleAlpha >= 1f) _titleLineAlpha = Mathf.MoveTowards(_titleLineAlpha, 1f, dt / 2f);
            }
        }

        void EnsureStyles()
        {
            int h = Screen.height;
            if (_subStyle != null && _subStyle.fontSize == Mathf.RoundToInt(h * 0.034f)) return;
            _subStyle = new GUIStyle { font = _serif, fontSize = Mathf.RoundToInt(h * 0.034f), alignment = TextAnchor.LowerCenter, wordWrap = true, richText = false };
            _subStyle.normal.textColor = new Color(0.93f, 0.9f, 0.84f);
            _promptStyle = new GUIStyle(_subStyle) { fontSize = Mathf.RoundToInt(h * 0.026f), alignment = TextAnchor.MiddleLeft, wordWrap = false };
            _keyStyle = new GUIStyle(_promptStyle) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(h * 0.02f) };
            _titleStyle = new GUIStyle(_subStyle) { fontSize = Mathf.RoundToInt(h * 0.11f), alignment = TextAnchor.MiddleCenter };
            _titleLineStyle = new GUIStyle(_subStyle) { font = _serifItalic != null ? _serifItalic : _serif, fontSize = Mathf.RoundToInt(h * 0.034f), alignment = TextAnchor.MiddleCenter };
            _markerStyle = new GUIStyle(_subStyle) { fontSize = Mathf.RoundToInt(h * 0.03f), alignment = TextAnchor.MiddleCenter, wordWrap = false };
            _beginStyle = new GUIStyle(_titleLineStyle) { fontSize = Mathf.RoundToInt(h * 0.024f) };
            _debugStyle = new GUIStyle { fontSize = Mathf.Max(11, Mathf.RoundToInt(h * 0.015f)), alignment = TextAnchor.UpperLeft, wordWrap = false };
            _debugStyle.normal.textColor = new Color(0.6f, 1f, 0.6f);
        }

        void OnGUI()
        {
            EnsureStyles();
            float w = Screen.width, h = Screen.height;

            DrawMarkers();
            DrawSkyCursor(w, h);

            if (_fade > 0.001f)
            {
                GUI.color = new Color(0, 0, 0, _fade);
                GUI.DrawTexture(new Rect(0, 0, w, h), _white);
                GUI.color = Color.white;
            }

            if (_current.HasValue)
            {
                // 2 lines max: the box is wide enough for that at this font size.
                var r = new Rect(w * 0.15f, h * 0.8f, w * 0.7f, h * 0.14f);
                Shadowed(r, _current.Value.Text, _subStyle, 1f);
            }

            if (_promptAlpha > 0.01f) DrawPrompt(w, h);

            if (_titleAlpha > 0f)
            {
                Shadowed(new Rect(0, h * 0.36f, w, h * 0.16f), SliceScript.TitleWord, _titleStyle, _titleAlpha);
                if (_titleLineAlpha > 0f)
                    Shadowed(new Rect(0, h * 0.53f, w, h * 0.06f), SliceScript.TitleLine.Text, _titleLineStyle, _titleLineAlpha);
            }

            if (ShowBeginScreen)
            {
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(0, 0, w, h), _white);
                GUI.color = Color.white;
                // Browsers need a click before audio and pointer lock work.
                Shadowed(new Rect(0, h * 0.47f, w, h * 0.06f), "click", _beginStyle, 0.55f + 0.2f * Mathf.Sin(Time.unscaledTime * 2f));
            }

            if (ShowDebug && !string.IsNullOrEmpty(DebugText))
            {
                GUI.color = new Color(0, 0, 0, 0.6f);
                GUI.DrawTexture(new Rect(8, 8, w * 0.36f, h * 0.5f), _white);
                GUI.color = Color.white;
                GUI.Label(new Rect(14, 12, w * 0.35f, h * 0.5f), DebugText, _debugStyle);
            }
        }

        void DrawPrompt(float w, float h)
        {
            bool climb = _shownPrompt == ControlPrompt.Climb;
            string key = climb ? (UsingGamepad ? "A" : "SPACE") : (UsingGamepad ? "R ↑" : "MOUSE ↑");
            string word = climb ? "climb" : "look up";
            float kh = h * 0.045f;
            float kw = Mathf.Max(kh, key.Length * h * 0.017f + h * 0.02f);
            float x = w * 0.5f - (kw + h * 0.12f) * 0.5f, y = h * 0.66f;
            GUI.color = new Color(1, 1, 1, 0.18f * _promptAlpha);
            GUI.DrawTexture(new Rect(x, y, kw, kh), _white);
            GUI.color = new Color(1, 1, 1, 0.55f * _promptAlpha);
            GUI.DrawTexture(new Rect(x, y + kh - 2, kw, 2), _white);
            GUI.color = Color.white;
            Shadowed(new Rect(x, y, kw, kh), key, _keyStyle, _promptAlpha);
            Shadowed(new Rect(x + kw + h * 0.015f, y, h * 0.3f, kh), word, _promptStyle, _promptAlpha);
        }

        void DrawMarkers()
        {
            foreach (var m in Markers)
            {
                if (m.Screen.z <= 0f) continue;
                var p = new Vector2(m.Screen.x, Screen.height - m.Screen.y);
                float s = Screen.height * 0.05f * (m.Size <= 0f ? 1f : m.Size);
                var style = _markerStyle;
                var prev = style.fontSize;
                style.fontSize = Mathf.RoundToInt(prev * (m.Size <= 0f ? 1f : m.Size));
                var c = m.Highlight ? new Color(1f, 0.85f, 0.45f) : new Color(0.93f, 0.9f, 0.84f, 0.85f);
                var old = style.normal.textColor;
                style.normal.textColor = c;
                Shadowed(new Rect(p.x - s, p.y - s * 0.5f, s * 2f, s), m.Text, style, 1f);
                style.normal.textColor = old;
                style.fontSize = prev;
            }
        }

        void DrawSkyCursor(float w, float h)
        {
            if (!SkyCursor.HasValue) return;
            var c = SkyCursor.Value;
            float size = h * (0.035f + 0.01f * SkyCursorPulse);
            var r = new Rect(c.x * w - size * 0.5f, (1f - c.y) * h - size * 0.5f, size, size);
            GUI.color = new Color(1f, 0.92f, 0.7f, 0.55f + 0.35f * SkyCursorPulse);
            GUI.DrawTexture(r, _dot);
            GUI.color = Color.white;
        }

        static void Shadowed(Rect r, string text, GUIStyle style, float alpha)
        {
            var col = style.normal.textColor;
            style.normal.textColor = new Color(0, 0, 0, 0.75f * alpha);
            GUI.Label(new Rect(r.x + 2, r.y + 2, r.width, r.height), text, style);
            style.normal.textColor = new Color(col.r, col.g, col.b, col.a * alpha);
            GUI.Label(r, text, style);
            style.normal.textColor = col;
        }

        static Texture2D SoftDot(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "SoftDot" };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a;
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            t.SetPixels32(px);
            t.Apply();
            return t;
        }
    }
}
