using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace RingOut.Game
{
    public enum InputSource
    {
        Keyboard,
        Gamepad,
        Touch,
    }

    /// <summary>
    /// Keyboard, gamepad and touch through the Input System. Read once per frame with
    /// <see cref="Read"/>: a move vector, a camera turn and the action button. On a touch screen
    /// a finger on the left of the screen becomes a floating stick and a tap on the right is the
    /// action button.
    /// </summary>
    public sealed class GameInput
    {
        const float StickDeadZone = 0.2f;
        const float StickStep = 0.6f;
        /// <summary>The touch stick's reach in screen heights.</summary>
        const float TouchReach = 0.11f;

        int stickVertical;
        int stickHorizontal;
        int lastFrame = -1;
        int stickFinger = -1;

        static Keyboard Keys => Keyboard.current;

        /// <summary>Where to roll, in stick space (x right, y forward), at most length 1.</summary>
        public Vector2 Move { get; private set; }

        /// <summary>How fast to turn the camera, -1 (left) to 1 (right).</summary>
        public float Orbit { get; private set; }

        /// <summary>The action button (dash or smash) went down this frame.</summary>
        public bool Action { get; private set; }

        public InputSource Source { get; private set; } = InputSource.Keyboard;

        /// <summary>While a finger holds the touch stick: its centre and the knob, in screen pixels.</summary>
        public Vector2? StickOrigin { get; private set; }
        public Vector2 StickKnob { get; private set; }

        public void Read()
        {
            if (lastFrame == Time.frameCount)
            {
                return;
            }
            lastFrame = Time.frameCount;
            Vector2 move = Vector2.zero;
            float orbit = 0f;
            bool action = false;

            Keyboard keys = Keys;
            if (keys != null)
            {
                Vector2 typed = new Vector2(
                    (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1f : 0f) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1f : 0f),
                    (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1f : 0f) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1f : 0f));
                float turn = (keys.eKey.isPressed ? 1f : 0f) - (keys.qKey.isPressed ? 1f : 0f);
                bool pressed = keys.spaceKey.wasPressedThisFrame || keys.leftShiftKey.wasPressedThisFrame || keys.jKey.wasPressedThisFrame;
                if (typed != Vector2.zero || turn != 0f || pressed)
                {
                    Source = InputSource.Keyboard;
                }
                move += typed;
                orbit += turn;
                action |= pressed;
            }

            foreach (Gamepad pad in Gamepad.all)
            {
                Vector2 stick = pad.leftStick.ReadValue() + pad.dpad.ReadValue();
                if (stick.sqrMagnitude < StickDeadZone * StickDeadZone)
                {
                    stick = Vector2.zero;
                }
                float turn = pad.rightStick.ReadValue().x;
                if (Mathf.Abs(turn) < StickDeadZone)
                {
                    turn = 0f;
                }
                turn += (pad.rightShoulder.isPressed ? 1f : 0f) - (pad.leftShoulder.isPressed ? 1f : 0f);
                bool pressed = pad.buttonSouth.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame || pad.rightTrigger.wasPressedThisFrame;
                if (stick != Vector2.zero || turn != 0f || pressed)
                {
                    Source = InputSource.Gamepad;
                }
                move += stick;
                orbit += turn;
                action |= pressed;
            }

            ReadTouch(ref move, ref action);

            Move = Vector2.ClampMagnitude(move, 1f);
            Orbit = Mathf.Clamp(orbit, -1f, 1f);
            Action = action;
        }

        void ReadTouch(ref Vector2 move, ref bool action)
        {
            Touchscreen screen = Touchscreen.current;
            if (screen == null)
            {
                StickOrigin = null;
                return;
            }
            bool stickHeld = false;
            foreach (TouchControl finger in screen.touches)
            {
                int id = finger.touchId.ReadValue();
                Vector2 position = finger.position.ReadValue();
                if (finger.press.wasPressedThisFrame)
                {
                    Source = InputSource.Touch;
                    if (position.x < Screen.width * 0.45f && stickFinger < 0)
                    {
                        stickFinger = id;
                        StickOrigin = position;
                    }
                    else if (position.x >= Screen.width * 0.55f)
                    {
                        action = true;
                    }
                }
                if (id == stickFinger && finger.press.isPressed && StickOrigin.HasValue)
                {
                    stickHeld = true;
                    float reach = Screen.height * TouchReach;
                    Vector2 offset = Vector2.ClampMagnitude(position - StickOrigin.Value, reach);
                    StickKnob = StickOrigin.Value + offset;
                    move += offset / reach;
                }
            }
            if (!stickHeld)
            {
                stickFinger = -1;
                StickOrigin = null;
            }
        }

        public bool Pause => (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.pKey.wasPressedThisFrame)) ||
            AnyPad(pad => pad.startButton.wasPressedThisFrame);

        public bool Mute => Keys != null && Keys.mKey.wasPressedThisFrame;

        public bool Confirm =>
            (Keys != null && (Keys.enterKey.wasPressedThisFrame || Keys.numpadEnterKey.wasPressedThisFrame || Keys.spaceKey.wasPressedThisFrame)) ||
            AnyPad(pad => pad.buttonSouth.wasPressedThisFrame);

        public bool Back =>
            (Keys != null && (Keys.escapeKey.wasPressedThisFrame || Keys.backspaceKey.wasPressedThisFrame)) ||
            AnyPad(pad => pad.buttonEast.wasPressedThisFrame);

        /// <summary>-1 for a step up a menu, +1 for a step down, 0 otherwise.</summary>
        public int VerticalStep()
        {
            int step = 0;
            if (Keys != null)
            {
                if (Keys.upArrowKey.wasPressedThisFrame || Keys.wKey.wasPressedThisFrame) step = -1;
                if (Keys.downArrowKey.wasPressedThisFrame || Keys.sKey.wasPressedThisFrame) step = 1;
            }
            if (step != 0)
            {
                Source = InputSource.Keyboard;
                return step;
            }
            return PadStep(true, ref stickVertical);
        }

        /// <summary>-1 for a step left through a setting, +1 for a step right, 0 otherwise.</summary>
        public int HorizontalStep()
        {
            int step = 0;
            if (Keys != null)
            {
                if (Keys.leftArrowKey.wasPressedThisFrame || Keys.aKey.wasPressedThisFrame) step = -1;
                if (Keys.rightArrowKey.wasPressedThisFrame || Keys.dKey.wasPressedThisFrame) step = 1;
            }
            if (step != 0)
            {
                Source = InputSource.Keyboard;
                return step;
            }
            return PadStep(false, ref stickHorizontal);
        }

        int PadStep(bool vertical, ref int held)
        {
            int step = 0;
            int stickSign = 0;
            foreach (Gamepad pad in Gamepad.all)
            {
                if (vertical)
                {
                    if (pad.dpad.up.wasPressedThisFrame) step = -1;
                    if (pad.dpad.down.wasPressedThisFrame) step = 1;
                }
                else
                {
                    if (pad.dpad.left.wasPressedThisFrame) step = -1;
                    if (pad.dpad.right.wasPressedThisFrame) step = 1;
                }
                Vector2 stick = pad.leftStick.ReadValue();
                float axis = vertical ? -stick.y : stick.x;
                if (Mathf.Abs(axis) > StickStep)
                {
                    stickSign = axis > 0f ? 1 : -1;
                }
            }
            if (step == 0 && stickSign != 0 && stickSign != held)
            {
                step = stickSign;
            }
            held = stickSign;
            if (step != 0)
            {
                Source = InputSource.Gamepad;
            }
            return step;
        }

        static bool AnyPad(System.Func<Gamepad, bool> test)
        {
            foreach (Gamepad pad in Gamepad.all)
            {
                if (test(pad))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
