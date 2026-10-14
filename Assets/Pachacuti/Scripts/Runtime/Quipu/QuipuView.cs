using System.Collections.Generic;
using Pachacuti.Core;
using UnityEngine;

namespace Pachacuti
{
    /// <summary>
    /// The quipu screen (pitch slice §7 UI): a full-screen 3D cord rendered in the
    /// scene so the firelight moves on it; hover/stick highlights a cluster; a small
    /// numeral appears beside it only after the player commits a guess; a dial of
    /// digits per position (number keys, arrows, d-pad); confirm with interact.
    /// No timer, no score.
    /// </summary>
    public sealed class QuipuView : MonoBehaviour
    {
        const float PendantSpacing = 0.11f, PendantLength = 0.5f, ClusterStep = 0.105f, UnitsY = -0.43f;

        sealed class PendantVisual
        {
            public Transform Root;
            public readonly List<Transform>[] Clusters = new List<Transform>[4];
            public readonly Vector3[] ClusterCenter = new Vector3[4];
            public int Positions;
        }

        readonly List<PendantVisual> _pendants = new List<PendantVisual>();
        Material _cotton, _cottonHot, _knot, _knotHot;
        Transform _hand;
        Vector3 _handRest;
        int _active;
        float _tapT = -1f;
        int _tapPosition;
        Vector2 _cursor;   // screen-space degrees offset
        public int Hover { get; private set; } = -1;
        public Vector3 CameraPosition { get; private set; }
        public Vector3 LookAt { get; private set; }

        public static QuipuView Create(Transform parent, Vector3 anchor, Vector3 towardViewer)
        {
            var go = new GameObject("Quipu");
            go.transform.SetParent(parent, false);
            go.transform.position = anchor;
            go.transform.rotation = Quaternion.LookRotation(-towardViewer.normalized, Vector3.up);
            var q = go.AddComponent<QuipuView>();
            q._cotton = MaterialKit.Lit(new Color(0.78f, 0.7f, 0.55f), Surface.Plain, 0.05f);
            q._cottonHot = MaterialKit.Lit(new Color(1f, 0.9f, 0.7f), Surface.Plain, 0.1f);
            q._knot = MaterialKit.Lit(new Color(0.62f, 0.52f, 0.38f), Surface.Plain, 0.05f);
            q._knotHot = MaterialKit.Lit(new Color(1f, 0.78f, 0.45f), Surface.Plain, 0.2f);
            q.CameraPosition = anchor + towardViewer.normalized * 0.95f + Vector3.down * 0.18f;
            q.LookAt = anchor + Vector3.down * 0.22f;
            // Ninan's hand.
            q._hand = Greybox.Primitive(go.transform, PrimitiveType.Sphere, "Ninan's hand", Vector3.zero, new Vector3(0.06f, 0.045f, 0.09f),
                                        MaterialKit.Lit(new Color(0.35f, 0.24f, 0.17f))).transform;
            q._handRest = anchor + go.transform.right * 0.32f + Vector3.down * 0.05f + towardViewer.normalized * 0.05f;
            q._hand.position = q._handRest;
            go.SetActive(false);
            return q;
        }

