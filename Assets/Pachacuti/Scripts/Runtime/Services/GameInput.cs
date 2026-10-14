using Pachacuti.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pachacuti
{
    /// <summary>One frame of player intent, already mapped from devices.</summary>
    public struct InputFrame
    {
        public Vector2 Move;          // WASD / left stick, magnitude ≤ 1
        public Vector2 Look;          // degrees this frame: x = yaw, y = pitch (up positive)
        public Vector2 Cursor;        // soft-cursor delta, in degrees of view, for sky-map and quipu
        public bool RunHeld;          // Shift / left stick click / LT
        public bool CrouchToggled;    // Ctrl / B
        public bool ContextPressed;   // Space / A: climb, jump, drop
        public bool ContextHeld;
        public InteractInput Interact; // E / X: interact, strike (hold), hide (tap)
        public bool DropLoadPressed;  // Q / Y
        public bool SelectPressed;    // mouse click / A: sky-map select
        public int Digit;             // 0–9 pressed this frame, else −1 (quipu dial)
        public bool DialUp, DialDown, DialLeft, DialRight; // arrows / d-pad
        public bool AnyPressed;
        public bool UsingGamepad;
        // Facilitator / developer keys (never shown to testers).
        public bool DebugToggle;      // F1
        public bool SkipBeat;         // F8
        public bool ReducedFlashToggle; // F9
        public bool Escape;
    }

    /// <summary>
    /// Polls the Input System devices directly (no action asset needed for the slice).
    /// Bindings follow pitch slice §10: one context action per key; the strike/hide
    /// split on a single key (hold vs. tap) is deliberate.
    /// </summary>
    public sealed class GameInput
    {
        public float MouseDegreesPerPixel = 0.11f;
        public float StickDegreesPerSecond = 150f;
        public bool InvertY;

        float _interactHeld;
        bool _interactDown;

        public InputFrame Poll(float dt)
        {
            var f = new InputFrame { Digit = -1 };
            var kb = Keyboard.current;
            var mouse = Mouse.current;
            var pad = Gamepad.current;

            Vector2 move = Vector2.zero;
            bool interactNow = false;

            if (kb != null)
            {
                if (kb[Key.W].isPressed) move.y += 1;
                if (kb[Key.S].isPressed) move.y -= 1;
                if (kb[Key.D].isPressed) move.x += 1;
                if (kb[Key.A].isPressed) move.x -= 1;
                f.RunHeld |= kb[Key.LeftShift].isPressed || kb[Key.RightShift].isPressed;
                f.CrouchToggled |= kb[Key.LeftCtrl].wasPressedThisFrame || kb[Key.RightCtrl].wasPressedThisFrame || kb[Key.C].wasPressedThisFrame;
                f.ContextPressed |= kb[Key.Space].wasPressedThisFrame;
                f.ContextHeld |= kb[Key.Space].isPressed;
                interactNow |= kb[Key.E].isPressed || kb[Key.Enter].isPressed;
                f.DropLoadPressed |= kb[Key.Q].wasPressedThisFrame;
                f.DialUp |= kb[Key.UpArrow].wasPressedThisFrame;
                f.DialDown |= kb[Key.DownArrow].wasPressedThisFrame;
                f.DialLeft |= kb[Key.LeftArrow].wasPressedThisFrame;
                f.DialRight |= kb[Key.RightArrow].wasPressedThisFrame;
                f.Digit = ReadDigit(kb);
                f.DebugToggle = kb[Key.F1].wasPressedThisFrame;
                f.SkipBeat = kb[Key.F8].wasPressedThisFrame;
                f.ReducedFlashToggle = kb[Key.F9].wasPressedThisFrame;
                f.Escape = kb[Key.Escape].wasPressedThisFrame;
                f.AnyPressed |= kb[Key.Space].wasPressedThisFrame || kb[Key.Enter].wasPressedThisFrame || kb[Key.E].wasPressedThisFrame;
            }

            if (mouse != null)
            {
                Vector2 d = mouse.delta.ReadValue() * MouseDegreesPerPixel;
                f.Look += new Vector2(d.x, InvertY ? -d.y : d.y);
                f.Cursor += new Vector2(d.x, d.y);
                f.SelectPressed |= mouse.leftButton.wasPressedThisFrame;
                f.AnyPressed |= mouse.leftButton.wasPressedThisFrame;
            }

            if (pad != null)
            {
                Vector2 ls = pad.leftStick.ReadValue();
                if (ls.sqrMagnitude > 0.02f) { move += ls; f.UsingGamepad = true; }
                Vector2 rs = pad.rightStick.ReadValue();
                if (rs.sqrMagnitude > 0.02f)
                {
                    var look = rs * (StickDegreesPerSecond * dt);
                    f.Look += new Vector2(look.x, InvertY ? -look.y : look.y);
                    f.Cursor += rs * (StickDegreesPerSecond * 0.6f * dt);
                    f.UsingGamepad = true;
                }
                f.RunHeld |= pad.leftStickButton.isPressed || pad.leftTrigger.isPressed;
                f.CrouchToggled |= pad.buttonEast.wasPressedThisFrame;
                f.ContextPressed |= pad.buttonSouth.wasPressedThisFrame;
                f.ContextHeld |= pad.buttonSouth.isPressed;
                f.SelectPressed |= pad.buttonSouth.wasPressedThisFrame;
                interactNow |= pad.buttonWest.isPressed;
                f.DropLoadPressed |= pad.buttonNorth.wasPressedThisFrame;
                f.DialUp |= pad.dpad.up.wasPressedThisFrame;
                f.DialDown |= pad.dpad.down.wasPressedThisFrame;
                f.DialLeft |= pad.dpad.left.wasPressedThisFrame;
                f.DialRight |= pad.dpad.right.wasPressedThisFrame;
                f.AnyPressed |= pad.buttonSouth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame;
            }

            f.Move = Vector2.ClampMagnitude(move, 1f);
            f.Interact = TrackInteract(interactNow, dt);
            f.AnyPressed |= f.Interact.Pressed;
            return f;
        }

        InteractInput TrackInteract(bool down, float dt)
        {
            var i = new InteractInput();
            if (down && !_interactDown)
            {
                i.Pressed = true;
                _interactHeld = 0f;
            }
            if (down)
            {
                _interactHeld += dt;
                i.Held = true;
            }
            if (!down && _interactDown) i.Released = true;
            i.HeldSeconds = _interactHeld;
            _interactDown = down;
            return i;
        }

        static int ReadDigit(Keyboard kb)
        {
            for (int d = 0; d <= 9; d++)
            {
                Key top = d == 0 ? Key.Digit0 : Key.Digit1 + (d - 1);
                Key pad = Key.Numpad0 + d;
                if (kb[top].wasPressedThisFrame || kb[pad].wasPressedThisFrame) return d;
            }
            return -1;
        }
    }
}
