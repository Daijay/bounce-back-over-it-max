using UnityEngine;

public struct BodyImpact
{
    public float speed;
    public Vector2 normal;
    public Vector2 point;
    public BlockKind kind;
}

public class SurfaceEffects
{
    readonly Rigidbody2D rb;
    readonly Collider2D bodyCol;
    readonly PhysicsMaterial2D bodyMat;
    readonly BodySettings body;
    readonly SurfaceSettings s;
    readonly float scale;

    bool stickyActive;
    bool stickyReleased;
    float stickyBlend;
    float stickyDamping;

    public float PadSpeedCapOverride;

    int padChain;
    float lastPadBounce = -100f;

    public int StickyState { get { return !stickyActive ? 0 : (stickyReleased ? 2 : 1); } }

    public SurfaceEffects(Rigidbody2D rb, Collider2D bodyCol, PhysicsMaterial2D bodyMat, BodySettings body,
        SurfaceSettings surfaces, float rigScale)
    {
        this.rb = rb;
        this.bodyCol = bodyCol;
        this.bodyMat = bodyMat;
        this.body = body;
        s = surfaces;
        scale = rigScale;
    }

    public void Sticky(ContactTracker contacts, float pullOffset, bool superJump, float dt)
    {
        bool touching = !superJump && contacts.BodyTouches(BlockKind.Purple);
        if (superJump)
        {
            stickyBlend = 0f;
        }
        stickyBlend = Mathf.MoveTowards(stickyBlend, touching ? 1f : 0f, dt / Mathf.Max(0.001f, s.stickyBlendTime));

        if (touching)
        {
            if (!stickyActive)
            {
                stickyActive = true;
                stickyReleased = false;
            }

            bool hammerAir = contacts.HammerAirborne;
            if (hammerAir)
            {
                stickyReleased = false;
            }

            if (!stickyReleased)
            {
                if (!hammerAir && pullOffset >= s.stickyBreakawayOffset * scale)
                {
                    stickyReleased = true;
                }
                else
                {
                    rb.linearVelocity *= Mathf.Lerp(1f, s.stickyHoldKeep, stickyBlend);
                }
            }

            stickyDamping = stickyReleased ? s.stickyReleasedDamping : s.stickyHoldDamping;
        }
        else
        {
            stickyActive = false;
            stickyReleased = false;
        }

        float baseDamping = superJump ? body.airLinearDamping : Mathf.Lerp(body.airLinearDamping, body.bodyLinearDamping, contacts.Blend);
        rb.linearDamping = Mathf.Lerp(baseDamping, stickyDamping, stickyBlend);
    }

    public void UpdateFriction()
    {
        float friction = stickyActive ? s.stickyBodyFriction : body.bodyFriction;
        if (Mathf.Approximately(bodyMat.friction, friction))
        {
            return;
        }
        bodyMat.friction = friction;
        bodyCol.sharedMaterial = bodyMat;
    }

    public void HandleImpact(BodyImpact hit)
    {
        Vector2 n = hit.normal;
        if (Vector2.Dot(n, rb.position - hit.point) < 0f)
        {
            n = -n;
        }

        if (hit.kind == BlockKind.Green && n.y >= s.padTopNormalY)
        {
            PadBounce(hit, n);
        }
        else
        {
            padChain = 0;
        }

        if (hit.kind == BlockKind.Purple)
        {
            Vector2 v = rb.linearVelocity;
            rb.linearVelocity = v - hit.normal * Vector2.Dot(v, hit.normal);
        }
    }

    void PadBounce(BodyImpact hit, Vector2 n)
    {
        if (hit.speed < s.padMinImpactSpeed * scale)
        {
            return;
        }
        if (Time.fixedTime - lastPadBounce > s.padChainResetTime)
        {
            padChain = 0;
        }

        float multiplier = s.padFirstBounceMultiplier * Mathf.Pow(s.padBounceDecay, padChain);
        float outSpeed = Mathf.Min(hit.speed * multiplier, Mathf.Max(s.maxPadSpeed * scale, PadSpeedCapOverride));

        Vector2 v = rb.linearVelocity;
        Vector2 sideways = v - n * Vector2.Dot(v, n);
        rb.linearVelocity = sideways + n * outSpeed;

        padChain++;
        lastPadBounce = Time.fixedTime;
    }
}
