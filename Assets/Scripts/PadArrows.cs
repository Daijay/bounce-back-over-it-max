using UnityEngine;

public class PadArrows : MonoBehaviour
{
    public Transform[] arrows;
    public float period = 0.5f;
    public float bobPixels = 3f;
    public float phaseStep = 0.2f;

    [SerializeField] Vector3[] home;

    void OnEnable()
    {
        if (arrows == null)
        {
            return;
        }
        if (home == null || home.Length != arrows.Length)
        {
            home = new Vector3[arrows.Length];
            for (int i = 0; i < arrows.Length; i++)
            {
                home[i] = arrows[i] != null ? arrows[i].localPosition : Vector3.zero;
            }
        }
    }

    void OnDisable()
    {
        if (arrows == null || home == null || home.Length != arrows.Length)
        {
            return;
        }
        for (int i = 0; i < arrows.Length; i++)
        {
            if (arrows[i] != null)
            {
                arrows[i].localPosition = home[i];
            }
        }
    }

    void Update()
    {
        if (arrows == null || home == null || home.Length != arrows.Length)
        {
            return;
        }
        float cycle = Time.time / Mathf.Max(0.05f, period);
        for (int i = 0; i < arrows.Length; i++)
        {
            if (arrows[i] == null)
            {
                continue;
            }
            float t = cycle - i * phaseStep;
            float frac = t - Mathf.Floor(t);
            float wave = frac < 0.5f ? frac * 2f : (1f - frac) * 2f;
            float lift = Mathf.Floor(wave * bobPixels + 0.5f) * MapStyle.PixelWorld;
            arrows[i].localPosition = new Vector3(home[i].x, home[i].y + lift, home[i].z);
        }
    }
}
