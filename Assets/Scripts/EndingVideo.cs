using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class EndingVideo : MonoBehaviour
{
    public float fadeTime = 0.8f;
    public float prepareTimeout = 6f;

    public string[] files = { "Ending/part1.mp4", "Ending/part2.mp4" };

    public static bool Playing;

    VideoPlayer player;
    RenderTexture texture;
    bool showFrame;
    float black = 1f;

    public bool Done { get; private set; }

    string PathOf(int part)
    {
        return Application.streamingAssetsPath + "/" + files[part - 1];
    }

    public bool HasFiles
    {
        get
        {
#if UNITY_WEBGL
            return true;
#else
            for (int i = 1; i <= files.Length; i++)
            {
                if (!System.IO.File.Exists(PathOf(i)))
                {
                    return false;
                }
            }
            return true;
#endif
        }
    }
    public void Begin()
    {
        Done = false;
        StopAllCoroutines();
        StartCoroutine(Run());
    }

    void OnDisable()
    {
        Cleanup();
    }

    void Cleanup()
    {
        Playing = false;
        showFrame = false;
        if (player != null)
        {
            player.Stop();
        }
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
            texture = null;
        }
        if (smallTexture != null)
        {
            smallTexture.Release();
            Destroy(smallTexture);
            smallTexture = null;
        }
        if (smallReadback != null)
        {
            Destroy(smallReadback);
            smallReadback = null;
        }
    }

    IEnumerator Run()
    {
        Playing = true;
        black = 0f;
        yield return Fade(1f);
        for (int part = 1; part <= files.Length; part++)
        {
            yield return PlayPart(part);
            yield return Fade(1f);
        }
        showFrame = false;
        black = 1f;
        Done = true;
    }

    public void Finish()
    {
        Cleanup();
    }

    IEnumerator Fade(float target)
    {
        float start = black;
        float end = target;
        for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
        {
            black = Mathf.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / fadeTime));
            yield return null;
        }
        black = end;
    }

    string lastError;
    float firstFrameAt = -1f;
    RenderTexture smallTexture;
    Texture2D smallReadback;

    void OnFrameReady(VideoPlayer vp, long frameIdx)
    {
        if (firstFrameAt < 0f)
        {
            firstFrameAt = Time.unscaledTime;
        }
    }

    bool FrameIsBlack()
    {
        if (texture == null)
        {
            return true;
        }
        if (smallTexture == null)
        {
            smallTexture = new RenderTexture(8, 8, 0, RenderTextureFormat.ARGB32);
            smallReadback = new Texture2D(8, 8, TextureFormat.RGBA32, false);
        }
        Graphics.Blit(texture, smallTexture);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = smallTexture;
        smallReadback.ReadPixels(new Rect(0f, 0f, 8f, 8f), 0, 0);
        RenderTexture.active = previous;
        Color32[] pixels = smallReadback.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].r > 8 || pixels[i].g > 8 || pixels[i].b > 8)
            {
                return false;
            }
        }
        return true;
    }

    void OnVideoError(VideoPlayer vp, string message)
    {
        lastError = message;
        Debug.LogWarning("EndingVideo: the video player reported: " + message);
    }

    IEnumerator Prepare(int part)
    {
#if UNITY_WEBGL
        string[] tries = { PathOf(part) };
#else
        string plain = PathOf(part).Replace(System.IO.Path.DirectorySeparatorChar, '/');
        string address = new System.Uri(PathOf(part)).AbsoluteUri;
        string[] tries = { plain, address };
#endif
        for (int i = 0; i < tries.Length; i++)
        {
            player.Stop();
            player.url = tries[i];
            player.Prepare();
            float waited = 0f;
            while (!player.isPrepared && waited < prepareTimeout)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            if (player.isPrepared)
            {
                yield break;
            }
        }
    }

    IEnumerator PlayPart(int part)
    {
        if (player == null)
        {
            GameObject go = new GameObject("Ending Video Player");
            go.transform.SetParent(transform, false);
            player = go.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.isLooping = false;
            player.waitForFirstFrame = true;
            player.skipOnDrop = true;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.errorReceived += OnVideoError;
            player.sendFrameReadyEvents = true;
            player.frameReady += OnFrameReady;
        }
        player.source = VideoSource.Url;
        lastError = null;
        yield return Prepare(part);
        if (!player.isPrepared)
        {
            Debug.LogWarning("EndingVideo: part " + part + " did not load (" + PathOf(part) + "), skipped. " + lastError);
            yield break;
        }

        int w = Mathf.Max(16, (int)player.width);
        int h = Mathf.Max(16, (int)player.height);
        if (texture == null || texture.width != w || texture.height != h)
        {
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }
            texture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
            texture.Create();
        }
        player.targetTexture = texture;
        for (ushort track = 0; track < player.audioTrackCount; track++)
        {
            player.SetDirectAudioVolume(track, Mathf.Clamp01(SoundEffects.masterVolume));
        }
        bool finished = false;
        VideoPlayer.EventHandler onEnd = vp => finished = true;
        player.loopPointReached += onEnd;
        firstFrameAt = -1f;
        float playStarted = Time.unscaledTime;
        player.Play();
        float start = 0f;
        while (!player.isPlaying && start < 3f)
        {
            start += Time.unscaledDeltaTime;
            yield return null;
        }
        showFrame = true;
        yield return Fade(0f);
        float length = (float)player.length;
        bool logged = false;
        bool seenPicture = false;
        float blackFor = 0f;
        float nextCheck = 0f;
        while (!finished && (player.isPlaying || player.time < length - 0.2f) && player.time < length + 1f)
        {
            if (!logged && firstFrameAt >= 0f)
            {
                logged = true;
                Debug.Log("EndingVideo: part " + part + " (" + files[part - 1] + ") playing, " + player.width + " x " + player.height + ", " + player.frameCount
                    + " frames, first frame " + (firstFrameAt - playStarted).ToString("0.00") + " s after Play.");
            }
            if (!seenPicture && player.isPlaying && Time.unscaledTime >= nextCheck)
            {
                nextCheck = Time.unscaledTime + 0.25f;
                if (FrameIsBlack())
                {
                    blackFor += 0.25f;
                    if (blackFor >= 1.5f)
                    {
                        Debug.LogWarning("EndingVideo: part " + part + " is prepared and playing but the frame stays black (" + files[part - 1]
                            + ", " + player.width + " x " + player.height + ", first frame "
                            + (firstFrameAt < 0f ? "never" : (firstFrameAt - playStarted).ToString("0.00") + " s") + "). Skipped.");
                        player.Stop();
                        break;
                    }
                }
                else
                {
                    seenPicture = true;
                }
            }
            if (!player.isPlaying && player.isPrepared)
            {
                player.Play();
            }
            yield return null;
        }
        player.loopPointReached -= onEnd;
        if (!logged)
        {
            Debug.Log("EndingVideo: part " + part + " (" + files[part - 1] + ") played, " + player.width + " x " + player.height + ", " + player.frameCount
                + " frames, first frame " + (firstFrameAt < 0f ? "never reported" : (firstFrameAt - playStarted).ToString("0.00") + " s after Play") + ".");
        }
        yield return Fade(1f);
        showFrame = false;
    }

    void OnGUI()
    {
        if (!Playing || Event.current.type != EventType.Repaint)
        {
            return;
        }
        GUI.depth = -250;
        float w = Screen.width;
        float h = Screen.height;
        Color old = GUI.color;
        if (showFrame && texture != null)
        {
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float scale = Mathf.Min(w / texture.width, h / texture.height);
            float vw = texture.width * scale;
            float vh = texture.height * scale;
            GUI.DrawTexture(new Rect((w - vw) * 0.5f, (h - vh) * 0.5f, vw, vh), texture, ScaleMode.StretchToFill, false);
        }
        if (black > 0.001f)
        {
            GUI.color = new Color(0f, 0f, 0f, black);
            GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);
        }
        GUI.color = old;
    }
}
