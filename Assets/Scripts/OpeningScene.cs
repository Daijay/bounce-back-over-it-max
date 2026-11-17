using UnityEngine;

public class OpeningScene : MonoBehaviour
{
    public bool enabledOpening = true;
    public float fadeTime = 0.9f;
    public float charsPerSecond = 28f;

    const float BedLength = 12f;
    const float FallLength = 4.5f;
    const float CardLength = 3.5f;
    const float EndFade = 1.4f;

    static readonly float[] LineTimes = { 1f, 4.6f, 8.2f };
    static readonly string[] Lines =
    {
        "Last night I dreamed about the cheese moon.",
        "I was standing on it, then I fell.",
        "I want to go back up.",
    };

    static bool played;

    Texture2D bedroom;
    Texture2D rat;
    Texture2D moon;
    bool loaded;
    bool seenMenu;
    bool running;
    float startTime;
    GUIStyle textStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        played = false;
        if (FindAnyObjectByType<OpeningScene>() == null)
        {
            new GameObject("OpeningScene").AddComponent<OpeningScene>();
        }
    }

    float SceneEnd { get { return BedLength + FallLength + CardLength; } }

    void Update()
    {
        if (!enabledOpening || played)
        {
            return;
        }
        if (!running)
        {
            if (MainMenu.Showing)
            {
                seenMenu = true;
            }
            else if (seenMenu)
            {
                Begin();
            }
            return;
        }

        PlayerController player = PlayerController.Instance;
        if (player != null)
        {
            player.Frozen = true;
        }
        float t = Time.unscaledTime - startTime;
        if (MouseInput.EscapePressed && t < SceneEnd)
        {
            startTime = Time.unscaledTime - SceneEnd;
            t = SceneEnd;
        }
        if (t >= SceneEnd + EndFade)
        {
            running = false;
            played = true;
            if (player != null)
            {
                player.Frozen = false;
            }
        }
    }

    void Begin()
    {
        if (!loaded)
        {
            loaded = true;
            bedroom = Resources.Load<Texture2D>("Opening/bedroom");
            rat = Resources.Load<Texture2D>("Opening/rat");
            moon = Resources.Load<Texture2D>("Opening/moon_dream");
        }
        if (bedroom == null)
        {
            played = true;
            return;
        }
        running = true;
        startTime = Time.unscaledTime;
        PlayerController player = PlayerController.Instance;
        if (player != null)
        {
            player.Frozen = true;
        }
    }

    void OnGUI()
    {
        if (!running || Event.current.type != EventType.Repaint)
        {
            return;
        }
        GUI.depth = -300;
        float t = Time.unscaledTime - startTime;
        float w = Screen.width;
        float h = Screen.height;
        MakeStyle();

        float black;
        if (t < BedLength)
        {
            DrawBedroom(w, h, t);
            black = SceneFade(t, 0f, BedLength);
        }
        else if (t < BedLength + FallLength)
        {
            DrawFall(w, h, t - BedLength);
            black = SceneFade(t, BedLength, BedLength + FallLength);
        }
        else if (t < SceneEnd)
        {
            DrawCard(w, h, t - BedLength - FallLength);
            black = SceneFade(t, BedLength + FallLength, SceneEnd);
        }
        else
        {
            black = 1f - Mathf.Clamp01((t - SceneEnd - 0.2f) / (EndFade - 0.2f));
        }
        if (black > 0f)
        {
            Fill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, black));
        }
        if (t < SceneEnd - 1f)
        {
            Small(w, h, "Esc to skip");
        }
    }

    float SceneFade(float t, float from, float to)
    {
        float fadeIn = 1f - Mathf.Clamp01((t - from) / fadeTime);
        float fadeOut = Mathf.Clamp01((t - (to - fadeTime)) / fadeTime);
        return Mathf.Max(fadeIn, fadeOut);
    }

    void DrawBedroom(float w, float h, float t)
    {
        float scale = Mathf.Max(w / 150f, h / 100f);
        Vector2 origin = new Vector2((w - 150f * scale) * 0.5f, (h - 100f * scale) * 0.5f);
        GUI.DrawTexture(new Rect(origin.x, origin.y, 150f * scale, 100f * scale), bedroom, ScaleMode.StretchToFill, false);
        Fill(new Rect(0f, 0f, w, h), new Color(0.02f, 0.01f, 0.06f, 0.45f));

        if (rat != null)
        {
            float ratW = 31f * 0.78f * scale;
            float ratH = 27f * 0.78f * scale;
            Vector2 centre = new Vector2(origin.x + 30f * scale, origin.y + 58.5f * scale);
            float breath = 1f + 0.03f * Mathf.Sin(t * 1.6f);
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(90f, centre);
            GUI.DrawTexture(new Rect(centre.x - ratW * 0.5f, centre.y - ratH * 0.5f * breath, ratW, ratH * breath), rat, ScaleMode.StretchToFill, true);
            GUI.matrix = old;
        }

        int current = -1;
        for (int i = 0; i < LineTimes.Length; i++)
        {
            if (t >= LineTimes[i])
            {
                current = i;
            }
        }
        if (current >= 0)
        {
            string line = Lines[current];
            int count = Mathf.Clamp(Mathf.FloorToInt((t - LineTimes[current]) * charsPerSecond), 0, line.Length);
            if (count > 0)
            {
                textStyle.fontSize = GuiText.Size(h, 0.036f, 14);
                DrawOutlined(new Rect(w * 0.1f, h * 0.84f, w * 0.8f, h * 0.1f), line.Substring(0, count), Color.white);
            }
        }
    }

    void DrawFall(float w, float h, float s)
    {
        Fill(new Rect(0f, 0f, w, h), new Color(0.02f, 0.03f, 0.09f, 1f));
        float starSize = Mathf.Max(2f, Mathf.Round(h * 0.006f));
        for (int i = 0; i < 4; i++)
        {
            float x = Mathf.Round(w * (0.15f + 0.23f * i));
            float y = Mathf.Repeat(h * (0.3f + 0.27f * i) - s * h * 0.35f, h);
            Fill(new Rect(x, Mathf.Round(y), starSize, starSize), new Color(1f, 1f, 0.9f, 0.9f));
        }
        if (moon != null)
        {
            float picH = h * 0.4f;
            float picW = picH * moon.width / moon.height;
            GUI.DrawTexture(new Rect(Mathf.Round(w * 0.5f - picW * 0.5f), Mathf.Round(h * 0.03f), picW, picH), moon, ScaleMode.StretchToFill, true);
        }
        if (rat != null)
        {
            float size = h * 0.16f;
            float ratH = size * 27f / 31f;
            Vector2 centre = new Vector2(w * 0.5f, Mathf.Lerp(h * 0.4f, h * 0.8f, Mathf.Clamp01(s / FallLength)));
            Matrix4x4 old = GUI.matrix;
            GUIUtility.RotateAroundPivot(s * 60f, centre);
            GUI.DrawTexture(new Rect(centre.x - size * 0.5f, centre.y - ratH * 0.5f, size, ratH), rat, ScaleMode.StretchToFill, true);
            GUI.matrix = old;
        }
    }

    void DrawCard(float w, float h, float s)
    {
        Fill(new Rect(0f, 0f, w, h), Color.black);
        textStyle.fontSize = GuiText.Size(h, 0.05f, 16);
        DrawOutlined(new Rect(0f, h * 0.36f, w, h * 0.08f), "MISSION", new Color(1f, 0.82f, 0.3f, 1f));
        textStyle.fontSize = GuiText.Size(h, 0.06f, 18);
        string line = "Reach the moon.";
        int count = Mathf.Clamp(Mathf.FloorToInt((s - 0.6f) * 20f), 0, line.Length);
        DrawOutlined(new Rect(0f, h * 0.46f, w, h * 0.1f), line.Substring(0, count), Color.white);
    }

    static void Fill(Rect r, Color c)
    {
        Color old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void MakeStyle()
    {
        if (textStyle == null)
        {
            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.wordWrap = true;
        }
    }

    void DrawOutlined(Rect rect, string text, Color colour)
    {
        float o = Mathf.Max(1f, Mathf.Round(Screen.height * 0.0035f));
        Color saved = textStyle.normal.textColor;
        textStyle.normal.textColor = new Color(0f, 0f, 0f, 0.85f * colour.a);
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx != 0 || dy != 0)
                {
                    GuiText.Label(new Rect(rect.x + dx * o, rect.y + dy * o, rect.width, rect.height), text, textStyle);
                }
            }
        }
        textStyle.normal.textColor = colour;
        GuiText.Label(rect, text, textStyle);
        textStyle.normal.textColor = saved;
    }

    void Small(float w, float h, string text)
    {
        textStyle.fontSize = GuiText.Size(h, 0.02f, 10);
        textStyle.alignment = TextAnchor.MiddleRight;
        textStyle.normal.textColor = new Color(1f, 1f, 1f, 0.55f);
        GuiText.Label(new Rect(w * 0.6f, h * 0.95f, w * 0.38f, h * 0.04f), text, textStyle);
        textStyle.alignment = TextAnchor.MiddleCenter;
    }
}
