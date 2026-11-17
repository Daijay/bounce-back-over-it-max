using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(330)]
public class ShaderDecor : MonoBehaviour
{
    const int IceLimit = 300;

    readonly List<GameObject> overlays = new List<GameObject>();
    readonly List<MeshFilter> sparkleFilters = new List<MeshFilter>();
    readonly List<MeshFilter> sparkleSources = new List<MeshFilter>();
    Transform haze;
    SpriteRenderer hazeRenderer;
    Camera cam;
    ChargeAura aura;
    bool built;
    float nextFind;
    float nextSync;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindAnyObjectByType<ShaderDecor>() == null)
        {
            new GameObject("ShaderDecor").AddComponent<ShaderDecor>();
        }
    }

    void OnEnable()
    {
        GraphicsQuality.Changed += Refresh;
    }

    void OnDisable()
    {
        GraphicsQuality.Changed -= Refresh;
    }

    void Refresh()
    {
        bool max = GraphicsQuality.Max;
        for (int i = 0; i < overlays.Count; i++)
        {
            if (overlays[i] != null)
            {
                overlays[i].SetActive(max);
            }
        }
        if (aura != null)
        {
            aura.UseShader(max);
        }
    }

    GameObject Track(GameObject go)
    {
        overlays.Add(go);
        return go;
    }

    SpriteRenderer MakeQuad(string objectName, Transform parent, Material material, int order, Color colour)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(parent, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = FxAssets.Ember;
        sr.sharedMaterial = material;
        sr.sortingOrder = order;
        sr.color = colour;
        Track(go);
        return sr;
    }

    void Build()
    {
        built = true;
        BuildLava();
        BuildSnow();
        BuildIce();
        BuildHaze();
        aura = FindAnyObjectByType<ChargeAura>();
        Refresh();
    }

    void BuildLava()
    {
        Material m = FxAssets.Custom("BBHeatShimmer");
        if (m == null)
        {
            return;
        }
        LavaZone[] zones = FindObjectsByType<LavaZone>(FindObjectsSortMode.None);
        for (int i = 0; i < zones.Length; i++)
        {
            Collider2D c = zones[i].GetComponent<Collider2D>();
            if (c == null)
            {
                continue;
            }
            Bounds b = c.bounds;
            float height = 4.5f;
            SpriteRenderer sr = MakeQuad("Heat Shimmer", null, m, 8, Color.white);
            float native = sr.sprite.bounds.size.x;
            sr.transform.position = new Vector3(b.center.x, b.max.y + height * 0.5f, 0f);
            sr.transform.localScale = new Vector3(b.size.x / native, height / native, 1f);
        }
    }

    void BuildSnow()
    {
        Material m = FxAssets.Custom("BBSparkle");
        if (m == null)
        {
            return;
        }
        SnowSurface[] surfaces = FindObjectsByType<SnowSurface>(FindObjectsSortMode.None);
        for (int i = 0; i < surfaces.Length; i++)
        {
            MeshFilter source = surfaces[i].filter != null ? surfaces[i].filter : surfaces[i].GetComponent<MeshFilter>();
            MeshRenderer sourceRenderer = surfaces[i].GetComponent<MeshRenderer>();
            if (source == null || source.sharedMesh == null)
            {
                continue;
            }
            GameObject go = Track(new GameObject("Snow Sparkle"));
            go.transform.SetParent(source.transform, false);
            MeshFilter filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = source.sharedMesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = m;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.sortingOrder = (sourceRenderer != null ? sourceRenderer.sortingOrder : MapStyle.OrderSnow) + 1;
            sparkleFilters.Add(filter);
            sparkleSources.Add(source);
        }
    }

    void BuildIce()
    {
        Material m = FxAssets.Custom("BBIceShine");
        GameObject levelObject = EndingFind.ByName("Level");
        if (m == null || levelObject == null)
        {
            return;
        }
        BlockType[] blocks = levelObject.GetComponentsInChildren<BlockType>(false);
        int made = 0;
        for (int i = 0; i < blocks.Length && made < IceLimit; i++)
        {
            if (blocks[i].kind != BlockKind.Blue)
            {
                continue;
            }
            SpriteRenderer source = blocks[i].GetComponent<SpriteRenderer>();
            if (source == null)
            {
                source = blocks[i].GetComponentInChildren<SpriteRenderer>();
            }
            if (source == null || source.sprite == null)
            {
                continue;
            }
            GameObject go = Track(new GameObject("Ice Shine"));
            go.transform.SetParent(source.transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.flipX = source.flipX;
            sr.flipY = source.flipY;
            sr.drawMode = source.drawMode;
            if (source.drawMode != SpriteDrawMode.Simple)
            {
                sr.size = source.size;
            }
            sr.sharedMaterial = m;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder + 1;
            made++;
        }
    }

    void BuildHaze()
    {
        Material m = FxAssets.Custom("BBHaze");
        if (m == null)
        {
            return;
        }
        int order = -60;
        BackgroundParallax parallax = FindAnyObjectByType<BackgroundParallax>();
        if (parallax != null && parallax.layers != null)
        {
            for (int i = 0; i < parallax.layers.Length; i++)
            {
                BackgroundParallax.Layer layer = parallax.layers[i];
                if (layer != null && layer.name != null && layer.name.ToLower().Contains("back"))
                {
                    order = layer.sortingOrder + 1;
                    break;
                }
            }
        }
        hazeRenderer = MakeQuad("Parallax Haze", null, m, order, Color.white);
        haze = hazeRenderer.transform;
    }

    void LateUpdate()
    {
        if (!built)
        {
            if (Time.unscaledTime >= nextFind && PlayerController.Instance != null)
            {
                nextFind = Time.unscaledTime + 1f;
                if (EndingFind.ByName("Level") != null)
                {
                    Build();
                }
            }
            return;
        }
        if (cam == null)
        {
            cam = Camera.main;
        }
        if (haze != null && cam != null && hazeRenderer != null)
        {
            float halfH = cam.orthographicSize;
            float halfW = halfH * cam.aspect;
            float native = hazeRenderer.sprite.bounds.size.x;
            Vector3 c = cam.transform.position;
            haze.position = new Vector3(c.x, c.y, 0f);
            haze.localScale = new Vector3((halfW * 2f + 2f) / native, (halfH * 2f + 2f) / native, 1f);
        }
        if (Time.unscaledTime >= nextSync)
        {
            nextSync = Time.unscaledTime + 0.5f;
            for (int i = 0; i < sparkleFilters.Count; i++)
            {
                if (sparkleFilters[i] != null && sparkleSources[i] != null && sparkleFilters[i].sharedMesh != sparkleSources[i].sharedMesh)
                {
                    sparkleFilters[i].sharedMesh = sparkleSources[i].sharedMesh;
                }
            }
        }
    }
}
