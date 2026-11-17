using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(300)]
public class PostFx : MonoBehaviour
{
    Camera cam;
    Volume volume;
    VolumeProfile profile;
    Bloom bloom;
    Vignette vignette;
    ColorAdjustments grade;
    FilmGrain grain;
    ChromaticAberration chroma;
    float nextFind;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindAnyObjectByType<PostFx>() == null)
        {
            new GameObject("PostFx").AddComponent<PostFx>();
        }
    }

    void OnEnable()
    {
        GraphicsQuality.Changed += Refresh;
        Build();
    }

    void OnDisable()
    {
        GraphicsQuality.Changed -= Refresh;
    }

    void OnDestroy()
    {
        if (profile != null)
        {
            Destroy(profile);
        }
    }

    void Build()
    {
        cam = Camera.main;
        if (cam == null)
        {
            return;
        }
        cam.allowHDR = true;
        cam.allowMSAA = true;
        UniversalAdditionalCameraData data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.None;

        if (volume == null)
        {
            GameObject go = new GameObject("Post Volume");
            go.transform.SetParent(transform, false);
            volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 10f;
            profile = ScriptableObject.CreateInstance<VolumeProfile>();
            profile.hideFlags = HideFlags.DontSave;
            volume.sharedProfile = profile;

            bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1f);
            bloom.scatter.Override(0.6f);
            bloom.tint.Override(new Color(1f, 0.95f, 0.88f, 1f));

            vignette = profile.Add<Vignette>(true);
            vignette.color.Override(new Color(0.04f, 0.02f, 0.03f, 1f));
            vignette.smoothness.Override(0.55f);
            vignette.rounded.Override(false);

            grade = profile.Add<ColorAdjustments>(true);
            grade.postExposure.Override(0.05f);
            grade.contrast.Override(8f);
            grade.saturation.Override(8f);
            grade.colorFilter.Override(new Color(1f, 0.985f, 0.95f, 1f));

            grain = profile.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Thin1);
            grain.response.Override(0.85f);

            chroma = profile.Add<ChromaticAberration>(true);
            chroma.intensity.Override(0f);
        }
        Refresh();
    }

    void Refresh()
    {
        if (volume == null)
        {
            return;
        }
        bool max = GraphicsQuality.Max;
        bloom.highQualityFiltering.Override(max);
        bloom.intensity.Override(max ? 0.55f : 0.3f);
        vignette.intensity.Override(max ? 0.24f : 0.18f);
        grain.intensity.Override(max ? 0.06f : 0f);
        grain.active = max;
        chroma.active = max;
    }

    void Update()
    {
        if (volume == null || cam == null)
        {
            if (Time.unscaledTime >= nextFind)
            {
                nextFind = Time.unscaledTime + 1f;
                Build();
            }
            return;
        }
        float eruption = EruptionEffects.Active ? EruptionEffects.Intensity : 0f;
        chroma.intensity.Override(GraphicsQuality.Max ? Mathf.Clamp01(eruption) * 0.6f : 0f);
        float baseBloom = GraphicsQuality.Max ? 0.55f : 0.3f;
        bloom.intensity.Override(baseBloom + eruption * 0.6f);
        float baseVignette = GraphicsQuality.Max ? 0.24f : 0.18f;
        vignette.intensity.Override(baseVignette + eruption * 0.12f);
    }
}
