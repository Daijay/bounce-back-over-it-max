using UnityEngine;

public class HammerAim
{
    readonly AimSettings s;
    Camera cam;
    CameraFollow follow;
    Vector2 lastScreen;
    Vector2 anchorScreen;
    bool haveAnchor;
    bool cursorSeen;
    Vector2 rawOffset;
    Vector2 smoothed;
    float deadzoneWorld;

    public Vector2 Offset { get; private set; }
    public bool HasAim { get; private set; }
    public bool InDeadzone { get; private set; }
    public bool MouseValid { get; private set; }

    public HammerAim(AimSettings settings, Vector2 startOffset)
    {
        s = settings;
        rawOffset = startOffset;
        smoothed = startOffset;
        Offset = startOffset;
    }

    public void Sample()
    {
        Vector2 screen;
        MouseValid = MouseInput.TryGetValidPosition(out screen);
        if (!MouseValid)
        {
            return;
        }
        lastScreen = screen;
        HasAim = true;
    }

    public void Step(float reach, float deadzone, bool touching, float contactBlend, float dt)
    {
        deadzoneWorld = deadzone;

        float still = s.mouseStillPixels;
        bool mouseMoved = !haveAnchor || (lastScreen - anchorScreen).sqrMagnitude > still * still;
        if (HasAim && (mouseMoved || !touching || !cursorSeen))
        {
            Vector2 offset;
            if (ReadOffset(reach, out offset))
            {
                rawOffset = offset;
                if (!cursorSeen)
                {
                    cursorSeen = true;
                    smoothed = rawOffset;
                }
            }
            anchorScreen = lastScreen;
            haveAnchor = true;
        }

        if (s.cursorSmoothTime <= 0f)
        {
            smoothed = rawOffset;
        }
        else
        {
            float smoothTime = Mathf.Lerp(s.cursorSmoothTime, s.contactCursorSmoothTime, contactBlend);
            float alpha = 1f - Mathf.Exp(-dt / Mathf.Max(0.0001f, smoothTime));
            smoothed += (rawOffset - smoothed) * alpha;
        }

        Offset = Vector2.ClampMagnitude(smoothed, reach);
    }

    bool ReadOffset(float reach, out Vector2 offset)
    {
        offset = Vector2.zero;
        if (cam == null)
        {
            cam = Camera.main;
            if (cam != null)
            {
                follow = cam.GetComponent<CameraFollow>();
            }
        }

        float halfHeight = Screen.height * 0.5f;
        if (halfHeight < 1f)
        {
            return false;
        }

        float dropPixels = 0f;
        if (cam != null && cam.orthographic && follow != null)
        {
            dropPixels = follow.verticalOffset / (2f * cam.orthographicSize) * Screen.height;
        }
        Vector2 bodyOnScreen = new Vector2(Screen.width * 0.5f, halfHeight - dropPixels);

        float pixelsPerReach = halfHeight * Mathf.Max(0.05f, s.cursorReachFraction);
        Vector2 v = (lastScreen - bodyOnScreen) / pixelsPerReach * reach;
        if (Safe.Bad(v))
        {
            return false;
        }
        InDeadzone = v.sqrMagnitude <= deadzoneWorld * deadzoneWorld;
        if (InDeadzone)
        {
            return false;
        }
        offset = Vector2.ClampMagnitude(v, reach);
        return true;
    }
}
