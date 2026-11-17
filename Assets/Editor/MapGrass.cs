using System.IO;
using UnityEditor;
using UnityEngine;

public static class MapGrass
{
    public const string TuftName = "grass_tuft";
    public const string GroundName = "ground_field";
    public const float GroundLeft = -16f;
    public const float GroundRight = MapArt.FootEnd;
    public const float GroundDepth = 10.02f;
    public const int GroundTopPad = 5;

    public static int GroundOriginPixel()
    {
        return Mathf.RoundToInt(GroundLeft / MapStyle.PixelWorld);
    }

    public static int GroundWidthPixels()
    {
        return Mathf.RoundToInt(GroundRight / MapStyle.PixelWorld) - GroundOriginPixel();
    }

    public static int GroundLineRow()
    {
        return GroundTopPad + Mathf.RoundToInt(MapArt.FootTop / MapStyle.PixelWorld);
    }

    public static int GroundHeightPixels()
    {
        return GroundLineRow() + Mathf.RoundToInt(GroundDepth / MapStyle.PixelWorld);
    }

    static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    public static void WriteTextures()
    {
        bool changed = false;
        int width = GroundWidthPixels();
        int height = GroundHeightPixels();
        int[] rise = new int[width];
        float[] grassScale = new float[width];
        float[] dirtScale = new float[width];
        for (int c = 0; c < width; c++)
        {
            float x = (GroundOriginPixel() + c + 0.5f) * MapStyle.PixelWorld;
            rise[c] = Mathf.RoundToInt(MapArt.FootHeight(x) / MapStyle.PixelWorld);
            grassScale[c] = 1f - Smooth((x - MapArt.GrassTaperStart) / (MapArt.FootEnd - MapArt.GrassTaperStart));
            dirtScale[c] = 1f - Smooth((x - MapArt.DirtTaperStart) / 4.5f);
        }
        changed |= Write(GroundName, MapArt.Ground(width, height, GroundLineRow(), GroundOriginPixel(), MapMinimal.Apply("rock", MapTextureGen.Rock(128, 5), 128, 128), 31, rise, grassScale, dirtScale), width, height);
        changed |= Write(TuftName, MapTextureGen.GrassTuft(0), MapTextureGen.GrassTuftSize, MapTextureGen.GrassTuftSize);
        changed |= Write("pad_look", MapArt.PadLook(PadWidthPixels()), PadWidthPixels(), 13);
        changed |= Write("pad_arrow", MapArt.PadArrow(), 15, 7);
        changed |= Write("snow", MapArt.Snow(70), 64, 64);
        changed |= Write("force_field", MapArt.ForceField(), 16, 32);
        changed |= Write("space_star", MapArt.White(2, 2), 2, 2);
        changed |= Write("space_gradient", MapSpaceArt.SpaceGradient(), MapSpaceArt.GradientWidth, MapSpaceArt.GradientRows());
        if (changed)
        {
            AssetDatabase.Refresh();
        }
    }
    public static int PadWidthPixels()
    {
        return Mathf.RoundToInt(2.8f / MapStyle.PixelWorld);
    }

    static bool Write(string name, byte[] topDown, int width, int height)
    {
        string path = MapAssets.Folder + "/" + name + ".png";
        topDown = MapMinimal.Apply(name, topDown, width, height);
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
        Object.DestroyImmediate(tex);

        if (File.Exists(path) && SameBytes(File.ReadAllBytes(path), png))
        {
            return false;
        }
        File.WriteAllBytes(path, png);
        return true;
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
