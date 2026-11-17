using System.Collections.Generic;
using UnityEngine;

public static class NormalMaps
{
    static readonly Dictionary<Texture, Texture2D> cache = new Dictionary<Texture, Texture2D>();

    public static Texture2D For(Texture2D source)
    {
        Texture2D found;
        if (cache.TryGetValue(source, out found))
        {
            return found;
        }
        Texture2D result = null;
        if (source.width <= 2048 && source.height <= 2048)
        {
            result = Build(source, 2.2f);
        }
        cache[source] = result;
        return result;
    }

    static Texture2D Build(Texture2D source, float strength)
    {
        int w = source.width;
        int h = source.height;
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        RenderTexture previous = RenderTexture.active;
        Graphics.Blit(source, rt);
        RenderTexture.active = rt;
        Texture2D copy = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        copy.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        copy.Apply(false, false);
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        Color32[] src = copy.GetPixels32();
        Object.Destroy(copy);
        float[] height = new float[w * h];
        for (int i = 0; i < src.Length; i++)
        {
            float lum = (src[i].r * 0.3f + src[i].g * 0.59f + src[i].b * 0.11f) / 255f;
            height[i] = lum * (src[i].a / 255f);
        }
        Color32[] pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float l = height[y * w + Mathf.Max(0, x - 1)];
                float r = height[y * w + Mathf.Min(w - 1, x + 1)];
                float d = height[Mathf.Max(0, y - 1) * w + x];
                float u = height[Mathf.Min(h - 1, y + 1) * w + x];
                Vector3 n = new Vector3((l - r) * strength, (d - u) * strength, 1f).normalized;
                pixels[y * w + x] = new Color32((byte)((n.x * 0.5f + 0.5f) * 255f), (byte)((n.y * 0.5f + 0.5f) * 255f), (byte)((n.z * 0.5f + 0.5f) * 255f), 255);
            }
        }
        Texture2D result = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
        result.filterMode = FilterMode.Point;
        result.wrapMode = TextureWrapMode.Clamp;
        result.hideFlags = HideFlags.DontSave;
        result.SetPixels32(pixels);
        result.Apply(false, true);
        return result;
    }
}
