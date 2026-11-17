using UnityEngine;


[System.Serializable]
public class BodySettings
{
    [Header("Body (one rigidbody, a vertical capsule)")]
    public float bodyMass = 1f;
    public Vector2 bodySize = new Vector2(0.373f, 0.683f);
    public Vector2 bodyOffset = new Vector2(0f, -0.051f);
    public float gravity = 9.81f;
    public float bodyLinearDamping = 2f;
    public float airLinearDamping = 0f;
    public float bodyAngularDamping = 0.05f;
    public float bodyFriction = 0.05f;
    public float bodyBounciness = 0f;
    public bool freezeBodyRotation = true;
}

[System.Serializable]
public class HammerSettings
{
    [Header("Hammer head (a second, almost massless rigidbody on a rope)")]
    public float reach = 0.9f;
    public float headRadius = 0.0337f;
    public float headMass = 0.0001f;
    public float headGravityScale = 1f;
    public float headFriction = 100000f;
    public float iceHeadGrip = 1.2f;
    public float iceHeadSlide = 0.5f;

    [Header("How the hammer follows the cursor")]
    public float followTime = 0.09f;
    public float restDistance = 0.004f;
    public float startAimAngle = 45f;
    public float startAimLength = 0.7f;

    [Header("Pulling the body while the hammer is planted")]
    public float pullGain = 80f;
    public float pullDamping = 10f;
    public float plantedSpeedLimit = 6f;
}

[System.Serializable]
public class AimSettings
{
    public float cursorReachFraction = 0.5f;
    public float cursorSmoothTime = 0.02f;
    public float contactCursorSmoothTime = 0.06f;
    public float aimDeadzoneRadius = 0.25f;
    public float mouseStillPixels = 2f;
}

[System.Serializable]
public class ContactSettings
{
    public float contactGraceTime = 0.05f;
    [Header("Grip (the planted pull)")]
    public float gripGraceTime = 0.03f;
    public float contactSeparation = 0.02f;
    public float contactBlendTime = 0.05f;
    public float releaseBlendTime = 0.01f;
}

[System.Serializable]
public class SuperJumpSettings
{
    [Header("Yellow charge pool (filled only by landings, never decays)")]
    public float chargeMax = 100f;
    public float chargeThresholdSpeed = 10f;
    public float chargeExponent = 1.5f;
    public float chargeScale = 1.1f;
    public float landingMinNormalY = 0.3f;

    [Header("Super jump (Space spends the whole pool, straight up)")]
    public float superJumpMinCharge = 10f;
    public float superJumpMinSpeed = 6.24f;
    public float superJumpMaxSpeed = 20.8f;
    public float superJumpHorizontalKeep = 0.3f;
    public float superJumpSpeedCap = 22f;
    public float superJumpMaxDuration = 2f;

    [Header("Crash and dizzy")]
    public float liftoffGraceTime = 0.15f;
    public float crashDropSpeed = 8f;
    public float crashHorizontalKeep = 0.3f;
    public float dizzyDuration = 1.5f;
    public float dizzyPullMultiplier = 0.3f;
}

[System.Serializable]
public class ArmStretchSettings
{
    [Header("Arm / rope stretch (the head farther from the body than the reach)")]
    public float armStretchScale = 0.5f;
    public float armMaxStretch = 0.2f;
    public float armCorrectionPerStep = 0.5f;
}

[System.Serializable]
public class LaunchReleaseSettings
{
    [Header("Letting go of the stick when launching")]
    public bool enabled = true;
    public float launchReleaseSpeed = 6f;
    public float minReachFraction = 0.5f;
    public float pulledBackSpeedFraction = 0.5f;
    public float releaseHoldOff = 0.15f;
    public float releaseMaxHold = 0.5f;
}

[System.Serializable]
public class LavaDeathSettings
{
    public float lavaDeathSeconds = 2f;
    public float lavaBreakGrace = 0.3f;
    public bool resetChargeOnDeath = false;
    public Color burnTint = new Color(1f, 0.35f, 0.15f);
}

[System.Serializable]
public class SurfaceSettings
{
    [Header("Bounce pad (body only, the TOP face only)")]
    public float padTopNormalY = 0.7f;
    public float padFirstBounceMultiplier = 1.2f;
    public float padBounceDecay = 0.6f;
    public float padChainResetTime = 1.5f;
    public float maxPadSpeed = 7.2f;
    public float padMinImpactSpeed = 0.6f;

    [Header("Sticky (body only, the hammer treats purple as normal ground)")]
    public float stickyBreakawayOffset = 0.5f;
    public float stickyBodyFriction = 4f;
    public float stickyHoldDamping = 25f;
    public float stickyReleasedDamping = 4f;
    public float stickyHoldKeep = 0.05f;
    public float stickyBlendTime = 0.06f;
}

[System.Serializable]
public class LimitSettings
{
    [Header("Body speed")]
    public float safetyMaxSpeed = 60f;
    public float maxUpwardSpeed = 6f;

    [Header("No phasing through walls")]
    public float maxHammerStepDiameters = 1.5f;
    public float sweepSkin = 0f;
    public float penetrationTolerance = 0.015f;
}

[System.Serializable]
public class VisualSettings
{
    [Header("Shapes (rig units, primitives only)")]
    public Vector2 potSize = new Vector2(0.37f, 0.26f);
    public Vector2 potOffset = new Vector2(0f, -0.26f);
    public Vector2 torsoSize = new Vector2(0.28f, 0.26f);
    public Vector2 torsoOffset = new Vector2(0f, 0f);
    public float headDiameter = 0.22f;
    public Vector2 headOffset = new Vector2(-0.025f, 0.2f);
    public Vector2 shoulderOffset = new Vector2(0.087f, 0f);
    public float armThickness = 0.04f;
    public float shaftLength = 0.75f;
    public float shaftThickness = 0.025f;
    public float shaftCenterBack = 0.327f;
    public Vector2 hammerHeadSize = new Vector2(0.07f, 0.1f);
    public float handleBack = 0.563f;

    [Header("Colours")]
    public Color potColor = new Color(0.88f, 0.52f, 0.15f);
    public Color torsoColor = new Color(0.25f, 0.45f, 0.82f);
    public Color headColor = new Color(0.95f, 0.78f, 0.62f);
    public Color armColor = new Color(0.36f, 0.36f, 0.36f);
    public Color shaftColor = new Color(0.55f, 0.35f, 0.2f);
    public Color hammerHeadColor = new Color(0.85f, 0.12f, 0.12f);
}
