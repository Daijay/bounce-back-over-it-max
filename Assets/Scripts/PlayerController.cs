using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    public const float DefaultRigScale = 2.5f;

    public static PlayerController Instance { get; private set; }

    [Header("Rig")]
    public float rigScale = DefaultRigScale;
    public Rigidbody2D hammerBody;
    public PlayerVisuals visuals;

    public BodySettings body = new BodySettings();
    public HammerSettings hammer = new HammerSettings();
    public AimSettings aim = new AimSettings();
    public ContactSettings contact = new ContactSettings();
    public SuperJumpSettings superJump = new SuperJumpSettings();
    public LavaDeathSettings lavaDeath = new LavaDeathSettings();
    public LaunchReleaseSettings launchRelease = new LaunchReleaseSettings();
    public ArmStretchSettings armStretch = new ArmStretchSettings();
    public SurfaceSettings surfaces = new SurfaceSettings();
    public LimitSettings limits = new LimitSettings();
    public float startupSettleTime = 0.3f;

    public float CurrentHeight { get; private set; }
    public float BestHeight { get; private set; }
    public float ChargePool { get { return jumper != null ? jumper.Pool : 0f; } }
    public float ChargeMax { get { return Mathf.Max(0.01f, superJump.chargeMax); } }
    public float ChargeLevel { get { return Mathf.Clamp01(ChargePool / ChargeMax); } }
    public float MinJumpCharge { get { return superJump.superJumpMinCharge; } }
    public bool CanSuperJump { get { return jumper != null && jumper.CanJump; } }
    public bool SuperJumpActive { get { return jumper != null && jumper.Active; } }
    public bool Dizzy { get { return jumper != null && jumper.Dizzy; } }
    public float LavaProgress { get { return lava != null ? lava.Progress : 0f; } }
    public bool Frozen { get; set; }
    public float SkyGravityMultiplier = 1f;
    public Collider2D BodyCollider { get { return bodyCol; } }
    public Collider2D HeadCollider { get { return headCol; } }
    public float DizzyLeft { get { return jumper != null ? jumper.DizzyLeft : 0f; } }
    public float HammerAimError { get; private set; }
    public bool HammerRecovered { get; private set; }
    public Vector2 LastPull { get; private set; }
    public float Scale { get { return scale; } }

    public Rigidbody2D BodyRb { get { return rb; } }
    public Rigidbody2D HammerRb { get { return hammerBody; } }
    public HammerAim Aim { get { return aimer; } }
    public ContactTracker Contacts { get { return contacts; } }
    public SurfaceEffects SurfaceFx { get { return surfaceFx; } }
    public SuperJump Jumper { get { return jumper; } }

    Rigidbody2D rb;
    CapsuleCollider2D bodyCol;
    CircleCollider2D headCol;
    PhysicsMaterial2D headMat;
    HammerAim aimer;
    ContactTracker contacts;
    SuperJump jumper;
    LavaExposure lava;
    CutsceneDirector cutscene;
    SurfaceEffects surfaceFx;
    MotionLimits motion;
    HammerGuard guard;

    float scale = 1f;
    [SerializeField] float startY;
    float startupEnd;
    [SerializeField] Vector2 spawnPos;
    [SerializeField] float savedPool;
    float baseBodyGravity = 1f;
    float baseHeadGravity = 1f;
    bool jumpRequested;

    readonly BodyImpact[] impacts = new BodyImpact[8];
    int impactCount;

    bool InStartup { get { return Time.fixedTime < startupEnd; } }

    void Awake()
    {
        Instance = this;
        if (!InitRuntime(true))
        {
            return;
        }

        startY = rb.position.y;
        spawnPos = rb.position;
        startupEnd = Time.fixedTime + startupSettleTime;

        if (GetComponent<GlitchRecorder>() == null)
        {
            gameObject.AddComponent<GlitchRecorder>();
        }
        if (GetComponent<DizzyStars>() == null)
        {
            gameObject.AddComponent<DizzyStars>();
        }
        if (GetComponent<EndingOverlay>() == null)
        {
            gameObject.AddComponent<EndingOverlay>();
        }
        if (GetComponent<CutsceneDirector>() == null)
        {
            gameObject.AddComponent<CutsceneDirector>();
        }
        if (GetComponent<SkyStage>() == null)
        {
            gameObject.AddComponent<SkyStage>();
        }
        if (GetComponent<MainMenu>() == null)
        {
            gameObject.AddComponent<MainMenu>();
        }
        if (GetComponent<LavaEffects>() == null)
        {
            gameObject.AddComponent<LavaEffects>();
        }
        if (GetComponent<ChargeAura>() == null)
        {
            gameObject.AddComponent<ChargeAura>();
        }
    }

    void OnEnable()
    {
        if (!Application.isPlaying || aimer != null || hammerBody == null)
        {
            return;
        }
        Instance = this;
        InitRuntime(false);
    }

    bool InitRuntime(bool fresh)
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCol = GetComponent<CapsuleCollider2D>();

        if (hammerBody == null)
        {
            Debug.LogError("PlayerController: no hammer assigned. Run BounceBack > Build Test Scene.");
            enabled = false;
            return false;
        }
        headCol = hammerBody.GetComponent<CircleCollider2D>();

        ApplyRig();

        PhysicsMaterial2D bodyMat = new PhysicsMaterial2D("PlayerBody");
        bodyMat.friction = body.bodyFriction;
        bodyMat.bounciness = body.bodyBounciness;
        bodyCol.sharedMaterial = bodyMat;
        headMat = new PhysicsMaterial2D("HammerHead");
        headMat.friction = hammer.headFriction;
        headMat.bounciness = 0f;
        headCol.sharedMaterial = headMat;

        Vector2 startOffset;
        if (fresh)
        {
            float angle = hammer.startAimAngle * Mathf.Deg2Rad;
            startOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (hammer.startAimLength * hammer.reach * scale);
            hammerBody.position = rb.position + startOffset;
            hammerBody.rotation = hammer.startAimAngle;
            hammerBody.linearVelocity = Vector2.zero;
        }
        else
        {
            startOffset = hammerBody.position - rb.position;
        }

        aimer = new HammerAim(aim, startOffset);
        contacts = new ContactTracker(rb, hammerBody, contact);
        jumper = new SuperJump(superJump);
        lava = new LavaExposure(lavaDeath);
        if (!fresh)
        {
            jumper.SetPool(savedPool);
        }
        surfaceFx = new SurfaceEffects(rb, bodyCol, bodyMat, body, surfaces, scale);
        motion = new MotionLimits(rb, limits, scale);
        guard = new HammerGuard(hammerBody, headCol, limits, scale);
        return true;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void ApplyRig()
    {
        scale = Mathf.Max(0.05f, rigScale);
        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
        }
        if (bodyCol == null)
        {
            bodyCol = GetComponent<CapsuleCollider2D>();
        }
        if (headCol == null && hammerBody != null)
        {
            headCol = hammerBody.GetComponent<CircleCollider2D>();
        }

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.useAutoMass = false;
        rb.mass = body.bodyMass;
        baseBodyGravity = Mathf.Max(0f, body.gravity) / Mathf.Abs(GameSettings.Gravity);
        rb.gravityScale = baseBodyGravity;
        rb.linearDamping = body.bodyLinearDamping;
        rb.angularDamping = body.bodyAngularDamping;
        rb.constraints = body.freezeBodyRotation ? RigidbodyConstraints2D.FreezeRotation : RigidbodyConstraints2D.None;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.sleepMode = RigidbodySleepMode2D.NeverSleep;
        bodyCol.direction = CapsuleDirection2D.Vertical;
        bodyCol.size = body.bodySize * scale;
        bodyCol.offset = body.bodyOffset * scale;

        if (hammerBody == null || headCol == null)
        {
            return;
        }

        hammerBody.bodyType = RigidbodyType2D.Dynamic;
        hammerBody.useAutoMass = false;
        hammerBody.mass = hammer.headMass;
        baseHeadGravity = hammer.headGravityScale * scale;
        hammerBody.gravityScale = baseHeadGravity;
        hammerBody.linearDamping = 0f;
        hammerBody.angularDamping = 0.05f;
        hammerBody.constraints = RigidbodyConstraints2D.FreezeRotation;
        hammerBody.interpolation = RigidbodyInterpolation2D.Interpolate;
        hammerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        hammerBody.sleepMode = RigidbodySleepMode2D.NeverSleep;
        headCol.radius = hammer.headRadius * scale;
        headCol.offset = Vector2.zero;

        DistanceJoint2D rope = hammerBody.GetComponent<DistanceJoint2D>();
        if (rope == null)
        {
            rope = hammerBody.gameObject.AddComponent<DistanceJoint2D>();
        }
        rope.connectedBody = rb;
        rope.autoConfigureConnectedAnchor = false;
        rope.anchor = Vector2.zero;
        rope.connectedAnchor = Vector2.zero;
        rope.autoConfigureDistance = false;
        rope.distance = hammer.reach * scale;
        rope.maxDistanceOnly = true;
        rope.enableCollision = false;
        rope.breakForce = Mathf.Infinity;

        if (Application.isPlaying)
        {
            Physics2D.IgnoreCollision(headCol, bodyCol, true);
        }

        if (visuals != null)
        {
            visuals.Layout(scale);
        }
    }
    public void ReportLava(float dt)
    {
        if (lava != null)
        {
            lava.Touching(dt, Time.fixedTime);
        }
    }

    public void Die()
    {
        Vector2 deathSpot = rb.position;
        LavaEffects fx = GetComponent<LavaEffects>();
        if (fx != null)
        {
            fx.PlayBurn(new Vector3(deathSpot.x, deathSpot.y, transform.position.z), scale / DefaultRigScale);
        }

        lava.Reset();
        if (lavaDeath.resetChargeOnDeath)
        {
            jumper.Reset();
            savedPool = 0f;
        }
        else
        {
            jumper.CancelJump();
        }
        impactCount = 0;
        jumpRequested = false;
        startupEnd = Time.fixedTime + startupSettleTime;

        TeleportTo(spawnPos);

        CutsceneDirector director = GetComponent<CutsceneDirector>();
        if (director != null)
        {
            director.OnPlayerDied();
        }
    }

    public void TeleportTo(Vector2 pos)
    {
        impactCount = 0;
        jumpRequested = false;
        jumper.CancelJump();
        startupEnd = Time.fixedTime + startupSettleTime;

        rb.position = pos;
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        float angle = hammer.startAimAngle * Mathf.Deg2Rad;
        Vector2 startOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (hammer.startAimLength * hammer.reach * scale);
        hammerBody.position = pos + startOffset;
        hammerBody.rotation = hammer.startAimAngle;
        hammerBody.linearVelocity = Vector2.zero;
        hammerBody.angularVelocity = 0f;
        Physics2D.SyncTransforms();

        contacts = new ContactTracker(rb, hammerBody, contact);
        guard = new HammerGuard(hammerBody, headCol, limits, scale);

        GlitchRecorder recorder = GetComponent<GlitchRecorder>();
        if (recorder != null)
        {
            recorder.NotifyTeleport();
        }
    }
    public void Kick(Vector2 velocity)
    {
        if (rb == null || jumper == null)
        {
            return;
        }
        Frozen = false;
        jumper.CancelJump();
        impactCount = 0;
        rb.linearVelocity = velocity;
    }

    float FlyCap()
    {
        return jumper.Active ? superJump.superJumpSpeedCap : 0f;
    }

    void ApplyGravity()
    {
        float body = Frozen ? 0f : baseBodyGravity * SkyGravityMultiplier;
        if (!Mathf.Approximately(rb.gravityScale, body))
        {
            rb.gravityScale = body;
        }
        float head = Frozen ? 0f : baseHeadGravity;
        if (!Mathf.Approximately(hammerBody.gravityScale, head))
        {
            hammerBody.gravityScale = head;
        }
    }

    void ReleaseIfLaunching()
    {
        LaunchReleaseSettings l = launchRelease;
        if (!l.enabled || contacts.Blend <= 0f || jumper.Active)
        {
            return;
        }
        Vector2 toBody = rb.position - hammerBody.position;
        float dist = toBody.magnitude;
        if (dist < l.minReachFraction * hammer.reach * scale || dist < 0.0001f)
        {
            return;
        }
        Vector2 v = rb.linearVelocity;
        float away = Vector2.Dot(v, toBody / dist);
        bool release = away > l.launchReleaseSpeed;
        if (!release && away > l.launchReleaseSpeed * l.pulledBackSpeedFraction)
        {
            Vector2 toTarget = (hammerBody.position - aimer.Offset) - rb.position;
            release = Vector2.Dot(v, toTarget) < 0f;
        }
        if (release)
        {
            contacts.ForceRelease(Time.fixedTime, l.releaseHoldOff, l.releaseMaxHold);
        }
    }

    void LimitArmStretch()
    {
        ArmStretchSettings a = armStretch;
        if (jumper.Active)
        {
            return;
        }
        float reach = hammer.reach * scale;
        Vector2 toBody = rb.position - hammerBody.position;
        float dist = toBody.magnitude;
        if (dist <= reach || dist < 0.0001f)
        {
            return;
        }
        float excess = dist - reach;
        float maxExcess = Mathf.Max(0f, a.armMaxStretch) * reach;
        float allowed = Mathf.Min(excess * Mathf.Clamp01(a.armStretchScale), maxExcess);
        if (excess <= allowed + 0.0005f)
        {
            return;
        }
        float surplus = excess - allowed;
        float move = excess > maxExcess ? surplus : surplus * Mathf.Clamp01(a.armCorrectionPerStep);
        Vector2 dir = toBody / dist;
        if (contacts.Blend > 0f)
        {
            rb.position -= dir * move;
            float outward = Vector2.Dot(rb.linearVelocity, dir);
            if (outward > 0f)
            {
                rb.linearVelocity -= dir * outward;
            }
        }
        else
        {
            hammerBody.position += dir * move;
        }
    }

    bool DebugKeysOn()
    {
        if (cutscene == null)
        {
            cutscene = GetComponent<CutsceneDirector>();
        }
        return cutscene != null && cutscene.ending.debugKeys;
    }

    void Update()
    {
        if (!Frozen && MouseInput.SuperJumpPressed && !(MouseInput.ShiftHeld && DebugKeysOn()))
        {
            jumpRequested = true;
        }
    }

    void FixedUpdate()
    {
        if (aimer == null)
        {
            return;
        }
        float dt = Time.fixedDeltaTime;

        ApplyGravity();
        if (Frozen)
        {
            rb.linearVelocity = Vector2.zero;
            hammerBody.linearVelocity = Vector2.zero;
            impactCount = 0;
            jumpRequested = false;
            return;
        }
        if (lava.Tick(Time.fixedTime))
        {
            Die();
            return;
        }

        aimer.Sample();
        contacts.Refresh(dt);
        ReleaseIfLaunching();
        UpdateHeadFriction();
        aimer.Step(hammer.reach * scale, aim.aimDeadzoneRadius * scale, !contacts.HammerAirborne, contacts.Blend, dt);

        HammerRecovered = guard.Recover();
        LimitArmStretch();

        surfaceFx.PadSpeedCapOverride = FlyCap();
        HandleImpacts();
        if (jumpRequested)
        {
            jumpRequested = false;
            jumper.TryLaunch(rb, contacts, Time.fixedTime);
        }
        jumper.Tick(rb, Time.fixedTime);
        bool flying = jumper.Active;
        float pullOffset = PullBody(dt);
        surfaceFx.Sticky(contacts, pullOffset, flying, dt);
        surfaceFx.UpdateFriction();
        motion.CapSpeeds(flying ? FlyCap() : 0f);
        savedPool = jumper.Pool;

        CurrentHeight = rb.position.y - startY;
        BestHeight = Mathf.Max(BestHeight, CurrentHeight);
        MoveHammer(dt);
    }

    void UpdateHeadFriction()
    {
        float friction = hammer.headFriction;
        if (contacts.HammerOnIce)
        {
            float block = Mathf.Max(0.0001f, contacts.HammerIceFriction);
            float gripAngle = Mathf.Atan(Mathf.Max(0f, hammer.iceHeadGrip)) * Mathf.Rad2Deg;
            float bodyAngle = Mathf.Atan(Mathf.Sqrt(Mathf.Max(0f, body.bodyFriction) * block)) * Mathf.Rad2Deg;
            float holdAngle = Mathf.Lerp(gripAngle, bodyAngle, Mathf.Clamp01(hammer.iceHeadSlide));
            float grip = Mathf.Tan(Mathf.Min(holdAngle, 85f) * Mathf.Deg2Rad);
            friction = Mathf.Min(hammer.headFriction, grip * grip / block);
        }
        if (Mathf.Approximately(headMat.friction, friction))
        {
            return;
        }
        headMat.friction = friction;
        headCol.sharedMaterial = headMat;
    }

    float PullBody(float dt)
    {
        LastPull = Vector2.zero;
        float blend = contacts.Blend;
        if (blend <= 0f || jumper.Active)
        {
            return 0f;
        }

        Vector2 target = hammerBody.position - aimer.Offset;
        Vector2 toTarget = target - rb.position;
        if (Safe.Bad(toTarget))
        {
            return 0f;
        }

        Vector2 acceleration = toTarget * hammer.pullGain - rb.linearVelocity * hammer.pullDamping;
        Vector2 force = acceleration * (rb.mass * blend * jumper.PullMultiplier);
        rb.AddForce(force);
        LastPull = force;

        float cap = hammer.plantedSpeedLimit * scale;
        Vector2 v = rb.linearVelocity;
        if (v.sqrMagnitude > cap * cap)
        {
            rb.linearVelocity = v.normalized * cap;
        }
        return toTarget.magnitude;
    }

    void MoveHammer(float dt)
    {
        Vector2 bodyPos = rb.position;
        Vector2 headPos = hammerBody.position;
        Vector2 gap = bodyPos + aimer.Offset - headPos;
        if (Safe.Bad(gap))
        {
            hammerBody.linearVelocity = Vector2.zero;
            return;
        }
        HammerAimError = gap.magnitude;

        Vector2 move = Vector2.zero;
        if (!HammerRecovered && HammerAimError > hammer.restDistance * scale)
        {
            float follow = 1f - Mathf.Exp(-dt / Mathf.Max(0.001f, hammer.followTime));
            move = gap * follow;
            float maxStep = hammer.headRadius * scale * 2f * Mathf.Max(0.1f, limits.maxHammerStepDiameters);
            move = Vector2.ClampMagnitude(move, maxStep);
            move = guard.LimitMove(headPos, move);
        }
        hammerBody.linearVelocity = move / dt;

        Vector2 pointing = headPos + move - bodyPos;
        if (pointing.sqrMagnitude > 1e-6f)
        {
            hammerBody.rotation = Mathf.Atan2(pointing.y, pointing.x) * Mathf.Rad2Deg;
        }
    }

    void HandleImpacts()
    {
        for (int i = 0; i < impactCount; i++)
        {
            BodyImpact hit = impacts[i];
            surfaceFx.HandleImpact(hit);
            if (!InStartup)
            {
                Vector2 n = hit.normal;
                if (Vector2.Dot(n, rb.position - hit.point) < 0f)
                {
                    n = -n;
                }
                if (jumper.AddLanding(hit.speed, n))
                {
                    SoundEffects.PlayHitGround(hit.speed, superJump.chargeThresholdSpeed);
                }
            }
        }
        impactCount = 0;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        BlockType block;
        if (!BlockType.TryGet(collision.collider, out block))
        {
            return;
        }
        BlockKind kind = block.kind;
        if (jumper != null)
        {
            jumper.OnBodyContact(collision.collider, Time.fixedTime);
        }
        if (impactCount >= impacts.Length)
        {
            return;
        }

        ContactPoint2D contactPoint = collision.GetContact(0);
        BodyImpact hit;
        hit.speed = Mathf.Abs(Vector2.Dot(collision.relativeVelocity, contactPoint.normal));
        hit.normal = contactPoint.normal;
        hit.point = contactPoint.point;
        hit.kind = kind;
        impacts[impactCount++] = hit;
    }
}
