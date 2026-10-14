// Compile-only stand-ins for the handful of package APIs the project uses
// (com.unity.inputsystem 1.20, com.unity.render-pipelines.core/universal 17.3).
// Every member here was copied from the package source at those versions; this
// file is NOT part of the Unity project and never ships. It lets
// `dotnet build tools/UnityCompileCheck` type-check the runtime and editor
// code in a container without a Unity licence.
#pragma warning disable CS0067, CS0649, CS1591
using System;

namespace UnityEngine.InputSystem
{
    namespace Controls
    {
        public abstract class InputControl<TValue> where TValue : struct
        {
            public TValue ReadValue() => default;
        }
        public class ButtonControl : InputControl<float>
        {
            public bool isPressed => false;
            public bool wasPressedThisFrame => false;
            public bool wasReleasedThisFrame => false;
        }
        public class KeyControl : ButtonControl { }
        public class Vector2Control : InputControl<Vector2> { }
        public class DeltaControl : Vector2Control { }
        public class StickControl : Vector2Control { }
        public class DpadControl : Vector2Control
        {
            public ButtonControl up { get; set; }
            public ButtonControl down { get; set; }
            public ButtonControl left { get; set; }
            public ButtonControl right { get; set; }
        }
    }

    public enum Key
    {
        None, Space, Enter, Tab, Backquote, Quote, Semicolon, Comma, Period, Slash, Backslash, LeftBracket, RightBracket,
        Minus, Equals, A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
        Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9, Digit0,
        LeftShift, RightShift, LeftAlt, RightAlt, AltGr = RightAlt, LeftCtrl, RightCtrl, LeftMeta, RightMeta,
        ContextMenu, Escape, LeftArrow, RightArrow, UpArrow, DownArrow, Backspace, PageDown, PageUp, Home, End, Insert, Delete,
        CapsLock, NumLock, PrintScreen, ScrollLock, Pause, NumpadEnter, NumpadDivide, NumpadMultiply, NumpadPlus, NumpadMinus,
        NumpadPeriod, NumpadEquals, Numpad0, Numpad1, Numpad2, Numpad3, Numpad4, Numpad5, Numpad6, Numpad7, Numpad8, Numpad9,
        F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    }

    public class InputDevice { }

    public class Keyboard : InputDevice
    {
        public static Keyboard current { get; private set; }
        public Controls.KeyControl this[Key key] => null;
    }

    public class Pointer : InputDevice
    {
        public Controls.Vector2Control position { get; protected set; }
        public Controls.DeltaControl delta { get; protected set; }
    }

    public class Mouse : Pointer
    {
        public static Mouse current { get; private set; }
        public Controls.DeltaControl scroll { get; protected set; }
        public Controls.ButtonControl leftButton { get; protected set; }
        public Controls.ButtonControl rightButton { get; protected set; }
    }

    public class Gamepad : InputDevice
    {
        public static Gamepad current { get; private set; }
        public Controls.ButtonControl buttonWest { get; protected set; }
        public Controls.ButtonControl buttonNorth { get; protected set; }
        public Controls.ButtonControl buttonSouth { get; protected set; }
        public Controls.ButtonControl buttonEast { get; protected set; }
        public Controls.ButtonControl leftStickButton { get; protected set; }
        public Controls.ButtonControl startButton { get; protected set; }
        public Controls.DpadControl dpad { get; protected set; }
        public Controls.ButtonControl leftShoulder { get; protected set; }
        public Controls.ButtonControl rightShoulder { get; protected set; }
        public Controls.StickControl leftStick { get; protected set; }
        public Controls.StickControl rightStick { get; protected set; }
        public Controls.ButtonControl leftTrigger { get; protected set; }
        public Controls.ButtonControl rightTrigger { get; protected set; }
    }
}

namespace UnityEngine.Rendering
{
    public abstract class VolumeParameter { }
    public class VolumeParameter<T> : VolumeParameter
    {
        public VolumeParameter() { }
        public VolumeParameter(T value, bool overrideState = false) { }
        public virtual T value { get; set; }
        public virtual bool overrideState { get; set; }
        public void Override(T x) { }
    }
    public class BoolParameter : VolumeParameter<bool> { public BoolParameter(bool value, bool overrideState = false) { } }
    public class FloatParameter : VolumeParameter<float> { public FloatParameter(float value, bool overrideState = false) { } }
    public class MinFloatParameter : FloatParameter { public MinFloatParameter(float value, float min, bool overrideState = false) : base(value, overrideState) { } }
    public class ClampedFloatParameter : FloatParameter { public ClampedFloatParameter(float value, float min, float max, bool overrideState = false) : base(value, overrideState) { } }
    public class ColorParameter : VolumeParameter<Color> { public ColorParameter(Color value, bool hdr, bool showAlpha, bool showEyeDropper, bool overrideState = false) { } }

    public partial class VolumeComponent : ScriptableObject
    {
        public bool active = true;
    }

    public sealed class VolumeProfile : ScriptableObject
    {
        public T Add<T>(bool overrides = false) where T : VolumeComponent => null;
    }

    public class Volume : MonoBehaviour
    {
        public bool isGlobal { get; set; }
        public float priority = 0f;
        public float weight = 1f;
        public VolumeProfile sharedProfile = null;
        public VolumeProfile profile { get; set; }
    }
}

namespace UnityEngine.Rendering.Universal
{
    public sealed class Bloom : VolumeComponent
    {
        public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0f);
        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);
        public ColorParameter tint = new ColorParameter(Color.white, false, false, true);
    }
    public sealed class Vignette : VolumeComponent
    {
        public ColorParameter color = new ColorParameter(Color.black, false, false, true);
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter smoothness = new ClampedFloatParameter(0.2f, 0.01f, 1f);
    }
    public sealed class FilmGrain : VolumeComponent
    {
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
        public ClampedFloatParameter response = new ClampedFloatParameter(0.8f, 0f, 1f);
    }
    public sealed class ColorAdjustments : VolumeComponent
    {
        public FloatParameter postExposure = new FloatParameter(0f);
        public ClampedFloatParameter contrast = new ClampedFloatParameter(0f, -100f, 100f);
        public ColorParameter colorFilter = new ColorParameter(Color.white, true, false, true);
        public ClampedFloatParameter hueShift = new ClampedFloatParameter(0f, -180f, 180f);
        public ClampedFloatParameter saturation = new ClampedFloatParameter(0f, -100f, 100f);
    }
    public enum TonemappingMode { None, Neutral, ACES }
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode>
    {
        public TonemappingModeParameter(TonemappingMode value, bool overrideState = false) { }
    }
    public sealed class Tonemapping : VolumeComponent
    {
        public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None);
    }
    public enum AntialiasingMode { None, FastApproximateAntialiasing, SubpixelMorphologicalAntiAliasing, TemporalAntiAliasing }
    public class UniversalAdditionalCameraData : MonoBehaviour
    {
        public bool renderPostProcessing { get; set; }
        public AntialiasingMode antialiasing { get; set; }
    }
    public static class CameraExtensions
    {
        public static UniversalAdditionalCameraData GetUniversalAdditionalCameraData(this Camera camera) => null;
    }
}

#if PACHACUTI_COMPILECHECK_TESTS
namespace UnityEngine.TestTools
{
    // com.unity.test-framework attributes (compile-only).
    [System.AttributeUsage(System.AttributeTargets.Method)] public class UnityTestAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Method)] public class UnitySetUpAttribute : System.Attribute { }
    [System.AttributeUsage(System.AttributeTargets.Method)] public class UnityTearDownAttribute : System.Attribute { }
}
#endif
