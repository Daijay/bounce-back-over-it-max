using System.IO;
using UnityEditor;
using UnityEngine;

public static class CalmMapTextures
{
    const string Folder = "Assets/Textures/Map";
    const int RockSize = 128;
    const int LavaSize = 64;
    const int RockSeed = 5;
    const int LavaSeed = 9;

    [MenuItem("BounceBack/Calm Map Textures")]
    public static void Run()
    {
        Write(Folder + "/rock.png", MapTextureGen.Rock(RockSize, RockSeed), RockSize);
        for (int frame = 0; frame < 4; frame++)
        {
            Write(Folder + "/lava_" + frame + ".png", MapTextureGen.Lava(LavaSize, LavaSeed, frame), LavaSize);
        }
        string[] oldNames = { "boulder_s", "boulder_m", "boulder_l" };
        for (int i = 0; i < oldNames.Length; i++)
        {
            AssetDatabase.DeleteAsset(Folder + "/" + oldNames[i] + ".png");
        }
        for (int size = 0; size < MapRockDetail.BoulderPixels.Length; size++)
        {
            for (int variant = 0; variant < MapRockDetail.Variants; variant++)
            {
                int pixels = MapRockDetail.BoulderPixels[size];
                Write(Folder + "/" + MapRockDetail.BoulderName(size, variant) + ".png",
                    MapTextureGen.Boulder(pixels, MapRockDetail.OutlineSeed(size, variant)), pixels);
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("BounceBack: rock face, lava and boulder pictures redrawn in " + Folder + ". Run BounceBack > Build Map to use the boulders.");
    }

    static void Write(string path, byte[] topDown, int size)
    {
        topDown = MapMinimal.Apply(Path.GetFileNameWithoutExtension(path), topDown, size, size);
        Color32[] pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            int row = size - 1 - y;
            for (int x = 0; x < size; x++)
            {
                int i = (y * size + x) * 4;
                pixels[row * size + x] = new Color32(topDown[i], topDown[i + 1], topDown[i + 2], topDown[i + 3]);
            }
        }
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.SetPixels32(pixels);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }
}
