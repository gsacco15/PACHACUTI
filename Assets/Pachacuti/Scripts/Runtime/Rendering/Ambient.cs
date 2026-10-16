using UnityEngine;
using UnityEngine.Rendering;

namespace Pachacuti
{
    /// <summary>
    /// Flat ambient fill that looks the same in the Editor and in builds. URP lights everything from
    /// the ambient probe, and with no baked lighting a player does not reliably rebuild that probe
    /// when RenderSettings.ambientLight changes at runtime, so the probe is set directly.
    /// </summary>
    public static class Ambient
    {
        /// <param name="color">sRGB, as typed in a colour picker.</param>
        public static void Set(Color color)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = color;
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color);
            RenderSettings.ambientProbe = probe;
        }
    }
}
