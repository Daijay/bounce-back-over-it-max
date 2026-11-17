using UnityEngine;

public class SuperJump
{
    readonly SuperJumpSettings s;
    readonly Collider2D[] launchSurfaces = new Collider2D[16];
    int launchCount;
    float pool;
    float startTime;
    float dizzyEnd = -100f;
    bool active;
    bool crashPending;

    public float Pool { get { return pool; } }
    public float Max { get { return Mathf.Max(0.01f, s.chargeMax); } }
    public bool Active { get { return active; } }
    public bool CanJump { get { return pool >= s.superJumpMinCharge; } }
    public bool Dizzy { get { return Time.fixedTime < dizzyEnd; } }
    public float DizzyLeft { get { return Mathf.Max(0f, dizzyEnd - Time.fixedTime); } }
    public float PullMultiplier { get { return Dizzy ? s.dizzyPullMultiplier : 1f; } }

    public SuperJump(SuperJumpSettings settings)
    {
        s = settings;
    }

    public void SetPool(float value)
    {
        pool = Mathf.Clamp(value, 0f, Max);
    }

    public void Reset()
    {
        pool = 0f;
        active = false;
        crashPending = false;
        dizzyEnd = -100f;
    }

    public void CancelJump()
    {
        active = false;
        crashPending = false;
        dizzyEnd = -100f;
    }

    public bool AddLanding(float impactSpeed, Vector2 n)
    {
        if (impactSpeed < s.chargeThresholdSpeed || n.y < s.landingMinNormalY)
        {
            return false;
        }
        float over = impactSpeed - s.chargeThresholdSpeed;
        pool = Mathf.Min(Max, pool + s.chargeScale * Mathf.Pow(over, s.chargeExponent));
        return true;
    }

    public bool TryLaunch(Rigidbody2D rb, ContactTracker contacts, float now)
    {
        if (pool < s.superJumpMinCharge)
        {
            return false;
        }
        float t = Mathf.Clamp01(pool / Max);
        Vector2 v = rb.linearVelocity;
        if (Safe.Bad(v))
        {
            v = Vector2.zero;
        }
        rb.linearVelocity = new Vector2(v.x * s.superJumpHorizontalKeep, Mathf.Lerp(s.superJumpMinSpeed, s.superJumpMaxSpeed, t));

        launchCount = contacts.CopyBodyColliders(launchSurfaces);
        pool = 0f;
        active = true;
        crashPending = false;
        startTime = now;
        return true;
    }

    public void OnBodyContact(Collider2D other, float now)
    {
        if (!active || now - startTime < s.liftoffGraceTime)
        {
            return;
        }
        for (int i = 0; i < launchCount; i++)
        {
            if (launchSurfaces[i] == other)
            {
                return;
            }
        }
        crashPending = true;
    }

    public void Tick(Rigidbody2D rb, float now)
    {
        if (!active)
        {
            return;
        }
        if (crashPending)
        {
            Vector2 v = rb.linearVelocity;
            rb.linearVelocity = new Vector2(v.x * s.crashHorizontalKeep, -Mathf.Abs(s.crashDropSpeed));
            dizzyEnd = now + s.dizzyDuration;
            active = false;
            crashPending = false;
            return;
        }
        if (rb.linearVelocity.y < 0f || now - startTime >= s.superJumpMaxDuration)
        {
            active = false;
        }
    }
}
