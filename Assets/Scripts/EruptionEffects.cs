using UnityEngine;

public class EruptionEffects : MonoBehaviour
{
    Sprite ember;
    Sprite disc;
    Sprite ring;
    Sprite smoke;
    Sprite halo;
    Material material;
    Material alphaMaterial;
    public float eruptionDuration = 7f;
    public float fadeOut = 1.5f;
    public float mouthX = 73f;
    public float mouthY = 0f;
    public float mouthHalfWidth = 8f;
    public float columnHeight = 190f;
    public int poolSize = 600;
    public float lavaLeft = 64.5f;
    public float lavaRight = 81.5f;
    public float lavaCentreY = -3.5f;
    public float lavaHeight = 7f;

    const string PartName = "Eruption Part";
    const float PixelWorld = 0.0301f;
    const int Blob = 0;
    const int Chunk = 1;
    const int Smoke = 2;
    const int Ember = 3;
    const int Boil = 4;
    const int ColumnDiscs = 14;

    static readonly float[] Gravity = { 24f, 22f, -2f, 0f, 14f };
    static readonly int[] Orders = { 8, 8, 5, 9, 8 };

    SpriteRenderer[] sprites;
    Vector2[] position;
    Vector2[] velocity;
    float[] age;
    float[] life;
    float[] size0;
    float[] size1;
    float[] alpha0;
    float[] streak;
    float[] spin;
    float[] nativeSize;
    Color[] colour0;
    Color[] colour1;
    byte[] kind;
    int next;
    float startTime;
    bool playing;
    float blobCarry;
    float chunkCarry;
    float smokeCarry;
    float emberCarry;
    float boilCarry;
    bool burstOne;
    bool burstTwo;

    SpriteRenderer flashWhite;
    SpriteRenderer flashYellow;
    SpriteRenderer flashOrange;
    SpriteRenderer fireball;
    SpriteRenderer core;
    SpriteRenderer fireHalo;
    SpriteRenderer skyTint;
    SpriteRenderer lavaGlow;
    SpriteRenderer coreStreak;
    SpriteRenderer[] columnDiscs;
    SpriteRenderer[] rings;
    float[] ringStart;
    Camera cam;

    public static float Intensity { get; private set; }
    public static bool Active { get; private set; }
    public static float Elapsed { get; private set; }
    public static Vector2 Mouth { get; private set; }

    static EruptionEffects instance;

    public static void Play()
    {
        if (instance == null)
        {
            GameObject go = new GameObject("EruptionEffects");
            go.SetActive(false);
            instance = go.AddComponent<EruptionEffects>();
            go.transform.position = new Vector3(instance.mouthX, instance.mouthY, 0f);
        }
        instance.gameObject.SetActive(false);
        instance.gameObject.SetActive(true);
    }

    public static void Stop()
    {
        if (instance != null)
        {
            instance.gameObject.SetActive(false);
        }
        Active = false;
        Intensity = 0f;
    }

    void OnEnable()
    {
        RemoveParts();
        if (!Application.isPlaying)
        {
            return;
        }
        ember = FxAssets.Ember;
        disc = FxAssets.Disc;
        ring = FxAssets.Ring;
        smoke = FxAssets.Smoke;
        halo = FxAssets.Halo;
        material = FxAssets.Glow;
        alphaMaterial = FxAssets.Alpha;
        Mouth = new Vector2(mouthX, mouthY);
        Active = true;
        Intensity = 1f;
        Elapsed = 0f;
        cam = Camera.main;
        int count = Mathf.Max(32, poolSize);
        sprites = new SpriteRenderer[count];
        position = new Vector2[count];
        velocity = new Vector2[count];
        age = new float[count];
        life = new float[count];
        size0 = new float[count];
        size1 = new float[count];
        alpha0 = new float[count];
        streak = new float[count];
        spin = new float[count];
        nativeSize = new float[count];
        colour0 = new Color[count];
        colour1 = new Color[count];
        kind = new byte[count];
        for (int i = 0; i < count; i++)
        {
            sprites[i] = Make(ember, 8);
        }
        flashWhite = Make(ember, 9);
        flashYellow = Make(ember, 9);
        flashOrange = Make(ember, 9);
        fireball = Make(disc, 7);
        core = Make(disc, 7);
        fireHalo = Make(halo, 6);
        skyTint = Make(halo, -58);
        lavaGlow = Make(ember, 9);
        coreStreak = Make(ember, 6);
        columnDiscs = new SpriteRenderer[ColumnDiscs];
        for (int i = 0; i < ColumnDiscs; i++)
        {
            columnDiscs[i] = Make(disc, 6);
        }
        rings = new SpriteRenderer[3];
        ringStart = new float[] { 0f, 0.4f, 0.9f };
        for (int i = 0; i < rings.Length; i++)
        {
            rings[i] = Make(ring, 9);
        }

        next = 0;
        blobCarry = 0f;
        chunkCarry = 0f;
        smokeCarry = 0f;
        emberCarry = 0f;
        boilCarry = 0f;
        burstOne = false;
        burstTwo = false;
        startTime = Time.time;
        playing = true;
        HideAll();
    }

