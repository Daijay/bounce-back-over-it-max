using UnityEngine;

[DefaultExecutionOrder(150)]
public class ChargeAura : MonoBehaviour
{
    [Header("Look")]
    public Color glowTint = new Color(1f, 0.85f, 0.15f);
    public int sortingOrder = 9;
    public int maxFlames = 12;
    public int boltCount = 3;
    public int trailLines = 3;
    public int trailSegments = 6;

    const float FlameAspect = 8f / 5f;

    static Sprite glowSprite;
    static Sprite flameSprite;
    static Sprite boltSprite;

    PlayerController owner;
    PlayerVisuals visuals;
    SpriteRenderer glow;
    Transform glowT;
    SpriteRenderer[] flames;
    Transform[] flameT;
    float[] flamePhase;
    float[] flameX;
    float[] flameSpeed;
    SpriteRenderer[] bolts;
    Transform[] boltT;
    float[] boltNext;
    float[] boltOx;
    float[] boltOy;
    bool[] boltOn;
    SpriteRenderer[] trail;
    Transform[] trailT;
    bool shown;

    void Awake()
    {
        Ensure();
    }

    void OnEnable()
    {
        if (Application.isPlaying && flames == null)
        {
            Ensure();
        }
    }


    static Color32 FlameColor(char c)
    {
        switch (c)
        {
            case 'W': return new Color32(255, 255, 190, 255);
            case 'Y': return new Color32(255, 222, 40, 255);
            case 'o': return new Color32(255, 170, 20, 255);
            default: return new Color32(0, 0, 0, 0);
        }
    }

