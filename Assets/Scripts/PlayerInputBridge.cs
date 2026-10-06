using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ThirdPersonDemo
{
    /// <summary>
    /// 輸入橋接層：同時相容 Unity 新舊輸入系統 (New Input System & Legacy Input)
    /// </summary>
    public static class PlayerInputBridge
    {
        public static Vector2 GetMoveInput()
        {
            Vector2 input = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) input.y += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) input.y -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) input.x += 1f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) input.x -= 1f;
            }

            if (Gamepad.current != null)
            {
                Vector2 stick = Gamepad.current.leftStick.ReadValue();
                if (stick.sqrMagnitude > 0.04f)
                {
                    input = stick;
                }
            }
#else
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
#endif
            return Vector2.ClampMagnitude(input, 1f);
        }

        public static Vector2 GetLookInput()
        {
            Vector2 delta = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                // 新輸入系統 delta 為像素位移，乘以係數以獲得舒適靈敏度
                delta = Mouse.current.delta.ReadValue() * 0.12f;
            }

            if (Gamepad.current != null)
            {
                Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
                if (rightStick.sqrMagnitude > 0.04f)
                {
                    delta += rightStick * 2.5f;
                }
            }
#else
            delta.x = Input.GetAxis("Mouse X") * 2f;
            delta.y = Input.GetAxis("Mouse Y") * 2f;
#endif
            return delta;
        }

        public static float GetZoomInput()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                return Mathf.Clamp(scroll, -1f, 1f);
            }
            return 0f;
#else
            return Input.GetAxis("Mouse ScrollWheel");
#endif
        }

        public static bool GetJumpDown()
        {
#if ENABLE_INPUT_SYSTEM
            bool kb = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool pad = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            return kb || pad;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }

        public static bool GetSprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            bool kb = Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);
            bool pad = Gamepad.current != null && (Gamepad.current.leftStickButton.isPressed || Gamepad.current.rightTrigger.isPressed);
            return kb || pad;
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        public static bool GetCursorToggleDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool GetMouseClickDown()
        {
#if ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }
}
