using UnityEngine;

[DefaultExecutionOrder(320)]
public class FxBursts : MonoBehaviour
{
    const float LavaX = 73f;
    const float LavaY = 0f;

    static FxBursts instance;

    ParticleSystem dust;
    ParticleSystem sparks;
    ParticleSystem embers;
    ParticleSystem snow;
    Camera cam;
    Bounds snowBounds;
    bool hasSnow;
    float nextFind;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Create()
    {
        if (FindAnyObjectByType<FxBursts>() == null)
        {
            new GameObject("FxBursts").AddComponent<FxBursts>();
        }
    }

    void OnEnable()
    {
        instance = this;
        if (dust == null)
        {
            dust = Make("Landing Dust", FxAssets.Smoke, FxAssets.ParticleAlpha, 11, 400);
            sparks = Make("Hammer Sparks", FxAssets.Spark, FxAssets.ParticleAdditive, 12, 400);
            embers = Make("Lava Embers", FxAssets.Disc, FxAssets.ParticleAdditive, 9, 600);
            snow = Make("Snow", FxAssets.Disc, FxAssets.ParticleAlpha, 19, 800);
            FadeOut(dust, -0.15f, 1.6f);
            FadeOut(sparks, 1.6f, 0.2f);
            ConfigureAmbient(embers, 3.5f, 0.05f, 0.12f);
            ConfigureAmbient(snow, 7f, 0.06f, 0.14f);
        }
    }

    void OnDisable()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    ParticleSystem Make(string objectName, Sprite sprite, Material material, int order, int max)
    {
        GameObject go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = max;
        main.startLifetime = 1f;
        main.startSpeed = 0f;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = false;
        ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = order;
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (material != null)
        {
            Material copy = new Material(material);
            copy.hideFlags = HideFlags.DontSave;
            copy.mainTexture = sprite.texture;
            renderer.sharedMaterial = copy;
        }
        ps.Play();
        return ps;
    }

    static void FadeOut(ParticleSystem ps, float gravity, float growth)
    {
        ParticleSystem.MainModule main = ps.main;
        main.gravityModifier = gravity;
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, growth));
    }

    void ConfigureAmbient(ParticleSystem ps, float life, float sizeMin, float sizeMax)
    {
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.7f, life);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
        ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.4f;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
    }

    void FindSnow()
    {
        SnowSurface[] surfaces = FindObjectsByType<SnowSurface>(FindObjectsSortMode.None);
        for (int i = 0; i < surfaces.Length; i++)
        {
            Renderer r = surfaces[i].GetComponent<Renderer>();
            if (r == null)
            {
                continue;
            }
            if (!hasSnow)
            {
                snowBounds = r.bounds;
                hasSnow = true;
            }
            else
            {
                snowBounds.Encapsulate(r.bounds);
            }
        }
    }

    void Update()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null)
            {
                return;
            }
        }
        if (!hasSnow && Time.unscaledTime >= nextFind)
        {
            nextFind = Time.unscaledTime + 3f;
            FindSnow();
        }
        float scale = GraphicsQuality.ParticleScale;
        Vector3 c = cam.transform.position;
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;

        bool nearLava = Mathf.Abs(c.x - LavaX) < 40f && c.y < 30f && c.y > -20f;
        SetAmbient(embers, nearLava, 22f * scale, new Vector3(LavaX, LavaY + 0.3f, 0f), new Vector3(15f, 0.2f, 1f), new Vector2(0f, 1.6f));

        bool inSnow = hasSnow && c.y > snowBounds.min.y - 12f && c.y < snowBounds.max.y + 18f && c.x > snowBounds.min.x - 25f && c.x < snowBounds.max.x + 25f;
        SetAmbient(snow, inSnow, 45f * scale, new Vector3(c.x, c.y + halfH + 1f, 0f), new Vector3(halfW * 2.2f, 0.5f, 1f), new Vector2(0.4f, -1.3f));
    }

    static void SetAmbient(ParticleSystem ps, bool on, float rate, Vector3 position, Vector3 size, Vector2 velocity)
    {
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = on;
        emission.rateOverTime = rate;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.position = position - ps.transform.position;
        shape.scale = size;
        ParticleSystem.VelocityOverLifetimeModule v = ps.velocityOverLifetime;
        v.enabled = true;
        v.space = ParticleSystemSimulationSpace.World;
        v.x = velocity.x;
        v.y = velocity.y;
        v.z = 0f;
    }

    public static void Dust(Vector2 point, Vector2 normal, float speed, BlockKind kind)
    {
        if (instance == null || speed < 4f)
        {
            return;
        }
        Color tint = new Color(0.78f, 0.72f, 0.64f, 0.7f);
        if (kind == BlockKind.Blue)
        {
            tint = new Color(0.85f, 0.95f, 1f, 0.8f);
        }
        else if (kind == BlockKind.Green)
        {
            tint = new Color(0.7f, 0.95f, 0.6f, 0.6f);
        }
        else if (kind == BlockKind.Purple)
        {
            tint = new Color(0.75f, 0.55f, 0.9f, 0.6f);
        }
        int count = Mathf.RoundToInt(Mathf.Lerp(4f, 16f, Mathf.InverseLerp(4f, 30f, speed)) * GraphicsQuality.ParticleScale);
        Vector2 tangent = new Vector2(-normal.y, normal.x);
        ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            float side = Random.Range(-1f, 1f);
            Vector2 v = tangent * side * Random.Range(1.5f, 4f) + normal * Random.Range(0.3f, 1.8f);
            p.position = new Vector3(point.x + tangent.x * side * 0.3f, point.y + tangent.y * side * 0.3f, 0f);
            p.velocity = new Vector3(v.x, v.y, 0f);
            p.startLifetime = Random.Range(0.35f, 0.75f);
            p.startSize = Random.Range(0.25f, 0.55f);
            p.startColor = tint;
            p.rotation = Random.Range(0f, 360f);
            instance.dust.Emit(p, 1);
        }
    }

    public static void Sparks(Vector2 point, Vector2 normal, float speed)
    {
        if (instance == null || speed < 3f)
        {
            return;
        }
        int count = Mathf.RoundToInt(Mathf.Lerp(4f, 14f, Mathf.InverseLerp(3f, 25f, speed)) * GraphicsQuality.ParticleScale);
        ParticleSystem.EmitParams p = new ParticleSystem.EmitParams();
        for (int i = 0; i < count; i++)
        {
            Vector2 dir = (normal + Random.insideUnitCircle * 0.9f).normalized;
            Vector2 v = dir * Random.Range(3f, 9f);
            p.position = new Vector3(point.x, point.y, 0f);
            p.velocity = new Vector3(v.x, v.y, 0f);
            p.startLifetime = Random.Range(0.2f, 0.45f);
            p.startSize = Random.Range(0.12f, 0.28f);
            p.startColor = Color.Lerp(new Color(1f, 0.95f, 0.6f, 1f), new Color(1f, 0.55f, 0.15f, 1f), Random.value);
            instance.sparks.Emit(p, 1);
        }
    }
}
