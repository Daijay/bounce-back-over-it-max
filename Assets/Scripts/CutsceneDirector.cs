using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class EndingFind
{
    static readonly List<GameObject> roots = new List<GameObject>(64);
    static readonly List<Transform> all = new List<Transform>(1024);
    static readonly HashSet<string> warned = new HashSet<string>();

    public static GameObject ByName(string objectName)
    {
        for (int s = 0; s < SceneManager.sceneCount; s++)
        {
            Scene scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded)
            {
                continue;
            }
            scene.GetRootGameObjects(roots);
            for (int r = 0; r < roots.Count; r++)
            {
                roots[r].GetComponentsInChildren<Transform>(true, all);
                for (int i = 0; i < all.Count; i++)
                {
                    if (all[i].name == objectName)
                    {
                        GameObject found = all[i].gameObject;
                        roots.Clear();
                        all.Clear();
                        return found;
                    }
                }
            }
        }
        roots.Clear();
        all.Clear();
        return null;
    }

    public static void WarnMissing(string objectName)
    {
        if (warned.Add(objectName))
        {
            Debug.LogWarning("Ending: scene object \"" + objectName + "\" was not found (searched inactive objects too). That part of the ending is off.");
        }
    }
}

[DefaultExecutionOrder(160)]
public class CutsceneDirector : MonoBehaviour
{
    public EndingSettings ending = new EndingSettings();

    PlayerController player;
    EndingOverlay overlay;
    Camera cam;
    CameraFollow follow;
    AscensionGlow glow;
    EndingVideo video;
    Collider2D eruptionTrigger;
    Collider2D moonCol;

    [SerializeField] int phase;
    [SerializeField] bool moonDone;
    bool floating;
    float floatTime;
    float floatY;
    float floatAngle;
    float floatCentreX;
    Vector2 floatStart;
    Vector2 hammerOffset;
    float hammerStartRotation;
    int findTries;
    float retryAt;

    float shakeAmount;
    bool shaking;

    void Awake()
    {
        Init();
    }

