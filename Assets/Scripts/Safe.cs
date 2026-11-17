using UnityEngine;

public static class Safe
{
    public static bool Bad(float f)
    {
        return float.IsNaN(f) || float.IsInfinity(f);
    }

    public static bool Bad(Vector2 v)
    {
        return Bad(v.x) || Bad(v.y);
    }
}
