using System.Collections.Generic;
using UnityEngine;

public static class MapRockDetail
{

    public static List<Vector2> AddSteps(IList<Vector2> face, int seed, float stepsPerUnit)
    {
        System.Random rng = new System.Random(seed);
        List<Vector2> result = new List<Vector2>();
        int lastStepSegment = -10;
        for (int i = 0; i < face.Count; i++)
        {
            result.Add(face[i]);
            if (i == face.Count - 1)
            {
                break;
            }

            Vector2 a = face[i];
            Vector2 b = face[i + 1];
            float length = (b - a).magnitude;
            if (length < 1.7f || i - lastStepSegment < 2)
            {
                continue;
            }
            if (rng.NextDouble() > stepsPerUnit * length)
            {
                continue;
            }

            Vector2 dir = (b - a) / length;
            Vector2 outward = new Vector2(-dir.y, dir.x);
            float start = Range(rng, 0.3f, 0.5f) * length;
            float width = Mathf.Min(Range(rng, 0.55f, 0.95f), length - start - 0.3f);
            if (width < 0.45f)
            {
                continue;
            }

            bool notch = rng.NextDouble() < 0.33;
            float height = notch ? -Range(rng, 0.28f, 0.42f) : Range(rng, 0.3f, 0.55f);
            Vector2 p0 = a + dir * start;
            Vector2 p1 = p0 + outward * height;
            Vector2 p2 = p1 + dir * width;
            Vector2 p3 = p0 + dir * width;
            result.Add(p0);
            result.Add(p1);
            result.Add(p2);
            result.Add(p3);
            lastStepSegment = i;
        }
        return result;
    }

    static float Range(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    public static List<Vector2> AddStepsNear(IList<Vector2> face, IList<Vector2> targets, int seed)
    {
        System.Random rng = new System.Random(seed);
        bool[] used = new bool[Mathf.Max(1, face.Count - 1)];
        Dictionary<int, Vector2> stepAt = new Dictionary<int, Vector2>();
        for (int t = 0; t < targets.Count; t++)
        {
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i + 1 < face.Count; i++)
            {
                if (used[i] || (face[i + 1] - face[i]).magnitude < 1.0f)
                {
                    continue;
                }
                float distance = DistanceToSegment(targets[t], face[i], face[i + 1]);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            if (best >= 0)
            {
                used[best] = true;
                stepAt[best] = targets[t];
            }
        }

        List<Vector2> result = new List<Vector2>();
        for (int i = 0; i < face.Count; i++)
        {
            result.Add(face[i]);
            Vector2 target;
            if (i + 1 >= face.Count || !stepAt.TryGetValue(i, out target))
            {
                continue;
            }

            Vector2 a = face[i];
            Vector2 b = face[i + 1];
            float length = (b - a).magnitude;
            Vector2 dir = (b - a) / length;
            Vector2 outward = new Vector2(-dir.y, dir.x);
            float width = Mathf.Min(Range(rng, 0.6f, 0.85f), length - 0.4f);
            float along = Vector2.Dot(target - a, dir);
            float start = Mathf.Clamp(along - width * 0.5f, 0.2f, length - width - 0.2f);
            float height = Range(rng, 0.42f, 0.55f);
            Vector2 p0 = a + dir * start;
            Vector2 p1 = p0 + outward * height;
            Vector2 p2 = p1 + dir * width;
            Vector2 p3 = p0 + dir * width;
            result.Add(p0);
            result.Add(p1);
            result.Add(p2);
            result.Add(p3);
        }
        return result;
    }


    public static readonly int[] BoulderPixels = { 32, 50, 76 };
    public const int Variants = 3;
    static readonly string[] SizeLetters = { "s", "m", "l" };

    public const float MaxRise = 2.2f;
    const float BoulderGapMin = 4.3f;
    const float BoulderGapMax = 4.4f;
    const float MaxArcFar = 7.5f;
    const float SinkFraction = 0.15f;

    public struct Boulder
    {
        public Vector2 centre;
        public int size;
        public int variant;
        public float rotation;
    }

    public class FacePlan
    {
        public List<Boulder> boulders = new List<Boulder>();
        public List<Vector2> stepTargets = new List<Vector2>();
        public List<float> footholds = new List<float>();
    }

    public static string BoulderName(int size, int variant)
    {
        return "boulder_" + SizeLetters[size] + variant;
    }

    public static int OutlineSeed(int size, int variant)
    {
        return 1 + size * 10 + variant;
    }

    static readonly Dictionary<int, Vector2[]> outlineCache = new Dictionary<int, Vector2[]>();

    public static Vector2[] Outline(int size, int variant)
    {
        int key = size * 10 + variant;
        Vector2[] points;
        if (outlineCache.TryGetValue(key, out points))
        {
            return points;
        }
        float[] pixels = MapTextureGen.BoulderOutline(BoulderPixels[size], OutlineSeed(size, variant));
        float half = BoulderPixels[size] * 0.5f;
        points = new Vector2[pixels.Length / 2];
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Vector2((pixels[i * 2] - half) * MapStyle.PixelWorld, (half - pixels[i * 2 + 1]) * MapStyle.PixelWorld);
        }
        outlineCache[key] = points;
        return points;
    }

