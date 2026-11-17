using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class MapAssets
{
    public const string Folder = "Assets/Textures/Map";
    public const string GeneratedFolder = "Assets/Generated/Map";

    const string FallbackSpritePath = "Assets/Generated/White.png";

    static readonly HashSet<string> repeatNames = new HashSet<string>
    {
        "rock", "ice", "sticky", "pad", "gold", "crust",
        "lava_0", "lava_1", "lava_2", "lava_3", "plank", "rope",
        "grass", "grass_top", "grass_fill", "tree_trunk", "snow"
    };

    static readonly HashSet<string> warnedNames = new HashSet<string>();

    public static void ImportAll()
    {
        AssetDatabase.Refresh();
        EnsureGeneratedFolders();

        if (!Directory.Exists(Folder))
        {
            Debug.LogWarning("MapAssets: " + Folder + " not found, nothing to import.");
            return;
        }

        string[] files = Directory.GetFiles(Folder, "*.png");
        foreach (string file in files)
        {
            string assetPath = Folder + "/" + Path.GetFileName(file);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            string name = Path.GetFileNameWithoutExtension(file);
            if (ApplyImportSettings(importer, name))
            {
                importer.SaveAndReimport();
            }
        }
    }

    public static Sprite GetSprite(string name)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "/" + name + ".png");
        if (sprite != null)
        {
            return sprite;
        }

        if (warnedNames.Add(name))
        {
            Debug.LogWarning("MapAssets: sprite '" + name + "' not found in " + Folder + ", using the fallback.");
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(FallbackSpritePath);
    }

    public static Texture2D GetTexture(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/" + name + ".png");
    }

    public static Vector2 TextureWorldSize(string name)
    {
        Texture2D texture = GetTexture(name);
        if (texture == null)
        {
            return Vector2.one * 4f;
        }
        return new Vector2(texture.width, texture.height) * MapStyle.PixelWorld;
    }

    public static Material GetTileMaterial(string textureName)
    {
        string path = GeneratedFolder + "/Mat_" + textureName + ".mat";
        Texture2D texture = GetTexture(textureName);
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                Debug.LogError("MapAssets: no sprite shader found, cannot make " + path);
                return null;
            }

            EnsureGeneratedFolders();
            mat = new Material(shader);
            mat.mainTexture = texture;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        if (texture != null)
        {
            mat.mainTexture = texture;
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    public static Mesh SaveMesh(Mesh mesh, string name)
    {
        if (mesh == null)
        {
            return null;
        }

        EnsureGeneratedFolders();
        string path = GeneratedFolder + "/Meshes/" + name + ".asset";
        if (AssetDatabase.LoadMainAssetAtPath(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }
        AssetDatabase.CreateAsset(mesh, path);
        return AssetDatabase.LoadAssetAtPath<Mesh>(path);
    }

    public static void ClearMeshes()
    {
        string meshFolder = GeneratedFolder + "/Meshes";
        if (!Directory.Exists(meshFolder))
        {
            return;
        }

        string[] files = Directory.GetFiles(meshFolder, "*.asset");
        foreach (string file in files)
        {
            AssetDatabase.DeleteAsset(meshFolder + "/" + Path.GetFileName(file));
        }
    }

    static void EnsureGeneratedFolders()
    {
        EnsureFolder("Assets", "Generated");
        EnsureFolder("Assets/Generated", "Map");
        EnsureFolder(GeneratedFolder, "Meshes");
    }

    static bool ApplyImportSettings(TextureImporter importer, string name)
    {
        bool changed = false;
        bool pivotBottom = name == "wood_post" || name == "sign_post" || name == "grass_tuft";
        TextureWrapMode wrap = repeatNames.Contains(name) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }
        if (importer.spriteImportMode != SpriteImportMode.Single)
        {
            importer.spriteImportMode = SpriteImportMode.Single;
            changed = true;
        }
        if (!Mathf.Approximately(importer.spritePixelsPerUnit, MapStyle.PixelsPerUnit))
        {
            importer.spritePixelsPerUnit = MapStyle.PixelsPerUnit;
            changed = true;
        }
        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            changed = true;
        }
        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }
        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }
        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }
        if (importer.isReadable)
        {
            importer.isReadable = false;
            changed = true;
        }
        if (importer.npotScale != TextureImporterNPOTScale.None)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
            changed = true;
        }
        if (importer.maxTextureSize != 2048)
        {
            importer.maxTextureSize = 2048;
            changed = true;
        }
        if (importer.wrapMode != wrap)
        {
            importer.wrapMode = wrap;
            changed = true;
        }

        SpriteAlignment alignment = pivotBottom ? SpriteAlignment.Custom : SpriteAlignment.Center;
        Vector2 pivot = pivotBottom ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (settings.spriteMeshType != SpriteMeshType.FullRect
            || settings.spriteAlignment != (int)alignment
            || (settings.spritePivot - pivot).sqrMagnitude > 0.000001f)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)alignment;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
            changed = true;
        }

        return changed;
    }

    static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
