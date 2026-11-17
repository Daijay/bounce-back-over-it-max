using UnityEngine;

[DefaultExecutionOrder(150)]
public class DizzyStars : MonoBehaviour
{
    public int starCount = 4;
    public Color starColor = new Color(1f, 0.88f, 0.15f);
    public float starSize = 0.22f;
    public float orbitWidth = 0.55f;
    public float orbitHeight = 0.16f;
    public float lift = 0.22f;
    public float turnsPerSecond = 1.4f;
    public int sortingOrder = 20;

    const int PixelGrid = 7;
    static Sprite starSprite;

    PlayerController owner;
    PlayerVisuals visuals;
    SpriteRenderer[] stars;
    Transform[] starTransforms;
    bool shown;

    void Awake()
    {
        Ensure();
    }

    void OnEnable()
    {
        if (Application.isPlaying && stars == null)
        {
            Ensure();
        }
    }

    static Sprite StarSprite
    {
        get
        {
            if (starSprite == null)
            {
                string[] rows =
                {
                    "...#...",
                    "...#...",
                    "..###..",
                    "#######",
                    "..###..",
                    "...#...",
                    "...#...",
                };
                Texture2D tex = new Texture2D(PixelGrid, PixelGrid, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.hideFlags = HideFlags.DontSave;
                for (int y = 0; y < PixelGrid; y++)
                {
                    for (int x = 0; x < PixelGrid; x++)
                    {
                        bool on = rows[PixelGrid - 1 - y][x] == '#';
                        tex.SetPixel(x, y, on ? Color.white : Color.clear);
                    }
                }
                tex.Apply();
                float pixelsPerUnit = PixelGrid / 0.22f;
                starSprite = Sprite.Create(tex, new Rect(0f, 0f, PixelGrid, PixelGrid), new Vector2(0.5f, 0.5f), pixelsPerUnit);
                starSprite.name = "GeneratedDizzyStar";
                starSprite.hideFlags = HideFlags.DontSave;
            }
            return starSprite;
        }
    }

    void Ensure()
    {
        owner = GetComponent<PlayerController>();
        visuals = GetComponent<PlayerVisuals>();

        int count = Mathf.Clamp(starCount, 1, 8);
        stars = new SpriteRenderer[count];
        starTransforms = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            string childName = "Dizzy Star " + i;
            Transform t = transform.Find(childName);
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
            sr.sprite = StarSprite;
            sr.color = starColor;
            sr.sortingOrder = sortingOrder;
            t.gameObject.SetActive(false);
            stars[i] = sr;
            starTransforms[i] = t;
        }
        shown = false;
    }

    void LateUpdate()
    {
        if (owner == null || stars == null)
        {
            return;
        }
        bool dizzy = owner.Dizzy;
        if (dizzy != shown)
        {
            shown = dizzy;
            for (int i = 0; i < starTransforms.Length; i++)
            {
                starTransforms[i].gameObject.SetActive(dizzy);
            }
        }
        if (!shown)
        {
            return;
        }

        float k = owner.Scale / PlayerController.DefaultRigScale;
        Vector3 top = visuals != null ? visuals.HeadTopWorld() : transform.position + Vector3.up * (1.5f * owner.Scale);
        float baseAngle = Time.time * turnsPerSecond * Mathf.PI * 2f;
        float step = Mathf.PI * 2f / stars.Length;
        float fade = Mathf.Clamp01(owner.DizzyLeft / 0.25f);
        Color c = starColor;
        c.a = starColor.a * fade;

        for (int i = 0; i < stars.Length; i++)
        {
            float a = baseAngle + i * step;
            Transform t = starTransforms[i];
            t.position = new Vector3(top.x + Mathf.Cos(a) * orbitWidth * k, top.y + (lift + Mathf.Sin(a) * orbitHeight) * k, transform.position.z);
            t.localScale = new Vector3(k, k, 1f);
            stars[i].color = c;
        }
    }
}
