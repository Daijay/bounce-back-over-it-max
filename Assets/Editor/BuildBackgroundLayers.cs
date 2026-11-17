using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class BuildBackgroundLayers
{
    public static int insertRows = 40;
    public static float bgPixelsPerUnit = 200f;
    public static int periodClouds = 900;
    public static int treeLayerCount = 0;
    public static int[] treeMinHeightPx = { 15, 18, 21, 24 };
    public static int[] treeMaxHeightPx = { 22, 27, 32, 37 };
    public static int[] treeSpacing = { 6, 7, 8, 9 };
    public static float[] treeDarkness = { 1.00f, 0.92f, 0.82f, 0.72f };
    public static float[] treeHaze = { 0.26f, 0.16f, 0.08f, 0f };
    public static int treeSeed = 7;
    public static int treePeriodPx = 1200;
    public static int treeSinkPx = 1;

    public static int customGrassUpscale = 1;

    public const string SourceFile = "background_main.png";
    public const string SpriteFolder = "Assets/Sprites";
    public const string BackgroundFolder = "Assets/Sprites/Background";
    public const string LayerFolder = "Assets/Sprites/Background/Layers";
    public const string TallPath = BackgroundFolder + "/background_main_tall.png";
    public const string PreviewPath = LayerFolder + "/preview.png";
    public const string GrassTopPath = LayerFolder + "/grass_top_row.txt";
    public const string GroundRowPath = LayerFolder + "/layers_ground_row.txt";
    public const string CustomGrassPath = BackgroundFolder + "/grass_custom.png";
    public const string FillSkyPath =LayerFolder + "/fill_sky_top.png";
    public const string FillGroundPath = LayerFolder + "/fill_ground.png";

    public static int TreeLayers
    {
        get { return Mathf.Clamp(treeLayerCount, 0, BackgroundLayerSplitter.MaxTreeLayers); }
    }

    public static string LayerFile(int layer)
    {
        return (layer + 1).ToString("00") + "_" + BackgroundLayerSplitter.LayerName(layer) + ".png";
    }

    public static string LayerPath(int layer)
    {
        return LayerFolder + "/" + LayerFile(layer);
    }

    public static readonly float[] TreeFollow = { 0.855f, 0.85f, 0.84f, 0.83f };

    public static float LayerFollow(int layer)
    {
        float[] fixedFollow = { 0.98f, 0.94f, 0.90f, 0.92f, 0.88f, 0.86f };
        if (layer < BackgroundLayerSplitter.Trees)
        {
            return fixedFollow[layer];
        }
        if (layer < BackgroundLayerSplitter.Grass)
        {
            return TreeFollow[BackgroundLayerSplitter.MaxTreeLayers - TreeLayers + (layer - BackgroundLayerSplitter.Trees)];
        }
        return 0.82f;
    }

    public static int LayerOrder(int layer)
    {
        if (layer < BackgroundLayerSplitter.Trees)
        {
            return -100 + 2 * layer;
        }
        if (layer < BackgroundLayerSplitter.Grass)
        {
            return -88 + (layer - BackgroundLayerSplitter.Trees);
        }
        return -84;
    }

    static void DeleteStaleLayerFiles()
    {
        if (!Directory.Exists(LayerFolder))
        {
            return;
        }
        string[] files = Directory.GetFiles(LayerFolder, "*.png");
        for (int i = 0; i < files.Length; i++)
        {
            string file = Path.GetFileName(files[i]);
            bool numbered = file.Length > 3 && char.IsDigit(file[0]) && char.IsDigit(file[1]) && file[2] == '_';
            if (!numbered)
            {
                continue;
            }
            bool current = false;
            for (int layer = 0; layer < BackgroundLayerSplitter.LayerCount; layer++)
            {
                if (LayerFile(layer) == file)
                {
                    current = true;
                    break;
                }
            }
            if (!current)
            {
                AssetDatabase.DeleteAsset(LayerFolder + "/" + file);
            }
        }
    }

    [MenuItem("BounceBack/Build Background Layers")]
    public static void Build()
    {
        string sourcePath = FindSource();
        if (sourcePath == null)
        {
            Debug.LogError("BounceBack: " + SourceFile + " not found under Assets (Assets/Sprites first), Downloads or Desktop.");
            return;
        }

        Texture2D source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        source.LoadImage(File.ReadAllBytes(sourcePath));
        int width = source.width;
        int height = source.height;
        byte[] original = ToTopDown(source.GetPixels32(), width, height);
        UnityEngine.Object.DestroyImmediate(source);

        if (width != BackgroundLayerSplitter.SourceWidth || height != BackgroundLayerSplitter.SourceHeight)
        {
            Debug.LogError("BounceBack: " + sourcePath + " is " + width + " x " + height + ", the cut rules need "
                + BackgroundLayerSplitter.SourceWidth + " x " + BackgroundLayerSplitter.SourceHeight + ".");
            return;
        }

        int rows = Mathf.Max(0, insertRows);
        byte[] tall;
        BackgroundLayerSplitter.LayerImage[] layers;
        BackgroundLayerSplitter.TreeSettings treeSettingsForLog = new BackgroundLayerSplitter.TreeSettings();
        try
        {
            tall = BackgroundLayerSplitter.InsertRows(original, width, height, BackgroundLayerSplitter.InsertAfterRow, rows);
            BackgroundLayerSplitter.TreeSettings trees = treeSettingsForLog;
            BackgroundLayerSplitter.TreeLayerCount = TreeLayers;
            trees.minHeight = treeMinHeightPx;
            trees.maxHeight = treeMaxHeightPx;
            trees.spacing = treeSpacing;
            trees.darkness = treeDarkness;
            trees.haze = treeHaze;
            trees.seed = treeSeed;
            trees.period = treePeriodPx;
            trees.sink = treeSinkPx;
            layers = BackgroundLayerSplitter.Split(tall, width, height + rows, rows,
                periodClouds, trees);
        }
        catch (InvalidOperationException e)
        {
            Debug.LogError("BounceBack: " + e.Message);
            return;
        }
        int tallHeight = height + rows;

        int layersGroundRow = treeSettingsForLog.groundRowFine;

        int customTopRow = -1;
        string grassNote = "generated grass";
        if (File.Exists(CustomGrassPath))
        {
            BackgroundLayerSplitter.LayerImage custom = BuildCustomGrass(out customTopRow, out grassNote);
            if (custom != null)
            {
                layers[BackgroundLayerSplitter.Grass] = custom;
            }
            else
            {
                customTopRow = -1;
            }
        }

        EnsureFolder("Assets", "Sprites");
        EnsureFolder(SpriteFolder, "Background");
        EnsureFolder(BackgroundFolder, "Layers");

        DeleteStaleLayerFiles();

        WritePng(TallPath, tall, width, tallHeight);
        for (int i = 0; i < layers.Length; i++)
        {
            WritePng(LayerPath(i), layers[i].rgba, layers[i].width, layers[i].height);
        }

        int previewWidth;
        int previewHeight;
        byte[] preview = BackgroundLayerSplitter.BuildPreview(layers, out previewWidth, out previewHeight);
        WritePng(PreviewPath, preview, previewWidth, previewHeight);

        byte r;
        byte g;
        byte b;
        BackgroundLayerSplitter.DominantRowColor(layers[BackgroundLayerSplitter.Sky].rgba, layers[BackgroundLayerSplitter.Sky].width, 0, out r, out g, out b);
        WritePng(FillSkyPath, new byte[] { r, g, b, 255 }, 1, 1);
        LayerImageRow(layers[BackgroundLayerSplitter.Grass], out r, out g, out b);
        WritePng(FillGroundPath, new byte[] { r, g, b, 255 }, 1, 1);

        int grassRow = customTopRow >= 0 ? customTopRow : layersGroundRow;
        File.WriteAllText(GrassTopPath, grassRow.ToString());
        File.WriteAllText(GroundRowPath, layersGroundRow.ToString());

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        ConfigureSprite(TallPath, bgPixelsPerUnit, new Vector2(0.5f, 0.5f));
        for (int i = 0; i < layers.Length; i++)
        {
            float pivotX = (layers[i].pad + BackgroundLayerSplitter.SourceWidth * BackgroundLayerSplitter.Scale * 0.5f) / layers[i].width;
            ConfigureSprite(LayerPath(i), bgPixelsPerUnit, new Vector2(pivotX, 0.5f));
        }
        ConfigureSprite(PreviewPath, bgPixelsPerUnit, new Vector2(0.5f, 0.5f));
        ConfigureSprite(FillSkyPath, 1f, new Vector2(0.5f, 0.5f));
        ConfigureSprite(FillGroundPath, 1f, new Vector2(0.5f, 0.5f));
        AssetDatabase.SaveAssets();

        Debug.Log("BounceBack: background layers written to " + LayerFolder + " (" + BackgroundLayerSplitter.LayerCount + " layers, original resolution). Grass: " + grassNote
            + ". Trees: " + TreeLayers + " layers, shortest mountain peak " + treeSettingsForLog.measuredPeak + " px above the grass, tallest tree "
            + treeSettingsForLog.appliedMax + " px. Grass top at row " + grassRow + ", picture height " + layers[0].height + ". Next: BounceBack > Add Background.");
    }

    static BackgroundLayerSplitter.LayerImage BuildCustomGrass(out int topRow, out string note)
    {
        topRow = -1;
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!tex.LoadImage(File.ReadAllBytes(CustomGrassPath)))
        {
            UnityEngine.Object.DestroyImmediate(tex);
            note = "generated grass (grass_custom.png could not be read)";
            Debug.LogWarning("BounceBack: " + CustomGrassPath + " could not be read, using the generated grass.");
            return null;
        }
        int w = tex.width;
        int h = tex.height;
        byte[] data = ToTopDown(tex.GetPixels32(), w, h);
        UnityEngine.Object.DestroyImmediate(tex);

        if (customGrassUpscale > 1)
        {
            data = BackgroundDetail.Upscale2x(data, w, h, true, false);
            w *= BackgroundDetail.Scale;
            h *= BackgroundDetail.Scale;
        }

        int top = BackgroundLayerSplitter.FirstGrassRow(data, w, h, 3);
        if (w < 8 || h < 8 || top < 0)
        {
            note = "generated grass (grass_custom.png is empty or too small)";
            Debug.LogWarning("BounceBack: " + CustomGrassPath + " has no visible grass, using the generated grass.");
            return null;
        }
        int solid = BackgroundLayerSplitter.FirstGrassRow(data, w, h, 40);
        BackgroundLayerSplitter.FillSolidGrass(data, w, h, solid < 0 ? top : Mathf.Max(solid, top));

        float seam = ColumnDifference(data, w, h, top, w - 1, 0);
        float typical = 0f;
        int samples = 0;
        for (int x = 0; x + 1 < w; x++)
        {
            typical += ColumnDifference(data, w, h, top, x, x + 1);
            samples++;
        }
        typical /= Mathf.Max(1, samples);
        bool seamless = seam <= typical * 1.5f + 4f;

        BackgroundLayerSplitter.LayerImage image;
        if (seamless)
        {
            image = new BackgroundLayerSplitter.LayerImage { name = "grass", width = w, height = h, pad = 0, mirrored = false, rgba = data };
        }
        else
        {
            image = BackgroundLayerSplitter.MakeMirrored("grass", data, w, h);
        }
        image.pad = (image.width - BackgroundLayerSplitter.SourceWidth * BackgroundLayerSplitter.Scale) / 2;

        topRow = top;
        note = "your grass_custom.png (" + w + " x " + h + ", top row " + top + ", " + (seamless ? "seamless as it is" : "edges mirrored for a seamless loop") + ")";
        return image;
    }

    static float ColumnDifference(byte[] data, int w, int h, int firstRow, int xa, int xb)
    {
        float sum = 0f;
        int rows = 0;
        for (int y = firstRow; y < h; y++)
        {
            int a = (y * w + xa) * 4;
            int b = (y * w + xb) * 4;
            if ((data[a + 3] == 0) != (data[b + 3] == 0))
            {
                sum += 255f;
            }
            else if (data[a + 3] != 0)
            {
                sum += (Mathf.Abs(data[a] - data[b]) + Mathf.Abs(data[a + 1] - data[b + 1]) + Mathf.Abs(data[a + 2] - data[b + 2])) / 3f;
            }
            rows++;
        }
        return rows > 0 ? sum / rows : 0f;
    }

    static void LayerImageRow(BackgroundLayerSplitter.LayerImage layer, out byte r, out byte g, out byte b)
    {
        BackgroundLayerSplitter.DominantRowColor(layer.rgba, layer.width, layer.height - 1, out r, out g, out b);
    }

    static string FindSource()
    {
        string inSprites = SpriteFolder + "/" + SourceFile;
        if (File.Exists(inSprites))
        {
            return inSprites;
        }

        string[] guids = AssetDatabase.FindAssets("background_main t:Texture2D", new[] { "Assets" });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (Path.GetFileName(path) == SourceFile)
            {
                return path;
            }
        }

        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string[] places =
        {
            Path.Combine(home, "Downloads", SourceFile),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), SourceFile)
        };
        for (int i = 0; i < places.Length; i++)
        {
            if (File.Exists(places[i]))
            {
                return places[i];
            }
        }
        return null;
    }

    public static Color ReadFillColor(string path)
    {
        if (!File.Exists(path))
        {
            return Color.black;
        }
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        Color32 c = tex.GetPixel(0, 0);
        UnityEngine.Object.DestroyImmediate(tex);
        return c;
    }

    static byte[] ToTopDown(Color32[] pixels, int width, int height)
    {
        byte[] data = new byte[width * height * 4];
        for (int y = 0; y < height; y++)
        {
            int row = height - 1 - y;
            for (int x = 0; x < width; x++)
            {
                Color32 c = pixels[row * width + x];
                int i = (y * width + x) * 4;
                data[i] = c.r;
                data[i + 1] = c.g;
                data[i + 2] = c.b;
                data[i + 3] = c.a;
            }
        }
        return data;
    }

    static void WritePng(string path, byte[] topDown, int width, int height)
    {
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
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    static void ConfigureSprite(string path, float pixelsPerUnit, Vector2 pivot)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("BounceBack: could not set import settings for " + path);
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.isReadable = false;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 4096;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
