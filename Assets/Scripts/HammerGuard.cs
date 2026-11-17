using UnityEngine;

public class HammerGuard
{
    readonly Rigidbody2D hammer;
    readonly CircleCollider2D headCol;
    readonly LimitSettings s;
    readonly float scale;
    readonly Collider2D[] hits = new Collider2D[16];
    readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    ContactFilter2D solidFilter;
    Vector2 lastValidPosition;

    public HammerGuard(Rigidbody2D hammer, CircleCollider2D headCol, LimitSettings limits, float rigScale)
    {
        this.hammer = hammer;
        this.headCol = headCol;
        s = limits;
        scale = rigScale;
        solidFilter = new ContactFilter2D();
        solidFilter.useTriggers = false;
        lastValidPosition = hammer.position;
    }

    public bool Recover()
    {
        bool deep = false;
        int n = headCol.Overlap(solidFilter, hits);
        for (int i = 0; i < n; i++)
        {
            Collider2D other = hits[i];
            BlockType block;
            if (!BlockType.TryGet(other, out block))
            {
                continue;
            }
            ColliderDistance2D d = headCol.Distance(other);
            if (d.isOverlapped && -d.distance > s.penetrationTolerance * scale)
            {
                deep = true;
                break;
            }
        }

        if (!deep)
        {
            lastValidPosition = hammer.position;
            return false;
        }

        hammer.position = lastValidPosition;
        hammer.linearVelocity = Vector2.zero;
        return true;
    }

    public Vector2 LimitMove(Vector2 from, Vector2 move)
    {
        float length = move.magnitude;
        if (length < 0.0001f)
        {
            return move;
        }
        Vector2 dir = move / length;
        int n = Physics2D.CircleCast(from, headCol.radius, dir, solidFilter, castHits, length + s.sweepSkin);

        float allowed = length;
        for (int i = 0; i < n; i++)
        {
            RaycastHit2D hit = castHits[i];
            BlockType block;
            if (!BlockType.TryGet(hit.collider, out block))
            {
                continue;
            }
            if (Vector2.Dot(dir, hit.normal) > -0.001f)
            {
                continue;
            }
            allowed = Mathf.Min(allowed, Mathf.Max(0f, hit.distance - s.sweepSkin));
        }
        return dir * allowed;
    }
}
