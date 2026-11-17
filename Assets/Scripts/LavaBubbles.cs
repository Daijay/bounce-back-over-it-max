using UnityEngine;

public class LavaBubbles : MonoBehaviour
{
    public Sprite[] frames;
    public float xMin;
    public float xMax;
    public float surfaceY;
    public float floorY;
    public int count = 26;
    public float minRiseSpeed = 0.5f;
    public float maxRiseSpeed = 1.4f;
    public float popTime = 0.2f;
    public float respawnDepth = 0.25f;
    public int seed = 7;
    public int sortingOrder = MapStyle.OrderBubbles;

    Transform[] tr;
    SpriteRenderer[] sr;
    float[] x;
    float[] y;
    float[] speed;
    float[] popTimer;
    uint rng;

    void Awake()
    {
        Build();
    }

    void OnEnable()
    {
        if (tr == null && Application.isPlaying)
        {
            Build();
        }
    }

    void Build()
    {
        if (frames == null || frames.Length < 4 || count <= 0)
        {
            enabled = false;
            return;
        }

        for (int c = transform.childCount - 1; c >= 0; c--)
        {
            Transform child = transform.GetChild(c);
            if (child.name == "Bubble")
            {
                Destroy(child.gameObject);
            }
        }

        rng = (uint)seed;
        if (rng == 0u)
        {
            rng = 0x9E3779B9u;
        }

        tr = new Transform[count];
        sr = new SpriteRenderer[count];
        x = new float[count];
        y = new float[count];
        speed = new float[count];
        popTimer = new float[count];

        for (int i = 0; i < count; i++)
        {
            GameObject go = new GameObject("Bubble");
            Transform t = go.transform;
            t.SetParent(transform, false);

            SpriteRenderer r = go.AddComponent<SpriteRenderer>();
            r.sortingOrder = sortingOrder;
            r.sprite = frames[0];

            tr[i] = t;
            sr[i] = r;

            Respawn(i, true);
        }
    }

    void Update()
    {
        if (tr == null)
        {
            return;
        }
        float dt = Time.deltaTime;

        for (int i = 0; i < count; i++)
        {
            if (popTimer[i] >= 0f)
            {
                popTimer[i] += dt;
                y[i] = surfaceY;
                SetFrame(i, 3);
                if (popTimer[i] > popTime)
                {
                    Respawn(i, false);
                }
            }
            else
            {
                y[i] += speed[i] * dt;

                if (y[i] >= surfaceY)
                {
                    popTimer[i] = 0f;
                    y[i] = surfaceY;
                    SetFrame(i, 3);
                }
                else
                {
                    float progress = Mathf.InverseLerp(floorY, surfaceY, y[i]);
                    int f = progress < 0.35f ? 0 : (progress < 0.7f ? 1 : 2);
                    SetFrame(i, f);
                }
            }

            tr[i].position = new Vector3(MapStyle.Snap(x[i]), MapStyle.Snap(y[i]), 0f);
        }
    }

    void Respawn(int i, bool spread)
    {
        x[i] = Mathf.Lerp(xMin, xMax, Rand01());
        if (spread)
        {
            y[i] = Mathf.Lerp(floorY, surfaceY - 0.3f, Rand01());
        }
        else
        {
            y[i] = Mathf.Lerp(floorY, floorY + respawnDepth, Rand01());
        }
        speed[i] = Mathf.Lerp(minRiseSpeed, maxRiseSpeed, Rand01());
        popTimer[i] = -1f;
    }

    void SetFrame(int i, int f)
    {
        Sprite s = frames[f];
        if (sr[i].sprite != s)
        {
            sr[i].sprite = s;
        }
    }

    float Rand01()
    {
        rng ^= rng << 13;
        rng ^= rng >> 17;
        rng ^= rng << 5;
        return (rng & 0xFFFFFFu) / 16777216f;
    }
}