    static Sprite FromRows(string name, string[] rows)
    {
        int h = rows.Length;
        int w = rows[0].Length;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.hideFlags = HideFlags.DontSave;
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                tex.SetPixel(x, y, FlameColor(rows[h - 1 - y][x]));
            }
        }
        tex.Apply();
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f), w);
        sprite.name = name;
        sprite.hideFlags = HideFlags.DontSave;
        return sprite;
    }

    static Sprite FlameSprite
    {
        get
        {
            if (flameSprite == null)
            {
                flameSprite = FromRows("GeneratedFlame", new[]
                {
                    "..Y..",
                    "..Y..",
                    ".YYY.",
                    ".YWY.",
                    "oYWYo",
                    "oYWYo",
                    ".oYo.",
                    "..o..",
                });
            }
            return flameSprite;
        }
    }

    static Sprite BoltSprite
    {
        get
        {
            if (boltSprite == null)
            {
                boltSprite = FromRows("GeneratedBolt", new[]
                {
                    "..WW.",
                    ".WW..",
                    "WW...",
                    ".WWW.",
                    "...WW",
                    "..WW.",
                    ".WW..",
                    "WW...",
                });
            }
            return boltSprite;
        }
    }

    static Sprite GlowSprite
    {
        get
        {
            if (glowSprite == null)
            {
                const int size = 48;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.hideFlags = HideFlags.DontSave;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = (x + 0.5f - size * 0.5f) / (size * 0.5f);
                        float dy = (y + 0.5f - size * 0.5f) / (size * 0.5f);
                        float d = Mathf.Sqrt(dx * dx + dy * dy);
                        Color c;
                        if (d < 0.3f)
                        {
                            c = new Color(1f, 0.97f, 0.6f, 0.95f);
                        }
                        else if (d < 0.55f)
                        {
                            c = new Color(1f, 0.9f, 0.25f, 0.6f);
                        }
                        else if (d < 0.8f)
                        {
                            c = new Color(1f, 0.8f, 0.1f, 0.32f);
                        }
                        else if (d < 1f)
                        {
                            c = new Color(1f, 0.75f, 0.05f, 0.14f);
                        }
                        else
                        {
                            c = Color.clear;
                        }
                        tex.SetPixel(x, y, c);
                    }
                }
                tex.Apply();
                glowSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                glowSprite.name = "GeneratedGlow";
                glowSprite.hideFlags = HideFlags.DontSave;
            }
            return glowSprite;
        }
    }


    SpriteRenderer MakePart(string childName, Sprite sprite, out Transform t)
    {
        t = transform.Find(childName);
        if (t == null)
        {
            GameObject go = new GameObject(childName);
            t = go.transform;
            t.SetParent(transform, false);
        }
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = t.gameObject.AddComponent<SpriteRenderer>();
        }
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.enabled = false;
        return sr;
    }

    void Ensure()
    {
        owner = GetComponent<PlayerController>();
        visuals = GetComponent<PlayerVisuals>();

        glow = MakePart("Charge Glow", GlowSprite, out glowT);

        int nf = Mathf.Clamp(maxFlames, 1, 32);
        flames = new SpriteRenderer[nf];
        flameT = new Transform[nf];
        flamePhase = new float[nf];
        flameX = new float[nf];
        flameSpeed = new float[nf];
        for (int i = 0; i < nf; i++)
        {
            flames[i] = MakePart("Charge Flame " + i, FlameSprite, out flameT[i]);
            flamePhase[i] = Random.value;
            flameX[i] = Random.Range(-0.5f, 0.5f);
            flameSpeed[i] = Random.Range(0.7f, 1.3f);
        }

        int nb = Mathf.Clamp(boltCount, 1, 8);
        bolts = new SpriteRenderer[nb];
        boltT = new Transform[nb];
        boltNext = new float[nb];
        boltOx = new float[nb];
        boltOy = new float[nb];
        boltOn = new bool[nb];
        for (int i = 0; i < nb; i++)
        {
            bolts[i] = MakePart("Charge Bolt " + i, BoltSprite, out boltT[i]);
            boltNext[i] = Time.time + Random.Range(0f, 0.2f);
        }

        int lines = Mathf.Clamp(trailLines, 1, 5);
        int segs = Mathf.Clamp(trailSegments, 1, 12);
        trail = new SpriteRenderer[lines * segs];
        trailT = new Transform[lines * segs];
        for (int i = 0; i < trail.Length; i++)
        {
            trail[i] = MakePart("Charge Trail " + i, SpriteFactory.White, out trailT[i]);
        }
        shown = false;
    }

    static void Show(SpriteRenderer sr, bool on)
    {
        if (sr.enabled != on)
        {
            sr.enabled = on;
        }
    }

    void HideAll()
    {
        Show(glow, false);
        for (int i = 0; i < flames.Length; i++)
        {
            Show(flames[i], false);
        }
        for (int i = 0; i < bolts.Length; i++)
        {
            Show(bolts[i], false);
            boltOn[i] = false;
        }
        for (int i = 0; i < trail.Length; i++)
        {
            Show(trail[i], false);
        }
    }


    void LateUpdate()
    {
        if (owner == null || visuals == null || flames == null)
        {
            return;
        }

        bool flying = owner.SuperJumpActive;
        float pool = owner.ChargePool;
        bool on = !owner.Dizzy && (flying || pool >= owner.MinJumpCharge);
        if (!on)
        {
            if (shown)
            {
                HideAll();
                shown = false;
            }
            return;
        }
        shown = true;

        float level = flying ? 1f : Mathf.Clamp01(pool / owner.ChargeMax);
        bool full = flying || level >= 0.999f;
        float t = Time.time;
        float dt = Time.deltaTime;
        Vector3 c = visuals.BodyCenterWorld();
        float u = visuals.BodyHeight;
        float w = visuals.BodyWidth;

        UpdateGlow(c, u, level, full, t);
        UpdateFlames(c, u, w, level, full, t, dt);
        UpdateBolts(c, u, w, full, t);
        UpdateTrail(c, u, w, flying, t);
    }

    void UpdateGlow(Vector3 c, float u, float level, bool full, float t)
    {
        float size = Mathf.Lerp(0.8f, 1.5f, level) * (full ? 1f + 0.1f * Mathf.Sin(t * 18f) : 1f);
        glowT.position = new Vector3(c.x, c.y, c.z + 0.02f);
        glowT.localScale = new Vector3(u * 0.8f * size, u * 1.2f * size, 1f);

        float flicker = 0.85f + 0.3f * (Mathf.PerlinNoise(t * 14f, 3.7f) - 0.5f);
        Color col = glowTint;
        float alpha = Mathf.Lerp(0.3f, 0.95f, level) * flicker;
        if (full)
        {
            float f = 0.5f + 0.5f * Mathf.Sin(t * 14f);
            col = Color.Lerp(glowTint, Color.white, f * 0.55f);
            alpha = 0.8f + 0.2f * f;
        }
        col.a = Mathf.Clamp01(alpha);
        glow.color = col;
        Show(glow, true);
    }

    void UpdateFlames(Vector3 c, float u, float w, float level, bool full, float t, float dt)
    {
        int visible = full ? flames.Length : Mathf.RoundToInt(Mathf.Lerp(3f, flames.Length - 2f, level));
        float rate = full ? 2.8f : Mathf.Lerp(0.8f, 1.7f, level);
        float strength = Mathf.Lerp(0.6f, 1.15f, level);
        for (int i = 0; i < flames.Length; i++)
        {
            SpriteRenderer sr = flames[i];
            if (i >= visible)
            {
                Show(sr, false);
                continue;
            }

            float p = flamePhase[i] + dt * rate * flameSpeed[i];
            if (p >= 1f)
            {
                p = 0f;
                flameX[i] = Random.Range(-0.5f, 0.5f);
                flameSpeed[i] = Random.Range(0.7f, 1.3f);
            }
            flamePhase[i] = p;

            float sway = Mathf.Sin(t * 9f + i * 1.7f) * 0.05f * u * p;
            float px = c.x + flameX[i] * w * 1.1f + sway;
            float py = c.y - 0.3f * u + p * 0.95f * u;
            float s = (1f - 0.6f * p) * strength;
            flameT[i].position = new Vector3(px, py, c.z + 0.01f);
            flameT[i].localScale = new Vector3(0.12f * u * s, 0.2f * u * s / FlameAspect, 1f);
            float a = Mathf.Clamp01(1.4f * (1f - p)) * Mathf.Lerp(0.55f, 1f, level);
            sr.color = new Color(1f, 1f, 1f, a);
            Show(sr, true);
        }
    }

    void UpdateBolts(Vector3 c, float u, float w, bool full, float t)
    {
        for (int i = 0; i < bolts.Length; i++)
        {
            SpriteRenderer sr = bolts[i];
            if (!full)
            {
                Show(sr, false);
                boltOn[i] = false;
                continue;
            }

            if (t >= boltNext[i])
            {
                if (boltOn[i])
                {
                    boltOn[i] = false;
                    boltNext[i] = t + Random.Range(0.08f, 0.3f);
                }
                else
                {
                    boltOn[i] = true;
                    boltNext[i] = t + Random.Range(0.06f, 0.12f);
                    boltOx[i] = Random.Range(-0.9f, 0.9f) * w;
                    boltOy[i] = Random.Range(-0.4f, 0.45f) * u;
                    float flip = Random.value < 0.5f ? -1f : 1f;
                    boltT[i].localScale = new Vector3(0.1f * u * flip, 0.3f * u / FlameAspect, 1f);
                    boltT[i].rotation = Quaternion.Euler(0f, 0f, Random.Range(-25f, 25f));
                    sr.color = new Color(1f, 1f, 0.75f, 1f);
                }
            }
            if (boltOn[i])
            {
                boltT[i].position = new Vector3(c.x + boltOx[i], c.y + boltOy[i], c.z + 0.01f);
            }
            Show(sr, boltOn[i]);
        }
    }

    void UpdateTrail(Vector3 c, float u, float w, bool flying, float t)
    {
        if (!flying)
        {
            for (int i = 0; i < trail.Length; i++)
            {
                Show(trail[i], false);
            }
            return;
        }

        Vector2 v = owner.BodyRb != null ? owner.BodyRb.linearVelocity : Vector2.zero;
        float speed = v.magnitude;
        Vector2 back = speed > 0.5f ? -v / speed : Vector2.down;
        Vector2 perp = new Vector2(-back.y, back.x);
        Quaternion rot = Quaternion.Euler(0f, 0f, Mathf.Atan2(back.y, back.x) * Mathf.Rad2Deg - 90f);
        float seg = 0.2f * u * Mathf.Clamp(speed / 12f, 0.6f, 1.3f);
        int segs = trail.Length / Mathf.Max(1, trailLines);
        int lines = trail.Length / Mathf.Max(1, segs);
        float mid = (lines - 1) * 0.5f;

        for (int l = 0; l < lines; l++)
        {
            bool centre = Mathf.Abs(l - mid) < 0.01f;
            float lateral = (l - mid) * 0.3f * w;
            for (int k = 0; k < segs; k++)
            {
                int idx = l * segs + k;
                float taper = 1f - k / (float)segs;
                float along = 0.45f * u + k * seg * 1.1f;
                Vector2 pos = (Vector2)c + back * along + perp * lateral;
                float width = (centre ? 0.1f : 0.045f) * u * taper;
                float alpha = Mathf.Pow(taper, 1.3f) * (centre ? 0.85f : 0.6f) * (0.8f + 0.2f * Mathf.PerlinNoise(t * 20f, idx));
                trailT[idx].position = new Vector3(pos.x, pos.y, c.z + 0.01f);
                trailT[idx].rotation = rot;
                trailT[idx].localScale = new Vector3(width, seg * 1.05f, 1f);
                trail[idx].color = new Color(1f, 0.9f, 0.25f, alpha);
                Show(trail[idx], true);
            }
        }
    }
}
