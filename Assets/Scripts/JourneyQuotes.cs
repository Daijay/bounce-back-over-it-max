using UnityEngine;

public class JourneyQuotes : MonoBehaviour
{
    [System.Serializable]
    public struct Line
    {
        public float height;
        public string text;
    }

    public Line[] lines =
    {
        new Line { height = 0f, text = "Higher." },
        new Line { height = 0f, text = "It is quiet up here." },
        new Line { height = 0f, text = "Almost there." },
        new Line { height = 0f, text = "The moon." },
    };


    public float floatStartY = 100f;
    public float resetBelow = 60f;
    public float charsPerSecond = 32f;
    public float holdTime = 4.5f;
    public float fadeTime = 1.2f;
    public float screenHeightShare = 0.78f;
    public int guiDepth = -2;

    int shown = -1;
    int active = -1;
    float activeStart;
    GUIStyle style;
    GUIStyle outline;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindAnyObjectByType<JourneyQuotes>() == null)
        {
            new GameObject("JourneyQuotes").AddComponent<JourneyQuotes>();
        }
    }

    void Update()
    {
        PlayerController player = PlayerController.Instance;
        if (player == null || player.BodyRb == null)
        {
            return;
        }
        SpreadAlongFloat();
        float y = player.BodyRb.position.y;
        if (y < resetBelow)
        {
            shown = -1;
            active = -1;
            return;
        }
        int next = shown + 1;
        if (next < lines.Length && y >= lines[next].height)
        {
            shown = next;
            active = next;
            activeStart = Time.time;
        }
    }

    bool spread;
    float nextFind;

    void SpreadAlongFloat()
    {
        if (spread || Time.time < nextFind)
        {
            return;
        }
        nextFind = Time.time + 1f;
        GameObject moon = EndingFind.ByName("CheeseMoon");
        GameObject start = EndingFind.ByName("EruptionTrigger");
        if (moon == null)
        {
            return;
        }
        if (start != null)
        {
            floatStartY = start.transform.position.y;
        }
        float top = moon.transform.position.y - 10f;
        for (int i = 0; i < lines.Length; i++)
        {
            float k = lines.Length > 1 ? i / (float)(lines.Length - 1) : 0f;
            lines[i].height = Mathf.Lerp(floatStartY, top, k);
        }
        spread = true;
    }

    void OnGUI()
    {
        if (active < 0 || active >= lines.Length || Event.current.type != EventType.Repaint)
        {
            return;
        }
        PlayerController player = PlayerController.Instance;
        EndingOverlay overlay = EndingOverlay.Instance;
        bool cinematic = player == null || MainMenu.Showing
            || (overlay != null && overlay.Fade > 0.01f)
            || EndingOverlay.HideHud;
        if (cinematic)
        {
            return;
        }

        string full = lines[active].text;
        float age = Time.time - activeStart;
        float typed = full.Length / Mathf.Max(1f, charsPerSecond);
        float alpha = 1f;
        float endTime = typed + holdTime;
        if (age > endTime)
        {
            alpha = 1f - Mathf.Clamp01((age - endTime) / Mathf.Max(0.01f, fadeTime));
            if (alpha <= 0f)
            {
                active = -1;
                return;
            }
        }
        int count = Mathf.Clamp(Mathf.FloorToInt(age * charsPerSecond), 0, full.Length);
        if (count <= 0)
        {
            return;
        }
        string text = full.Substring(0, count);

        float w = Screen.width;
        float h = Screen.height;
        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.wordWrap = true;
            outline = new GUIStyle(style);
        }
        int size = Mathf.Max(12, Mathf.RoundToInt(h * 0.032f));
        style.fontSize = size;
        outline.fontSize = size;

        GUI.depth = guiDepth;
        Rect rect = new Rect(w * 0.15f, h * screenHeightShare - size * 1.5f, w * 0.7f, size * 3f);
        Color old = GUI.color;
        float o = Mathf.Max(1f, h * 0.0035f);
        outline.normal.textColor = new Color(0f, 0f, 0f, 0.85f * alpha);
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx != 0 || dy != 0)
                {
                    GuiText.Label(new Rect(rect.x + dx * o, rect.y + dy * o, rect.width, rect.height), text, outline);
                }
            }
        }
        style.normal.textColor = new Color(1f, 1f, 1f, alpha);
        GuiText.Label(rect, text, style);
        GUI.color = old;
    }
}
