using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MapVolcano
{
    public const string BackdropName = "volcano_backdrop";
    public const string GlowName = "volcano_glow";

    const float LavaLeft = 64.5f;
    const float LavaRight = 81.5f;
    const float LavaBottom = -7f;
    const float LavaTop = 0f;
    const float BackdropLeft = 59f;
    const float BackdropRight = 87.5f;
    const float BackdropTop = 25.3f;
    const float BackdropBottom = -8f;

    public const int BackdropOrder = 0;
    public const int GlowOrder = 3;

    static readonly int Gray = 0x2B2B2E;
    static readonly float[] StopHeight = { 0f, 2.5f, 5f, 7.5f, 10f };
    static readonly int[] StopColour = { 0xD24A16, 0x9C2A12, 0x5C1C16, 0x3C2822, 0x2B2B2E };
    const float GradientHeight = 10f;
    const int Bands = 16;

    const float GlowRadius = 5f;
    static readonly float[] GlowAlpha = { 0.32f, 0.24f, 0.17f, 0.10f, 0.05f };
    const int GlowRgb = 0xFF6A1E;
    const float GlowLeft = 59.5f;
    const float GlowRight = 86.5f;
    const float GlowBottom = -10f;
    const float GlowTop = 5.5f;

    public static void Build(Transform parent, List<List<Vector2>> wallPolygons)
    {
        float pixel = MapStyle.PixelWorld;

        int bw = Mathf.RoundToInt((BackdropRight - BackdropLeft) / pixel);
        int rows = Mathf.RoundToInt((BackdropTop - BackdropBottom) / pixel);
        byte[] backdrop = new byte[bw * rows * 4];
        float bandHeight = GradientHeight / Bands;
        for (int r = 0; r < rows; r++)
        {
            float y = BackdropTop - (r + 0.5f) * pixel;
            int rgb = BackdropColour(y - LavaTop, bandHeight);
            for (int c = 0; c < bw; c++)
            {
                int i = (r * bw + c) * 4;
                backdrop[i] = (byte)(rgb >> 16);
                backdrop[i + 1] = (byte)((rgb >> 8) & 255);
                backdrop[i + 2] = (byte)(rgb & 255);
                backdrop[i + 3] = 255;
            }
        }

        int glowW = Mathf.RoundToInt((GlowRight - GlowLeft) / pixel);
        int glowH = Mathf.RoundToInt((GlowTop - GlowBottom) / pixel);
        byte[] glow = new byte[glowW * glowH * 4];
        for (int r = 0; r < glowH; r++)
        {
            float y = GlowTop - (r + 0.5f) * pixel;
            for (int c = 0; c < glowW; c++)
            {
                float x = GlowLeft + (c + 0.5f) * pixel;
                float dx = Mathf.Max(0f, Mathf.Max(LavaLeft - x, x - LavaRight));
                float dy = Mathf.Max(0f, Mathf.Max(LavaBottom - y, y - LavaTop));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                if (d >= GlowRadius)
                {
                    continue;
                }
                if (!InsideAny(wallPolygons, x, y))
                {
                    continue;
                }
                int i = (r * glowW + c) * 4;
                glow[i] = (byte)(GlowRgb >> 16);
                glow[i + 1] = (byte)((GlowRgb >> 8) & 255);
                glow[i + 2] = (byte)(GlowRgb & 255);
                glow[i + 3] = (byte)Mathf.RoundToInt(255f * GlowAlpha[Mathf.Min(GlowAlpha.Length - 1, (int)d)]);
            }
        }

        WritePng(BackdropName, backdrop, bw, rows);
        WritePng(GlowName, glow, glowW, glowH);
        AssetDatabase.Refresh();
        MapAssets.ImportAll();

        Transform group = new GameObject("Volcano").transform;
        group.SetParent(parent, false);

        MakeSprite("Volcano Backdrop", group, BackdropName,
            new Vector2(BackdropLeft + bw * pixel * 0.5f, BackdropTop - rows * pixel * 0.5f), BackdropOrder);

        MakeSprite("Volcano Wall Glow", group, GlowName,
            new Vector2(GlowLeft + glowW * pixel * 0.5f, GlowBottom + glowH * pixel * 0.5f), GlowOrder);
    }

    static int BackdropColour(float height, float bandHeight)
    {
        if (height >= GradientHeight)
        {
            return Gray;
        }
        float middle = height <= 0f ? 0f : (Mathf.Floor(height / bandHeight) + 0.5f) * bandHeight;
        for (int s = 0; s + 1 < StopHeight.Length; s++)
        {
            if (middle <= StopHeight[s + 1])
            {
                float t = (middle - StopHeight[s]) / (StopHeight[s + 1] - StopHeight[s]);
                return Mix(StopColour[s], StopColour[s + 1], t);
            }
        }
        return Gray;
    }

    static int Mix(int a, int b, float t)
    {
        int r = Mathf.RoundToInt(Mathf.Lerp((a >> 16) & 255, (b >> 16) & 255, t));
        int g = Mathf.RoundToInt(Mathf.Lerp((a >> 8) & 255, (b >> 8) & 255, t));
        int bl = Mathf.RoundToInt(Mathf.Lerp(a & 255, b & 255, t));
        return (r << 16) | (g << 8) | bl;
    }

    static bool InsideAny(List<List<Vector2>> polygons, float x, float y)
    {
        for (int p = 0; p < polygons.Count; p++)
        {
            if (Inside(polygons[p], x, y))
            {
                return true;
            }
        }
        return false;
    }

    static bool Inside(List<Vector2> polygon, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            if ((a.y > y) != (b.y > y) && x < (b.x - a.x) * (y - a.y) / (b.y - a.y) + a.x)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    static SpriteRenderer MakeSprite(string name, Transform parent, string spriteName, Vector2 position, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MapAssets.GetSprite(spriteName);
        sr.sortingOrder = order;
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/SpriteUnlit.mat");
        if (material != null)
        {
            sr.sharedMaterial = material;
        }
        return sr;
    }

    static void WritePng(string name, byte[] topDown, int width, int height)
    {
        string path = MapAssets.Folder + "/" + name + ".png";
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            int row = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                int i = (y * width + x) * 4;
                pixels[row * width + x] = new Color32(topDown[i], topDown[i + 1], topDown[i + 2], topDown[i + 3]);
            }
        }
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        byte[] png = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);

        if (File.Exists(path))
        {
            if (SameBytes(File.ReadAllBytes(path), png))
            {
                return;
            }
        }
        File.WriteAllBytes(path, png);
    }

    static bool SameBytes(byte[] a, byte[] b)
    {
        if (a.Length != b.Length)
        {
            return false;
        }
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
            {
                return false;
            }
        }
        return true;
    }
}