    public static void Extent(Boulder boulder, out float top, out float bottom)
    {
        Vector2[] outline = Outline(boulder.size, boulder.variant);
        float radians = boulder.rotation * Mathf.Deg2Rad;
        float cos = Mathf.Cos(radians);
        float sin = Mathf.Sin(radians);
        top = float.MinValue;
        bottom = float.MaxValue;
        for (int i = 0; i < outline.Length; i++)
        {
            float y = outline[i].x * sin + outline[i].y * cos + boulder.centre.y;
            top = Mathf.Max(top, y);
            bottom = Mathf.Min(bottom, y);
        }
    }

    public static float Top(Boulder boulder)
    {
        float top;
        float bottom;
        Extent(boulder, out top, out bottom);
        return top;
    }

    public static FacePlan PlanFace(IList<Vector2> control, int seed, Rect skip, bool startBoulder, float maxRise = MaxRise, float gapScale = 1f)
    {
        System.Random rng = new System.Random(seed);
        FacePlan plan = new FacePlan();
        float total = PathLength(control);

        float previousArc = 0f;
        float previousTop = control[0].y + 0.8f;
        if (startBoulder)
        {
            Boulder first = Make(control, 0.5f, PickSize(rng), rng.Next(Variants), rng);
            previousArc = 0.5f;
            previousTop = Top(first);
            plan.footholds.Add(previousTop);
            if (!skip.Contains(first.centre))
            {
                plan.boulders.Add(first);
            }
        }
        else
        {
            plan.footholds.Add(previousTop);
        }

        while (previousArc < total - 0.6f)
        {
            float gap = Range(rng, BoulderGapMin * gapScale, BoulderGapMax * gapScale);
            int size = PickSize(rng);
            int variant = rng.Next(Variants);
            float limit = Mathf.Min(total - 0.3f, previousArc + MaxArcFar);

            float arc = previousArc + 1.5f;
            Boulder candidate = Make(control, Mathf.Min(arc, limit), size, variant, rng);
            for (; arc <= limit; arc += 0.05f)
            {
                candidate = Make(control, arc, size, variant, rng, candidate.rotation);
                if (Mathf.Abs(Top(candidate) - previousTop) >= gap)
                {
                    break;
                }
            }
            arc = Mathf.Min(arc, limit);
            candidate = Make(control, arc, size, variant, rng, candidate.rotation);

            float candidateTop = Top(candidate);
            float difference = Mathf.Abs(candidateTop - previousTop);
            if (arc >= total - 0.35f && difference < maxRise)
            {
                break;
            }

            int steps = Mathf.Max(0, Mathf.CeilToInt(difference / maxRise) - 1);
            for (int i = 1; i <= steps; i++)
            {
                float wanted = previousTop + (candidateTop - previousTop) * i / (steps + 1) - 0.3f;
                float bestArc = previousArc;
                float bestError = float.MaxValue;
                for (float t = previousArc + 0.6f; t <= arc - 0.6f; t += 0.05f)
                {
                    Vector2 p;
                    Vector2 d;
                    PointAt(control, t, out p, out d);
                    float error = Mathf.Abs(p.y - wanted);
                    if (error < bestError)
                    {
                        bestError = error;
                        bestArc = t;
                    }
                }
                Vector2 point;
                Vector2 direction;
                PointAt(control, bestArc, out point, out direction);
                plan.footholds.Add(point.y + 0.3f);
                if (!skip.Contains(point))
                {
                    plan.stepTargets.Add(point);
                }
            }

            plan.footholds.Add(candidateTop);
            if (!skip.Contains(candidate.centre))
            {
                plan.boulders.Add(candidate);
            }
            previousTop = candidateTop;
            previousArc = arc;
        }
        return plan;
    }

    public static FacePlan PlanSlope(IList<Vector2> face, int seed, float rise, Rect skip)
    {
        System.Random rng = new System.Random(seed);
        FacePlan plan = new FacePlan();
        float total = PathLength(face);
        float previousTop = face[0].y;
        plan.footholds.Add(previousTop);
        int size = rng.NextDouble() < 0.6 ? 0 : 1;
        int variant = rng.Next(Variants);
        float rotation = Range(rng, -8f, 8f);
        for (float arc = 1f; arc < total - 0.4f; arc += 0.05f)
        {
            Boulder candidate = Make(face, arc, size, variant, rng, rotation);
            float top = Top(candidate);
            if (top - previousTop < rise)
            {
                continue;
            }
            if (!skip.Contains(candidate.centre))
            {
                plan.boulders.Add(candidate);
            }
            plan.footholds.Add(top);
            previousTop = top;
            size = rng.NextDouble() < 0.6 ? 0 : 1;
            variant = rng.Next(Variants);
            rotation = Range(rng, -8f, 8f);
        }
        return plan;
    }

