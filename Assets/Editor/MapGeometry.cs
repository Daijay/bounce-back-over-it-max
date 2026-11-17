using System.Collections.Generic;
using UnityEngine;

public static class MapGeometry
{
    public static List<Vector2> BumpyPath(IList<Vector2> control, int seed, float bumpiness)
    {
        List<Vector2> result = new List<Vector2>();
        System.Random rng = new System.Random(seed);

        if (control == null || control.Count == 0)
        {
            return result;
        }

        if (bumpiness <= 0.001f)
        {
            result.AddRange(control);
            return result;
        }

        if (control.Count == 1)
        {
            result.Add(control[0]);
            return result;
        }

        float maxOffset = 0.7f * bumpiness;
        float minStep = 0.2f * bumpiness;
        float maxStep = 0.5f * bumpiness;

        for (int s = 0; s < control.Count - 1; s++)
        {
            Vector2 a = control[s];
            Vector2 b = control[s + 1];
            Vector2 seg = b - a;
            float len = seg.magnitude;
            Vector2 dir = len > 0f ? seg / len : Vector2.zero;
            Vector2 normal = new Vector2(-dir.y, dir.x);

            if (len < 1.6f)
            {
                AppendPoint(result, a);
                AppendPoint(result, b);
                continue;
            }

            List<float> bounds = new List<float>();
            bounds.Add(0f);
            float pos = 0f;
            while (true)
            {
                float rem = len - pos;
                if (rem <= 2.0f && bounds.Count > 1)
                {
                    break;
                }

                float pieceLen = NextRange(rng, 0.8f, 2.0f);
                if (rem - pieceLen < 0.8f)
                {
                    pieceLen = rem - 0.8f;
                }

                pos += pieceLen;
                bounds.Add(pos);
            }
            bounds.Add(len);

            int k = bounds.Count - 1;
            float[] offsets = new float[k];
            offsets[0] = 0f;
            offsets[k - 1] = 0f;
            for (int i = 1; i < k - 1; i++)
            {
                float step = NextRange(rng, minStep, maxStep);
                int sign = rng.Next(2) == 0 ? 1 : -1;
                float cand = offsets[i - 1] + sign * step;
                if (cand > maxOffset || cand < -maxOffset)
                {
                    sign = -sign;
                    cand = offsets[i - 1] + sign * step;
                }

                offsets[i] = Mathf.Clamp(cand, -maxOffset, maxOffset);
            }

            for (int i = 0; i < k; i++)
            {
                Vector2 p0 = (i == 0) ? a : a + dir * bounds[i];
                Vector2 p1 = (i == k - 1) ? b : a + dir * bounds[i + 1];
                Vector2 off = normal * offsets[i];
                AppendPoint(result, p0 + off);
                AppendPoint(result, p1 + off);
            }
        }

        return result;
    }
    public static float SignedArea(IList<Vector2> poly)
    {
        float sum = 0f;
        int n = poly.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = poly[i];
            Vector2 b = poly[(i + 1) % n];
            sum += a.x * b.y - b.x * a.y;
        }

