using UnityEngine;

public class CheeseMoonFx : MonoBehaviour
{
    public Transform visual;
    public SpriteRenderer glow;
    public float bobAmplitude = 0.35f;
    public float bobPeriod = 9f;
    public float pulsePeriod = 5f;
    public float glowMin = 0.7f;
    public float glowMax = 1f;

    float lastGlow = -1f;

    void OnEnable()
    {
        lastGlow = -1f;
    }

    void OnDisable()
    {
        if (visual != null)
        {
            visual.localPosition = Vector3.zero;
        }
    }

    void Update()
    {
        if (visual != null)
        {
            float lift = Mathf.Sin(Time.time * (2f * Mathf.PI / bobPeriod)) * bobAmplitude;
            visual.localPosition = new Vector3(0f, MapStyle.Snap(lift), 0f);
        }
        if (glow != null)
        {
            float alpha = Mathf.Lerp(glowMin, glowMax, 0.5f + 0.5f * Mathf.Sin(Time.time * (2f * Mathf.PI / pulsePeriod)));
            if (Mathf.Abs(alpha - lastGlow) > 0.01f)
            {
                lastGlow = alpha;
                glow.color = new Color(1f, 1f, 1f, alpha);
            }
        }
    }
}