    public static float LargestGap(List<float> footholds)
    {
        float largest = 0f;
        for (int i = 1; i < footholds.Count; i++)
        {
            largest = Mathf.Max(largest, Mathf.Abs(footholds[i] - footholds[i - 1]));
        }
        return largest;
    }

    static Boulder Make(IList<Vector2> control, float arc, int size, int variant, System.Random rng, float rotation = float.NaN)
    {
        Vector2 point;
        Vector2 dir;
        PointAt(control, arc, out point, out dir);
        Vector2 outward = new Vector2(-dir.y, dir.x);

        Boulder boulder = new Boulder();
        boulder.size = size;
        boulder.variant = variant;
        boulder.rotation = float.IsNaN(rotation) ? Range(rng, -8f, 8f) : rotation;
        boulder.centre = point + outward * (BoulderPixels[size] * MapStyle.PixelWorld * 0.5f * SinkFraction);
        return boulder;
    }

    static int PickSize(System.Random rng)
    {
        double roll = rng.NextDouble();
        return roll < 0.25 ? 0 : (roll < 0.7 ? 1 : 2);
    }

    static float PathLength(IList<Vector2> path)
    {
        float total = 0f;
        for (int i = 0; i + 1 < path.Count; i++)
        {
            total += (path[i + 1] - path[i]).magnitude;
        }
        return total;
    }

    static void PointAt(IList<Vector2> path, float arc, out Vector2 point, out Vector2 dir)
    {
        float walked = 0f;
        for (int i = 0; i + 1 < path.Count; i++)
        {
            float length = (path[i + 1] - path[i]).magnitude;
            if (arc <= walked + length || i == path.Count - 2)
            {
                dir = length > 0f ? (path[i + 1] - path[i]) / length : Vector2.right;
                point = path[i] + dir * Mathf.Clamp(arc - walked, 0f, length);
                return;
            }
            walked += length;
        }
        point = path[0];
        dir = Vector2.right;
    }


    public const float ClearDepth = 6.5f;
    public const float FadeDepth = 24f;
    public const float MaxAlpha = 0.93f;
    const float MaxEdge = 2.2f;

    public static Mesh ShadeMesh(Mesh triangulated, IList<Vector2> polygon, string name)
    {
        List<Vector2[]> edges = new List<Vector2[]>();
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];
            if (a.y > -9.5f && b.y > -9.5f)
            {
                edges.Add(new Vector2[] { a, b });
            }
        }

        Vector3[] sourceVerts = triangulated.vertices;
        int[] sourceTris = triangulated.triangles;
        List<Vector2> verts = new List<Vector2>();
        for (int t = 0; t + 2 < sourceTris.Length; t += 3)
        {
            Split(new Vector2(sourceVerts[sourceTris[t]].x, sourceVerts[sourceTris[t]].y),
                new Vector2(sourceVerts[sourceTris[t + 1]].x, sourceVerts[sourceTris[t + 1]].y),
                new Vector2(sourceVerts[sourceTris[t + 2]].x, sourceVerts[sourceTris[t + 2]].y), verts);
        }

        Vector3[] positions = new Vector3[verts.Count];
        Color32[] colours = new Color32[verts.Count];
        int[] triangles = new int[verts.Count];
        for (int i = 0; i < verts.Count; i++)
        {
            positions[i] = new Vector3(verts[i].x, verts[i].y, 0f);
            triangles[i] = i;
            colours[i] = new Color32(6, 9, 18, (byte)Mathf.RoundToInt(255f * AlphaAt(verts[i], edges)));
        }

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.indexFormat = verts.Count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = positions;
        mesh.colors32 = colours;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        return mesh;
    }

    public static float AlphaAt(Vector2 point, List<Vector2[]> surface)
    {
        float best = float.MaxValue;
        for (int i = 0; i < surface.Count; i++)
        {
            best = Mathf.Min(best, DistanceToSegment(point, surface[i][0], surface[i][1]));
        }
        float t = Mathf.Clamp01((best - ClearDepth) / FadeDepth);
        t = t * t * (3f - 2f * t);
        return MaxAlpha * t;
    }

    static float DistanceToSegment(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSquared = ab.sqrMagnitude;
        float t = lengthSquared > 0f ? Mathf.Clamp01(Vector2.Dot(p - a, ab) / lengthSquared) : 0f;
        return (p - (a + ab * t)).magnitude;
    }

    static void Split(Vector2 a, Vector2 b, Vector2 c, List<Vector2> output)
    {
        float longest = Mathf.Max((a - b).magnitude, Mathf.Max((b - c).magnitude, (c - a).magnitude));
        if (longest <= MaxEdge)
        {
            output.Add(a);
            output.Add(b);
            output.Add(c);
            return;
        }
        Vector2 ab = (a + b) * 0.5f;
        Vector2 bc = (b + c) * 0.5f;
        Vector2 ca = (c + a) * 0.5f;
        Split(a, ab, ca, output);
        Split(ab, b, bc, output);
        Split(ca, bc, c, output);
        Split(ab, bc, ca, output);
    }
}
