using UnityEngine;

[DefaultExecutionOrder(90)]
public class SkyStage : MonoBehaviour
{
    PlayerController player;
    CutsceneDirector director;
    EndingOverlay overlay;
    [SerializeField] bool entered;

    void Awake()
    {
        Init();
    }

    void OnEnable()
    {
        if (Application.isPlaying && player == null)
        {
            Init();
        }
    }

    void Init()
    {
        player = GetComponent<PlayerController>();
        director = GetComponent<CutsceneDirector>();
        overlay = GetComponent<EndingOverlay>();
    }

    public void ResetSky()
    {
        entered = false;
        if (player != null)
        {
            player.SkyGravityMultiplier = 1f;
        }
    }

    void FixedUpdate()
    {
        if (player == null || player.BodyRb == null || director == null)
        {
            return;
        }
        EndingSettings settings = director.ending;
        float multiplier = settings.GravityScaleAt(player.BodyRb.position.y);
        if (!entered && multiplier < settings.lowGravityTextBelow)
        {
            entered = true;
            if (overlay != null)
            {
                overlay.ShowLowGravity(settings.lowGravityTextTime);
            }
        }
        player.SkyGravityMultiplier = multiplier;
    }
}
