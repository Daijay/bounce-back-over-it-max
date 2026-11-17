using UnityEngine;

public static class SoundEffects
{
    public static float masterVolume = 1f;
    public static float hitGroundVolume = 1f;
    public static float clickVolume = 1f;
    public static float clickMinInterval = 0.05f;

    public static float hitGroundMinVolume = 0.25f;
    public static float hitGroundFullSpeed = 30f;
    public static float hitGroundMinPitch = 0.92f;
    public static float hitGroundMaxPitch = 1.08f;
    public static float hitGroundMinInterval = 0.15f;

    const string ObjectName = "SoundEffects (runtime)";
    const int SourceCount = 6;

    static AudioSource[] sources;
    static int nextSource;
    static AudioClip hitGround;
    static AudioClip click;
    static bool loaded;
    static float lastHitGround = -10f;
    static float lastClick = -10f;

    static bool Ready()
    {
        if (EndingOverlay.HideHud)
        {
            return false;
        }
        if (sources == null || sources[0] == null)
        {
            GameObject go = GameObject.Find(ObjectName);
            if (go == null)
            {
                go = new GameObject(ObjectName);
                go.hideFlags = HideFlags.HideInHierarchy;
                Object.DontDestroyOnLoad(go);
            }
            sources = new AudioSource[SourceCount];
            AudioSource[] existing = go.GetComponents<AudioSource>();
            for (int i = 0; i < SourceCount; i++)
            {
                AudioSource s = i < existing.Length ? existing[i] : go.AddComponent<AudioSource>();
                s.playOnAwake = false;
                s.spatialBlend = 0f;
                sources[i] = s;
            }
        }
        if (!loaded)
        {
            loaded = true;
            hitGround = Resources.Load<AudioClip>("Sfx/hit_ground");
            click = Resources.Load<AudioClip>("Sfx/click");
        }
        return true;
    }

    static void Play(AudioClip clip, float volume, float pitch)
    {
        if (clip == null)
        {
            return;
        }
        AudioSource s = sources[nextSource];
        nextSource = (nextSource + 1) % sources.Length;
        s.pitch = pitch;
        s.volume = Mathf.Clamp01(volume * masterVolume);
        s.clip = clip;
        s.Play();
    }
    public static void Click(float volume = 1f)
    {
        if (Time.unscaledTime - lastClick < clickMinInterval || !Ready())
        {
            return;
        }
        lastClick = Time.unscaledTime;
        Play(click, clickVolume * volume, Random.Range(0.95f, 1.05f));
    }

    public static void PlayHitGround(float impactSpeed, float thresholdSpeed)
    {
        if (Time.unscaledTime - lastHitGround < hitGroundMinInterval || !Ready())
        {
            return;
        }
        lastHitGround = Time.unscaledTime;
        float t = Mathf.InverseLerp(thresholdSpeed, hitGroundFullSpeed, impactSpeed);
        float volume = Mathf.Lerp(hitGroundMinVolume, 1f, t) * hitGroundVolume;
        Play(hitGround, volume, Random.Range(hitGroundMinPitch, hitGroundMaxPitch));
    }
}
