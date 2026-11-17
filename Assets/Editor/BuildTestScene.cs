using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildTestScene
{
    const string SpritePath = "Assets/Generated/White.png";
    const string CirclePath = "Assets/Generated/Circle.png";
    const string MaterialPath = "Assets/Generated/SpriteUnlit.mat";
    const string GroundMaterialPath = "Assets/Generated/DefaultGround.physicsMaterial2D";
    const string ScenePath = "Assets/Scenes/TestLevel.unity";
    const string ArtFolder = "Assets/Sprites";
    const string BodyArtFile = "rat_main_1.png";

    static Sprite whiteSprite;
    static Sprite circleSprite;
    static Sprite bodyArtSprite;
    static Vector2 bodyArtPixels;
    static Material spriteMaterial;
    static Transform levelRoot;

    [MenuItem("BounceBack/Build Test Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EnsureFolder("Assets", "Generated");
        EnsureFolder("Assets", "Scenes");
        ApplyProjectSettings();
        whiteSprite = GetOrCreateSprite(SpritePath, false);
        circleSprite = GetOrCreateSprite(CirclePath, true);
        spriteMaterial = GetOrCreateMaterial();
        PrepareArt();

        BackgroundParallax carriedBackground = StartFreshScene();

        levelRoot = new GameObject("Level").transform;

        MapAssets.ImportAll();
        MapAssets.ClearMeshes();
        BuildMap.BuildLevel(levelRoot, 12345, 1f);

        PlayerController controller = BuildPlayer(new Vector3(BuildMap.StartPoint.x, BuildMap.StartPoint.y, 0f));
        GameObject player = controller.gameObject;

        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(BuildMap.StartPoint.x, 2f, -10f);
        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.12f, 0.18f);
        if (carriedBackground != null)
        {
            cam.backgroundColor = carriedBackground.skyTopColor;
        }
        camGo.AddComponent<AudioListener>();
        CameraFollow follow = camGo.AddComponent<CameraFollow>();
        follow.target = player.transform;

        GameObject hud = new GameObject("HUD");
        GameHUD gameHud = hud.AddComponent<GameHUD>();
        gameHud.player = controller;

        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("BounceBack: built and saved " + ScenePath + ". Press Play.");
    }

    static BackgroundParallax StartFreshScene()
    {
        Scene active = SceneManager.GetActiveScene();
        BackgroundParallax background = FindBackground(active);
        if (background == null && File.Exists(ScenePath) && active.path != ScenePath)
        {
            Scene saved = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            background = FindBackground(saved);
        }

        if (background == null)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return null;
        }

        Scene fresh = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(background.gameObject, fresh);
        SceneManager.SetActiveScene(fresh);
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene other = SceneManager.GetSceneAt(i);
            if (other != fresh)
            {
                EditorSceneManager.CloseScene(other, true);
            }
        }
        return background;
    }

    static BackgroundParallax FindBackground(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "Background")
            {
                BackgroundParallax found = roots[i].GetComponent<BackgroundParallax>();
                if (found != null)
                {
                    return found;
                }
            }
        }
        return null;
    }

    static PlayerController BuildPlayer(Vector3 floorSpot)
    {
        BodySettings bodyDefaults = new BodySettings();
        float rigScale = PlayerController.DefaultRigScale;
        float spawnY = floorSpot.y + 0.05f + (bodyDefaults.bodySize.y * 0.5f - bodyDefaults.bodyOffset.y) * rigScale;
        Vector3 spawn = new Vector3(floorSpot.x, spawnY, 0f);

        GameObject player = new GameObject("Player");
        player.transform.position = spawn;
        player.AddComponent<Rigidbody2D>();
        player.AddComponent<CapsuleCollider2D>();
        PlayerController controller = player.AddComponent<PlayerController>();
        PlayerVisuals visuals = player.AddComponent<PlayerVisuals>();
        player.AddComponent<GlitchRecorder>();

        GameObject hammer = new GameObject("Hammer");
        hammer.transform.position = spawn + new Vector3(1f, 1f, 0f);
        Rigidbody2D hammerRb = hammer.AddComponent<Rigidbody2D>();
        hammer.AddComponent<CircleCollider2D>();
        hammer.AddComponent<DistanceJoint2D>();
        hammer.AddComponent<HammerContact>().owner = controller;

        visuals.hammer = hammer.transform;
        visuals.pot = AddShape("Pot", player.transform, whiteSprite);
        visuals.torso = AddShape("Torso", player.transform, whiteSprite);
        visuals.head = AddShape("Head", player.transform, circleSprite);
        visuals.leftArm = AddShape("Left Arm", player.transform, whiteSprite);
        visuals.rightArm = AddShape("Right Arm", player.transform, whiteSprite);
        visuals.shaft = AddShape("Shaft", hammer.transform, whiteSprite);
        visuals.hammerHead = AddShape("Hammer Head Shape", hammer.transform, whiteSprite);

        visuals.bodyArtTransform = AddShape("Body Art", player.transform, null);
        visuals.hammerHeadArtTransform = AddShape("Hammer Head Art", hammer.transform, null);
        visuals.handleArtTransform = AddShape("Handle Art", player.transform, null);
        visuals.bodyArt.sprite = bodyArtSprite;
        visuals.bodyArt.artPixels = bodyArtPixels;

        controller.hammerBody = hammerRb;
        controller.visuals = visuals;
        controller.ApplyRig();
        return controller;
    }

    static void PrepareArt()
    {
        bodyArtSprite = null;
        bodyArtPixels = Vector2.zero;
        string path = ArtFolder + "/" + BodyArtFile;
        if (!File.Exists(path))
        {
            Debug.LogWarning("BounceBack: " + path + " not found, the player keeps its flat shapes.");
            return;
        }

        Vector2 pixels;
        Vector2 pivot;
        MeasureArt(path, out pixels, out pivot);
        ImportArtSprite(path, true, pivot);
        bodyArtSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        bodyArtPixels = pixels;
    }

    static void MeasureArt(string path, out Vector2 pixels, out Vector2 pivot)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.LoadImage(File.ReadAllBytes(path));
        int w = tex.width;
        int h = tex.height;
        Color32[] data = tex.GetPixels32();
        int minX = w, maxX = -1, minY = h, maxY = -1;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                if (data[y * w + x].a > 0)
                {
                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }
        }
        Object.DestroyImmediate(tex);

        if (maxX < 0)
        {
            pixels = new Vector2(w, h);
            pivot = new Vector2(0.5f, 0.5f);
            return;
        }
        pixels = new Vector2(maxX - minX + 1, maxY - minY + 1);
        pivot = new Vector2((minX + maxX + 1) * 0.5f / w, (minY + maxY + 1) * 0.5f / h);
    }

    static void ImportArtSprite(string path, bool pixelArt, Vector2 pivot)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer == null)
        {
            return;
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.filterMode = pixelArt ? FilterMode.Point : FilterMode.Bilinear;
        importer.spritePixelsPerUnit = 100f;
        importer.SaveAndReimport();

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    static Transform AddShape(string name, Transform parent, Sprite sprite)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sharedMaterial = spriteMaterial;
        return go.transform;
    }

    static void ApplyProjectSettings()
    {
        Object[] timeAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset");
        if (timeAssets != null && timeAssets.Length > 0)
        {
            SerializedObject time = new SerializedObject(timeAssets[0]);
            SerializedProperty fixedStep = time.FindProperty("Fixed Timestep");
            SerializedProperty maxStep = time.FindProperty("Maximum Allowed Timestep");
            if (fixedStep != null)
            {
                fixedStep.floatValue = GameSettings.FixedStep;
            }
            if (maxStep != null)
            {
                maxStep.floatValue = GameSettings.MaxFrameTime;
            }
            time.ApplyModifiedProperties();
        }

        Object[] physicsAssets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/Physics2DSettings.asset");
        if (physicsAssets != null && physicsAssets.Length > 0)
        {
            SerializedObject physics = new SerializedObject(physicsAssets[0]);
            SerializedProperty velocityIterations = physics.FindProperty("m_VelocityIterations");
            SerializedProperty positionIterations = physics.FindProperty("m_PositionIterations");
            SerializedProperty gravity = physics.FindProperty("m_Gravity");
            SerializedProperty defaultMaterial = physics.FindProperty("m_DefaultMaterial");
            if (velocityIterations != null)
            {
                velocityIterations.intValue = GameSettings.VelocityIterations;
            }
            if (positionIterations != null)
            {
                positionIterations.intValue = GameSettings.PositionIterations;
            }
            if (gravity != null)
            {
                gravity.vector2Value = new Vector2(0f, GameSettings.Gravity);
            }
            if (defaultMaterial != null)
            {
                defaultMaterial.objectReferenceValue = GetOrCreateGroundMaterial();
            }
            physics.ApplyModifiedProperties();
        }

        for (int a = 0; a < 32; a++)
        {
            for (int b = a; b < 32; b++)
            {
                Physics2D.IgnoreLayerCollision(a, b, false);
            }
        }

        QualitySettings.vSyncCount = 0;
    }
    static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
        {
            AssetDatabase.CreateFolder(parent, name);
        }
    }

    static Sprite GetOrCreateSprite(string path, bool disc)
    {
        Sprite existing = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (existing != null)
        {
            return existing;
        }

        int size = disc ? 64 : 1;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float alpha = 1f;
                if (disc)
                {
                    float dx = x + 0.5f - radius;
                    float dy = y + 0.5f - radius;
                    alpha = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = size;
        importer.mipmapEnabled = false;
        importer.filterMode = disc ? FilterMode.Bilinear : FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Material GetOrCreateMaterial()
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
        Material mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, MaterialPath);
        return mat;
    }

    static PhysicsMaterial2D GetOrCreateGroundMaterial()
    {
        PhysicsMaterial2D existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(GroundMaterialPath);
        if (existing != null)
        {
            return existing;
        }

        PhysicsMaterial2D mat = new PhysicsMaterial2D("DefaultGround");
        mat.friction = GameSettings.DefaultFriction;
        mat.bounciness = GameSettings.DefaultBounciness;
        AssetDatabase.CreateAsset(mat, GroundMaterialPath);
        return mat;
    }
}
