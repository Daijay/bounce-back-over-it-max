using UnityEngine;

public static class MapStyle
{
    public const float PixelWorld = 0.0301f;
    public const float PixelsPerUnit = 1f / PixelWorld;

    public const int OrderLava = 1;
    public const int OrderRock = 2;
    public const int OrderRockShade = 3;
    public const int OrderSolid = 4;
    public const int OrderDecor = 5;
    public const int OrderLavaFront = 7;
    public const int OrderBubbles = 8;
    public const int OrderSnow = 18;
    public const int OrderBoundary = 30;

    public static float Snap(float value)
    {
        return Mathf.Round(value / PixelWorld) * PixelWorld;
    }
}
