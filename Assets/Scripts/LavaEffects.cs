using UnityEngine;

[DefaultExecutionOrder(150)]
public class LavaEffects : MonoBehaviour
{
    [Header("Danger bar (sizes in world units at the default rig scale)")]
    public float barWidth = 0.9f;
    public float barHeight = 0.12f;
    public float barLift = 0.7f;
    public Color barBackColor = new Color(0f, 0f, 0f, 0.75f);
    public Color barLowColor = new Color(1f, 0.8f, 0.1f);
    public Color barHighColor = new Color(1f, 0.12f, 0.05f);
    public int barSortingOrder = 22;

    [Header("Burn sparks")]
    public int sparkCount = 16;
    public float burnTime = 0.5f;
    public float sparkSize = 0.1f;
    public int sparkSortingOrder = 23;

    PlayerController owner;
    PlayerVisuals visuals;
    SpriteRenderer barBack;
    SpriteRenderer barFill;
    Transform barBackT;
    Transform barFillT;
    SpriteRenderer[] sparks;
    Transform[] sparkT;
    float[] sparkX;
    float[] sparkY;
    float[] sparkVx;
    float[] sparkVy;
    float[] sparkAge;
    float[] sparkLife;
    Color[] sparkColor;
    float sparkScale = 1f;
    bool sparksActive;

    static readonly Color[] Palette =
    {
        new Color(1f, 0.8f, 0.2f),
        new Color(1f, 0.55f, 0.1f),
        new Color(1f, 0.3f, 0.05f),
        new Color(0.85f, 0.12f, 0.05f),
    };

    void Awake()
    {
        Ensure();
    }

    void OnEnable()
    {
        if (Application.isPlaying && sparks == null)
        {
            Ensure();
        }
    }

    SpriteRenderer MakePart(string childName, int order, out Transform t)
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
        sr.sprite = SpriteFactory.White;
        sr.sortingOrder = order;
        sr.enabled = false;
        return sr;
    }

    void Ensure()
    {
        owner = GetComponent<PlayerController>();
        visuals = GetComponent<PlayerVisuals>();

        barBack = MakePart("Lava Bar Back", barSortingOrder, out barBackT);
        barFill = MakePart("Lava Bar Fill", barSortingOrder + 1, out barFillT);

        int n = Mathf.Clamp(sparkCount, 1, 64);
        sparks = new SpriteRenderer[n];
        sparkT = new Transform[n];
        sparkX = new float[n];
        sparkY = new float[n];
        sparkVx = new float[n];
        sparkVy = new float[n];
        sparkAge = new float[n];
        sparkLife = new float[n];
        sparkColor = new Color[n];
        for (int i = 0; i < n; i++)
        {
            sparks[i] = MakePart("Burn Spark " + i, sparkSortingOrder, out sparkT[i]);
            sparkAge[i] = 1f;
            sparkLife[i] = 1f;
        }
        sparksActive = false;
    }

    public void PlayBurn(Vector3 pos, float k)
    {
        if (sparks == null)
        {
            return;
        }
        sparkScale = k;
        for (int i = 0; i < sparks.Length; i++)
        {
            sparkX[i] = pos.x + Random.Range(-0.4f, 0.4f) * k;
            sparkY[i] = pos.y + Random.Range(-0.6f, 0.5f) * k;
            sparkVx[i] = Random.Range(-0.8f, 0.8f) * k;
            sparkVy[i] = Random.Range(1.5f, 3.2f) * k;
            sparkAge[i] = 0f;
            sparkLife[i] = burnTime * Random.Range(0.7f, 1.1f);
            sparkColor[i] = Palette[Random.Range(0, Palette.Length)];
            sparkT[i].position = new Vector3(sparkX[i], sparkY[i], pos.z);
            sparks[i].enabled = true;
        }
        sparksActive = true;
    }

    void LateUpdate()
    {
        if (sparks == null || owner == null)
        {
            return;
        }
        UpdateBar();
        UpdateSparks();
    }

    void UpdateBar()
    {
        float p = owner.LavaProgress;
        if (p <= 0f || visuals == null)
        {
            if (barBack.enabled)
            {
                barBack.enabled = false;
                barFill.enabled = false;
            }
            return;
        }

        float k = owner.Scale / PlayerController.DefaultRigScale;
        float w = barWidth * k;
        float h = barHeight * k;
        float pad = 0.03f * k;
        Vector3 top = visuals.HeadTopWorld();
        float y = top.y + barLift * k;

        barBackT.position = new Vector3(top.x, y, top.z);
        barBackT.localScale = new Vector3(w + pad * 2f, h + pad * 2f, 1f);
        barBack.color = barBackColor;

        float fillW = w * p;
        barFillT.position = new Vector3(top.x - w * 0.5f + fillW * 0.5f, y, top.z - 0.01f);
        barFillT.localScale = new Vector3(fillW, h, 1f);
        Color c = Color.Lerp(barLowColor, barHighColor, p);
        if (p > 0.75f)
        {
            c = Color.Lerp(c, Color.white, 0.25f + 0.25f * Mathf.Sin(Time.time * 20f));
        }
        barFill.color = c;

        barBack.enabled = true;
        barFill.enabled = true;
    }

    void UpdateSparks()
    {
        if (!sparksActive)
        {
            return;
        }
        float dt = Time.deltaTime;
        bool any = false;
        for (int i = 0; i < sparks.Length; i++)
        {
            if (!sparks[i].enabled)
            {
                continue;
            }
            float age = sparkAge[i] + dt;
            sparkAge[i] = age;
            if (age >= sparkLife[i])
            {
                sparks[i].enabled = false;
                continue;
            }
            any = true;
            float t = age / sparkLife[i];
            sparkX[i] += (sparkVx[i] + Mathf.Sin(age * 14f + i) * 0.4f * sparkScale) * dt;
            sparkY[i] += sparkVy[i] * dt;
            Vector3 pos = sparkT[i].position;
            sparkT[i].position = new Vector3(sparkX[i], sparkY[i], pos.z);
            float size = sparkSize * sparkScale * (1f - 0.6f * t);
            sparkT[i].localScale = new Vector3(size, size, 1f);
            Color c = Color.Lerp(sparkColor[i], Palette[3], t);
            c.a = 1f - t * t;
            sparks[i].color = c;
        }
        sparksActive = any;
    }
}
