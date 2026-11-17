using UnityEditor;
using UnityEngine;

public class OpeningAssets : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Opening/"))
        {
            return;
        }
        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.isReadable = false;
        importer.maxTextureSize = 2048;
    }
}
