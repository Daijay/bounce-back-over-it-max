using UnityEngine;

[DefaultExecutionOrder(200)]
[RequireComponent(typeof(Collider2D))]
public class LavaZone : MonoBehaviour
{
    [Header("Picture")]
    public Sprite[] frames;
    public SpriteRenderer body;
    public SpriteRenderer front;
    public float framesPerSecond = 5f;
    public float frontAlpha = 0.55f;

    [Header("Liquid feel (player body only)")]
    public float lavaSinkSpeed = 1f;
    public float lavaDamping = 4f;
    public float entryDamping = 12f;

    Collider2D zoneCollider;
    Collider2D bodyCollider;
    Collider2D headCollider;
    PlayerController player;
    Rigidbody2D playerRb;
    int frameIndex = -1;

    public float SurfaceY
    {
        get { return zoneCollider.bounds.max.y; }
    }

    void Awake()
    {
        zoneCollider = GetComponent<Collider2D>();
        zoneCollider.isTrigger = true;

        if (front != null)
        {
            front.color = new Color(1f, 1f, 1f, frontAlpha);
        }
    }

    void Update()
    {
        if (frames == null || frames.Length == 0)
        {
            return;
        }

        int index = (int)(Time.time * framesPerSecond) % frames.Length;
        if (index == frameIndex)
        {
            return;
        }
        frameIndex = index;

        if (body != null)
        {
            body.sprite = frames[index];
        }
        if (front != null)
        {
            front.sprite = frames[index];
        }
    }

    void FixedUpdate()
    {
        if (zoneCollider == null)
        {
            zoneCollider = GetComponent<Collider2D>();
            if (zoneCollider == null)
            {
                return;
            }
        }
        if (!FindPlayer())
        {
            return;
        }
        bool bodyTouch = zoneCollider.IsTouching(bodyCollider);
        if (bodyTouch || (headCollider != null && zoneCollider.IsTouching(headCollider)))
        {
            player.ReportLava(Time.fixedDeltaTime);
        }
        if (!bodyTouch)
        {
            return;
        }

        float submersion = Mathf.Clamp01(
            (zoneCollider.bounds.max.y - bodyCollider.bounds.min.y) / Mathf.Max(0.01f, bodyCollider.bounds.size.y));

        Vector2 v = playerRb.linearVelocity;
        if (float.IsNaN(v.x) || float.IsNaN(v.y))
        {
            return;
        }

        float dt = Time.fixedDeltaTime;

        float k = Mathf.Exp(-lavaDamping * dt * submersion);
        v.x *= k;
        if (v.y > 0f)
        {
            v.y *= k;
        }

        if (v.y < -lavaSinkSpeed)
        {
            float pull = 1f - Mathf.Exp(-entryDamping * dt * Mathf.Clamp01(submersion * 2f));
            v.y = Mathf.Lerp(v.y, -lavaSinkSpeed, pull);
        }

        playerRb.linearVelocity = v;
    }

    bool FindPlayer()
    {
        if (player != null && playerRb != null && bodyCollider != null)
        {
            return true;
        }

        player = PlayerController.Instance;
        if (player == null)
        {
            return false;
        }

        playerRb = player.BodyRb;
        if (playerRb == null)
        {
            return false;
        }

        bodyCollider = playerRb.GetComponent<CapsuleCollider2D>();
        headCollider = player.HammerRb != null ? player.HammerRb.GetComponent<Collider2D>() : null;
        return bodyCollider != null;
    }
}
