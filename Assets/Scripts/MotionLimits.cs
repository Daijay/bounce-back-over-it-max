using UnityEngine;

public class MotionLimits
{
    readonly Rigidbody2D rb;
    readonly LimitSettings s;
    readonly float scale;

    public MotionLimits(Rigidbody2D rb, LimitSettings settings, float rigScale)
    {
        this.rb = rb;
        s = settings;
        scale = rigScale;
    }

    public void CapSpeeds(float upwardCapOverride)
    {
        Vector2 v = rb.linearVelocity;
        if (Safe.Bad(v))
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }
        float max = Mathf.Max(0.01f, s.safetyMaxSpeed);
        bool changed = false;
        if (v.sqrMagnitude > max * max)
        {
            v = v.normalized * max;
            changed = true;
        }
        float maxUp = Mathf.Max(s.maxUpwardSpeed * scale, upwardCapOverride);
        if (v.y > maxUp)
        {
            v.y = maxUp;
            changed = true;
        }
        if (changed)
        {
            rb.linearVelocity = v;
        }
    }
}
