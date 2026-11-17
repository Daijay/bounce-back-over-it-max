using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class BuildMap
{
    public static readonly Vector2 StartPoint = new Vector2(7f, 0f);

    const int DefaultSeed = 12345;
    const float DefaultBumpiness = 1f;

    const string SpriteMaterialPath = "Assets/Generated/SpriteUnlit.mat";

    const float MinThickness = 0.25f;
    const float DeckThickness = 16f * MapStyle.PixelWorld;
    const float RopeThickness = 4f * MapStyle.PixelWorld;

    const float BridgeTop = 25.7f;

    const float StepsPerUnit = 0.07f;

    static int currentSeed;
    static int meshCounter;

    [MenuItem("BounceBack/Build Map")]
    public static void Build()
    {
        if (EditorApplication.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Build Map", "Stop Play mode first. Building (and the script reload it can trigger) while playing breaks the running Player.", "OK");
            return;
        }
        if (Object.FindAnyObjectByType<PlayerController>() == null)
        {
            EditorUtility.DisplayDialog("Build Map", "There is no Player in the open scene. Run BounceBack > Build Test Scene first.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Build Map",
                "This deletes the object named Level in the open scene and rebuilds the map from the sketch. The Player, Hammer, Main Camera, HUD and Background are not touched.",
                "Yes", "Cancel"))
        {
            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        int seed = DefaultSeed;
        float bumpiness = DefaultBumpiness;
        bool remembered = false;
        List<GameObject> oldLevels = new List<GameObject>();
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name != "Level")
            {
                continue;
            }
            oldLevels.Add(roots[i]);

            MapSettings settings = roots[i].GetComponent<MapSettings>();
            if (settings != null && !remembered)
            {
                seed = settings.seed;
                bumpiness = settings.bumpiness;
                remembered = true;
            }
        }

        foreach (GameObject old in oldLevels)
        {
            Undo.DestroyObjectImmediate(old);
        }

        MapGrass.WriteTextures();
        MapAssets.ImportAll();
        MapAssets.ClearMeshes();

        GameObject level = new GameObject("Level");
        Undo.RegisterCreatedObjectUndo(level, "Build Map");
        BuildLevel(level.transform, seed, bumpiness);

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("BounceBack: map built. Save the scene (Ctrl+S) and press Play.");
    }

    public static void BuildLevel(Transform root, int seed, float bumpiness)
    {
        meshCounter = 0;
        currentSeed = seed;
        MapGrass.WriteTextures();
        MapAssets.ImportAll();
        PlanBoulders(seed);

        MapSettings settings = root.gameObject.AddComponent<MapSettings>();
        settings.seed = seed;
        settings.bumpiness = bumpiness;

        Transform start = NewParent("Start", root);
        BuildStart(start);


        Transform leftMountain = NewParent("LeftMountain", root);
        BuildLeftMountain(leftMountain, seed, bumpiness);

        BuildGrassBlend(NewParent("GrassBlend", root), seed);

        Transform mainMountain = NewParent("MainMountain", root);
        BuildMainMountain(mainMountain, seed, bumpiness);

        Transform bridge = NewParent("Bridge", root);
        BuildBridge(bridge);

        Transform lavaPit = NewParent("LavaPit", root);
        BuildLavaPit(lavaPit, seed);

        Transform rightCliff = NewParent("RightCliff", root);
        BuildRightCliff(rightCliff, seed, bumpiness);
        List<List<Vector2>> pitWalls = new List<List<Vector2>>();
        if (mainPolygon != null)
        {
            pitWalls.Add(mainPolygon);
        }
        if (cliffPolygon != null)
        {
            pitWalls.Add(cliffPolygon);
        }
        MapVolcano.Build(root, pitWalls);
        Transform boulders = NewParent("Boulders", root);
        BuildBoulders(boulders);

        BuildSnow(NewParent("Snow", root), seed);
        MapSky.Build(root);

        MapProps.Build(root, seed, StartPoint.x);
    }

    static void BuildStart(Transform start)
    {
        GameObject ground = TiledBox("Ground", start, new Vector2(6f, -5f), new Vector2(44f, 10f), 0f, "rock", BlockKind.Grey, MapStyle.OrderSolid, true);
        SpriteRenderer rockPicture = ground.GetComponent<SpriteRenderer>();
        if (rockPicture != null)
        {
            rockPicture.enabled = false;
        }
        BuildGrassField(start);
        BuildBoundaries(start);
        GameObject playerStart = new GameObject("PlayerStart");
        playerStart.transform.SetParent(start, false);
        playerStart.transform.localPosition = new Vector3(StartPoint.x, StartPoint.y + 0.05f, 0f);
        playerStart.AddComponent<PlayerSpawn>();
    }


    static List<Vector2> leftFace;

    static List<Vector2> mainPolygon;
    static List<Vector2> cliffPolygon;

    static void BuildGrassField(Transform start)
    {
        float pixel = MapStyle.PixelWorld;
        float width = MapGrass.GroundWidthPixels() * pixel;
        float height = MapGrass.GroundHeightPixels() * pixel;
        float left = MapGrass.GroundOriginPixel() * pixel;
        float top = MapGrass.GroundLineRow() * pixel;
        MakeSprite("Ground Layers", start, MapGrass.GroundName, new Vector2(left + width * 0.5f, top - height * 0.5f), MapStyle.OrderRockShade);
    }

    static void BuildGrassBlend(Transform parent, int seed)
    {
        if (leftFace == null || leftFace.Count < 2)
        {
            return;
        }
        System.Random random = new System.Random(seed + 41);
        float startX = MapArt.GrassTaperStart + 1f;
        float length = MapArt.FootEnd + 1.2f - startX;
        float x = startX;
        int index = 0;
        while (x < startX + length)
        {
            float t = (x - startX) / length;
            float y;
            if (random.NextDouble() < 0.8 - 0.6 * t && FaceHeight(x, out y))
            {
                SpriteRenderer tuft = MakeSprite("Grass Tuft " + index, parent, MapGrass.TuftName,
                    new Vector2(x, y - MapStyle.PixelWorld), MapStyle.OrderSolid);
                tuft.flipX = random.Next(2) == 0;
                index++;
            }
            x += 0.18f + (float)random.NextDouble() * 0.22f;
        }
    }

    static bool FaceHeight(float x, out float y)
    {
        bool found = false;
        y = 0f;
        for (int i = 0; i + 1 < leftFace.Count; i++)
        {
            Vector2 a = leftFace[i];
            Vector2 b = leftFace[i + 1];
            if (((a.x <= x && b.x >= x) || (b.x <= x && a.x >= x)) &&!Mathf.Approximately(a.x, b.x))
            {
                float h = a.y + (b.y - a.y) * (x - a.x) / (b.x - a.x);
                if (!found || h > y)
                {
                    y = h;
                    found = true;
                }
            }
        }
        return found;
    }


    static List<Vector2> mainFace;
    static List<Vector2> cliffFace;
    static List<Vector2> cliffIce;
    static List<Vector2> towerIce;

    const float IceStartY = 25f;
    const float IceEndX = 56.6f;
    const float IceCapThickness = 0.3f;

    static float AddGrips(Transform parent, IList<Vector2> path, float minY, bool skipIceZone, string prefix, out int count)
    {
        count = 0;
        float largest = 0f;
        float lastTop = float.NaN;
        float walked = 0f;
        float next = 0f;
        for (int i = 0; i + 1 < path.Count; i++)
        {
            Vector2 a = path[i];
            Vector2 b = path[i + 1];
            float length = (b - a).magnitude;
            if (length < 0.01f)
            {
                continue;
            }
            Vector2 dir = (b - a) / length;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            while (next <= walked + length)
            {
                Vector2 point = a + dir * (next - walked);
                bool skip = skipIceZone && point.x > 47f && point.x < IceEndX && point.y >= IceStartY - 1f;
                if (normal.y > -0.2f && point.y >= minY && !skip)
                {
                    MapRockDetail.Boulder boulder = new MapRockDetail.Boulder();
                    boulder.size = 0;
                    boulder.variant = count % MapRockDetail.Variants;
                    boulder.rotation = 0f;
                    boulder.centre = point + normal * (0.07f + 0.12f);
                    MakeBoulder(parent, prefix + " " + count, boulder);
                    float top = MapRockDetail.Top(boulder);
                    if (!float.IsNaN(lastTop))
                    {
                        largest = Mathf.Max(largest, Mathf.Abs(top - lastTop));
                    }
                    lastTop = top;
                    count++;
                }
                next += 1.7f;
            }
            walked += length;
        }
        return largest;
    }
    static float MainIceLift(Vector2 p)
    {
        return (p.x > 47.5f && p.x < IceEndX && p.y >= IceStartY - 0.3f) ? 0.22f : -1f;
    }

    static float SlopeIceLift(Vector2 p)
    {
        return ((p.y >= 76f && p.y <= 82f) || (p.y >= 85f && p.y <= 91f) || (p.y >= 93f && p.y <= 98f)) ? 0.22f : -1f;
    }

    static float OnIceSurface(Vector2 p)
    {
        return 0f;
    }

    static void BuildSnow(Transform parent, int seed)
    {
        int count;
        int total = 0;
        float largest = 0f;
        if (leftFace != null)
        {
            MapCaps.Snow(parent, "Snow Left Mountain", leftFace, 15.4f, 25.7f, seed + 51, MapStyle.OrderSnow);
            largest = Mathf.Max(largest, AddGrips(parent, leftFace, 15.4f - 0.8f, false, "Snow Grip Left", out count));
            total += count;
        }
        if (mainFace != null)
        {
            MapCaps.Snow(parent, "Snow Main Mountain", mainFace, 20.5f, 34.1f, seed + 52, MapStyle.OrderSnow, MainIceLift);
            largest = Mathf.Max(largest, AddGrips(parent, mainFace, 20.5f - 0.8f, true, "Snow Grip Main", out count));
            total += count;
        }
        if (cliffFace != null)
        {
            MapCaps.Snow(parent, "Snow Cliff Face", cliffFace, 23.9f, 39.8f, seed + 53, MapStyle.OrderSnow);
            largest = Mathf.Max(largest, AddGrips(parent, cliffFace, 23.9f - 0.8f, false, "Snow Grip Cliff", out count));
            total += count;
        }
        if (cliffIce != null)
        {
            MapCaps.Snow(parent, "Snow Cliff Top", cliffIce, 23.9f, 39.8f, seed + 54, MapStyle.OrderSnow, OnIceSurface);
        }
        {
            List<Vector2> slopeFace = MapSlope.Face();
            MapCaps.Snow(parent, "Snow Right Mountain", slopeFace, 40f, MapSlope.PeakY, seed + 56, MapStyle.OrderSnow, SlopeIceLift);
            largest = Mathf.Max(largest, AddGrips(parent, slopeFace, 40f - 0.8f, false, "Snow Grip Right", out count));
            total += count;
        }
        if (towerIce != null)
        {
            MapCaps.Snow(parent, "Snow Tower Top", towerIce, 44.6f, 52.4f, seed + 55, MapStyle.OrderSnow, OnIceSurface);
        }
        Debug.Log("BounceBack: snow is physical now, " + total + " grip boulders placed on the snowy routes, largest rise between grips " + largest.ToString("0.00")
            + " (limit " + MapRockDetail.MaxRise + ", hammer reach 2.25).");

        GameObject trailObject = new GameObject("Snow Trail");
        trailObject.transform.SetParent(parent, false);
        trailObject.AddComponent<SnowTrail>();
    }

    static void BuildBoundaries(Transform start)
    {
        GameObject left = new GameObject("Left Boundary");
        left.transform.SetParent(start, false);
        float leftFace = StartPoint.x - 12f;
        left.transform.localPosition = new Vector3(leftFace - 0.6f, 25f, 0f);
        BoxCollider2D leftBox = left.AddComponent<BoxCollider2D>();
        leftBox.size = new Vector2(1.2f, 70f);
        BlockType leftBlock = left.AddComponent<BlockType>();
        leftBlock.kind = BlockKind.Grey;
        leftBlock.useArt = true;
        AddBoundaryGlow(left, leftFace, 1, 70f);

        float wallBottom = -10f;
        float wallTop = MapSky.MoonY + 30f;
        float wallHeight = wallTop - wallBottom;
        GameObject right = TiledBox("Right Boundary", start, new Vector2(MapSlope.BoundaryX + 1f, (wallTop + wallBottom) * 0.5f), new Vector2(2f, wallHeight), 0f, "rock", BlockKind.Grey, MapStyle.OrderSolid, true);
        SpriteRenderer rockPicture = right.GetComponent<SpriteRenderer>();
        if (rockPicture != null)
        {
            rockPicture.enabled = false;
        }
        AddBoundaryGlow(right, MapSlope.BoundaryX, -1, wallHeight);
    }

    public static void AddBoundaryGlow(GameObject wall, float faceX, int side, float height)
    {
        GameObject field = new GameObject("Force Field");
        field.transform.SetParent(wall.transform, false);
        field.transform.position = new Vector3(faceX, wall.transform.position.y, 0f);
        SpriteRenderer sr = field.AddComponent<SpriteRenderer>();
        sr.sprite = MapAssets.GetSprite("force_field");
        sr.sortingOrder = MapStyle.OrderBoundary;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(0.96f, height);
        sr.enabled = false;
        Material material = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
        if (material != null)
        {
            sr.sharedMaterial = material;
        }
        BoundaryGlow glow = wall.AddComponent<BoundaryGlow>();
        glow.field = sr;
        glow.faceX = faceX;
        glow.side = side;
    }
    static List<Vector2> LeftControl()
    {
        List<Vector2> path = new List<Vector2>();
        for (int i = 0; i <= MapArt.FootSegments; i++)
        {
            path.Add(new Vector2(MapArt.FootX(i), MapArt.FootY(i)));
        }
        path.AddRange(MakePath(
            32.6f, 12.2f, 34.1f, 19.2f,
            36.0f, 22.1f, 40.2f, 24.5f, 44.4f, 25.7f, 46.1f, 24.0f, 47.5f, 23.0f));
        return path;
    }

    static List<Vector2> MainControl()
    {
        return MakePath(
            47.5f, 23.0f, 48.6f, 25.4f, 51.6f, 31.4f, 53.4f, 33.6f, 55.0f, 34.1f,
            56.4f, 33.1f, 58.2f, 29.6f, 59.6f, 27.2f, 59.7f, 25.7f, 60.0f, 18.2f,
            61.2f, 11.0f, 63.0f, 6.2f, 64.9f, 1.9f, 65.4f, 0.0f, 66.2f, -7.0f);
    }

    static List<Vector2> CliffControl()
    {
        return MakePath(
            80.4f, -7f, 80.6f, 0f, 81.0f, 5.0f, 84.0f, 8.6f, 85.8f, 14.6f, 86.4f, 21.0f, 86.8f, 23.6f, 87.1f, 25.0f, MapSlope.StartX, MapSlope.StartY);
    }

    static void BuildLeftMountain(Transform parent, int seed, float bumpiness)
    {
        List<Vector2> control = LeftControl();

        List<Vector2> face = MapRockDetail.AddSteps(MapRockDetail.AddStepsNear(MapGeometry.BumpyPath(control, seed + 1, bumpiness), leftPlan.stepTargets, seed + 31), seed + 11, StepsPerUnit);
        leftFace = face;
        List<Vector2> polygon = new List<Vector2>(face);
        polygon.AddRange(MakePath(53f, 22f, 53f, -10f, MapArt.FootStart, -10f));
        RockMass("LeftMountain Rock", parent, polygon, "rock", BlockKind.Grey, MapStyle.OrderRock);
    }

    static void BuildMainMountain(Transform parent, int seed, float bumpiness)
    {
        List<Vector2> control = MainControl();

        List<Vector2> face = MapRockDetail.AddSteps(MapRockDetail.AddStepsNear(MapGeometry.BumpyPath(control, seed + 2, bumpiness), mainPlan.stepTargets, seed + 32), seed + 12, StepsPerUnit);
        List<Vector2> polygon = new List<Vector2>(face);
        polygon.AddRange(MakePath(66.2f, -10f, 47.5f, -10f));
        mainPolygon = polygon;
        mainFace = face;
        RockMass("MainMountain Rock", parent, polygon, "rock", BlockKind.Grey, MapStyle.OrderRock);

        List<Vector2> icePath = new List<Vector2>();
        bool started = false;
        for (int i = 0; i < face.Count; i++)
        {
            if (!started && face[i].y >= IceStartY && face[i].x < IceEndX)
            {
                started = true;
            }
            if (started)
            {
                if (face[i].x > IceEndX)
                {
                    break;
                }
                icePath.Add(face[i]);
            }
        }
        MapCaps.IceCapSmooth(parent, "Ice Cap Peak", icePath, IceCapThickness, MapStyle.OrderRockShade);
        int extra;
        float largest = AddGrips(parent, icePath, float.NegativeInfinity, false, "Ice Grip Boulder", out extra);
        Debug.Log("BounceBack: ice on the main peak: " + icePath.Count + " outline points, " + extra + " grip boulders on it, largest rise between them "
            + largest.ToString("0.00") + " (limit " + MapRockDetail.MaxRise + ", hammer reach 2.25).");
    }

    static void OneWay(GameObject plank)
    {
        BoxCollider2D box = plank.GetComponent<BoxCollider2D>();
        box.usedByEffector = true;
        PlatformEffector2D effector = plank.AddComponent<PlatformEffector2D>();
        effector.useOneWay = true;
        effector.useOneWayGrouping = true;
        effector.surfaceArc = 170f;
        effector.useSideFriction = false;
        effector.useSideBounce = false;
    }

    static void BuildBridge(Transform bridge)
    {
        TiledBox("BridgeAbutment", bridge, new Vector2(61.05f, BridgeTop - 0.25f), new Vector2(4.1f, 0.5f),
            0f, "rock", BlockKind.Grey, MapStyle.OrderSolid, true);

        for (int i = 0; i < 4; i++)
        {
            float width = 1.325f;
            float left = 64.9f + width * i;
            OneWay(TiledBox("LeftPlank " + i, bridge, new Vector2(left + width * 0.5f, BridgeTop - DeckThickness * 0.5f),
                new Vector2(width, DeckThickness), 0f, "plank", BlockKind.Grey, MapStyle.OrderSolid, true));
        }

        GameObject pad = TiledBox("Bounce Pad", bridge, new Vector2(68.8f, 25.9f), new Vector2(2.8f, 0.4f),
            0f, "pad", BlockKind.Green, MapStyle.OrderSolid, true);
        StylePad(pad);

        OneWay(TiledBox("Floating Plank", bridge, new Vector2(73.8f, 26.66f), new Vector2(3.2f, DeckThickness),
            3f, "plank", BlockKind.Grey, MapStyle.OrderSolid, true));

        for (int i = 0; i < 8; i++)
        {
            if (i == 4)
            {
                continue;
            }
            float width = 1.2875f;
            float left = 77.0f + width * i;
            OneWay(TiledBox("RightPlank " + i, bridge, new Vector2(left + width * 0.5f, BridgeTop - DeckThickness * 0.5f),
                new Vector2(width, DeckThickness), 0f, "plank", BlockKind.Grey, MapStyle.OrderSolid, true));
        }

        float[] leftPosts = { 60.0f, 62.0f, 65.2f, 66.9f, 69.9f };
        float[] rightPosts = { 78.5f, 80.5f, 83.5f, 85.5f, 87.0f };
        for (int i = 0; i < leftPosts.Length; i++)
        {
            Decor("Post L " + i, bridge, "wood_post", new Vector2(leftPosts[i], BridgeTop), 0f, MapStyle.OrderSolid, false);
        }
        for (int i = 0; i < rightPosts.Length; i++)
        {
            Decor("Post R " + i, bridge, "wood_post", new Vector2(rightPosts[i], BridgeTop), 0f, MapStyle.OrderSolid, false);
        }
        float[] ropeX = { 60.0f, 62.0f, 65.2f, 69.9f, 78.5f, 80.5f, 83.5f, 87.0f };
        for (int i = 0; i < ropeX.Length; i += 2)
        {
            RopeRun(bridge, ropeX[i], ropeX[i + 1], BridgeTop + 1.44f);
            RopeRun(bridge, ropeX[i], ropeX[i + 1], BridgeTop + 0.72f);
        }

        Decor("HangingPlank Left", bridge, "hanging_plank", new Vector2(70.0f, 24.9f), -80f, MapStyle.OrderDecor, false);
        Decor("HangingPlank Right", bridge, "hanging_plank", new Vector2(77.2f, 24.9f), 80f, MapStyle.OrderDecor, false);
    }

    static void StylePad(GameObject pad)
    {
        SpriteRenderer old = pad.GetComponent<SpriteRenderer>();
        if (old != null)
        {
            old.enabled = false;
        }
        float pixel = MapStyle.PixelWorld;
        float bodyHeight = 13f * pixel;
        MakeSprite("Pad Look", pad.transform, "pad_look", new Vector2(0f, 0.2f - bodyHeight * 0.5f), MapStyle.OrderSolid);

        PadArrows arrows = pad.AddComponent<PadArrows>();
        arrows.arrows = new Transform[4];
        float spacing = 0.62f;
        for (int i = 0; i < 4; i++)
        {
            float x = (i - 1.5f) * spacing;
            SpriteRenderer arrow = MakeSprite("Pad Arrow " + i, pad.transform, "pad_arrow", new Vector2(x, 0.2f + 0.06f + 3.5f * pixel), MapStyle.OrderDecor);
            arrows.arrows[i] = arrow.transform;
        }
    }
    static void BuildLavaPit(Transform lavaPit, int seed)
    {
        float lavaDepth = 11f;
        float lavaCentre = -lavaDepth * 0.5f;

        Sprite[] lavaFrames = new Sprite[]
        {
            MapAssets.GetSprite("lava_0"), MapAssets.GetSprite("lava_1"),
            MapAssets.GetSprite("lava_2"), MapAssets.GetSprite("lava_3")
        };
        Sprite[] bubbleFrames = new Sprite[]
        {
            MapAssets.GetSprite("bubble_0"), MapAssets.GetSprite("bubble_1"),
            MapAssets.GetSprite("bubble_2"), MapAssets.GetSprite("bubble_3")
        };

        GameObject lava = new GameObject("Lava");
        lava.transform.SetParent(lavaPit, false);
        lava.transform.localPosition = new Vector3(73f, lavaCentre, 0f);

        BoxCollider2D zone = lava.AddComponent<BoxCollider2D>();
        zone.size = new Vector2(17f, lavaDepth);
        zone.isTrigger = true;

        BlockType block = lava.AddComponent<BlockType>();
        block.kind = BlockKind.Lava;
        block.useArt = true;

        LavaZone lavaZone = lava.AddComponent<LavaZone>();
        lavaZone.frames = lavaFrames;

        SpriteRenderer body = MakeSprite("Body", lava.transform, "lava_0", Vector2.zero, MapStyle.OrderLava);
        body.drawMode = SpriteDrawMode.Tiled;
        body.tileMode = SpriteTileMode.Continuous;
        body.size = new Vector2(17f, lavaDepth);
        lavaZone.body = body;

        SpriteRenderer front = MakeSprite("Front", lava.transform, "lava_0", new Vector2(0.1f, -0.7f - lavaCentre), MapStyle.OrderLavaFront);
        front.drawMode = SpriteDrawMode.Tiled;
        front.tileMode = SpriteTileMode.Continuous;
        front.size = new Vector2(13.4f, 1.4f);
        front.color = new Color(1f, 1f, 1f, 0.55f);
        lavaZone.front = front;

        LavaBubbles bubbles = lava.AddComponent<LavaBubbles>();
        bubbles.frames = bubbleFrames;
        bubbles.xMin = 66f;
        bubbles.xMax = 80f;
        bubbles.surfaceY = 0f;
        bubbles.floorY = -10.5f;
        bubbles.count = 26;
        bubbles.seed = seed;
        bubbles.sortingOrder = MapStyle.OrderBubbles;
    }
    const float RightSmooth = 0.45f;
    static void BuildRightCliff(Transform parent, int seed, float bumpiness)
    {
        List<Vector2> polygon = MapGeometry.BumpyPath(CliffControl(), seed + 3, bumpiness * RightSmooth);
        List<Vector2> slope = MapSlope.Face();
        for (int i = 1; i < slope.Count; i++)
        {
            polygon.Add(slope[i]);
        }
        polygon.Add(new Vector2(MapSlope.BoundaryX, -10f));
        polygon.Add(new Vector2(80.4f, -10f));
        cliffPolygon = polygon;
        cliffFace = null;
        cliffIce = null;
        towerIce = null;
        RockMass("RightMountain Rock", parent, polygon, "rock", BlockKind.Grey, MapStyle.OrderRock);

        List<Vector2> climb = MapSlope.Climb();
        float[,] patches = { { 76f, 82f }, { 85f, 91f }, { 93f, 98f } };
        for (int k = 0; k < 3; k++)
        {
            List<Vector2> path = new List<Vector2>();
            for (int i = 0; i < climb.Count; i++)
            {
                if (climb[i].y >= patches[k, 0] && climb[i].y <= patches[k, 1])
                {
                    path.Add(climb[i]);
                }
            }
            if (path.Count > 3)
            {
                MapCaps.IceCapSmooth(parent, "Ice Cap Slope " + k, path, IceCapThickness, MapStyle.OrderRockShade);
            }
        }
    }
    static MapRockDetail.FacePlan leftPlan;
    static MapRockDetail.FacePlan mainPlan;
    static MapRockDetail.FacePlan slopePlan;

    static void PlanBoulders(int seed)
    {
        Rect none = new Rect(0f, 0f, 0f, 0f);
        leftPlan = MapRockDetail.PlanFace(LeftControl(), seed + 21, none, true, 1.6f, 1.5f);
        mainPlan = MapRockDetail.PlanFace(MainControl(), seed + 22, new Rect(58.3f, 25.0f, 6f, 1.7f), false);
        slopePlan = MapRockDetail.PlanSlope(MapSlope.Face(), seed + 23, 1.5f, new Rect(86.4f, 24.4f, 3.5f, 2.2f));
        Debug.Log("BounceBack: largest foothold gap on the left face " + MapRockDetail.LargestGap(leftPlan.footholds).ToString("0.00")
            + ", on the main mountain " + MapRockDetail.LargestGap(mainPlan.footholds).ToString("0.00")
            + ", on the right slope " + MapRockDetail.LargestGap(slopePlan.footholds).ToString("0.00") + " (hammer reach 2.25).");
    }

    static void BuildBoulders(Transform parent)
    {
        PlaceFace(parent, "Left", leftPlan, 0f, 0f);
        PlaceFace(parent, "Main", mainPlan, 58f, 24.3f);
        PlaceFace(parent, "Slope", slopePlan, 0f, 0f);

        System.Random random = new System.Random(currentSeed + 61);
        int index = 0;
        for (float x = -2.6f; x < 11f; x += 2.3f)
        {
            float place = x + ((float)random.NextDouble() - 0.5f) * 0.9f;
            double roll = random.NextDouble();
            int size = roll < 0.5 ? 0 : (roll < 0.85 ? 1 : 2);
            int variant = random.Next(MapRockDetail.Variants);
            float rotation = ((float)random.NextDouble() - 0.5f) * 14f;
            bool blocked = Mathf.Abs(place - StartPoint.x) < 2.3f || Mathf.Abs(place - 10.5f) < 1.5f || Mathf.Abs(place - 18.8f) < 1.5f || place < -3.2f;
            if (blocked)
            {
                continue;
            }
            LooseBoulder(parent, "Field Boulder " + index, size, variant, place, rotation);
            index++;
        }
    }

    static void LooseBoulder(Transform parent, string name, int size, int variant, float x, float rotation)
    {
        MapRockDetail.Boulder boulder = new MapRockDetail.Boulder();
        boulder.size = size;
        boulder.variant = variant;
        boulder.rotation = rotation;
        boulder.centre = new Vector2(x, 0f);
        float top;
        float bottom;
        MapRockDetail.Extent(boulder, out top, out bottom);
        boulder.centre.y = -bottom - 0.08f;
        MakeBoulder(parent, name, boulder, false);
    }

    static void PlaceFace(Transform parent, string label, MapRockDetail.FacePlan plan, float minX, float belowY)
    {
        for (int i = 0; i < plan.boulders.Count; i++)
        {
            if (plan.boulders[i].centre.x > minX && plan.boulders[i].centre.y < belowY)
            {
                continue;
            }
            bool onGrass = label == "Left" && plan.boulders[i].centre.x < 26f && plan.boulders[i].centre.y < 1.0f;
            MakeBoulder(parent, "Boulder " + label + " " + i, plan.boulders[i], !onGrass);
        }
    }

    static void MakeBoulder(Transform parent, string name, MapRockDetail.Boulder boulder, bool solid = true)
    {
        SpriteRenderer sr = MakeSprite(name, parent, MapRockDetail.BoulderName(boulder.size, boulder.variant), boulder.centre, MapStyle.OrderSolid);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, boulder.rotation);

        if (!solid)
        {
            return;
        }
        PolygonCollider2D shape = sr.gameObject.AddComponent<PolygonCollider2D>();
        shape.SetPath(0, MapRockDetail.Outline(boulder.size, boulder.variant));

        BlockType block = sr.gameObject.AddComponent<BlockType>();
        block.kind = BlockKind.Grey;
        block.useArt = true;
    }

    static void AddRockShade(GameObject rock, Mesh triangulated, List<Vector2> polygon, string meshName)
    {
        GameObject go = new GameObject("Shade");
        go.transform.SetParent(rock.transform, false);

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = MapAssets.SaveMesh(MapRockDetail.ShadeMesh(triangulated, polygon, meshName + "_shade"), meshName + "_shade");

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = GetShadeMaterial();
        mr.sortingOrder = MapStyle.OrderRockShade;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    public static Material GetShadeMaterial()
    {
        string path = MapAssets.GeneratedFolder + "/Mat_shade.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        if (!AssetDatabase.IsValidFolder(MapAssets.GeneratedFolder))
        {
            AssetDatabase.CreateFolder("Assets/Generated", "Map");
        }
        mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }


    static Transform NewParent(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    static void RockMass(string name, Transform parent, List<Vector2> polygon, string textureName, BlockKind kind, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        string meshName = name + "_" + meshCounter;
        meshCounter++;
        float tileSize = MapAssets.TextureWorldSize(textureName).x;
        Mesh raw = MapGeometry.CreateTiledMesh(polygon, tileSize, meshName);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = MapAssets.SaveMesh(raw, meshName);
        Mesh savedMesh = filter.sharedMesh;

        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = MapAssets.GetTileMaterial(textureName);
        mr.sortingOrder = order;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

        PolygonCollider2D shape = go.AddComponent<PolygonCollider2D>();
        shape.SetPath(0, polygon.ToArray());

        BlockType block = go.AddComponent<BlockType>();
        block.kind = kind;
        block.useArt = true;

        if (textureName == "rock")
        {
            AddRockShade(go, savedMesh, polygon, meshName);
        }
    }

    static GameObject TiledBox(string name, Transform parent, Vector2 center, Vector2 size, float rotationDegrees,
        string textureName, BlockKind kind, int order, bool solid)
    {
        if (solid)
        {
            if (size.x < MinThickness)
            {
                size.x = MinThickness;
            }
            if (size.y < MinThickness)
            {
                float top = center.y + size.y * 0.5f;
                size.y = MinThickness;
                center.y = top - size.y * 0.5f;
            }
        }

        SpriteRenderer sr = MakeSprite(name, parent, textureName, center, order);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = size;

        GameObject go = sr.gameObject;
        if (solid)
        {
            BoxCollider2D box = go.AddComponent<BoxCollider2D>();
            box.size = size;
        }

        BlockType block = go.AddComponent<BlockType>();
        block.kind = kind;
        block.useArt = true;
        return go;
    }

    static void Decor(string name, Transform parent, string spriteName, Vector2 position, float rotationDegrees, int order, bool flipX)
    {
        SpriteRenderer sr = MakeSprite(name, parent, spriteName, position, order);
        sr.transform.localRotation = Quaternion.Euler(0f, 0f, rotationDegrees);
        sr.flipX = flipX;

        BlockType block = sr.gameObject.AddComponent<BlockType>();
        block.kind = BlockKind.Grey;
        block.useArt = true;
    }

    static void RopeRun(Transform parent, float x1, float x2, float height)
    {
        float centreX = (x1 + x2) * 0.5f;
        float length = x2 - x1;
        TiledBox("Rope", parent, new Vector2(centreX, height), new Vector2(length, RopeThickness),
            0f, "rope", BlockKind.Grey, MapStyle.OrderSolid, false);
    }
    static SpriteRenderer MakeSprite(string name, Transform parent, string spriteName, Vector2 localPosition, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MapAssets.GetSprite(spriteName);
        sr.sortingOrder = order;

        Material spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteMaterialPath);
        if (spriteMaterial != null)
        {
            sr.sharedMaterial = spriteMaterial;
        }
        return sr;
    }

    static List<Vector2> MakePath(params float[] xy)
    {
        List<Vector2> path = new List<Vector2>();
        for (int i = 0; i + 1 < xy.Length; i += 2)
        {
            path.Add(new Vector2(xy[i], xy[i + 1]));
        }
        return path;
    }
}
