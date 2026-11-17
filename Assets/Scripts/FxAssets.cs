using UnityEngine;

public static class FxAssets
{
    static Material glowMaterial;
    static Material alphaMaterial;
    static Material litMaterial;
    static Material particleAdd;
    static Material particleAlpha;
    static Sprite ember;
    static Sprite disc;
    static Sprite ring;
    static Sprite smoke;
    static Sprite halo;
    static Sprite spark;

    public static Material Glow
    {
        get
        {
            if (glowMaterial == null)
            {
                glowMaterial = MakeMaterial("Shaders/BBGlowSprite", "Universal Render Pipeline/2D/Sprite-Unlit-Default");
            }
            return glowMaterial;
        }
    }

    public static Material Alpha
    {
        get
        {
            if (alphaMaterial == null)
            {
                alphaMaterial = MakeMaterial(null, "Universal Render Pipeline/2D/Sprite-Unlit-Default");
            }
            return alphaMaterial;
        }
    }

    public static Material Lit
    {
        get
        {
            if (litMaterial == null)
            {
                litMaterial = MakeMaterial("Shaders/BBSpriteLit", "Universal Render Pipeline/2D/Sprite-Lit-Default");
            }
            return litMaterial;
        }
    }

    public static Material ParticleAdditive
    {
        get
        {
            if (particleAdd == null)
            {
                particleAdd = MakeParticle(1f);
            }
            return particleAdd;
        }
    }

    public static Material ParticleAlpha
    {
        get
        {
            if (particleAlpha == null)
            {
                particleAlpha = MakeParticle(10f);
            }
            return particleAlpha;
        }
    }

    static Material MakeParticle(float destination)
    {
        Material m = MakeMaterial("Shaders/BBParticle", "Universal Render Pipeline/Particles/Unlit");
        if (m != null)
        {
            m.SetFloat("_Src", 5f);
            m.SetFloat("_Dst", destination);
            m.mainTexture = Disc.texture;
        }
        return m;
    }

    static Material MakeMaterial(string resourcePath, string fallbackShader)
    {
        Shader shader = resourcePath != null ? Resources.Load<Shader>(resourcePath) : null;
        if (shader == null)
        {
            shader = Shader.Find(fallbackShader);
        }
        if (shader == null)
        {
            return null;
        }
        Material m = new Material(shader);
        m.hideFlags = HideFlags.DontSave;
        return m;
    }

    public static Sprite Ember
    {
        get
        {
            if (ember == null)
            {
                ember = Build(4, FilterMode.Point, (x, y, n) => 1f);
            }
            return ember;
        }
    }

    public static Sprite Disc
    {
        get
        {
            if (disc == null)
            {
                disc = Build(64, FilterMode.Bilinear, (x, y, n) =>
                {
                    float d = Radius(x, y, n);
                    return Mathf.Clamp01((1f - d) * 6f);
                });
            }
            return disc;
        }
    }

    public static Sprite Ring
    {
        get
        {
            if (ring == null)
            {
                ring = Build(128, FilterMode.Bilinear, (x, y, n) =>
                {
                    float d = Radius(x, y, n);
                    float band = 1f - Mathf.Abs(d - 0.9f) / 0.1f;
                    return Mathf.Clamp01(band);
                });
            }
            return ring;
        }
    }

    public static Sprite Smoke
    {
        get
        {
            if (smoke == null)
            {
                smoke = Build(64, FilterMode.Bilinear, (x, y, n) =>
                {
                    float d = Radius(x, y, n);
                    float noise = Mathf.PerlinNoise(x * 0.18f + 3.1f, y * 0.18f + 7.7f);
                    float a = Mathf.Clamp01((1f - d) * 2.4f) * Mathf.Lerp(0.55f, 1f, noise);
                    return a * a;
                });
            }
            return smoke;
        }
    }

    public static Sprite Halo
    {
        get
        {
            if (halo == null)
            {
                halo = Build(128, FilterMode.Bilinear, (x, y, n) =>
                {
                    float a = Mathf.Clamp01(1f - Radius(x, y, n));
                    return a * a * (3f - 2f * a);
                });
            }
            return halo;
        }
    }

    public static Sprite Spark
    {
        get
        {
            if (spark == null)
            {
                spark = Build(32, FilterMode.Bilinear, (x, y, n) =>
                {
                    float d = Radius(x, y, n);
                    float cross = Mathf.Max(0f, 1f - Mathf.Min(Mathf.Abs(x - (n - 1) * 0.5f), Mathf.Abs(y - (n - 1) * 0.5f)) / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    return Mathf.Clamp01(a * a * 0.6f + cross * a);
                });
            }
            return spark;
        }
    }

    static float Radius(int x, int y, int n)
    {
        float half = (n - 1) * 0.5f;
        return Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
    }

    static Sprite Build(int n, FilterMode filter, System.Func<int, int, int, float> alpha)
    {
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = filter;
        tex.hideFlags = HideFlags.DontSave;
        Color32[] pixels = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(alpha(x, y, n)) * 255f);
                pixels[y * n + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply(false, true);
        Sprite s = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), 100f);
        s.hideFlags = HideFlags.DontSave;
        return s;
    }
}