    void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }
        if (player == null)
        {
            Init();
        }
        if (phase == 1)
        {
            floating = false;
            if (overlay != null)
            {
                overlay.Letterbox = 0f;
            }
            if (glow != null)
            {
                glow.Show(false);
            }
            player.Frozen = false;
            phase = 0;
        }
    }

    void OnDestroy()
    {
        if (overlay != null)
        {
            overlay.Letterbox = 0f;
            overlay.Fade = 0f;
        }
        ClearShake();
    }

    void Init()
    {
        player = GetComponent<PlayerController>();
        overlay = GetComponent<EndingOverlay>();
        cam = Camera.main;
        follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
        glow = GetComponent<AscensionGlow>();
        if (glow == null)
        {
            glow = gameObject.AddComponent<AscensionGlow>();
        }
        video = GetComponent<EndingVideo>();
        if (video == null)
        {
            video = gameObject.AddComponent<EndingVideo>();
        }
        FindObjects();
    }

    void FindObjects()
    {
        findTries++;
        retryAt = Time.time + 1f;
        if (eruptionTrigger == null)
        {
            GameObject go = EndingFind.ByName("EruptionTrigger");
            eruptionTrigger = go != null ? go.GetComponent<Collider2D>() : null;
        }
        if (moonCol == null)
        {
            GameObject go = EndingFind.ByName("CheeseMoon");
            moonCol = go != null ? go.GetComponent<Collider2D>() : null;
        }
        if (findTries >= 3)
        {
            if (eruptionTrigger == null)
            {
                EndingFind.WarnMissing("EruptionTrigger");
            }
            if (moonCol == null)
            {
                EndingFind.WarnMissing("CheeseMoon");
            }
        }
    }
    public bool MoonReboundStarted { get { return moonDone; } }

    static readonly RaycastHit2D[] rayHits = new RaycastHit2D[8];
    ContactFilter2D rayFilter;
    bool rayFilterReady;

    void DebugTeleport()
    {
        if (player == null || moonDone || player.BodyRb == null)
        {
            return;
        }
        if (eruptionTrigger == null)
        {
            GameObject go = EndingFind.ByName("EruptionTrigger");
            eruptionTrigger = go != null ? go.GetComponent<Collider2D>() : null;
        }
        if (eruptionTrigger == null)
        {
            EndingFind.WarnMissing("EruptionTrigger");
            return;
        }
        if (!rayFilterReady)
        {
            rayFilter = new ContactFilter2D();
            rayFilter.useTriggers = false;
            rayFilterReady = true;
        }

        Bounds b = eruptionTrigger.bounds;
        float x = b.min.x - ending.debugTeleportLeft;
        float groundY = b.min.y;
        int n = Physics2D.Raycast(new Vector2(x, b.max.y + 1f), Vector2.down, rayFilter, rayHits, 30f);
        for (int i = 0; i < n; i++)
        {
            BlockType block;
            if (BlockType.TryGet(rayHits[i].collider, out block))
            {
                groundY = rayHits[i].point.y;
                break;
            }
        }

        float lift = 0.05f + (player.body.bodySize.y * 0.5f - player.body.bodyOffset.y) * player.rigScale;
        ResetEnding();
        player.TeleportTo(new Vector2(x, groundY + lift));
        if (follow != null)
        {
            follow.Snap();
        }
    }

    public void OnPlayerDied()
    {
        if (moonDone)
        {
            return;
        }
        ResetEnding();
    }

    void ResetEnding()
    {
        StopAllCoroutines();
        phase = 0;
        floating = false;
        IgnoreRock(false);
        if (glow != null)
        {
            glow.Show(false);
        }
        ClearShake();
        if (overlay != null)
        {
            overlay.ResetState();
        }
        player.Frozen = false;
        SkyStage sky = GetComponent<SkyStage>();
        if (sky != null)
        {
            sky.ResetSky();
        }
    }


    void Update()
    {
        if (ending.debugKeys && phase != 1 && MouseInput.SuperJumpPressed && MouseInput.ShiftHeld && !MainMenu.Showing)
        {
            DebugTeleport();
        }
        UpdateShake();
    }

    void FixedUpdate()
    {
        if (player == null || player.BodyCollider == null)
        {
            return;
        }
        if (findTries < 3 && Time.time >= retryAt && (eruptionTrigger == null || moonCol == null))
        {
            FindObjects();
        }

        bool atTip = eruptionTrigger != null && eruptionTrigger.IsTouching(player.BodyCollider);
        bool high = player.BodyRb != null && player.BodyRb.position.y >= ending.floatStartY;
        if (phase == 0 && !moonDone && !MainMenu.Showing && (atTip || high))
        {
            phase = 1;
            StartCoroutine(Ascend());
        }
        if (floating)
        {
            MoveFloat(Time.fixedDeltaTime);
            if (ending.useEndingVideos && floatTime >= ending.videoStartTime && video != null && video.HasFiles)
            {
                floating = false;
                moonDone = true;
                phase = 2;
                StartCoroutine(VideoEnding());
            }
        }
        if (!moonDone && moonCol != null)
        {
            bool bodyTouch = moonCol.IsTouching(player.BodyCollider);
            bool headTouch = player.HeadCollider != null && moonCol.IsTouching(player.HeadCollider);
            if (bodyTouch || headTouch)
            {
                moonDone = true;
                floating = false;
                phase = 2;
                StartCoroutine(MoonEnding());
            }
        }
    }

    void UpdateShake()
    {
        if (follow == null)
        {
            return;
        }
        if (shakeAmount > 0.0001f)
        {
            follow.shakeOffset = Random.insideUnitCircle * shakeAmount;
            shaking = true;
        }
        else if (shaking)
        {
            ClearShake();
        }
    }

    void ClearShake()
    {
        shakeAmount = 0f;
        shaking = false;
        if (follow != null)
        {
            follow.shakeOffset = Vector2.zero;
        }
    }


    static float Smooth(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    IEnumerator Ascend()
    {
        EndingSettings e = ending;
        player.Frozen = true;
        overlay.LetterboxFraction = e.letterboxPercent;

        floatStart = player.BodyRb.position;
        floatY = floatStart.y;
        floatAngle = 0f;
        floatTime = 0f;
        floatCentreX = ClearX(floatStart);
        IgnoreRock(true);
        hammerOffset = player.HammerRb != null ? player.HammerRb.position - player.BodyRb.position : Vector2.up;
        hammerStartRotation = player.HammerRb != null ? player.HammerRb.rotation : 0f;
        floating = true;
        if (glow != null)
        {
            glow.Show(true);
        }

        for (float t = 0f; t < e.letterboxTime; t += Time.deltaTime)
        {
            overlay.Letterbox = Smooth(t / Mathf.Max(0.05f, e.letterboxTime));
            yield return null;
        }
        overlay.Letterbox = 1f;
    }

    void MoveFloat(float dt)
    {
        EndingSettings e = ending;
        Rigidbody2D rb = player.BodyRb;
        if (rb == null)
        {
            return;
        }
        floatTime += dt;
        float ramp = Smooth(floatTime / Mathf.Max(0.05f, e.ascendRampTime));
        floatY += e.ascendSpeed * ramp * dt;
        floatAngle += e.tumbleDegreesPerSecond * ramp * dt;
        float slide = Smooth(floatTime / Mathf.Max(0.05f, e.ascendCentreTime));
        Vector2 pos = new Vector2(Mathf.Lerp(floatStart.x, floatCentreX, slide), floatY);

        rb.position = pos;
        rb.rotation = floatAngle;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        Rigidbody2D hammer = player.HammerRb;
        if (hammer != null)
        {
            hammer.position = pos + (Vector2)(Quaternion.Euler(0f, 0f, floatAngle) * hammerOffset);
            hammer.rotation = hammerStartRotation + floatAngle;
            hammer.linearVelocity = Vector2.zero;
            hammer.angularVelocity = 0f;
        }
    }


    static readonly RaycastHit2D[] pathHits = new RaycastHit2D[16];
    static readonly Collider2D[] spotHits = new Collider2D[16];
    readonly List<Collider2D> ignored = new List<Collider2D>(512);
    readonly List<Collider2D> levelColliders = new List<Collider2D>(512);

    bool IsPlayerCollider(Collider2D c)
    {
        if (c == null)
        {
            return true;
        }
        if (c == player.BodyCollider || c == player.HeadCollider)
        {
            return true;
        }
        Rigidbody2D body = c.attachedRigidbody;
        return body != null && (body == player.BodyRb || body == player.HammerRb);
    }

    void PrepareFilter()
    {
        if (!rayFilterReady)
        {
            rayFilter = new ContactFilter2D();
            rayFilter.useTriggers = false;
            rayFilterReady = true;
        }
    }

    bool Blocked(Vector2 origin, Vector2 direction, float distance)
    {
        PrepareFilter();
        Bounds b = player.BodyCollider.bounds;
        int n = Physics2D.CapsuleCast(origin, b.size, CapsuleDirection2D.Vertical, 0f, direction, rayFilter, pathHits, distance);
        for (int i = 0; i < n; i++)
        {
            if (!IsPlayerCollider(pathHits[i].collider))
            {
                return true;
            }
        }
        return false;
    }

    bool InRock(Vector2 centre)
    {
        PrepareFilter();
        Bounds b = player.BodyCollider.bounds;
        int n = Physics2D.OverlapCapsule(centre, b.size, CapsuleDirection2D.Vertical, 0f, rayFilter, spotHits);
        for (int i = 0; i < n; i++)
        {
            if (!IsPlayerCollider(spotHits[i]))
            {
                return true;
            }
        }
        return false;
    }

    float ClearX(Vector2 position)
    {
        float up = 140f;
        Vector2 offset = (Vector2)player.BodyCollider.bounds.center - position;
        if (!Blocked(position + offset, Vector2.up, up))
        {
            return position.x;
        }
        for (float d = 0.25f; d <= 4f; d += 0.25f)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                Vector2 spot = position + new Vector2(side * d, 0f);
                if (!InRock(spot + offset) && !Blocked(position + offset, new Vector2(side, 0f), d) && !Blocked(spot + offset, Vector2.up, up))
                {
                    Debug.Log("Ending: the way up was blocked, the rat is moved " + (side * d).ToString("0.00") + " sideways in free space.");
                    return spot.x;
                }
            }
        }
        Debug.LogWarning("Ending: no clear way straight up within 4 units, the float starts from where the rat is.");
        return position.x;
    }

    void IgnoreRock(bool on)
    {
        Collider2D body = player.BodyCollider;
        Collider2D head = player.HeadCollider;
        if (on)
        {
            GameObject level = EndingFind.ByName("Level");
            levelColliders.Clear();
            if (level != null)
            {
                level.GetComponentsInChildren<Collider2D>(false, levelColliders);
            }
            for (int i = 0; i < levelColliders.Count; i++)
            {
                Collider2D c = levelColliders[i];
                if (c == null || c.isTrigger || IsPlayerCollider(c))
                {
                    continue;
                }
                if (body != null)
                {
                    Physics2D.IgnoreCollision(body, c, true);
                }
                if (head != null)
                {
                    Physics2D.IgnoreCollision(head, c, true);
                }
                ignored.Add(c);
            }
            return;
        }
        for (int i = 0; i < ignored.Count; i++)
        {
            Collider2D c = ignored[i];
            if (c == null)
            {
                continue;
            }
            if (body != null)
            {
                Physics2D.IgnoreCollision(body, c, false);
            }
            if (head != null)
            {
                Physics2D.IgnoreCollision(head, c, false);
            }
        }
        ignored.Clear();
    }


    IEnumerator VideoEnding()
    {
        float runTime = MainMenu.RunSeconds();
        video.Begin();
        while (!video.Done)
        {
            yield return null;
        }
        if (glow != null)
        {
            glow.Show(false);
        }
        overlay.Letterbox = 0f;
        overlay.Fade = 1f;
        video.Finish();
        string[] lines = { "I woke up.", "The whole climb was a dream." };
        for (int i = 0; i < lines.Length; i++)
        {
            for (int c = 1; c <= lines[i].Length; c++)
            {
                overlay.SetStoryLine(lines[i].Substring(0, c));
                yield return new WaitForSecondsRealtime(1f / 28f);
            }
            yield return new WaitForSecondsRealtime(2f);
            overlay.SetStoryLine("");
            yield return new WaitForSecondsRealtime(0.6f);
        }
        int minutes = Mathf.FloorToInt(runTime / 60f);
        float seconds = runTime - minutes * 60f;
        player.Frozen = true;
        overlay.ShowCard("Time  " + minutes + ":" + seconds.ToString("00.00"));
    }


    IEnumerator MoonEnding()
    {
        EndingSettings e = ending;
        overlay.TriggerFlash(e.moonFlashTime);
        IgnoreRock(false);
        if (glow != null)
        {
            glow.Show(false);
        }

        Vector2 away = Vector2.down;
        if (moonCol != null && player.BodyRb != null)
        {
            Vector2 d = player.BodyRb.position - (Vector2)moonCol.bounds.center;
            if (d.sqrMagnitude > 0.0001f)
            {
                away = d.normalized;
            }
        }
        player.Kick(away * e.moonKickSpeed);

        float bars = overlay.Letterbox;
        for (float t = 0f; t < e.moonShakeTime; t += Time.deltaTime)
        {
            shakeAmount = e.moonShakeAmount * (1f - t / Mathf.Max(0.05f, e.moonShakeTime));
            overlay.Letterbox = bars * (1f - Smooth(t / Mathf.Max(0.05f, e.letterboxTime)));
            yield return null;
        }
        overlay.Letterbox = 0f;
        ClearShake();

        float runTime = MainMenu.RunSeconds();
        for (float t = 0f; t < e.fadeTime; t += Time.deltaTime)
        {
            overlay.Fade = Smooth(t / Mathf.Max(0.05f, e.fadeTime));
            yield return null;
        }
        overlay.Fade = 1f;

        int minutes = Mathf.FloorToInt(runTime / 60f);
        float seconds = runTime - minutes * 60f;
        player.Frozen = true;
        overlay.ShowCard("Time  " + minutes + ":" + seconds.ToString("00.00"));
    }
}
