using UnityEngine;

public class ContactTracker
{
    readonly Rigidbody2D body;
    readonly Rigidbody2D hammer;
    readonly ContactSettings s;
    readonly ContactPoint2D[] bodyContacts = new ContactPoint2D[32];
    readonly ContactPoint2D[] hammerContacts = new ContactPoint2D[16];
    int bodyCount;
    int hammerCount;
    float lastHammerContact = -100f;
    float lastIceContact = -100f;
    float releasedAt = -100f;
    float releaseHoldOff;
    bool needFreshContact;
    bool inContact;

    public bool HammerAirborne { get { return !inContact; } }
    public float Blend { get; private set; }
    public float GapBeforeStep { get; private set; }

    public bool BodyOnBlock { get; private set; }
    public bool HammerOnBlock { get; private set; }
    public bool HammerOnIce { get; private set; }
    public float HammerIceFriction { get; private set; }

    public ContactTracker(Rigidbody2D body, Rigidbody2D hammer, ContactSettings settings)
    {
        this.body = body;
        this.hammer = hammer;
        s = settings;
    }

    public void Refresh(float dt)
    {
        bodyCount = body.GetContacts(bodyContacts);
        hammerCount = hammer.GetContacts(hammerContacts);

        BodyOnBlock = false;
        for (int i = 0; i < bodyCount; i++)
        {
            BlockType b;
            if (BlockType.TryGet(bodyContacts[i].collider, out b))
            {
                BodyOnBlock = true;
                break;
            }
        }
        HammerOnBlock = false;
        bool sawIce = false;
        bool sawOther = false;
        float iceFriction = 0f;
        for (int i = 0; i < hammerCount; i++)
        {
            BlockType b;
            if (hammerContacts[i].separation <= s.contactSeparation && BlockType.TryGet(hammerContacts[i].collider, out b))
            {
                HammerOnBlock = true;
                if (b.kind == BlockKind.Blue)
                {
                    sawIce = true;
                    iceFriction = b.iceFriction;
                }
                else
                {
                    sawOther = true;
                }
            }
        }

        float now = Time.fixedTime;

        if (sawIce && !sawOther)
        {
            lastIceContact = now;
            HammerIceFriction = iceFriction;
            HammerOnIce = true;
        }
        else if (HammerOnIce && (sawOther || now - lastIceContact > s.contactGraceTime))
        {
            HammerOnIce = false;
        }
        GapBeforeStep = now - lastHammerContact;
        if (HammerOnBlock)
        {
            lastHammerContact = now;
        }

        if (needFreshContact && (!HammerOnBlock || now - releasedAt > releaseHoldOffMax))
        {
            needFreshContact = false;
        }
        bool mayGrip = !needFreshContact && now - releasedAt >= releaseHoldOff;

        if (!inContact)
        {
            if (HammerOnBlock && mayGrip)
            {
                inContact = true;
            }
        }
        else if (now - lastHammerContact > s.gripGraceTime)
        {
            inContact = false;
        }

        float blendTime = inContact ? s.contactBlendTime : s.releaseBlendTime;
        Blend = Mathf.MoveTowards(Blend, inContact ? 1f : 0f, dt / Mathf.Max(0.001f, blendTime));
    }

    public int CopyBodyColliders(Collider2D[] dst)
    {
        int n = 0;
        for (int i = 0; i < bodyCount && n < dst.Length; i++)
        {
            BlockType block;
            if (BlockType.TryGet(bodyContacts[i].collider, out block))
            {
                dst[n++] = bodyContacts[i].collider;
            }
        }
        return n;
    }

    float releaseHoldOffMax = 0.5f;

    public void ForceRelease(float now, float holdOff, float maxHold)
    {
        inContact = false;
        Blend = 0f;
        releasedAt = now;
        releaseHoldOff = holdOff;
        releaseHoldOffMax = maxHold;
        needFreshContact = true;
    }

    public bool BodyTouches(BlockKind kind)
    {
        for (int i = 0; i < bodyCount; i++)
        {
            BlockType block;
            if (BlockType.TryGet(bodyContacts[i].collider, out block) && block.kind == kind)
            {
                return true;
            }
        }
        return false;
    }
}
