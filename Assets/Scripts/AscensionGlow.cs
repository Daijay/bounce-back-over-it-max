using UnityEngine;

public class AscensionGlow : MonoBehaviour
{
    public float size = 5f;
    public float pulsePeriod = 3.2f;

    static Sprite softSprite;
    SpriteRenderer[] rings;
    Transform holder;
    bool shown;

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        Build();
        Apply(shown);
    }

    void OnDisable()
    {
        if (holder != null)
        {
            holder.gameObject.SetActive(false);
        }
    }

    public void Show(bool on)
    {
        shown = on;
        Build();
        Apply(on);
    }

    void Apply(bool on)
    {
        if (holder != null)
        {
            holder.gameObject.SetActive(on);
        }
    }

    void Build()
    {
        if (holder != null)
        {
            return;
        }
        Transform existing = transform.Find("Ascension Glow");
        if (existing != null)
        {
            holder = existing;
            rings = holder.GetComponentsInChildren<SpriteRenderer>(true);
            return;
        }
        holder = new GameObject("Ascension Glow").transform;
        holder.SetParent(transform, false);
        holder.localPosition = Vector3.zero;
        rings = new SpriteRenderer[3];
        float[] scale = { 1f, 0.66f, 0.38f };
        for (int i = 0; i < rings.Length; i++)
        {
            GameObject go = new GameObject("Ring " + i);
            go.transform.SetParent(holder, false);
            go.transform.localScale = new Vector3(size * scale[i], size * scale[i], 1f);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SoftSprite();
            sr.sortingOrder = 8;
            sr.color = new Color(1f, 1f, 1f, 0.12f);
            rings[i] = sr;
        }
    }

    void Update()
    {
        if (!shown || rings == null)
        {
            return;
        }
        float t = Time.time * (2f * Mathf.PI / Mathf.Max(0.5f, pulsePeriod));
        for (int i = 0; i < rings.Length; i++)
        {
            if (rings[i] == null)
            {
                continue;
            }
            float pulse = 0.5f + 0.5f * Mathf.Sin(t - i * 0.7f);
            rings[i].color = new Color(1f, 1f, 1f, Mathf.Lerp(0.07f, 0.2f, pulse) * (1f + i * 0.35f));
        }
    }

    static Sprite SoftSprite()
    {
        if (softSprite != null)
        {
            return softSprite;
        }
        int n = 64;
        Texture2D tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.hideFlags = HideFlags.DontSave;
        Color32[] pixels = new Color32[n * n];
        float half = (n - 1) * 0.5f;
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - half) * (x - half) + (y - half) * (y - half)) / half;
                float a = Mathf.Clamp01(1f - d);
                a = a * a * (3f - 2f * a);
                pixels[y * n + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
            }
        }
        tex.SetPixels32(pixels);
        tex.Apply();
        softSprite = Sprite.Create(tex, new Rect(0f, 0f, n, n), new Vector2(0.5f, 0.5f), n);
        softSprite.hideFlags = HideFlags.DontSave;
        return softSprite;
    }
}
