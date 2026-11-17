using UnityEngine;

[DefaultExecutionOrder(-200)]
public class PlayerSpawn : MonoBehaviour
{
    public Vector2 playerOffset = Vector2.zero;
    public bool startPointIsFloor = true;

    void Awake()
    {
        PlayerController controller = Object.FindAnyObjectByType<PlayerController>();
        if (controller == null)
        {
            return;
        }

        Vector3 spot = transform.position + (Vector3)playerOffset;
        if (startPointIsFloor)
        {
            spot.y += 0.05f + (controller.body.bodySize.y * 0.5f - controller.body.bodyOffset.y) * controller.rigScale;
        }
        Transform playerT = controller.transform;
        playerT.position = new Vector3(spot.x, spot.y, playerT.position.z);

        if (controller.hammerBody != null)
        {
            controller.hammerBody.transform.position = playerT.position + new Vector3(1f, 1f, 0f);
            controller.hammerBody.linearVelocity = Vector2.zero;
        }

        Rigidbody2D body = controller.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
        }

        Physics2D.SyncTransforms();
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + (Vector3)playerOffset, 0.3f);
    }
}
