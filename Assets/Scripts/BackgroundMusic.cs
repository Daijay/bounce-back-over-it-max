using UnityEngine;

public class BackgroundMusic : MonoBehaviour
{
    public float musicVolume = 0.35f;
    public float fadeInTime = 3f;

    public float cinematicQuiet = 0.25f;
    public float cinematicFadeOut = 0.6f;
    public float cinematicFadeIn = 2.5f;

    AudioSource source;
    float started;
    float cinematicGain = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindAnyObjectByType<BackgroundMusic>() != null)
        {
            return;
        }
        AudioClip clip = Resources.Load<AudioClip>("Music/background");
        if (clip == null)
        {
            return;
        }
        GameObject go = new GameObject("BackgroundMusic");
        DontDestroyOnLoad(go);
        BackgroundMusic music = go.AddComponent<BackgroundMusic>();
        music.source = go.AddComponent<AudioSource>();
        music.source.clip = clip;
        music.source.loop = true;
        music.source.spatialBlend = 0f;
        music.source.playOnAwake = false;
        music.source.volume = 0f;
        music.started = Time.unscaledTime;
        music.source.Play();
    }

    void Update()
    {
        if (source == null)
        {
            return;
        }
        float fade = Mathf.Clamp01((Time.unscaledTime - started) / Mathf.Max(0.01f, fadeInTime));
        float target = EndingVideo.Playing ? 0f : (InCinematic() ? cinematicQuiet : 1f);
        float speed = target < cinematicGain ? 1f / Mathf.Max(0.01f, cinematicFadeOut) : 1f / Mathf.Max(0.01f, cinematicFadeIn);
        cinematicGain = Mathf.MoveTowards(cinematicGain, target, speed * Time.unscaledDeltaTime);
        source.volume = Mathf.Clamp01(musicVolume * SoundEffects.masterVolume) * fade * cinematicGain;
    }

    static bool InCinematic()
    {
        EndingOverlay overlay = EndingOverlay.Instance;
        return overlay != null && (overlay.Letterbox > 0.01f || overlay.Fade > 0.01f || EndingOverlay.HideHud);
    }
}
