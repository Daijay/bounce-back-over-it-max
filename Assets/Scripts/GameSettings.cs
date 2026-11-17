using UnityEngine;

public static class GameSettings
{
    public const int TargetFrameRate = 60;
    public const float FixedStep = 1f / 120f;
    public const float MaxFrameTime = 0.1f;

    public const int VelocityIterations = 8;
    public const int PositionIterations = 3;
    public const float Gravity = -9.81f;
    public const float DefaultFriction = 1f;
    public const float DefaultBounciness = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Apply()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = TargetFrameRate;
        Time.fixedDeltaTime = FixedStep;
        Time.maximumDeltaTime = MaxFrameTime;
        Physics2D.simulationMode = SimulationMode2D.FixedUpdate;
        Physics2D.gravity = new Vector2(0f, Gravity);
        Physics2D.velocityIterations = VelocityIterations;
        Physics2D.positionIterations = PositionIterations;
    }
}