        /// <summary>Build the main cord and its pendants (pitch slice §7 "The cords").</summary>
        public void Build(IReadOnlyList<int> values)
        {
            foreach (var p in _pendants) Destroy(p.Root.gameObject);
            _pendants.Clear();
            var t = transform;
            float width = (values.Count + 1) * PendantSpacing;
            var main = Greybox.Primitive(t, PrimitiveType.Cylinder, "Main cord", Vector3.zero, new Vector3(0.012f, width * 0.5f + 0.1f, 0.012f), _cotton);
            main.transform.localPosition = Vector3.zero;
            main.transform.localRotation = Quaternion.Euler(0, 0, 90f);
            for (int i = 0; i < values.Count; i++)
            {
                var pv = new PendantVisual { Root = new GameObject("Pendant " + (i + 1)).transform };
                pv.Root.SetParent(t, false);
                pv.Root.localPosition = new Vector3(-width * 0.5f + (i + 1) * PendantSpacing, 0, 0);
                var cord = Greybox.Primitive(pv.Root, PrimitiveType.Cylinder, "Cord", Vector3.zero, new Vector3(0.008f, PendantLength * 0.5f, 0.008f), _cotton);
                cord.transform.localPosition = new Vector3(0, -PendantLength * 0.5f, 0);
                var pendant = Pendant.FromValue(values[i]);
                pv.Positions = pendant.PositionCount;
                for (int p = 0; p < 4; p++)
                {
                    pv.Clusters[p] = new List<Transform>();
                    pv.ClusterCenter[p] = new Vector3(0, UnitsY + p * ClusterStep, 0);
                }
                foreach (var c in pendant.Clusters) BuildCluster(pv, c);
                _pendants.Add(pv);
            }
            _hand.SetAsLastSibling();
        }

        void BuildCluster(PendantVisual pv, KnotCluster c)
        {
            Vector3 center = pv.ClusterCenter[c.Position];
            switch (c.Type)
            {
                case KnotType.Single:
                    for (int k = 0; k < c.KnotCount; k++)
                        pv.Clusters[c.Position].Add(Knot(pv.Root, center + Vector3.up * ((k - (c.KnotCount - 1) * 0.5f) * 0.019f), new Vector3(0.022f, 0.016f, 0.022f)));
                    break;
                case KnotType.Long:
                    // A long knot: one elongated knot with visible turns.
                    float len = 0.012f * c.Turns + 0.01f;
                    pv.Clusters[c.Position].Add(Knot(pv.Root, center, new Vector3(0.016f, len * 0.5f, 0.016f), PrimitiveType.Cylinder));
                    for (int k = 0; k < c.Turns; k++)
                        pv.Clusters[c.Position].Add(Knot(pv.Root, center + Vector3.up * ((k - (c.Turns - 1) * 0.5f) * 0.012f), new Vector3(0.022f, 0.003f, 0.022f), PrimitiveType.Cylinder));
                    break;
                case KnotType.FigureEight:
                    pv.Clusters[c.Position].Add(Knot(pv.Root, center + new Vector3(0.006f, 0.008f, 0), new Vector3(0.018f, 0.016f, 0.018f)));
                    pv.Clusters[c.Position].Add(Knot(pv.Root, center + new Vector3(-0.006f, -0.008f, 0), new Vector3(0.018f, 0.016f, 0.018f)));
                    break;
            }
        }

        Transform Knot(Transform parent, Vector3 localPos, Vector3 scale, PrimitiveType type = PrimitiveType.Sphere)
        {
            var k = Greybox.Primitive(parent, type, "Knot", Vector3.zero, scale, _knot).transform;
            k.localPosition = localPos;
            return k;
        }

        public void SetActivePendant(int index)
        {
            _active = index;
            Hover = -1;
            _cursor = Vector2.zero;
            for (int i = 0; i < _pendants.Count; i++)
                foreach (var r in _pendants[i].Root.GetComponentsInChildren<MeshRenderer>())
                    if (r.name == "Cord") r.sharedMaterial = i == index ? _cottonHot : _cotton;
        }

        public Vector3 ClusterWorld(int pendant, int position) =>
            _pendants[pendant].Root.TransformPoint(_pendants[pendant].ClusterCenter[Mathf.Clamp(position, 0, 3)]);

        /// <summary>Ninan taps the knot the player miscounted (§7).</summary>
        public void Tap(int position)
        {
            _tapT = 0f;
            _tapPosition = position;
        }

        /// <summary>Ninan's hand points at a pendant.</summary>
        public void Point(int pendant) => _handTarget = pendant >= 0 ? ClusterWorld(pendant, 1) + transform.forward * -0.04f + Vector3.right * 0.03f : (Vector3?)null;
        Vector3? _handTarget;