    void OnDisable()
    {
        playing = false;
        Active = false;
        Intensity = 0f;
        RemoveParts();
    }

    void RemoveParts()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == PartName)
            {
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        sprites = null;
        columnDiscs = null;
        rings = null;
        flashWhite = null;
        flashYellow = null;
        flashOrange = null;
        fireball = null;
        core = null;
        fireHalo = null;
        skyTint = null;
        lavaGlow = null;
        coreStreak = null;
    }

    SpriteRenderer Make(Sprite sprite, int order)
    {
        GameObject go = new GameObject(PartName);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (material != null)
        {
            sr.sharedMaterial = material;
        }
        sr.enabled = false;
        return sr;
    }

    void HideAll()
    {
        for (int i = 0; sprites != null && i < sprites.Length; i++)
        {
            sprites[i].enabled = false;
            life[i] = 0f;
        }
        SpriteRenderer[] fixedParts = { flashWhite, flashYellow, flashOrange, fireball, core, fireHalo, skyTint, lavaGlow, coreStreak };
        for (int i = 0; i < fixedParts.Length; i++)
        {
            if (fixedParts[i] != null)
            {
                fixedParts[i].enabled = false;
            }
        }
        for (int i = 0; columnDiscs != null && i < columnDiscs.Length; i++)
        {
            columnDiscs[i].enabled = false;
        }
        for (int i = 0; rings != null && i < rings.Length; i++)
        {
            rings[i].enabled = false;
        }
    }

    void Update()
    {
        if (!playing || sprites == null)
        {
            return;
        }
        float t = Time.time - startTime;
        Elapsed = t;
        if (t >= eruptionDuration)
        {
            playing = false;
            Active = false;
            Intensity = 0f;
            HideAll();
            return;
        }
        float dt = Time.deltaTime;
        float master = Mathf.Clamp01((eruptionDuration - t) / Mathf.Max(0.05f, fadeOut));
        Intensity = master;
        if (cam == null)
        {
            cam = Camera.main;
        }

        Spawning(t, dt);
        UpdateParticles(dt, master);
        UpdateBig(t, master);
    }

    void Spawning(float t, float dt)
    {
        if (!burstOne && t >= 0f)
        {
            burstOne = true;
            for (int i = 0; i < 110; i++)
            {
                SpawnBlob(70f, 1.6f, 1.4f);
            }
        }
        if (!burstTwo && t >= 0.35f)
        {
            burstTwo = true;
            for (int i = 0; i < 50; i++)
            {
                SpawnBlob(60f, 2.2f, 1.2f);
            }
        }

        if (t < 5f)
        {
            blobCarry += 55f * dt;
            while (blobCarry >= 1f)
            {
                blobCarry -= 1f;
                SpawnBlob(56f, 1.0f, 1.25f);
            }
        }
        if (t < 5.4f)
        {
            chunkCarry += 10f * dt;
            while (chunkCarry >= 1f)
            {
                chunkCarry -= 1f;
                float angle = (Random.value - 0.5f) * 2f * 1.1f;
                float speed = 12f + 28f * Random.value;
                Color c1 = new Color(0.22f, 0.04f, 0.03f, 1f);
                Spawn(Chunk, ember, new Vector2(mouthX + (Random.value - 0.5f) * mouthHalfWidth, mouthY + 1f), new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * speed,
                    5f, 0.7f + 1.0f * Random.value, 0.5f, new Color(1f, 0.5f, 0.1f, 1f), c1, 1f, 0f, (Random.value - 0.5f) * 540f);
            }
        }
        if (t < 5.8f)
        {
            smokeCarry += 18f * dt;
            while (smokeCarry >= 1f)
            {
                smokeCarry -= 1f;
                Spawn(Smoke, smoke, new Vector2(mouthX + (Random.value - 0.5f) * mouthHalfWidth * 1.4f, mouthY + 2f), new Vector2((Random.value - 0.5f) * 8f, 8f + 14f * Random.value),
                    5.2f, 5f + 3f * Random.value, 20f + 8f * Random.value, new Color(1f, 0.45f, 0.15f, 1f), new Color(0.16f, 0.14f, 0.15f, 1f), 0.65f, 0f, (Random.value - 0.5f) * 20f);
            }
        }
        if (t < 6.5f && cam != null)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            float unit = halfHeight / 8f;
            emberCarry += 22f * dt;
            while (emberCarry >= 1f)
            {
                emberCarry -= 1f;
                Vector3 c = cam.transform.position;
                Spawn(Ember, ember, new Vector2(c.x + (Random.value - 0.5f) * 2.2f * halfWidth, c.y + halfHeight + 1f),
                    new Vector2((Random.value - 0.5f) * 2.4f * unit, -(3f + 4f * Random.value) * unit), 5.5f, (0.12f + 0.1f * Random.value) * unit * 8f / 8f,
                    0.08f * unit, new Color(1f, 0.65f, 0.2f, 1f), new Color(0.6f, 0.1f, 0.04f, 1f), 1f, 0f, 0f);
            }
        }
        if (t < 6f)
        {
            boilCarry += 40f * dt;
            while (boilCarry >= 1f)
            {
                boilCarry -= 1f;
                Spawn(Boil, disc, new Vector2(Mathf.Lerp(lavaLeft + 1f, lavaRight - 1f, Random.value), mouthY + Random.value * 0.4f),
                    new Vector2((Random.value - 0.5f) * 4f, 4f + 8f * Random.value), 0.9f, 0.5f + 0.7f * Random.value, 0.2f,
                    new Color(1f, 0.95f, 0.7f, 1f), new Color(1f, 0.5f, 0.1f, 1f), 1f, 0f, 0f);
            }
        }
    }

    void SpawnBlob(float maxSpeed, float sizeScale, float spread)
    {
        float angle = (Random.value - 0.5f) * 2f * spread;
        float speed = 14f + (maxSpeed - 14f) * Mathf.Pow(Random.value, 0.7f);
        Vector2 start = new Vector2(mouthX + (Random.value - 0.5f) * mouthHalfWidth * 1.6f, mouthY + Random.value * 1.5f);
        Spawn(Blob, disc, start, new Vector2(Mathf.Sin(angle), Mathf.Cos(angle)) * speed, 3.8f + 1.2f * Random.value, (0.7f + 1.2f * Random.value) * sizeScale, 0.35f,
            new Color(1f, 0.93f, 0.55f, 1f), new Color(0.9f, 0.18f, 0.04f, 1f), 1f, 0.045f, 0f);
    }

    void Spawn(int type, Sprite sprite, Vector2 start, Vector2 speed, float lifetime, float startSize, float endSize, Color from, Color to,
        float alpha, float streakLength, float spinSpeed)
    {
        int i = next;
        next = (next + 1) % sprites.Length;
        kind[i] = (byte)type;
        position[i] = start;
        velocity[i] = speed;
        age[i] = 0f;
        life[i] = lifetime;
        size0[i] = startSize;
        size1[i] = endSize;
        alpha0[i] = alpha;
        streak[i] = streakLength;
        spin[i] = spinSpeed;
        colour0[i] = from;
        colour1[i] = to;
        SpriteRenderer sr = sprites[i];
        sr.sprite = sprite;
        nativeSize[i] = sprite != null ? sprite.bounds.size.x : PixelWorld;
        sr.sortingOrder = Orders[type];
        Material wanted = type == Smoke || type == Chunk ? alphaMaterial : material;
        if (wanted != null && sr.sharedMaterial != wanted)
        {
            sr.sharedMaterial = wanted;
        }
        sr.enabled = true;
    }

    void UpdateParticles(float dt, float master)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            if (life[i] <= 0f)
            {
                continue;
            }
            age[i] += dt;
            if (age[i] >= life[i])
            {
                life[i] = 0f;
                sprites[i].enabled = false;
                continue;
            }
            float k = age[i] / life[i];
            velocity[i].y -= Gravity[kind[i]] * dt;
            position[i] += velocity[i] * dt;
            SpriteRenderer sr = sprites[i];
            sr.transform.position = new Vector3(position[i].x, position[i].y, 0f);

            float size = Mathf.Lerp(size0[i], size1[i], k);
            float length = size;
            if (streak[i] > 0f)
            {
                length = size * (1f + velocity[i].magnitude * streak[i]);
                sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(velocity[i].y, velocity[i].x) * Mathf.Rad2Deg);
            }
            else if (spin[i] != 0f)
            {
                sr.transform.rotation = Quaternion.Euler(0f, 0f, spin[i] * age[i]);
            }
            sr.transform.localScale = new Vector3(length / nativeSize[i], size / nativeSize[i], 1f);

            float fade = Mathf.Min(1f, (life[i] - age[i]) / (kind[i] == Smoke ? 1.2f : 0.5f));
            if (kind[i] == Smoke)
            {
                fade *= Mathf.Min(1f, age[i] / 0.5f);
            }
            Color c = Color.Lerp(colour0[i], colour1[i], k);
            sr.color = new Color(c.r, c.g, c.b, alpha0[i] * fade * master);
        }
    }

    void UpdateBig(float t, float master)
    {
        float pulse = 0.5f + 0.5f * Mathf.Sin(t * 15f);
        float slow = 0.5f + 0.5f * Mathf.Sin(t * 4.3f);

        if (cam != null)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            Vector3 c = cam.transform.position;
            Flash(flashWhite, t, 0f, 0.45f, new Color(1f, 1f, 0.92f, 0.95f), c, halfWidth, halfHeight, master);
            Flash(flashYellow, t, 0.45f, 0.55f, new Color(1f, 0.85f, 0.3f, 0.55f), c, halfWidth, halfHeight, master);
            Flash(flashOrange, t, 1.3f, 0.7f, new Color(1f, 0.5f, 0.12f, 0.4f), c, halfWidth, halfHeight, master);
        }

        float grow = Mathf.Clamp01(t / 0.7f);
        float ease = 1f - (1f - grow) * (1f - grow);
        float radius = (1f + 19f * ease) + Mathf.Clamp01((t - 0.7f) / 1.5f) * 7f;
        float heat = Mathf.Clamp01(t / 3f);
        Color fire = Color.Lerp(new Color(1f, 0.97f, 0.8f, 1f), new Color(1f, 0.35f, 0.08f, 1f), heat);
        float fireAlpha = Mathf.Clamp01(1f - (t - 1.5f) / 2.5f) * 0.95f + 0.0f;
        Place(fireball, new Vector2(mouthX, mouthY + 4f + radius * 0.35f), radius * 2f, new Color(fire.r, fire.g, fire.b, Mathf.Max(0f, fireAlpha) * master));

        float coreSize = 11f + 5f * pulse + 2f * slow;
        Place(core, new Vector2(mouthX, mouthY + 3f), coreSize, new Color(1f, 0.96f + 0.04f * pulse, 0.75f, (0.85f + 0.15f * pulse) * master));
        Place(fireHalo, new Vector2(mouthX, mouthY + 6f), 90f + 20f * slow, new Color(1f, 0.6f, 0.2f, 0.9f * master));

        float tintIn = Mathf.Clamp01(t / 0.8f);
        Place(skyTint, new Vector2(mouthX, mouthY + 30f), 230f + 30f * slow, new Color(1f, 0.28f, 0.08f, 0.5f * tintIn * (0.8f + 0.2f * pulse) * master));

        float top = columnHeight * Mathf.Clamp01(t / 0.9f);
        float spacing = columnHeight / ColumnDiscs;
        for (int i = 0; i < ColumnDiscs; i++)
        {
            SpriteRenderer sr = columnDiscs[i];
            float y = mouthY + i * spacing * 0.9f;
            if (y > top)
            {
                sr.enabled = false;
                continue;
            }
            float share = (float)i / ColumnDiscs;
            float width = Mathf.Lerp(mouthHalfWidth * 2.3f, mouthHalfWidth * 1.5f, share) * (0.92f + 0.08f * Mathf.Sin(t * 9f + i * 1.7f));
            float x = mouthX + Mathf.Sin(t * 2.6f + i * 0.9f) * (1f + share * 3f);
            Color hot = Color.Lerp(new Color(1f, 0.55f, 0.12f, 1f), new Color(0.75f, 0.12f, 0.04f, 1f), share);
            Place(sr, new Vector2(x, y + spacing * 0.5f), width, new Color(hot.r, hot.g, hot.b, (0.7f - 0.25f * share) * (0.85f + 0.15f * pulse) * master));
        }
        coreStreak.enabled = true;
        float streakWidth = mouthHalfWidth * 0.9f * (0.85f + 0.15f * pulse);
        coreStreak.transform.position = new Vector3(mouthX, mouthY + top * 0.5f, 0f);
        coreStreak.transform.localScale = new Vector3(streakWidth / PixelWorld / 3f, Mathf.Max(0.1f, top) / (PixelWorld * 3f), 1f);
        coreStreak.color = new Color(1f, 0.9f, 0.55f, 0.55f * master);

        for (int i = 0; i < rings.Length; i++)
        {
            float local = (t - ringStart[i]) / 1.9f;
            SpriteRenderer sr = rings[i];
            if (local < 0f || local > 1f)
            {
                sr.enabled = false;
                continue;
            }
            float r = Mathf.Lerp(6f, 95f, 1f - (1f - local) * (1f - local));
            Place(sr, new Vector2(mouthX, mouthY + 3f), r * 2f, new Color(1f, 0.8f, 0.5f, (1f - local) * 0.9f * master));
        }

        float glow = Mathf.Clamp01(t / 0.6f);
        Color hotLava = Color.Lerp(new Color(1f, 0.55f, 0.15f, 1f), new Color(1f, 0.97f, 0.8f, 1f), Mathf.Clamp01(t / 2.5f));
        lavaGlow.enabled = true;
        lavaGlow.transform.position = new Vector3((lavaLeft + lavaRight) * 0.5f, lavaCentreY, 0f);
        lavaGlow.transform.localScale = new Vector3((lavaRight - lavaLeft) / (PixelWorld * 3f), lavaHeight / (PixelWorld * 3f), 1f);
        lavaGlow.color = new Color(hotLava.r, hotLava.g, hotLava.b, 0.88f * glow * (0.8f + 0.2f * pulse) * master);
    }

    void Place(SpriteRenderer sr, Vector2 centre, float diameter, Color colour)
    {
        if (sr == null)
        {
            return;
        }
        sr.enabled = colour.a > 0.004f;
        float native = sr.sprite != null ? sr.sprite.bounds.size.x : PixelWorld;
        sr.transform.position = new Vector3(centre.x, centre.y, 0f);
        float scale = diameter / native;
        sr.transform.localScale = new Vector3(scale, scale, 1f);
        sr.color = colour;
    }

    void Flash(SpriteRenderer sr, float t, float start, float length, Color colour, Vector3 camPosition, float halfWidth, float halfHeight, float master)
    {
        float local = (t - start) / length;
        if (local < 0f || local > 1f)
        {
            sr.enabled = false;
            return;
        }
        float envelope = local < 0.12f ? local / 0.12f : 1f - (local - 0.12f) / 0.88f;
        sr.enabled = true;
        sr.transform.position = new Vector3(camPosition.x, camPosition.y, 0f);
        sr.transform.localScale = new Vector3(halfWidth * 2.6f / (PixelWorld * 3f), halfHeight * 2.6f / (PixelWorld * 3f), 1f);
        sr.color = new Color(colour.r, colour.g, colour.b, colour.a * envelope * master);
    }
}
