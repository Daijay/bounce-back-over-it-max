using UnityEngine;

public class GameHUD : MonoBehaviour
{
    public PlayerController player;
    public float textScale = 0.035f;
    public float barWidth = 0.2f;
    public float barHeight = 0.025f;
    public Color chargeColor = new Color(1f, 0.85f, 0.1f);
    public Color goalColor = new Color(1f, 0.85f, 0.2f);

    GUIStyle style;
    bool showDebug;
    float fps;
    float nextDebugRefresh;

    string heightText = "Height: 0.0 m";
    string bestText = "Best: 0.0 m";
    int shownHeight = int.MinValue;
    int shownBest = int.MinValue;
    string chargeText = "Charge 0";
    int shownCharge = int.MinValue;
    string fpsText = "";
    string stepText = "";
    string errorText = "";

    void OnEnable()
    {
        style = null;
        if (player == null)
        {
            player = PlayerController.Instance;
        }
    }

    void Start()
    {
        if (player == null)
        {
            player = PlayerController.Instance;
        }
    }

    void Update()
    {
        if (player == null)
        {
            return;
        }

        int height = Mathf.RoundToInt(player.CurrentHeight * 10f);
        if (height != shownHeight)
        {
            shownHeight = height;
            heightText = "Height: " + (height / 10f).ToString("F1") + " m";
        }
        int best = Mathf.RoundToInt(player.BestHeight * 10f);
        if (best != shownBest)
        {
            shownBest = best;
            bestText = "Best: " + (best / 10f).ToString("F1") + " m";
        }

        int pool = Mathf.FloorToInt(player.ChargePool);
        if (pool != shownCharge)
        {
            shownCharge = pool;
            chargeText = "Charge " + pool;
        }

        if (MouseInput.DebugTogglePressed)
        {
            showDebug = !showDebug;
        }
        if (!showDebug)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        if (dt > 0f)
        {
            fps = fps <= 0f ? 1f / dt : Mathf.Lerp(fps, 1f / dt, 0.1f);
        }
        if (Time.unscaledTime >= nextDebugRefresh)
        {
            nextDebugRefresh = Time.unscaledTime + 0.25f;
            fpsText = "FPS: " + fps.ToString("F0");
            stepText = "Fixed step: " + Time.fixedDeltaTime.ToString("F5") + " s";
            errorText = "Hammer to cursor: " + player.HammerAimError.ToString("F2") + " m";
        }
    }

    void OnGUI()
    {
        if (player == null)
        {
            return;
        }
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
        }
        if (EndingOverlay.HideHud || MainMenu.Showing)
        {
            return;
        }

        float h = Screen.height;
        float w = Screen.width;
        int size = Mathf.RoundToInt(h * textScale);
        style.fontSize = size;
        float margin = h * 0.02f;
        float line = size * 1.3f;

        style.alignment = TextAnchor.UpperLeft;
        DrawText(new Rect(margin, margin, w * 0.5f, line), heightText, Color.white);
        DrawText(new Rect(margin, margin + line, w * 0.5f, line), bestText, Color.white);
        DrawText(new Rect(margin, margin + line * 2f, w * 0.5f, line), chargeText, Color.white);

        Rect bar = new Rect(margin, margin + line * 3f, w * barWidth, h * barHeight);
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(bar.x - 2f, bar.y - 2f, bar.width + 4f, bar.height + 4f), Texture2D.whiteTexture);
        Color fill = chargeColor;
        if (player.ChargeLevel >= 0.999f)
        {
            fill = Color.Lerp(chargeColor, Color.white, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 12f));
        }
        GUI.color = fill;
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * Mathf.Clamp01(player.ChargeLevel), bar.height), Texture2D.whiteTexture);
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        float tick = bar.x + bar.width * Mathf.Clamp01(player.MinJumpCharge / player.ChargeMax);
        GUI.DrawTexture(new Rect(tick - 1f, bar.y, 2f, bar.height), Texture2D.whiteTexture);
        GUI.color = old;
        if (player.CanSuperJump)
        {
            style.fontSize = Mathf.Max(8, Mathf.RoundToInt(bar.height * 0.8f));
            style.alignment = TextAnchor.MiddleCenter;
            DrawText(bar, "SPACE", Color.white);
        }

        if (showDebug)
        {
            style.fontSize = Mathf.RoundToInt(h * 0.025f);
            style.alignment = TextAnchor.UpperRight;
            float dl = style.fontSize * 1.3f;
            Rect r = new Rect(0f, margin, w - margin, dl);
            DrawText(r, fpsText, Color.white);
            r.y += dl;
            DrawText(r, stepText, Color.white);
            r.y += dl;
            DrawText(r, errorText, Color.white);
        }
    }

    void DrawText(Rect rect, string text, Color color)
    {
        Color old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        GuiText.Label(new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height), text, style);
        GUI.color = color;
        GuiText.Label(rect, text, style);
        GUI.color = old;
    }
}