        /// <summary>Handle hover and the digit dial. Returns true on confirm (interact).</summary>
        public bool Tick(in InputFrame input, QuipuLesson lesson, Camera cam, Overlay overlay, float dt)
        {
            if (_active < 0 || _active >= _pendants.Count || lesson.IsComplete) return false;
            var pv = _pendants[_active];

            // Hover: mouse moves a soft cursor over the clusters; up/down arrows or d-pad step it.
            _cursor += input.Cursor * 0.6f;
            int byCursor = -1;
            if (input.Cursor.sqrMagnitude > 0.0001f)
            {
                Vector2 c = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f) + _cursor * (Screen.height / 40f);
                float best = Screen.height * 0.06f;
                for (int p = 0; p < pv.Positions; p++)
                {
                    Vector3 sp = cam.WorldToScreenPoint(ClusterWorld(_active, p));
                    float d = Vector2.Distance(new Vector2(sp.x, sp.y), c);
                    if (d < best) { best = d; byCursor = p; }
                }
                _cursor = Vector2.ClampMagnitude(_cursor, 16f);
            }
            int before = Hover;
            if (byCursor >= 0) Hover = byCursor;
            if (input.DialUp) Hover = Hover < 0 ? pv.Positions - 1 : Mathf.Min(pv.Positions - 1, Hover + 1);
            if (input.DialDown) Hover = Hover < 0 ? 0 : Mathf.Max(0, Hover - 1);
            bool dialing = input.Digit >= 0 || input.DialLeft || input.DialRight;
            if (Hover < 0 && dialing) Hover = pv.Positions - 1; // typing first: start at the top cluster
            if (Hover >= pv.Positions) Hover = pv.Positions - 1;

            // Ninan names a position the first time the player moves to it (lesson cord only).
            if (Hover >= 0 && Hover != before && lesson.TryHoverHint(Hover, out var hint)) overlay.Say(hint);
            if (Hover < 0) { AnimateHand(dt); overlay.Markers.Clear(); return input.Interact.Pressed; }

            // The dial.
            if (input.Digit >= 0) lesson.SetGuess(Hover, input.Digit);
            if (input.DialRight) lesson.SetGuess(Hover, Mathf.Min(9, Mathf.Max(0, lesson.GetGuess(Hover)) + (lesson.IsCommitted(Hover) ? 1 : 0)));
            if (input.DialLeft) lesson.SetGuess(Hover, Mathf.Max(0, lesson.GetGuess(Hover) - 1));

            // Highlight + numerals beside committed clusters only.
            for (int p = 0; p < 4; p++)
                foreach (var k in pv.Clusters[p]) k.GetComponent<MeshRenderer>().sharedMaterial = p == Hover ? _knotHot : _knot;
            overlay.Markers.Clear();
            for (int p = 0; p < pv.Positions; p++)
            {
                Vector3 world = ClusterWorld(_active, p) + transform.right * -0.06f;
                Vector3 sp = cam.WorldToScreenPoint(world);
                if (lesson.IsCommitted(p))
                    overlay.Markers.Add(new Overlay.Marker { Screen = sp, Text = lesson.GetGuess(p).ToString(), Highlight = p == Hover, Size = 0.8f });
                else if (p == Hover)
                    overlay.Markers.Add(new Overlay.Marker { Screen = sp, Text = "·", Highlight = true, Size = 0.8f });
            }

            AnimateHand(dt);
            return input.Interact.Pressed;
        }

        void AnimateHand(float dt)
        {
            Vector3 target = _handTarget ?? _handRest;
            if (_tapT >= 0f)
            {
                _tapT += dt;
                Vector3 knot = ClusterWorld(_active, _tapPosition) + transform.forward * -0.03f;
                target = knot + Vector3.up * (Mathf.Abs(Mathf.Sin(_tapT * 9f)) * 0.02f);
                if (_tapT > 1.4f) _tapT = -1f;
            }
            _hand.position = Vector3.Lerp(_hand.position, target, 1f - Mathf.Exp(-dt * 8f));
        }

        public void ClearMarkers(Overlay overlay) => overlay.Markers.Clear();
    }
}
