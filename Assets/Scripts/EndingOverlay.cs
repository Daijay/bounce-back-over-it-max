using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class EndingOverlay : MonoBehaviour
{
    public static EndingOverlay Instance { get; private set; }

    public float Letterbox;
    public float LetterboxFraction = 0.12f;
    public float Fade;
    public Color cardColor = new Color(1f, 0.85f, 0.2f);

    float flash;
    float flashTime = 0.3f;
    float lowGravityUntil = -1f;
    string storyLine = "";
    float lowGravityTime = 2f;
    bool cardVisible;
    string cardTime = "";
    GUIStyle style;
    GUIStyle creditStyle;

    static readonly string[] Credits =
    {
        "<color=#8a8a8e>Main coder: </color><color=#ffffff>Jayden</color>",
        "<color=#8a8a8e>Play tester and sound effects: </color><color=#ffffff>Brent</color>",
        "<color=#8a8a8e>Artworks and design: </color><color=#ffffff>Tim</color>",
    };

    public static bool HideHud { get { return Instance != null && Instance.cardVisible; } }
    void OnEnable()
    {
        Instance = this;
        style = null;
        creditStyle = null;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
        Letterbox = 0f;
    }

    public void TriggerFlash(float seconds)
    {
        flash = 1f;
        flashTime = Mathf.Max(0.05f, seconds);
    }

    public void SetStoryLine(string text)
    {
        storyLine = text ?? "";
    }

    public void ShowLowGravity(float seconds)
    {
        lowGravityTime = Mathf.Max(0.1f, seconds);
        lowGravityUntil = Time.time + lowGravityTime;
    }

    public void ResetState()
    {
        flash = 0f;
        lowGravityUntil = -1f;
        Fade = 0f;
        Letterbox = 0f;
        storyLine = "";
    }

    public void ShowCard(string runTimeText)
    {
        cardTime = runTimeText;
        cardVisible = true;
    }

    void Update()
    {
        if (flash > 0f)
        {
            flash = Mathf.MoveTowards(flash, 0f, Time.deltaTime / flashTime);
        }
        if (cardVisible && MouseInput.RestartPressed)
        {
            Restart();
        }
    }

    static void Restart()
    {
        MainMenu.SkipNextMenu = true;
#if UNITY_EDITOR

        EditorSceneManager.LoadSceneInPlayMode(SceneManager.GetActiveScene().path, new LoadSceneParameters(LoadSceneMode.Single));
#else
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
#endif
    }

    static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void OnGUI()
    {
        GUI.depth = -100;
        if (Event.current.type != EventType.Repaint)
        {
            return;
        }
        float w = Screen.width;
        float h = Screen.height;

        if (Letterbox > 0f)
        {
            float bar = h * LetterboxFraction * Mathf.Clamp01(Letterbox);
            Fill(new Rect(0f, 0f, w, bar), Color.black);
            Fill(new Rect(0f, h - bar, w, bar), Color.black);
        }
        if (flash > 0f)
        {
            Fill(new Rect(0f, 0f, w, h), new Color(1f, 1f, 1f, flash));
        }

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
        }

        float left = lowGravityUntil - Time.time;
        if (left > 0f)
        {
            style.fontSize = Mathf.RoundToInt(h * 0.05f);
            DrawText(new Rect(0f, h * 0.16f, w, h * 0.08f), "LOW GRAVITY", new Color(0.6f, 0.9f, 1f, Mathf.Clamp01(left / 0.5f)));
        }

        if (Fade > 0f)
        {
            Fill(new Rect(0f, 0f, w, h), new Color(0f, 0f, 0f, Mathf.Clamp01(Fade)));
        }

        if (storyLine.Length > 0)
        {
            style.fontSize = GuiText.Size(h, 0.045f, 16);
            DrawText(new Rect(0f, h * 0.44f, w, h * 0.1f), storyLine, Color.white);
        }

        if (cardVisible)
        {
            style.fontSize = Mathf.RoundToInt(h * 0.1f);
            DrawText(new Rect(0f, h * 0.28f, w, h * 0.14f), "YOU BOUNCED BACK", cardColor);
            style.fontSize = Mathf.RoundToInt(h * 0.05f);
            DrawText(new Rect(0f, h * 0.46f, w, h * 0.08f), cardTime, Color.white);
            if (creditStyle == null)
            {
                creditStyle = new GUIStyle(style);
                creditStyle.richText = true;
                creditStyle.alignment = TextAnchor.MiddleCenter;
            }
            creditStyle.fontSize = Mathf.Max(9, Mathf.RoundToInt(Mathf.Min(h * 0.026f, w * 0.03f)));
            GUIStyle saved = style;
            style = creditStyle;
            for (int i = 0; i < Credits.Length; i++)
            {
                DrawText(new Rect(0f, h * (0.545f + i * 0.042f), w, h * 0.042f), Credits[i], Color.white);
            }
            style = saved;
            style.fontSize = Mathf.RoundToInt(h * 0.035f);
            DrawText(new Rect(0f, h * 0.70f, w, h * 0.06f), "Press R to play again", new Color(1f, 1f, 1f, 0.7f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f)));
        }
    }

    void DrawText(Rect rect, string text, Color color)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f * color.a);
        GuiText.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        GUI.color = color;
        GuiText.Label(rect, text, style);
        GUI.color = old;
    }
}
