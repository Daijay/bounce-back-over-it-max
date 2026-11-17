using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[DefaultExecutionOrder(310)]
public class LightRig : MonoBehaviour
{
    const int AdditiveStyle = 1;
    const string UnlitShaderName = "Universal Render Pipeline/2D/Sprite-Unlit-Default";
    const float PitMinX = 58f;
    const float PitMaxX = 88f;
    const float PitTopY = 4f;

    public Color sunColour = new Color(1f, 0.86f, 0.5f, 1f);
    public float sunIntensity = 0.32f;
    public float sunFadeStartY = 55f;
    public float sunFadeEndY = 90f;
    public float spaceStartY = 70f;
    public float spaceEndY = 140f;
    public float spaceAmbient = 0.74f;
    public float pitAmbient = 0.8f;

    Light2D global;
    Light2D sun;
    Light2D ratLight;
    Light2D moonLight;
    readonly List<Light2D> lavaLights = new List<Light2D>();
    readonly List<Collider2D> lavaZones = new List<Collider2D>();
    readonly List<float> lavaSeed = new List<float>();
    Camera cam;
    PlayerController player;
    Transform moon;
    Transform level;
    bool converted;
    bool shadowsAdded;
    bool normalsAdded;
    float nextFind;
    float darkness;
    int[] allLayers;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindAnyObjectByType<LightRig>() == null)
        {
            new GameObject("LightRig").AddComponent<LightRig>();
        }
    }

    void OnEnable()
    {
        GraphicsQuality.Changed += ApplyQuality;
    }

    void OnDisable()
    {
        GraphicsQuality.Changed -= ApplyQuality;
    }

    int[] Layers()
    {
        if (allLayers == null)
        {
            SortingLayer[] layers = SortingLayer.layers;
            allLayers = new int[layers.Length];
            for (int i = 0; i < layers.Length; i++)
            {
                allLayers[i] = layers[i].id;
            }
        }
        return allLayers;
    }

    Light2D MakeLight(string lightName, Light2D.LightType type, Color colour, float intensity, int blendStyle, Transform parent)
    {
        GameObject go = new GameObject(lightName);
        go.transform.SetParent(parent != null ? parent : transform, false);
        Light2D light = go.AddComponent<Light2D>();
        light.lightType = type;
        light.color = colour;
        light.intensity = intensity;
        light.blendStyleIndex = blendStyle;
        light.targetSortingLayers = Layers();
        return light;
    }

    void Setup()
    {
        cam = Camera.main;
        player = PlayerController.Instance;
        if (cam == null || player == null)
        {
            return;
        }

        global = MakeLight("Global Light", Light2D.LightType.Global, Color.white, 1f, 0, null);

        sun = MakeLight("Sun Light", Light2D.LightType.Point, sunColour, sunIntensity, AdditiveStyle, null);
        sun.pointLightOuterRadius = 60f;
        sun.pointLightInnerRadius = 4f;
        sun.falloffIntensity = 0.35f;

        ratLight = MakeLight("Rat Light", Light2D.LightType.Point, new Color(1f, 0.9f, 0.7f, 1f), 0.15f, AdditiveStyle, player.transform);
        ratLight.pointLightOuterRadius = 6f;
        ratLight.pointLightInnerRadius = 0.4f;
        ratLight.falloffIntensity = 0.5f;
        ratLight.shadowIntensity = 0.7f;

        GameObject moonObject = EndingFind.ByName("CheeseMoon");
        if (moonObject != null)
        {
            moon = moonObject.transform;
            moonLight = MakeLight("Moon Light", Light2D.LightType.Point, new Color(1f, 0.95f, 0.65f, 1f), 1.1f, AdditiveStyle, moon);
            moonLight.pointLightOuterRadius = 32f;
            moonLight.pointLightInnerRadius = 5f;
            moonLight.falloffIntensity = 0.45f;
        }

        LavaZone[] zones = FindObjectsByType<LavaZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length && lavaLights.Count < 6; i++)
        {
            Collider2D c = zones[i].GetComponent<Collider2D>();
            if (c == null)
            {
                continue;
            }
            Light2D light = MakeLight("Lava Light " + lavaLights.Count, Light2D.LightType.Point, new Color(1f, 0.42f, 0.1f, 1f), 1f, AdditiveStyle, null);
            light.pointLightOuterRadius = Mathf.Clamp(c.bounds.size.x * 0.7f, 9f, 16f);
            light.pointLightInnerRadius = 1f;
            light.falloffIntensity = 0.4f;
            light.shadowIntensity = 0.65f;
            lavaLights.Add(light);
            lavaZones.Add(c);
            lavaSeed.Add(Random.value * 100f);
        }

        GameObject levelObject = EndingFind.ByName("Level");
        level = levelObject != null ? levelObject.transform : null;
        ApplyQuality();
    }

    void ApplyQuality()
    {
        bool max = GraphicsQuality.Max;
        if (sun != null)
        {
            sun.enabled = max;
        }
        if (moonLight != null)
        {
            moonLight.enabled = max;
        }
        for (int i = 0; i < lavaLights.Count; i++)
        {
            lavaLights[i].shadowsEnabled = max;
            lavaLights[i].enabled = max || i < 2;
        }
        if (ratLight != null)
        {
            ratLight.shadowsEnabled = max;
        }
        if (max && !converted)
        {
            ConvertSprites();
        }
        if (max && !shadowsAdded)
        {
            AddShadowCasters();
        }
        if (max && !normalsAdded)
        {
            AddNormalMaps();
        }
    }

    void ConvertSprites()
    {
        if (level == null)
        {
            return;
        }
        converted = true;
        Material lit = FxAssets.Lit;
        if (lit == null)
        {
            return;
        }
        SpriteRenderer[] renderers = level.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            Material m = sr.sharedMaterial;
            if (m == null || m.shader == null || m.shader.name != UnlitShaderName)
            {
                continue;
            }
            if (moon != null && sr.transform.IsChildOf(moon))
            {
                continue;
            }
            sr.sharedMaterial = lit;
        }
    }

    void AddShadowCasters()
    {
        if (level == null)
        {
            return;
        }
        shadowsAdded = true;
        SpriteRenderer[] renderers = level.GetComponentsInChildren<SpriteRenderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            SpriteRenderer sr = renderers[i];
            Vector3 p = sr.transform.position;
            if (p.x < PitMinX || p.x > PitMaxX || p.y > PitTopY + 26f || p.y < -14f)
            {
                continue;
            }
            if (sr.GetComponentInParent<BlockType>() == null)
            {
                continue;
            }
            if (sr.GetComponent<ShadowCaster2D>() != null)
            {
                continue;
            }
            ShadowCaster2D caster = sr.gameObject.AddComponent<ShadowCaster2D>();
            caster.useRendererSilhouette = true;
            caster.selfShadows = false;
        }
        if (player != null)
        {
            SpriteRenderer[] parts = player.GetComponentsInChildren<SpriteRenderer>(false);
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].GetComponent<ShadowCaster2D>() == null)
                {
                    ShadowCaster2D caster = parts[i].gameObject.AddComponent<ShadowCaster2D>();
                    caster.useRendererSilhouette = true;
                    caster.selfShadows = false;
                }
            }
        }
    }

    void AddNormalMaps()
    {
        if (player == null)
        {
            return;
        }
        normalsAdded = true;
        SpriteRenderer[] parts = player.GetComponentsInChildren<SpriteRenderer>(true);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        for (int i = 0; i < parts.Length; i++)
        {
            SpriteRenderer sr = parts[i];
            if (sr.sprite == null || sr.sprite.texture == null)
            {
                continue;
            }
            Material m = sr.sharedMaterial;
            bool unlit = m != null && m.shader != null && m.shader.name == UnlitShaderName;
            if (unlit)
            {
                continue;
            }
            Texture2D normal = NormalMaps.For(sr.sprite.texture);
            if (normal == null)
            {
                continue;
            }
            sr.GetPropertyBlock(block);
            block.SetTexture("_NormalMap", normal);
            sr.SetPropertyBlock(block);
        }
    }

    void Update()
    {
        if (global == null)
        {
            if (Time.unscaledTime >= nextFind)
            {
                nextFind = Time.unscaledTime + 1f;
                Setup();
            }
            return;
        }
        if (cam == null)
        {
            cam = Camera.main;
            return;
        }

        Vector3 c = cam.transform.position;
        float space = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(spaceStartY, spaceEndY, c.y));
        float pit = (c.x > PitMinX && c.x < PitMaxX && c.y < PitTopY) ? 1f : 0f;
        float ambientTarget = Mathf.Min(Mathf.Lerp(1f, spaceAmbient, space), Mathf.Lerp(1f, pitAmbient, pit));
        darkness = Mathf.MoveTowards(darkness, Mathf.Max(space, pit * 0.7f), Time.unscaledDeltaTime * 1.5f);

        global.intensity = Mathf.Lerp(global.intensity, ambientTarget, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
        global.color = Color.Lerp(Color.white, new Color(0.82f, 0.88f, 1f, 1f), space);

        if (sun != null && sun.enabled)
        {
            float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(sunFadeStartY, sunFadeEndY, c.y));
            sun.intensity = sunIntensity * fade;
            sun.transform.position = new Vector3(c.x - 16f, c.y + 14f, 0f);
        }

        float eruption = EruptionEffects.Active ? EruptionEffects.Intensity : 0f;
        for (int i = 0; i < lavaLights.Count; i++)
        {
            Light2D light = lavaLights[i];
            if (light == null || !light.enabled || lavaZones[i] == null)
            {
                continue;
            }
            Bounds b = lavaZones[i].bounds;
            light.transform.position = new Vector3(b.center.x, b.max.y + 0.6f, 0f);
            float flicker = 0.85f + 0.15f * Mathf.Sin(Time.time * 7f + lavaSeed[i]) + 0.08f * Mathf.Sin(Time.time * 17.3f + lavaSeed[i] * 2f);
            bool near = Mathf.Abs(c.x - b.center.x) < 60f && Mathf.Abs(c.y - b.center.y) < 45f;
            float erupting = Mathf.Abs(b.center.x - EruptionEffects.Mouth.x) < 25f ? eruption : 0f;
            light.intensity = near ? flicker * (1f + 2.4f * erupting) : 0f;
        }

        if (ratLight != null)
        {
            ratLight.intensity = Mathf.Lerp(0.12f, 0.95f, darkness);
            ratLight.pointLightOuterRadius = Mathf.Lerp(5f, 8f, darkness);
        }

        if (moonLight != null && moonLight.enabled)
        {
            float distance = Mathf.Abs(c.y - moon.position.y);
            float reach = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(70f, 130f, distance));
            moonLight.intensity = 1.1f * reach * (0.92f + 0.08f * Mathf.Sin(Time.time * 1.7f));
        }
    }
}
