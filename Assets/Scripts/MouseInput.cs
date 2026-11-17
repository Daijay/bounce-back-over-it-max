using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class MouseInput
{
    static Vector2 lastPosition;

    public static Vector2 ScreenPosition
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                lastPosition = mouse.position.ReadValue();
            }
#else
            lastPosition = Input.mousePosition;
#endif
            return lastPosition;
        }
    }

    public static bool TryGetValidPosition(out Vector2 position)
    {
        position = ScreenPosition;
        if (Time.frameCount < 3 || !Application.isFocused)
        {
            return false;
        }
        if (position.x <= 0f && position.y <= 0f)
        {
            return false;
        }
        return position.x >= 0f && position.y >= 0f && position.x <= Screen.width && position.y <= Screen.height;
    }

    public static bool SuperJumpPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space);
#endif
        }
    }

    public static bool LeftClickPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }
    }

    public static bool LeftHeld
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            return mouse != null && mouse.leftButton.isPressed;
#else
            return Input.GetMouseButton(0);
#endif
        }
    }

    public static bool ShiftHeld
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }
    }

    public static bool EscapePressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }
    }

    public static bool RestartPressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.rKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }
    }

    public static bool DebugTogglePressed
    {
        get
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            return keyboard != null && keyboard.f3Key.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.F3);
#endif
        }
    }
}
