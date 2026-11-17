using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AddBackground
{
    const string ParentName = "Background";
    const string MaterialPath = "Assets/Generated/SpriteUnlit.mat";

    static readonly string[] FixedNames = { "Sky", "Far Clouds", "Fog", "Back Mountains", "Center Mountain", "Field" };
    static readonly string[] TreeObjectNames = { "Trees Far", "Trees Back", "Trees Mid", "Trees Front" };

    static string LayerObjectName(int i)
    {
        if (i < BackgroundLayerSplitter.Trees)
        {
            return FixedNames[i];
        }
        if (i < BackgroundLayerSplitter.Grass)
        {
            return TreeObjectNames[BackgroundLayerSplitter.MaxTreeLayers - BuildBackgroundLayers.TreeLayers + (i - BackgroundLayerSplitter.Trees)];
        }
        return "Grass";
    }

    [MenuItem("BounceBack/Add Background")]
    public static void Add()
    {
        BackgroundLayerSplitter.TreeLayerCount = BuildBackgroundLayers.TreeLayers;
        Sprite[] sprites = new Sprite[BackgroundLayerSplitter.LayerCount];
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>(BuildBackgroundLayers.LayerPath(i));
            if (sprites[i] == null)
            {
                Debug.LogError("BounceBack: " + BuildBackgroundLayers.LayerPath(i) + " is missing. Run BounceBack > Build Background Layers first.");
                return;
            }
        }
        Sprite fillSky = AssetDatabase.LoadAssetAtPath<Sprite>(BuildBackgroundLayers.FillSkyPath);
        Sprite fillGround = AssetDatabase.LoadAssetAtPath<Sprite>(BuildBackgroundLayers.FillGroundPath);
        if (fillSky == null || fillGround == null)
        {
            Debug.LogError("BounceBack: the fill pictures are missing. Run BounceBack > Build Background Layers first.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == ParentName)
            {
                Object.DestroyImmediate(roots[i]);
            }
        }

        Material material = GetMaterial();
        Color skyTop = BuildBackgroundLayers.ReadFillColor(BuildBackgroundLayers.FillSkyPath);

        GameObject parent = new GameObject(ParentName);
        BackgroundParallax parallax = parent.AddComponent<BackgroundParallax>();
        parallax.skyTopColor = skyTop;
        parallax.layers = new BackgroundParallax.Layer[BackgroundLayerSplitter.LayerCount];

        for (int i = 0; i < parallax.layers.Length; i++)
        {
            int order = BuildBackgroundLayers.LayerOrder(i);
            GameObject root = new GameObject(LayerObjectName(i));
            root.transform.SetParent(parent.transform, false);

            BackgroundParallax.Layer layer = new BackgroundParallax.Layer();
            layer.name = root.name;
            layer.followFactor = BuildBackgroundLayers.LayerFollow(i);
            layer.loop = true;
            layer.sortingOrder = order;
            layer.keepNearTop = false;
            layer.sprite = AddSprite("Tile", root.transform, sprites[i], material, order);
            if (layer.loop)
            {
                int tiles = BackgroundParallax.TileCount(sprites[i].rect.width, sprites[0].rect.height, parallax.backgroundScale, parallax.maxAspect);
                layer.extraTiles = new SpriteRenderer[tiles - 1];
                for (int k = 0; k < layer.extraTiles.Length; k++)
                {
                    layer.extraTiles[k] = AddSprite("Tile " + (k + 1), root.transform, sprites[i], material, order);
                }
            }
            if (i == BackgroundLayerSplitter.Sky)
            {
                layer.fillAbove = AddSprite("Fill Above", root.transform, fillSky, material, order - 1);
            }
            layer.grassTopLocalY = GrassTopLocalY(sprites[i], i == BackgroundLayerSplitter.Grass);
            if (i == BackgroundLayerSplitter.Grass)
            {
                layer.fillBelow = AddSprite("Fill Below", root.transform, fillGround, material, order - 1);
            }
            parallax.layers[i] = layer;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = Object.FindAnyObjectByType<Camera>();
        }
        Vector3 cameraPosition = Vector3.zero;
        float size = 8f;
        float aspect = 16f / 9f;
        if (cam != null)
        {
            cameraPosition = cam.transform.position;
            size = cam.orthographicSize;
            aspect = cam.aspect;
            cam.backgroundColor = skyTop;
            EditorUtility.SetDirty(cam);
        }
        parent.transform.position = Vector3.zero;
        parallax.PlaceAtCamera(cameraPosition, size, aspect);

        Undo.RegisterCreatedObjectUndo(parent, "Add Background");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = parent;
        Debug.Log("BounceBack: Background added to " + scene.name + ". Save the scene (Ctrl+S) to keep it.");
    }

    static float GrassTopLocalY(Sprite sprite, bool isGrassLayer)
    {
        int row;
        string path = isGrassLayer || !File.Exists(BuildBackgroundLayers.GroundRowPath)
            ? BuildBackgroundLayers.GrassTopPath
            : BuildBackgroundLayers.GroundRowPath;
        if (!File.Exists(path) || !int.TryParse(File.ReadAllText(path), out row) || row < 0)
        {
            Debug.LogWarning("BounceBack: grass top row not found, run BounceBack > Build Background Layers again. The grass will not be lined up with the ground.");
            return 0f;
        }
        return (sprite.rect.height * 0.5f - row) / sprite.pixelsPerUnit;
    }

    static SpriteRenderer AddSprite(string name, Transform parent, Sprite sprite, Material material, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = material;
        sr.sortingOrder = order;
        return sr;
    }

    static Material GetMaterial()
    {
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (existing != null)
        {
            return existing;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        if (!AssetDatabase.IsValidFolder("Assets/Generated"))
        {
            AssetDatabase.CreateFolder("Assets", "Generated");
        }
        Material mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }
}