        return sum * 0.5f;
    }

    public static Mesh CreateTiledMesh(IList<Vector2> polygon, float tileWorldSize, string name)
    {
        List<Vector2> poly = new List<Vector2>(polygon);
        float tile = tileWorldSize > 0f ? tileWorldSize : 1f;
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        bool usedUnity = false;
        Mesh unityMesh = null;
        GameObject temp = new GameObject("MapGeometry_TriangulationTemp");
        try
        {
            PolygonCollider2D pc = temp.AddComponent<PolygonCollider2D>();
            pc.SetPath(0, poly.ToArray());
            unityMesh = pc.CreateMesh(false, false);
        }
        catch (System.Exception)
        {
            unityMesh = null;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(temp);
        }

        if (unityMesh != null)
        {
            int[] ut = unityMesh.triangles;
            if (ut != null && ut.Length >= 3)
            {
                Vector3[] uv3 = unityMesh.vertices;
                for (int i = 0; i < uv3.Length; i++)
                {
                    verts.Add(new Vector3(uv3[i].x, uv3[i].y, 0f));
                }

                tris.AddRange(ut);
                usedUnity = true;
            }

            UnityEngine.Object.DestroyImmediate(unityMesh);
        }

        if (!usedUnity)
        {
            verts.Clear();
            tris.Clear();
            for (int i = 0; i < poly.Count; i++)
            {
                verts.Add(new Vector3(poly[i].x, poly[i].y, 0f));
            }

            tris.AddRange(EarClip(poly));
        }

        for (int t = 0; t + 2 < tris.Count; t += 3)
        {
            Vector3 a = verts[tris[t]];
            Vector3 b = verts[tris[t + 1]];
            Vector3 c = verts[tris[t + 2]];
            float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            if (cross > 0f)
            {
                int tmp = tris[t + 1];
                tris[t + 1] = tris[t + 2];
                tris[t + 2] = tmp;
            }
        }

        Vector2[] uvs = new Vector2[verts.Count];
        for (int i = 0; i < verts.Count; i++)
        {
            uvs[i] = new Vector2(verts[i].x / tile, verts[i].y / tile);
        }

        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.indexFormat = verts.Count > 65535
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.vertices = verts.ToArray();
        mesh.uv = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateBounds();
        return mesh;
    }
    static int[] EarClip(List<Vector2> pts)
    {
        const float eps = 1e-6f;
        List<int> result = new List<int>();
        int n = pts.Count;
        if (n < 3)
        {
            return result.ToArray();
        }

        List<int> cur = new List<int>(n);
        for (int i = 0; i < n; i++)
        {
            cur.Add(i);
        }

        if (SignedArea(pts) > 0f)
        {
            cur.Reverse();
        }

        int guard = 0;
        int maxIter = n * n + 10;
        while (cur.Count > 3 && guard < maxIter)
        {
            guard++;
            int m = cur.Count;
            bool clipped = false;

            for (int i = 0; i < m; i++)
            {
                int ip = (i - 1 + m) % m;
                int inx = (i + 1) % m;
                Vector2 a = pts[cur[ip]];
                Vector2 b = pts[cur[i]];
                Vector2 c = pts[cur[inx]];
                float turn = Cross(b - a, c - b);

                if (Mathf.Abs(turn) <= eps)
                {
                    cur.RemoveAt(i);
                    clipped = true;
                    break;
                }

                if (turn > 0f)
                {
                    continue;
                }

                if (AnyPointInside(pts, cur, ip, i, inx, a, b, c))
                {
                    continue;
                }

                result.Add(cur[ip]);
                result.Add(cur[i]);
                result.Add(cur[inx]);
                cur.RemoveAt(i);
                clipped = true;
                break;
            }

            if (!clipped)
            {
                result.Add(cur[m - 1]);
                result.Add(cur[0]);
                result.Add(cur[1]);
                cur.RemoveAt(0);
            }
        }

        if (cur.Count == 3)
        {
            result.Add(cur[0]);
            result.Add(cur[1]);
            result.Add(cur[2]);
        }

        return result.ToArray();
    }

    static bool AnyPointInside(List<Vector2> pts, List<int> cur, int ip, int i, int inx, Vector2 a, Vector2 b, Vector2 c)
    {
        const float eps = 1e-6f;
        for (int k = 0; k < cur.Count; k++)
        {
            if (k == ip || k == i || k == inx)
            {
                continue;
            }

            Vector2 p = pts[cur[k]];
            if (SamePoint(p, a) || SamePoint(p, b) || SamePoint(p, c))
            {
                continue;
            }

            float s1 = Cross(b - a, p - a);
            float s2 = Cross(c - b, p - b);
            float s3 = Cross(a - c, p - c);
            if (s1 <= eps && s2 <= eps && s3 <= eps)
            {
                return true;
            }
        }

        return false;
    }

    static float Cross(Vector2 u, Vector2 v)
    {
        return u.x * v.y - u.y * v.x;
    }

    static bool SamePoint(Vector2 p, Vector2 q)
    {
        return (p - q).sqrMagnitude < 1e-10f;
    }

    static void AppendPoint(List<Vector2> path, Vector2 p)
    {
        if (path.Count > 0 && (path[path.Count - 1] - p).magnitude < 0.001f)
        {
            return;
        }

        path.Add(p);
    }

    static float NextRange(System.Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
