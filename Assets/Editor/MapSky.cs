using UnityEditor;
using UnityEngine;

public static class MapSky
{
    public static float MoonY { get { return MapSlope.PeakY + 90f; } }

    public static void Build(Transform root)
    {
        BuildTrigger(root);
        float moonRadius = MapTextureGen.MoonSize * MapStyle.PixelWorld * 0.5f;
        BuildMoon(root, new Vector2(MapSlope.PeakX, MoonY), moonRadius);
        BuildSkySpace(root);
        BuildBarriers(root);
    }

    static void BuildTrigger(Transform root)
    {
        GameObject trigger = new GameObject("EruptionTrigger");
        trigger.transform.SetParent(root, false);
        trigger.transform.localPosition = new Vector3(MapSlope.PeakX, MapSlope.PeakY + 0.5f, 0f);
        BoxCollider2D box = trigger.AddComponent<BoxCollider2D>();
        box.size = new Vector2(6f, 4f);
        box.isTrigger = true;
    }

    static void BuildBarriers(Transform root)
    {
        float bottom = MapSlope.PeakY + 6f;
        float top = MoonY + 30f;
        float height = top - bottom;
        float centreY = (top + bottom) * 0.5f;
        for (int side = -1; side <= 1; side += 2)
        {
            GameObject wall = new GameObject(side < 0 ? "SkyBarrier Left" : "SkyBarrier Right");
            wall.transform.SetParent(root, false);
            float face = MapSlope.PeakX + side * 20f;
            wall.transform.localPosition = new Vector3(face + side * 0.6f, centreY, 0f);
            BoxCollider2D box = wall.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1.2f, height);
            BlockType block = wall.AddComponent<BlockType>();
            block.kind = BlockKind.Grey;
            block.useArt = true;
            BuildMap.AddBoundaryGlow(wall, face, -side, height);
        }
    }

    static void BuildMoon(Transform root, Vector2 centre, float radius)
    {
        GameObject moon = new GameObject("CheeseMoon");
        moon.transform.SetParent(root, false);
        moon.transform.localPosition = new Vector3(centre.x, centre.y, 0f);

        CircleCollider2D trigger = moon.AddComponent<CircleCollider2D>();
        trigger.radius = radius * 1.06f;
        trigger.isTrigger = true;

        Sprite dream = MapAssets.GetSprite("cheese_moon_dream");
        GameObject holder = new GameObject("Moon Holder");
        holder.transform.SetParent(moon.transform, false);
        GameObject picture = new GameObject("Moon Picture");
        picture.transform.SetParent(holder.transform, false);
        SpriteRenderer sr = picture.AddComponent<SpriteRenderer>();
        sr.sprite = dream;
        sr.sortingOrder = MapStyle.OrderRock;
        if (dream != null)
        {
            float scale = radius * 2.1f / dream.bounds.size.x;
            holder.transform.localScale = new Vector3(scale, scale, 1f);
            holder.transform.localPosition = new Vector3(0f, -dream.bounds.size.y * scale * 0.17f, 0f);
        }
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/SpriteUnlit.mat");
        if (material != null)
        {
            sr.sharedMaterial = material;
        }

        CheeseMoonFx fx = moon.AddComponent<CheeseMoonFx>();
        fx.visual = picture.transform;
    }

    static void BuildSkySpace(Transform root)
    {
        GameObject space = new GameObject("SkySpace");
        space.transform.SetParent(root, false);
        SkySpace sky = space.AddComponent<SkySpace>();
        sky.spriteMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Generated/SpriteUnlit.mat");
        sky.gradientSprite = MapAssets.GetSprite("space_gradient");
        sky.starSprite = MapAssets.GetSprite("space_star");
    }
}
