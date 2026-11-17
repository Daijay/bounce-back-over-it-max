using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class GraphicsQuality
{
    const string Key = "BounceBack.QualityMax";

    public static event System.Action Changed;

    static bool max = true;
    static bool loaded;

    public static bool Max
    {
        get
        {
            Load();
            return max;
        }
    }

    public static float ParticleScale
    {
        get { return Max ? 1f : 0.5f; }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot()
    {
        Load();
        Apply();
        Application.quitting -= RestoreAsset;
        Application.quitting += RestoreAsset;
    }

    static void Load()
    {
        if (loaded)
        {
            return;
        }
        loaded = true;
        max = PlayerPrefs.GetInt(Key, 1) != 0;
    }

    public static void Set(bool maxQuality)
    {
        Load();
        if (max == maxQuality)
        {
            return;
        }
        max = maxQuality;
        PlayerPrefs.SetInt(Key, max ? 1 : 0);
        PlayerPrefs.Save();
        Apply();
        if (Changed != null)
        {
            Changed();
        }
    }

    public static void ApplyFrameRate()
    {
        int refresh = Mathf.RoundToInt((float)Screen.currentResolution.refreshRateRatio.value);
        QualitySettings.vSyncCount = Mathf.Clamp(Mathf.RoundToInt(refresh / 60f), 1, 4);
        Application.targetFrameRate = GameSettings.TargetFrameRate;
    }

    public static void Apply()
    {
        Load();
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.shadows = max ? UnityEngine.ShadowQuality.All : UnityEngine.ShadowQuality.HardOnly;
        QualitySettings.shadowResolution = max ? UnityEngine.ShadowResolution.VeryHigh : UnityEngine.ShadowResolution.Medium;
        QualitySettings.antiAliasing = max ? 8 : 2;
        ApplyFrameRate();

        UniversalRenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (asset != null)
        {
            asset.supportsHDR = true;
            asset.msaaSampleCount = max ? 8 : 2;
            asset.renderScale = 1f;
        }
    }

    static void RestoreAsset()
    {
        UniversalRenderPipelineAsset asset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (asset != null)
        {
            asset.supportsHDR = true;
            asset.msaaSampleCount = 8;
        }
    }
}
