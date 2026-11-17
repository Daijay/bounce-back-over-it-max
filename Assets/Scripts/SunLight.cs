using UnityEngine;

public class SunLight : MonoBehaviour
{
    public bool sunOn = true;
    public Color sunColour = new Color(1f, 0.88f, 0.45f, 1f);
    public float glowStrength = 0.38f;
    public float rayStrength = 0.10f;
    public float tintStrength = 0.035f;
    public float fadeStartY = 55f;
    public float fadeEndY = 90f;
    public float swayPeriod = 9f;
    public int guiDepth = 10;

    static readonly float[] RayAngle = { 18f, 34f, 52f, 70f };
    static readonly float[] RayWidth = { 0.07f, 0.11f, 0.06f, 0.09f };
    static readonly float[] RayPhase = { 0f, 1.7f, 3.1f, 4.6f };

    Texture2D glowTexture;
    Texture2D rayTexture;
    Camera cam;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindAnyObjectByType<SunLight>() == null)
        {
            new GameObject("SunLight").AddComponent<SunLight>();
        }
    }

    void OnEnable()
    {
        if (glowTexture == null)
        {
            glowTexture = MakeGlow(64);
            rayTexture = MakeRay(32, 64);
        }
    }

    void OnDestroy()
    {
        if (glowTexture != null)
        {
            Destroy(glowTexture);
        }
        if (rayTexture != null)
        {
            Destroy(rayTexture);
        }
    }

    static Texture2D MakeGlow(int n)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.DontSave;
        Color32[] pixels = new Color32[n * n];
        float half = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    static Texture2D MakeRay(int w, int h)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        tex.hideFlags = HideFlags.DontSave;
        Color32[] pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            float along = 1f - y / (float)(h - 1);
            float lengthFade = along * along;
            for (int x = 0; x < w; x++)
            {
                float across = 1f - Mathf.Abs((x + 0.5f) / w * 2f - 1f);
                across = across * across * (3f - 2f * across);
                pixels[y * w + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(across * lengthFade * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (!sunOn || Event.current.type != EventType.Repaint || glowTexture == null)
        {
            return;
        }
        if (MainMenu.Showing || EndingOverlay.HideHud)
        {
            return;
        }
        if (cam == null)
        {
            cam = Camera.main;
        }
        float height = 1f;
        if (cam != null)
        {
            height = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(fadeStartY, fadeEndY, cam.transform.position.y));
        }
        if (height <= 0.002f)
        {
            return;
        }

        GUI.depth = guiDepth;
        float w = Screen.width;
        float h = Screen.height;
        Color old = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;

        GUI.color = new Color(sunColour.r, sunColour.g, sunColour.b, tintStrength * height);
        GUI.DrawTexture(new Rect(0f, 0f, w, h), Texture2D.whiteTexture);

        Vector2 sun = new Vector2(-w * 0.04f, -h * 0.06f);
        float length = Mathf.Sqrt(w * w + h * h) * 1.15f;
        float time = Time.unscaledTime * (2f * Mathf.PI / Mathf.Max(1f, swayPeriod));
        for (int i = 0; i < RayAngle.Length; i++)
        {
            float breathe = 0.65f + 0.35f * Mathf.Sin(time + RayPhase[i]);
            float angle = RayAngle[i] + Mathf.Sin(time * 0.5f + RayPhase[i]) * 1.5f;
            float width = RayWidth[i] * h;
            GUI.matrix = oldMatrix;
            GUIUtility.RotateAroundPivot(90f - angle, sun);
            GUI.color = new Color(sunColour.r, sunColour.g, sunColour.b, rayStrength * breathe * height);
            GUI.DrawTexture(new Rect(sun.x - width * 0.5f, sun.y, width, length), rayTexture);
        }
        GUI.matrix = oldMatrix;

        float glowSize = h * 1.1f;
        GUI.color = new Color(sunColour.r, sunColour.g, sunColour.b, glowStrength * height);
        GUI.DrawTexture(new Rect(sun.x - glowSize * 0.5f, sun.y - glowSize * 0.5f, glowSize, glowSize), glowTexture);

        GUI.color = old;
    }
}
