using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTimeY = 0.2f;
    public float smoothTimeX = 0.9f;
    public float verticalOffset = 1.5f;
    public float leadFactor = 0.25f;
    public float maxLead = 3f;
    public float horizontalLeadScale = 0.5f;
    public float leadResponse = 4f;
    public float cameraZ = -10f;

    [System.NonSerialized] public Vector2 shakeOffset;

    Rigidbody2D targetRb;
    Vector2 lead;
    float velX;
    float velY;
    Vector3 smoothed;
    bool hasSmoothed;

    void Start()
    {
        if (target == null && PlayerController.Instance != null)
        {
            target = PlayerController.Instance.transform;
        }
        if (target == null)
        {
            return;
        }

        targetRb = target.GetComponent<Rigidbody2D>();
        transform.position = new Vector3(target.position.x, target.position.y + verticalOffset, cameraZ);
    }

    public void Snap()
    {
        if (target == null)
        {
            return;
        }
        lead = Vector2.zero;
        velX = 0f;
        velY = 0f;
        smoothed = new Vector3(target.position.x, target.position.y + verticalOffset, cameraZ);
        hasSmoothed = true;
        transform.position = new Vector3(smoothed.x + shakeOffset.x, smoothed.y + shakeOffset.y, smoothed.z);
    }

    void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector2 vel = targetRb != null ? targetRb.linearVelocity : Vector2.zero;
        Vector2 wantedLead = new Vector2(
            Mathf.Clamp(vel.x * leadFactor, -maxLead, maxLead) * horizontalLeadScale,
            Mathf.Clamp(vel.y * leadFactor, -maxLead, maxLead));
        lead = Vector2.Lerp(lead, wantedLead, 1f - Mathf.Exp(-leadResponse * Time.deltaTime));

        if (!hasSmoothed)
        {
            smoothed = transform.position;
            hasSmoothed = true;
        }
        Vector3 p = smoothed;
        p.x = Mathf.SmoothDamp(p.x, target.position.x + lead.x, ref velX, smoothTimeX);
        p.y = Mathf.SmoothDamp(p.y, target.position.y + verticalOffset + lead.y, ref velY, smoothTimeY);
        p.z = cameraZ;
        smoothed = p;
        transform.position = new Vector3(p.x + shakeOffset.x, p.y + shakeOffset.y, p.z);
    }
}
