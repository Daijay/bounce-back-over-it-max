using UnityEngine;

public static class GuiText
{
    public static Rect Snap(Rect r)
    {
        float width = Mathf.Max(2f, Mathf.Round(r.width * 0.5f) * 2f);
        float height = Mathf.Max(2f, Mathf.Round(r.height * 0.5f) * 2f);
        return new Rect(Mathf.Round(r.x), Mathf.Round(r.y), width, height);
    }

    public static void Label(Rect rect, string text, GUIStyle style)
    {
        GUI.Label(Snap(rect), text, style);
    }

    public static int Size(float screenHeight, float share, int min)
    {
        return Mathf.Max(min, Mathf.RoundToInt(screenHeight * share));
    }
}
