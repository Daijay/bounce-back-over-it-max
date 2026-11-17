using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public static MainMenu Instance { get; private set; }

    public static bool SkipNextMenu;

    public static bool Showing { get { return Instance != null && Instance.visible; } }

    public static float RunSeconds()
    {
        if (Instance != null && Instance.started)
        {
            return Time.time - Instance.runStart;
        }
        return Time.timeSinceLevelLoad;
    }

    [Header("Look")]
    public float backgroundAlpha = 0.85f;
    public float fadeTime = 1.0f;
    public float fadeZoom = 0.08f;
    public float backgroundDim = 0.25f;
    public float hoverVolume = 0.25f;
    public Color panelColor = new Color(0.45f, 0.45f, 0.47f, 1f);
    public Color panelHoverColor = new Color(0.58f, 0.58f, 0.60f, 1f);
    public Color borderColor = new Color(0.22f, 0.22f, 0.24f, 1f);
    public Color hoverColor = new Color(0.85f, 0.10f, 0.10f, 1f);
    public Color titleColor = new Color(0.04f, 0.03f, 0.03f, 1f);
    public Color glowColor = new Color(0.55f, 0.02f, 0.04f, 1f);
    public Color outlineColor = new Color(0.40f, 0.03f, 0.05f, 1f);

    const string VolumeKey = "BounceBack.MasterVolume";
    const string Title = "BOUNCE BACK OVER IT";
    const string Credit = "Designed by Jayden, Brent, Tim";

#if UNITY_WEBGL
    const int MainButtons = 2;
#else
    const int MainButtons = 3;
