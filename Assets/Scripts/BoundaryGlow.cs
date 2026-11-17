using UnityEngine;

public class BoundaryGlow : MonoBehaviour
{
    public SpriteRenderer field;
    public float faceX;
    public int side = 1;
    public float fadeDistance = 8f;
    public float maxAlpha = 0.6f;
    public float pulseSpeed = 5f;
    public float scrollSteps = 8f;
    public int tilePixels = 32;

    [SerializeField] Vector3 home;
    [SerializeField] bool homeSet;
    float alpha = -1f;

    void OnEnable()
    {
        if (field == null)
        {
            return;
        }
        if (!homeSet)
        {
            home = field.transform.localPosition;
            homeSet = true;
        }
        field.enabled = false;
        alpha = -1f;
    }

    void OnDisable()
    {
        if (field != null && homeSet)
        {
            field.transform.localPosition = home;
            field.enabled = false;
        }
    }

    void Update()
    {
        PlayerController player = PlayerController.Instance;
        if (field == null || player == null || player.BodyRb == null)
        {
            return;
        }
        float distance = side > 0 ? player.BodyRb.position.x - faceX : faceX - player.BodyRb.position.x;
        float strength = Mathf.Clamp01(1f - distance / Mathf.Max(0.1f, fadeDistance));
        strength *= strength;

        if (strength <= 0.002f)
        {
            if (field.enabled)
            {
                field.enabled = false;
            }
            return;
        }
        float pulse = 0.8f + 0.2f * Mathf.Sin(Time.time * pulseSpeed);
        float target = strength * maxAlpha * pulse;
        if (!field.enabled)
        {
            field.enabled = true;
        }
        if (Mathf.Abs(target - alpha) > 0.004f)
        {
            alpha = target;
            field.color = new Color(1f, 1f, 1f, alpha);
        }
        float lift = (Mathf.Floor(Time.time * scrollSteps) % tilePixels) * MapStyle.PixelWorld;
        field.transform.localPosition = new Vector3(home.x, home.y + lift, home.z);
    }
}
