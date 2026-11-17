using UnityEngine;

public class LavaExposure
{
    readonly LavaDeathSettings s;
    float timer;
    float lastTouch = -100f;
    float lastCounted = -100f;

    public float Progress { get { return Mathf.Clamp01(timer / Mathf.Max(0.01f, s.lavaDeathSeconds)); } }

    public LavaExposure(LavaDeathSettings settings)
    {
        s = settings;
    }

    public void Touching(float dt, float now)
    {
        if (now - lastTouch > s.lavaBreakGrace)
        {
            timer = 0f;
        }
        if (now > lastCounted)
        {
            timer += dt;
            lastCounted = now;
        }
        lastTouch = now;
    }

    public bool Tick(float now)
    {
        if (timer > 0f && now - lastTouch > s.lavaBreakGrace)
        {
            timer = 0f;
        }
        return timer >= s.lavaDeathSeconds;
    }

    public void Reset()
    {
        timer = 0f;
        lastTouch = -100f;
        lastCounted = -100f;
    }
}
