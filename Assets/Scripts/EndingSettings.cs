using UnityEngine;

[System.Serializable]
public class EndingSettings
{
    [Header("Float up to the moon")]
    public float letterboxPercent = 0.12f;
    public float letterboxTime = 0.4f;
    public float ascendSpeed = 3.5f;
    public float ascendRampTime = 2f;
    public float ascendCentreTime = 1.5f;
    public float floatStartY = 96f;
    public float tumbleDegreesPerSecond = 25f;
    public bool useEndingVideos = true;
    public float videoStartTime = 4.5f;

    [Header("Gravity fades to zero on the right slope")]
    public float gravityEaseStartY = 33f;
    public float gravityEaseEndY = 98f;
    public float gravityEaseMinScale = 0f;
    public float lowGravityTextBelow = 0.6f;
    public float lowGravityTextTime = 2f;

    [Header("Debug")]
    public bool debugKeys = true;
    public float debugTeleportLeft = 3f;

    [Header("Moon rebound")]
    public float moonKickSpeed = 20f;
    public float moonFlashTime = 0.3f;
    public float moonShakeTime = 0.6f;
    public float moonShakeAmount = 0.5f;
    public float fadeTime = 1.2f;

    public float GravityScaleAt(float y)
    {
        float t = Mathf.Clamp01((y - gravityEaseStartY) / Mathf.Max(0.01f, gravityEaseEndY - gravityEaseStartY));
        t = t * t * (3f - 2f * t);
        return Mathf.Lerp(1f, Mathf.Clamp01(gravityEaseMinScale), t);
    }
}
