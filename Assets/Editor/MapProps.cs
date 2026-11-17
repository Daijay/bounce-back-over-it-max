using UnityEditor;
using UnityEngine;

public static class MapProps
{
    public static bool Enabled = true;

    static Material material;

    public static void Build(Transform root, int seed, float startX)
    {
        if (!Enabled)
        {
            return;
        }
        material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/SpriteUnlit.mat");
        Transform props = new GameObject("Props").transform;
        props.SetParent(root, false);
        System.Random random = new System.Random(seed + 7700);

        int trees = 0;
        for (float x = -14f; x < 11f; x += 2.6f + (float)random.NextDouble() * 1.4f)
        {
            if (Mathf.Abs(x - startX) < 2.4f)
            {
                continue;
            }
            float scale = 3.2f + (float)random.NextDouble() * 1.6f;
            Place(props, "Tree " + trees, "prop_tree", new Vector2(x, -0.12f), scale, random.Next(2) == 0, 4);
            trees++;
        }

        string[] rockNames = { "prop_rock_cube", "prop_rock_ball", "prop_rock_shard" };
        float[] rockScale = { 2.2f, 3.0f, 3.2f };
        int rocks = 0;
        for (float x = -12.5f; x < 11f; x += 2.1f + (float)random.NextDouble() * 2.2f)
        {
            if (Mathf.Abs(x - startX) < 1.6f)
            {
                continue;
            }
            int kind = random.Next(3);
            float scale = rockScale[kind] * (0.8f + (float)random.NextDouble() * 0.5f);
            Place(props, "Rock " + rocks, rockNames[kind], new Vector2(x, -0.08f), scale, random.Next(2) == 0, 5);
            rocks++;
        }

        Place(props, "Globe Start", "prop_globe", new Vector2(startX - 3.6f, -0.05f), 2.6f, false, 5);

        
        float bracketY = 25.7f - 16f * MapStyle.PixelWorld - 0.5f - 0.78f;
        PlaceCentred(props, "Bridge Bracket Left", "prop_bracket", new Vector2(64.2f, bracketY), 2f, false, 5);
        PlaceCentred(props, "Bridge Bracket Right", "prop_bracket", new Vector2(83.6f, bracketY), 2f, true, 5);

        int stars = 0;
        for (float y = MapSlope.PeakY + 8f; y < MapSky.MoonY - 12f; y += 13f + (float)random.NextDouble() * 3f)
        {
            float x = MapSlope.PeakX + ((float)random.NextDouble() - 0.5f) * 3f;
            PlaceCentred(props, "Star " + stars, "prop_star", new Vector2(x, y), 3.0f, false, 5);
            stars++;
        }

        Debug.Log("BounceBack: props scattered: " + trees + " trees, " + rocks + " rocks, 1 globe, 2 bridge brackets, " + stars + " stars (pictures only, no colliders).");
    }

    static void Place(Transform parent, string name, string spriteName, Vector2 position, float scale, bool flip, int order)
    {
        GameObject go = Make(parent, name, spriteName, position, scale, flip, order);
        SpriteRenderer sr = go.GetComponent<SpriteRenderer>();
        if (sr.sprite != null)
        {
            float half = sr.sprite.bounds.size.y * 0.5f * scale;
            go.transform.localPosition = new Vector3(position.x, position.y + half, 0f);
        }
    }

    static void PlaceCentred(Transform parent, string name, string spriteName, Vector2 position, float scale, bool flip, int order)
    {
        Make(parent, name, spriteName, position, scale, flip, order);
    }

    static GameObject Make(Transform parent, string name, string spriteName, Vector2 position, float scale, bool flip, int order)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(position.x, position.y, 0f);
        go.transform.localScale = new Vector3(scale, scale, 1f);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = MapAssets.GetSprite(spriteName);
        sr.flipX = flip;
        sr.sortingOrder = order;
        if (material != null)
        {
            sr.sharedMaterial = material;
        }
        return go;
    }
}