#endif
    static readonly string[] MainLabels = { "PLAY", "SETTINGS", "QUIT" };
    static readonly Vector2[] Dir8 =
    {
        new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(0.7f, 0.7f), new Vector2(-0.7f, 0.7f), new Vector2(0.7f, -0.7f), new Vector2(-0.7f, -0.7f),
    };

    const float PicW = 300f;
    const float PicH = 200f;
    const float CapeAnchorX = 200f;
    const float CapeAnchorY = 71f;
    const int CapeLength = 44;
    const int CapeBlock = 2;
    const float CapeWaveSpeed = 1.1f;
    static readonly Color CapeLight = new Color(0.50f, 0.17f, 0.20f, 1f);
    static readonly Color CapeMid = new Color(0.38f, 0.12f, 0.16f, 1f);
    static readonly Color CapeDark = new Color(0.26f, 0.08f, 0.12f, 1f);

    PlayerController player;
    Texture2D picture;
    bool pictureTried;
    [SerializeField] bool visible = true;
    [SerializeField] bool started;
    [SerializeField] float runStart;
    bool fading;
    float fadeStart;
    bool settingsOpen;
    bool dragging;
    int hoverIndex = -1;
    int shownPercent = -1;
    string percentText = "100%";

    readonly Rect[] rects = new Rect[6];
    GUIStyle titleStyle;
    GUIStyle buttonStyle;
    GUIStyle smallStyle;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        if (SkipNextMenu)
        {
            SkipNextMenu = false;
            visible = false;
            started = true;
            runStart = Time.time;
            return;
        }
        visible = true;
        started = false;
        Freeze();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        Instance = this;
        titleStyle = null;
        buttonStyle = null;
        smallStyle = null;
        picture = null;
        pictureTried = false;
        if (player == null)
        {
            player = GetComponent<PlayerController>();
        }
        SoundEffects.masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f));
        if (visible && !started)
        {
            Freeze();
        }
    }
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Freeze()
    {
        if (player != null)
        {
            player.Frozen = true;
        }
    }


    void Layout()
    {
        float w = Screen.width;
        float h = Screen.height;
        float bw = Mathf.Min(h * 0.55f, w * 0.7f);
        float bh = h * 0.085f;
        float gap = h * 0.025f;
        float x = (w - bw) * 0.5f;
        float y = h * 0.46f;
        for (int i = 0; i < 3; i++)
        {
            rects[i] = new Rect(x, y + i * (bh + gap), bw, bh);
        }
        rects[3] = new Rect(x, y + bh * 0.9f, bw, bh * 0.5f);
        rects[4] = new Rect(x, y + bh * 0.9f + bh + gap, bw, bh);
        rects[5] = new Rect(x, y + (bh + gap) * 2f + bh * 0.9f + gap, bw, bh);
    }


    void Update()
    {
        if (!visible)
        {
            return;
        }
        if (fading)
        {
            if (Time.time - fadeStart >= fadeTime)
            {
                visible = false;
                fading = false;
            }
            return;
        }

        Layout();
        Vector2 mouse = MouseInput.ScreenPosition;
        mouse.y = Screen.height - mouse.y;
        bool click = MouseInput.LeftClickPressed;

        int hover = -1;
        if (!settingsOpen)
        {
            for (int i = 0; i < MainButtons; i++)
            {
                if (rects[i].Contains(mouse))
                {
                    hover = i;
                }
            }
        }
        else
        {
            Rect slider = rects[3];
            slider.y -= slider.height * 0.5f;
            slider.height *= 2f;
            if (slider.Contains(mouse))
            {
                hover = 3;
            }
            else if (rects[4].Contains(mouse))
            {
                hover = 4;
            }
            else if (rects[5].Contains(mouse))
            {
                hover = 5;
            }
        }
        if (hover != hoverIndex)
        {
            hoverIndex = hover;
            if (hover >= 0 && !dragging)
            {
                SoundEffects.Click(hoverVolume);
            }
        }

        if (dragging)
        {
            if (MouseInput.LeftHeld)
            {
                SetVolumeFromMouse(mouse.x);
            }
            else
            {
                dragging = false;
                PlayerPrefs.SetFloat(VolumeKey, SoundEffects.masterVolume);
                PlayerPrefs.Save();
            }
        }

        if (click && hover >= 0)
        {
            SoundEffects.Click();
            Press(hover, mouse.x);
        }

        int percent = Mathf.RoundToInt(SoundEffects.masterVolume * 100f);
        if (percent != shownPercent)
        {
            shownPercent = percent;
            percentText = percent + "%";
        }
    }

    void Press(int index, float mouseX)
    {
        if (!settingsOpen)
        {
            if (index == 0)
            {
                Play();
            }
            else if (index == 1)
            {
                settingsOpen = true;
                hoverIndex = -1;
            }
            else if (index == 2)
            {
                Quit();
            }
            return;
        }
        if (index == 3)
        {
            dragging = true;
            SetVolumeFromMouse(mouseX);
        }
        else if (index == 4)
        {
            Screen.fullScreen = !Screen.fullScreen;
        }
        else if (index == 5)
        {
            settingsOpen = false;
            hoverIndex = -1;
        }
    }

    void SetVolumeFromMouse(float mouseX)
    {
        Rect track = rects[3];
        SoundEffects.masterVolume = Mathf.Clamp01((mouseX - track.x) / Mathf.Max(1f, track.width));
    }

    void Play()
    {
        started = true;
        runStart = Time.time;
        fading = true;
        fadeStart = Time.time;
        if (player != null)
        {
            player.Frozen = false;
        }
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }


    static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    static void Border(Rect r, float t, Color color)
    {
        Fill(new Rect(r.x, r.y, r.width, t), color);
        Fill(new Rect(r.x, r.yMax - t, r.width, t), color);
        Fill(new Rect(r.x, r.y, t, r.height), color);
        Fill(new Rect(r.xMax - t, r.y, t, r.height), color);
    }

    static void Label(GUIStyle style, Rect rect, string text, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GuiText.Label(rect, text, style);
        GUI.color = old;
    }

    static void Ring(GUIStyle style, Rect rect, string text, float radius, Color color)
    {
        for (int i = 0; i < Dir8.Length; i++)
        {
            Label(style, new Rect(rect.x + Dir8[i].x * radius, rect.y + Dir8[i].y * radius, rect.width, rect.height), text, color);
        }
    }

    void MakeStyles()
    {
        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle = new GUIStyle(GUI.skin.label);
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        smallStyle = new GUIStyle(GUI.skin.label);
        smallStyle.alignment = TextAnchor.LowerLeft;
    }

    void OnGUI()
    {
        GUI.depth = -50;
        if (!visible || Event.current.type != EventType.Repaint)
        {
            return;
        }
        if (titleStyle == null)
        {
            MakeStyles();
        }
        Layout();

        float w = Screen.width;
        float h = Screen.height;
        float a = fading ? 1f - Mathf.Clamp01((Time.time - fadeStart) / Mathf.Max(0.01f, fadeTime)) : 1f;

        float ease = a * a * (3f - 2f * a);
        a = ease;
        DrawPicture(w, h, a);

        titleStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(Mathf.Min(h * 0.08f, w * 0.075f)));
        Rect titleRect = new Rect(0f, h * 0.18f, w, h * 0.16f);
        float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 1.3f);
        float glow = titleStyle.fontSize * 0.07f;
        Color g = glowColor;
        g.a = Mathf.Lerp(0.10f, 0.28f, pulse) * a;
        Ring(titleStyle, titleRect, Title, glow * 2.2f, g);
        g.a = Mathf.Lerp(0.18f, 0.42f, pulse) * a;
        Ring(titleStyle, titleRect, Title, glow, g);
        Color o = outlineColor;
        o.a = a;
        Ring(titleStyle, titleRect, Title, Mathf.Max(1f, titleStyle.fontSize * 0.02f), o);
        Color tc = titleColor;
        tc.a = a;
        Label(titleStyle, titleRect, Title, tc);

        buttonStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(h * 0.035f));
        float line = Mathf.Max(1f, h * 0.003f);
        if (!settingsOpen)
        {
            for (int i = 0; i < MainButtons; i++)
            {
                DrawButton(rects[i], MainLabels[i], hoverIndex == i, a, line);
            }
        }
        else
        {
            DrawSettings(a, line, h);
        }

        smallStyle.fontSize = Mathf.Max(8, Mathf.RoundToInt(h * 0.022f));
        Label(smallStyle, new Rect(h * 0.02f, 0f, w, h - h * 0.015f), Credit, new Color(0.6f, 0.6f, 0.62f, 0.8f * a));
    }

    void DrawPicture(float w, float h, float a)
    {
        if (!pictureTried)
        {
            pictureTried = true;
            picture = Resources.Load<Texture2D>("Menu/main_menu_background");
            if (picture != null)
            {
                picture.filterMode = FilterMode.Point;
            }
        }
        if (picture == null)
        {
            Fill(new Rect(0f, 0f, w, h), new Color(0.01f, 0.01f, 0.012f, backgroundAlpha * a));
            return;
        }
        float fade = fading ? Mathf.Clamp01((Time.time - fadeStart) / Mathf.Max(0.01f, fadeTime)) : 0f;
        float s = Mathf.Max(w / PicW, h / PicH) * (1f + fadeZoom * fade * fade * (3f - 2f * fade));
        float ox = (w - PicW * s) * 0.5f;
        float oy = (h - PicH * s) * 0.5f;
        Color old = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, a);
        GUI.DrawTexture(new Rect(ox, oy, PicW * s, PicH * s), picture, ScaleMode.StretchToFill, true);
        GUI.color = old;
        Fill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0.02f, backgroundDim * a));
        DrawCape(ox, oy, s, a);
    }

    void DrawCape(float ox, float oy, float s, float a)
    {
        float t = Time.unscaledTime;
        float b = CapeBlock;
        int columns = CapeLength / CapeBlock;
        for (int c = 0; c < columns; c++)
        {
            int i = c * CapeBlock;
            float k = c / (float)(columns - 1);
            float phase = t * CapeWaveSpeed - i * 0.16f;
            float wave = Mathf.Sin(phase) * (0.3f + 1.7f * k)
                       + Mathf.Sin(t * CapeWaveSpeed * 0.55f - i * 0.09f) * 0.8f * k;
            float top = CapeAnchorY + k * 8f + wave;
            float height = 6f + 8f * Mathf.Sin(k * Mathf.PI * 0.5f) - k * k * 4f;
            float x = Mathf.Round((CapeAnchorX - i - b) / b) * b;
            float y0 = Mathf.Round(top / b) * b;
            float hh = Mathf.Max(b * 2f, Mathf.Round(height / b) * b);
            float fold = Mathf.Sin(phase + 1.2f);
            Color body = fold > 0.45f ? CapeLight : (fold < -0.45f ? CapeDark : CapeMid);
            body.a = a;
            Fill(new Rect(ox + x * s, oy + y0 * s, b * s, hh * s), body);
            Color edge = CapeDark;
            edge.a = a;
            Fill(new Rect(ox + x * s, oy + (y0 + hh - b) * s, b * s, b * s), edge);
            Color rim = CapeMid;
            rim.a = a;
            Fill(new Rect(ox + x * s, oy + y0 * s, b * s, b * s), rim);
        }
    }

    void DrawButton(Rect r, string text, bool hover, float a, float line)
    {
        Color panel = hover ? panelHoverColor : panelColor;
        panel.a = a;
        Fill(r, panel);
        Color border = hover ? hoverColor : borderColor;
        border.a = a;
        Border(r, line, border);
        Color t = Color.white;
        t.a = a;
        Label(buttonStyle, r, text, t);
    }

    void DrawSettings(float a, float line, float h)
    {
        Rect track = rects[3];
        Rect labelRect = new Rect(track.x, track.y - h * 0.06f, track.width, h * 0.05f);
        buttonStyle.alignment = TextAnchor.MiddleLeft;
        Label(buttonStyle, labelRect, "Master volume", new Color(0.85f, 0.85f, 0.87f, a));
        buttonStyle.alignment = TextAnchor.MiddleRight;
        Label(buttonStyle, labelRect, percentText, new Color(0.85f, 0.85f, 0.87f, a));
        buttonStyle.alignment = TextAnchor.MiddleCenter;

        Color panel = panelColor;
        panel.a = a;
        Fill(track, panel);
        Color hot = hoverIndex == 3 || dragging ? hoverColor : borderColor;
        hot.a = a;
        Fill(new Rect(track.x, track.y, track.width * Mathf.Clamp01(SoundEffects.masterVolume), track.height), hot);
        Border(track, line, new Color(borderColor.r, borderColor.g, borderColor.b, a));

        Rect tog = rects[4];
        DrawButton(tog, Screen.fullScreen ? "FULLSCREEN: ON" : "FULLSCREEN: OFF", hoverIndex == 4, a, line);

        DrawButton(rects[5], "BACK", hoverIndex == 5, a, line);
    }
}
