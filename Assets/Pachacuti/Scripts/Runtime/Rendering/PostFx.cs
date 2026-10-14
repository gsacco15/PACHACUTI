using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Pachacuti
{
    public enum PostRig
    {
        Night,   // Kay Pacha: desaturated, cold, grain, vignette; no bloom (§13 "bloom (vision only)")
        Vision,  // saturated, impossible; bloom on the gold
        Day,     // the Cajamarca square
    }

    /// <summary>
    /// Pre-built post stacks swapped by volume weight in a single frame. Pitch slice
    /// §9 VFX: "The cut is a material/post swap, not a load: two lighting rigs and two
    /// post stacks pre-built; the vision rig is a single uniform toggle. Target: zero
    /// frames of hitch."
    /// </summary>
    public sealed class PostFx : MonoBehaviour
    {
        Volume _night, _vision, _day;
        ColorAdjustments _visionColor;
        public PostRig Current { get; private set; }

        public static PostFx Create(Transform parent, Camera camera)
        {
            var go = new GameObject("PostFx");
            go.transform.SetParent(parent, false);
            var fx = go.AddComponent<PostFx>();
            fx.Build();
            var data = camera.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
            }
            fx.Set(PostRig.Night);
            return fx;
        }

        void Build()
        {
            _night = MakeVolume("Night", 10f, p =>
            {
                p.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                var c = p.Add<ColorAdjustments>(true);
                c.postExposure.Override(0.35f);
                c.contrast.Override(14f);
                c.saturation.Override(-30f);
                c.colorFilter.Override(new Color(0.86f, 0.92f, 1f));
                var v = p.Add<Vignette>(true);
                v.intensity.Override(0.38f);
                v.smoothness.Override(0.45f);
                var g = p.Add<FilmGrain>(true);
                g.intensity.Override(0.28f);
                g.response.Override(0.7f);
                p.Add<Bloom>(true).intensity.Override(0f);
            });
            _vision = MakeVolume("Vision", 20f, p =>
            {
                p.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                _visionColor = p.Add<ColorAdjustments>(true);
                _visionColor.postExposure.Override(0.6f);
                _visionColor.contrast.Override(22f);
                _visionColor.saturation.Override(40f);
                _visionColor.colorFilter.Override(new Color(1f, 0.95f, 1.05f));
                var b = p.Add<Bloom>(true);
                b.intensity.Override(1.6f);
                b.threshold.Override(0.85f);
                b.scatter.Override(0.72f);
                b.tint.Override(new Color(1f, 0.85f, 0.6f));
                var v = p.Add<Vignette>(true);
                v.intensity.Override(0.28f);
                v.smoothness.Override(0.5f);
                p.Add<FilmGrain>(true).intensity.Override(0.08f);
            });
            _day = MakeVolume("Day", 30f, p =>
            {
                p.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);
                var c = p.Add<ColorAdjustments>(true);
                c.postExposure.Override(0.2f);
                c.contrast.Override(10f);
                c.saturation.Override(-5f);
                p.Add<Vignette>(true).intensity.Override(0.2f);
                p.Add<Bloom>(true).intensity.Override(0.6f);
            });
        }

        Volume MakeVolume(string name, float priority, System.Action<VolumeProfile> fill)
        {
            var go = new GameObject("Volume " + name);
            go.transform.SetParent(transform, false);
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = priority;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.name = name;
            fill(profile);
            vol.sharedProfile = profile;
            vol.weight = 0f;
            return vol;
        }

        public void Set(PostRig rig)
        {
            Current = rig;
            _night.weight = 1f; // base layer stays under the others
            _vision.weight = rig == PostRig.Vision ? 1f : 0f;
            _day.weight = rig == PostRig.Day ? 1f : 0f;
        }

        /// <summary>Per-shot grading inside the vision (e.g. the time strobe's snapshots).</summary>
        public void VisionTint(Color filter, float exposure)
        {
            if (_visionColor == null) return;
            _visionColor.colorFilter.Override(filter);
            _visionColor.postExposure.Override(exposure);
        }
    }
}
