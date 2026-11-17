using System.Collections.Generic;
using UnityEngine;

public static class MapSlope
{
    public const float StartX = 87.4f;
    public const float StartY = 25.65f;
    public const float Angle0 = 20f;
    public const float Angle1 = 44f;
    public const float Power = 1.15f;
    public const float PeakTarget = 100f;
    public const float CapLength = 16f;
    public const float EndAngle = -38f;
    public const float BoundaryGap = 14f;
    const float Step = 0.25f;

    static bool built;
    static List<Vector2> climb;
    static List<Vector2> face;
    static float peakX;
    static float peakY;
    static float boundaryX;

    public static float PeakX { get { Ensure(); return peakX; } }
    public static float PeakY { get { Ensure(); return peakY; } }
    public static float BoundaryX { get { Ensure(); return boundaryX; } }

    public static List<Vector2> Climb()
    {
        Ensure();
        return climb;
    }

    public static List<Vector2> Face()
    {
        Ensure();
        return face;
    }

    static void Ensure()
    {
        if (built)
        {
            return;
        }
        float low = 30f;
        float high = 500f;
        for (int i = 0; i < 60; i++)
        {
            float mid = (low + high) * 0.5f;
            List<Vector2> trial = Generate(mid, false);
            if (MaxY(trial) < PeakTarget)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }
        float length = (low + high) * 0.5f;
        face = Generate(length, true);
        climb = new List<Vector2>();
        int climbCount = Mathf.RoundToInt(length / Step) + 1;
        for (int i = 0; i < climbCount && i < face.Count; i++)
        {
            climb.Add(face[i]);
        }
        peakY = float.MinValue;
        for (int i = 0; i < face.Count; i++)
        {
            if (face[i].y > peakY)
            {
                peakY = face[i].y;
                peakX = face[i].x;
            }
        }
        boundaryX = peakX + BoundaryGap;
        Vector2 last = face[face.Count - 1];
        if (last.x < boundaryX)
        {
            face.Add(new Vector2(boundaryX, last.y + (boundaryX - last.x) * Mathf.Tan(EndAngle * Mathf.Deg2Rad)));
        }
        built = true;
    }

    static float MaxY(List<Vector2> points)
    {
        float top = float.MinValue;
        for (int i = 0; i < points.Count; i++)
        {
            top = Mathf.Max(top, points[i].y);
        }
        return top;
    }

    static List<Vector2> Generate(float climbLength, bool wobble)
    {
        List<Vector2> points = new List<Vector2>();
        Vector2 p = new Vector2(StartX, StartY);
        points.Add(p);
        int n = Mathf.Max(2, Mathf.RoundToInt(climbLength / Step));
        float ds = climbLength / n;
        for (int i = 0; i < n; i++)
        {
            float u = (i + 0.5f) / n;
            float angle = (Angle0 + (Angle1 - Angle0) * Mathf.Pow(u, Power)) * Mathf.Deg2Rad;
            p += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * ds;
            points.Add(p);
        }
        int climbEnd = points.Count - 1;
        int m = Mathf.RoundToInt(CapLength / Step);
        for (int i = 0; i < m; i++)
        {
            float u = (i + 0.5f) / m;
            u = u * u * (3f - 2f * u);
            float angle = (Angle1 + (EndAngle - Angle1) * u) * Mathf.Deg2Rad;
            p += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Step;
            points.Add(p);
        }
        int tail = Mathf.RoundToInt((BoundaryGap + 4f) / (Step * Mathf.Cos(EndAngle * Mathf.Deg2Rad)));
        for (int i = 0; i < tail; i++)
        {
            float angle = EndAngle * Mathf.Deg2Rad;
            p += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Step;
            points.Add(p);
        }
        if (wobble)
        {
            Wobble(points, climbEnd);
        }
        if (wobble)
        {
            float peak = MaxY(points);
            float peakAt = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                if (Mathf.Approximately(points[i].y, peak))
                {
                    peakAt = points[i].x;
                    break;
                }
            }
            for (int i = points.Count - 1; i > 0; i--)
            {
                if (points[i].x > peakAt + BoundaryGap)
                {
                    points.RemoveAt(i);
                }
                else
                {
                    break;
                }
            }
        }
        return points;
    }

    static void Wobble(List<Vector2> points, int climbEnd)
    {
        for (int i = 1; i < climbEnd; i++)
        {
            float arc = i * Step;
            float total = climbEnd * Step;
            float fade = Smooth01(arc / 8f) * Smooth01((total - arc) / 8f);
            float offset = (0.10f * Mathf.Sin(arc * (2f * Mathf.PI / 11f) + 0.7f) + 0.04f * Mathf.Sin(arc * (2f * Mathf.PI / 4.5f) + 2.1f)) * fade;
            Vector2 tangent = (points[i + 1] - points[i - 1]).normalized;
            Vector2 normal = new Vector2(-tangent.y, tangent.x);
            points[i] += normal * offset;
        }
    }

    static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }
}
