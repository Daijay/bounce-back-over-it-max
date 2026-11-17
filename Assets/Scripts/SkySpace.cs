using UnityEngine;

[DefaultExecutionOrder(110)]
public class SkySpace : MonoBehaviour
{
    public Material spriteMaterial;
    public Sprite gradientSprite;
    public Sprite starSprite;
    public Camera targetCamera;
    public float fadeStart = 55f;
    public float fadeEnd = 80f;
    public int starCount = 60;
    public int seed = 4242;

    const float SpritePixel = 0.0301f;
    const float GradientPixel = 0.16f;
    const int GradientTileWidth = 512;
    const float GradientWidth = 6000f;
    const float GradientCentreY = 130f;
    const float StarParallax = 0.1f;
    const float StarSize = 0.09f;
    const string PartName = "Space Part";

    Camera cam;
    SpriteRenderer gradient;
    SpriteRenderer[] stars;
    float[] starX;
    float[] starY;
    float[] starPhase;
    bool[] starTwinkles;

    void OnEnable()
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
        if (!Application.isPlaying)
        {
            return;
        }
        cam = targetCamera != null ? targetCamera : Camera.main;
        System.Random random = new System.Random(seed);

        gradient = Make(gradientSprite, -70);
        float gradientScale = GradientPixel / SpritePixel;
        gradient.drawMode = SpriteDrawMode.Tiled;
        gradient.tileMode = SpriteTileMode.Continuous;
        gradient.transform.localScale = new Vector3(gradientScale, gradientScale, 1f);
        float rows = gradientSprite != null ? gradientSprite.rect.height : 1625f;
        gradient.size = new Vector2(GradientWidth / gradientScale, rows * SpritePixel);

        stars = new SpriteRenderer[starCount];
        starX = new float[starCount];
        starY = new float[starCount];
        starPhase = new float[starCount];
        starTwinkles = new bool[starCount];
        for (int i = 0; i < starCount; i++)
        {
            starX[i] = (float)random.NextDouble();
            starY[i] = (float)random.NextDouble();
            starPhase[i] = (float)random.NextDouble() * 6.28f;
            starTwinkles[i] = i % 6 == 0;
            stars[i] = Make(starSprite, -64);
        }
        HideAll();
    }

    SpriteRenderer Make(Sprite sprite, int order)
    {
        GameObject go = new GameObject(PartName);
        go.transform.SetParent(transform, false);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;
        if (spriteMaterial != null)
        {
            sr.sharedMaterial = spriteMaterial;
        }
        sr.enabled = false;
        return sr;
    }

    void LateUpdate()
    {
        if (gradient == null)
        {
            return;
        }
        if (cam == null)
        {
            cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null)
            {
                return;
            }
        }
        Vector3 camPosition = cam.transform.position;
        float halfHeight = cam.orthographicSize;
        float halfWidth = halfHeight * cam.aspect;

        float s = Mathf.Clamp01((camPosition.y - fadeStart) / Mathf.Max(0.01f, fadeEnd - fadeStart));
        float opacity = s * s * (3f - 2f * s);
        if (opacity <= 0.003f)
        {
            if (gradient.enabled)
            {
                HideAll();
            }
            return;
        }

        float tileWorld = GradientTileWidth * GradientPixel;
        gradient.enabled = true;
        gradient.transform.position = new Vector3(Mathf.Round(camPosition.x / tileWorld) * tileWorld, GradientCentreY, 0f);
        gradient.color = new Color(1f, 1f, 1f, opacity);

        float unit = halfHeight / 8f;
        float scale = StarSize * unit / (2f * SpritePixel);
        float width = halfWidth * 2.2f;
        float height = halfHeight * 2.2f;
        float time = Time.time;
        for (int i = 0; i < stars.Length; i++)
        {
            float x = Mathf.Repeat(starX[i] * width - camPosition.x * StarParallax, width) - width * 0.5f;
            float y = Mathf.Repeat(starY[i] * height - camPosition.y * StarParallax, height) - height * 0.5f;
            SpriteRenderer sr = stars[i];
            sr.transform.position = new Vector3(camPosition.x + x, camPosition.y + y, 0f);
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            float a = starTwinkles[i] ? 0.45f + 0.45f * Mathf.Sin(time * 0.6f + starPhase[i]) : 0.8f;
            sr.color = new Color(0.9f, 0.95f, 1f, a * opacity * opacity);
            sr.enabled = true;
        }
    }

    void HideAll()
    {
        if (gradient != null)
        {
            gradient.enabled = false;
        }
        for (int i = 0; stars != null && i < stars.Length; i++)
        {
            stars[i].enabled = false;
        }
    }
}
