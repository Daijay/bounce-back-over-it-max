using System.Collections.Generic;
using UnityEngine;

public enum BlockKind
{
    Grey,
    Green,
    Blue,
    Purple,
    Gold,
    Lava,
    Crust,
    BurningSign
}

public static class SpriteFactory
{
    static Sprite white;
    static Sprite circle;

    public static Sprite White
    {
        get
        {
            if (white == null)
            {
                Texture2D tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                tex.SetPixel(0, 0, Color.white);
                tex.Apply();
                tex.filterMode = FilterMode.Point;
                white = Sprite.Create(tex, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                white.name = "GeneratedWhite";
            }
            return white;
        }
    }

    public static Sprite Circle
    {
        get
        {
            if (circle == null)
            {
                const int size = 64;
                Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                float radius = size * 0.5f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float dx = x + 0.5f - radius;
                        float dy = y + 0.5f - radius;
                        float edge = Mathf.Clamp01(radius - Mathf.Sqrt(dx * dx + dy * dy));
                        tex.SetPixel(x, y, new Color(1f, 1f, 1f, edge));
                    }
                }
                tex.Apply();
                circle = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
                circle.name = "GeneratedCircle";
            }
            return circle;
        }
    }
}

[System.Serializable]
public class BlockSettings
{
    public Color color = Color.white;
    public float friction = 0.6f;
    public float bounciness = 0f;

    public BlockSettings() { }

    public BlockSettings(Color color, float friction, float bounciness)
    {
        this.color = color;
        this.friction = friction;
        this.bounciness = bounciness;
    }
}

[DisallowMultipleComponent]
public class BlockType : MonoBehaviour
{
    static readonly Dictionary<Collider2D, BlockType> byCollider = new Dictionary<Collider2D, BlockType>();
    public static bool TryGet(Collider2D col, out BlockType block)
    {
        if (col == null)
        {
            block = null;
            return false;
        }
        return byCollider.TryGetValue(col, out block);
    }

    public BlockKind kind = BlockKind.Grey;

    public BlockSettings grey = new BlockSettings(new Color(0.55f, 0.56f, 0.6f), 0.6f, 0f);
    public BlockSettings green = new BlockSettings(new Color(0.25f, 0.8f, 0.35f), 0.4f, 0f);
    public BlockSettings blue = new BlockSettings(new Color(0.35f, 0.65f, 1f), 0f, 0f);
    public float iceFriction = 0.01f;
    public BlockSettings purple = new BlockSettings(new Color(0.6f, 0.3f, 0.85f), 0.6f, 0f);
    public BlockSettings gold = new BlockSettings(new Color(1f, 0.82f, 0.15f), 0.6f, 0f);
    public BlockSettings lava = new BlockSettings(new Color(1f, 0.45f, 0.05f), 0f, 0f);
    public BlockSettings crust = new BlockSettings(new Color(0.25f, 0.2f, 0.2f), 0.6f, 0f);
    public BlockSettings burningSign = new BlockSettings(new Color(0.45f, 0.3f, 0.15f), 0.6f, 0f);

    public bool useArt;

    Collider2D col;

    BlockSettings Settings
    {
        get
        {
            switch (kind)
            {
                case BlockKind.Green: return green;
                case BlockKind.Blue: return blue;
                case BlockKind.Purple: return purple;
                case BlockKind.Gold: return gold;
                case BlockKind.Lava: return lava;
                case BlockKind.Crust: return crust;
                case BlockKind.BurningSign: return burningSign;
                default: return grey;
            }
        }
    }

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (col != null)
        {
            byCollider[col] = this;
        }
        ApplyVisual();
        ApplyPhysics();
    }

    void OnEnable()
    {
        if (col == null)
        {
            col = GetComponent<Collider2D>();
        }
        if (col != null)
        {
            byCollider[col] = this;
        }
    }

    void OnDestroy()
    {
        if (col != null)
        {
            byCollider.Remove(col);
        }
    }

    public void ApplyVisual()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            return;
        }
        if (useArt)
        {
            sr.color = Color.white;
            return;
        }
        if (sr.sprite == null)
        {
            sr.sprite = SpriteFactory.White;
        }
        sr.color = Settings.color;
    }

    void ApplyPhysics()
    {
        if (col == null)
        {
            return;
        }
        BlockSettings s = Settings;
        PhysicsMaterial2D mat = new PhysicsMaterial2D("Block_" + kind);
        mat.friction = kind == BlockKind.Blue ? iceFriction : s.friction;
        mat.bounciness = s.bounciness;
        col.sharedMaterial = mat;
    }
}
