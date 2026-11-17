using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class MapCaps
{
    const float Sink = 0.05f;


    public static void IceCap(Transform parent, string name, IList<Vector2> path, float thickness, int order)
    {
        List<Vector2[]> pieces = new List<Vector2[]>();
        for (int i = 0; i + 1 < path.Count; i++)
        {
            AddQuad(pieces, path[i], path[i + 1], thickness, thickness);
            if (i > 0)
            {
                AddWedge(pieces, path[i - 1], path[i], path[i + 1], thickness);
            }
        }
        if (pieces.Count == 0)
        {
            return;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        MakeMeshRenderer(go, pieces, "ice", name, order);

        PolygonCollider2D shape = go.AddComponent<PolygonCollider2D>();
        shape.pathCount = pieces.Count;
        for (int p = 0; p < pieces.Count; p++)
        {
            shape.SetPath(p, pieces[p]);
        }
        BlockType block = go.AddComponent<BlockType>();
        block.kind = BlockKind.Blue;
        block.useArt = true;
    }

    public static void IceCapSmooth(Transform parent, string name, IList<Vector2> path, float minThickness, int order)
    {
        int n = path.Count;
        if (n < 2)
        {
            return;
        }
        Vector2[] normal = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            Vector2 sum = Vector2.zero;
            if (i > 0)
            {
                Vector2 d = (path[i] - path[i - 1]).normalized;
                sum += new Vector2(-d.y, d.x);
            }
            if (i + 1 < n)
            {
                Vector2 d = (path[i + 1] - path[i]).normalized;
                sum += new Vector2(-d.y, d.x);
            }
            normal[i] = sum.sqrMagnitude > 1e-6f ? sum.normalized : Vector2.up;
        }
        Vector2[] top = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            top[i] = path[i] + normal[i] * (minThickness + 0.05f);
        }
        for (int pass = 0; pass < 2; pass++)
        {
            Vector2[] copy = (Vector2[])top.Clone();
            for (int i = 1; i + 1 < n; i++)
            {
                top[i] = (copy[i - 1] + 2f * copy[i] + copy[i + 1]) * 0.25f;
            }
        }
        for (int i = 0; i < n; i++)
        {
            float height = Vector2.Dot(top[i] - path[i], normal[i]);
            if (height < minThickness)
            {
                top[i] += normal[i] * (minThickness - height);
            }
        }
        List<Vector2> polygon = new List<Vector2>(n * 2);
        for (int i = 0; i < n; i++)
        {
            polygon.Add(top[i]);
        }
        for (int i = n - 1; i >= 0; i--)
        {
            polygon.Add(path[i] - normal[i] * Sink);
        }
        if (!IsSimple(polygon))
        {
            Debug.LogWarning("BounceBack: the smooth ice cap '" + name + "' would cross itself, the segment strips are used instead.");
            IceCap(parent, name, path, minThickness + 0.05f, order);
            return;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        float tile = MapAssets.TextureWorldSize("ice").x;
        Mesh mesh = MapGeometry.CreateTiledMesh(polygon, tile, name);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = MapAssets.SaveMesh(mesh, name);
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = MapAssets.GetTileMaterial("ice");
        mr.sortingOrder = order;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
        PolygonCollider2D shape = go.AddComponent<PolygonCollider2D>();
        shape.SetPath(0, polygon.ToArray());
        BlockType block = go.AddComponent<BlockType>();
        block.kind = BlockKind.Blue;
        block.useArt = true;
    }

    static bool IsSimple(List<Vector2> p)
    {
        int n = p.Count;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = p[i];
            Vector2 b = p[(i + 1) % n];
            for (int j = i + 2; j < n; j++)
            {
                if (i == 0 && j == n - 1)
                {
                    continue;
                }
                Vector2 c = p[j];
                Vector2 d = p[(j + 1) % n];
                if (SegmentsCross(a, b, c, d))
                {
                    return false;
                }
            }
        }
        return true;
    }

    static bool SegmentsCross(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        float d1 = Cross(b - a, c - a);
        float d2 = Cross(b - a, d - a);
        float d3 = Cross(d - c, a - c);
        float d4 = Cross(d - c, b - c);
        return ((d1 > 0f && d2 < 0f) || (d1 < 0f && d2 > 0f)) && ((d3 > 0f && d4 < 0f) || (d3 < 0f && d4 > 0f));
    }

    static float Cross(Vector2 u, Vector2 v)
    {
        return u.x * v.y - u.y * v.x;
    }

    public const float SinkShare = 0.42f;
    public const float SnowMin = 0.6f;
    public const float SnowMax = 1.0f;

    public static void Snow(Transform parent, string name, IList<Vector2> path, float yMin, float yMax, int seed, int order,
        System.Func<Vector2, float> iceLift = null)
    {
        SnowMeshData visual = new SnowMeshData();
        List<Vector2[]> solid = new List<Vector2[]>();
        System.Random random = new System.Random(seed);
        float range = Mathf.Max(1f, yMax - yMin);
        Color32 snowWhite = new Color32(255, 255, 255, 255);
        Color32 iceGlint = new Color32(176, 232, 255, 255);

        bool previousIncluded = false;
        bool previousVisible = false;
        Vector2 previousStart = Vector2.zero;
        Vector2 previousEnd = Vector2.zero;
        Vector2 visibleEnd = Vector2.zero;
        int pieceIndex = 0;

        for (int i = 0; i + 1 < path.Count; i++)
        {
            Vector2 a = path[i];
            Vector2 b = path[i + 1];
            float length = (b - a).magnitude;
            if (length < 0.01f)
            {
                continue;
            }
            Vector2 dir = (b - a) / length;
            Vector2 normal = new Vector2(-dir.y, dir.x);
            int count = Mathf.Max(1, Mathf.CeilToInt(length / 0.45f));
            for (int k = 0; k < count; k++)
            {
                Vector2 p0 = Vector2.Lerp(a, b, (float)k / count);
                Vector2 p1 = Vector2.Lerp(a, b, (float)(k + 1) / count);
                Vector2 mid = (p0 + p1) * 0.5f;
                pieceIndex++;

                float border = yMin + (Noise(mid.x * 0.9f, seed) - 0.5f) * 3.2f + (Noise(mid.x * 3.1f, seed + 9) - 0.5f) * 1.2f;
                float height = Mathf.Clamp01((mid.y - yMin) / range);
                float roll = (float)random.NextDouble();
                bool inZone = mid.y >= border;
                bool include = inZone;
                if (include && normal.y <= 0.45f)
                {
                    include = normal.y > -0.1f ? roll <= 0.35f + 0.5f * height : roll <= 0.12f * height;
                }
                float lift = iceLift != null ? iceLift(mid) : -1f;
                bool overIce = lift >= 0f;

                float t0 = 0f;
                float t1 = 0f;
                if (include)
                {
                    t0 = Thickness(p0, yMin, range, seed);
                    t1 = Thickness(p1, yMin, range, seed);
                }
                bool visible = include;
                float v0 = t0;
                float v1 = t1;
                float base0 = 0f;
                if (visible && overIce)
                {
                    visible = (Hash(pieceIndex, seed) % 100u) < 58u;
                    v0 = 0.12f + 0.18f * Mathf.Clamp01((t0 - SnowMin) / (SnowMax - SnowMin));
                    v1 = 0.12f + 0.18f * Mathf.Clamp01((t1 - SnowMin) / (SnowMax - SnowMin));
                    base0 = lift;
                }

                if (visible)
                {
                    float c0 = overIce ? base0 + v0 * SinkShare : t0 * SinkShare;
                    float c1 = overIce ? base0 + v1 * SinkShare : t1 * SinkShare;
                    AddQuad(solid, p0, p1, c0, c1);
                    if (previousIncluded && (previousEnd - p0).sqrMagnitude < 1e-6f)
                    {
                        AddWedge(solid, previousStart, p0, p1, c0);
                    }
                    previousIncluded = true;
                    previousStart = p0;
                    previousEnd = p1;
                }
                else
                {
                    previousIncluded = false;
                }

                if (visible)
                {
                    visual.Quad(p0, p1, normal, base0, v0, v1, true, snowWhite, Sink, 0f);
                    if (previousVisible && (visibleEnd - p0).sqrMagnitude < 1e-6f)
                    {
                        visual.Wedge(previousStart2, p0, p1, base0, v0, snowWhite, Sink);
                    }
                    previousVisible = true;
                    previousStart2 = p0;
                    visibleEnd = p1;
                }
                else
                {
                    previousVisible = false;
                    if (overIce && inZone)
                    {
                        visual.Glint(p0, p1, normal, lift - 0.14f, lift - 0.03f, iceGlint);
                    }
                }
            }
        }
        if (visual.IsEmpty)
        {
            return;
        }

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        Mesh mesh = visual.Build(MapAssets.TextureWorldSize("snow").x, name);
        filter.sharedMesh = MapAssets.SaveMesh(mesh, name);
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = MapAssets.GetTileMaterial("snow");
        mr.sortingOrder = order;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;

        PolygonCollider2D shape = go.AddComponent<PolygonCollider2D>();
        shape.pathCount = solid.Count;
        for (int p = 0; p < solid.Count; p++)
        {
            shape.SetPath(p, solid[p]);
        }
        BlockType block = go.AddComponent<BlockType>();
        block.kind = BlockKind.Grey;
        block.useArt = true;
        SnowSurface surface = go.AddComponent<SnowSurface>();
        surface.sourceMesh = filter.sharedMesh;
        surface.filter = filter;
    }

    static Vector2 previousStart2;

    static uint Hash(int a, int b)
    {
        unchecked
        {
            uint h = (uint)(a * 73856093 ^ b * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return h;
        }
    }

    class SnowMeshData
    {
        readonly List<Vector3> vertices = new List<Vector3>();
        readonly List<Vector3> normals = new List<Vector3>();
        readonly List<Vector2> uvs = new List<Vector2>();
        readonly List<Vector2> uv2 = new List<Vector2>();
        readonly List<Color32> colours = new List<Color32>();
        readonly List<int> triangles = new List<int>();

        public bool IsEmpty { get { return vertices.Count == 0; } }

        void Add(Vector2 p, Vector2 normal, float thickness, bool compress, Color32 colour)
        {
            vertices.Add(new Vector3(p.x, p.y, 0f));
            normals.Add(new Vector3(normal.x, normal.y, 0f));
            uvs.Add(p);
            uv2.Add(new Vector2(thickness, compress ? 1f : 0f));
            colours.Add(colour);
        }

        void Fan(int first, int count)
        {
            for (int i = 1; i + 1 < count; i++)
            {
                Vector3 a = vertices[first];
                Vector3 b = vertices[first + i];
                Vector3 c = vertices[first + i + 1];
                float cross = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
                triangles.Add(first);
                triangles.Add(first + (cross > 0f ? i + 1 : i));
                triangles.Add(first + (cross > 0f ? i : i + 1));
            }
        }

        public void Quad(Vector2 a, Vector2 b, Vector2 n, float lift, float ta, float tb, bool compress, Color32 colour, float sink, float unused)
        {
            int first = vertices.Count;
            Add(a + n * (lift - sink), Vector2.zero, 0f, compress, colour);
            Add(b + n * (lift - sink), Vector2.zero, 0f, compress, colour);
            Add(b + n * (lift + tb), n, tb, compress, colour);
            Add(a + n * (lift + ta), n, ta, compress, colour);
            Fan(first, 4);
        }

        public void Wedge(Vector2 before, Vector2 corner, Vector2 after, float lift, float t, Color32 colour, float sink)
        {
            Vector2 d1 = (corner - before).normalized;
            Vector2 d2 = (after - corner).normalized;
            float cross = d1.x * d2.y - d1.y * d2.x;
            if (cross >= -0.01f)
            {
                return;
            }
            Vector2 n1 = new Vector2(-d1.y, d1.x);
            Vector2 n2 = new Vector2(-d2.y, d2.x);
            int first = vertices.Count;
            Add(corner + (n1 + n2).normalized * (lift - sink), Vector2.zero, 0f, true, colour);
            Add(corner + n1 * (lift + t), n1, t, true, colour);
            Add(corner + n2 * (lift + t), n2, t, true, colour);
            Fan(first, 3);
        }

        public void Glint(Vector2 a, Vector2 b, Vector2 n, float low, float high, Color32 colour)
        {
            int first = vertices.Count;
            Add(a + n * low, Vector2.zero, 0f, false, colour);
            Add(b + n * low, Vector2.zero, 0f, false, colour);
            Add(b + n * high, Vector2.zero, 0f, false, colour);
            Add(a + n * high, Vector2.zero, 0f, false, colour);
            Fan(first, 4);
        }

        public Mesh Build(float tile, string name)
        {
            Mesh mesh = new Mesh();
            mesh.name = name;
            mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            List<Vector2> world = new List<Vector2>(uvs.Count);
            for (int i = 0; i < uvs.Count; i++)
            {
                world.Add(uvs[i] / tile);
            }
            mesh.SetUVs(0, world);
            mesh.SetUVs(1, uv2);
            mesh.SetColors(colours);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
    static float Thickness(Vector2 p, float yMin, float range, int seed)
    {
        float height = Mathf.Clamp01((p.y - yMin) / range);
        float mix = Mathf.Clamp01(0.55f * height + 0.45f * Noise(p.x * 0.7f, seed + 3));
        return Mathf.Lerp(SnowMin, SnowMax, mix);
    }
    static float Noise(float x, int seed)
    {
        float f = Mathf.Floor(x);
        float t = x - f;
        t = t * t * (3f - 2f * t);
        float a = Lattice((int)f, seed);
        float b = Lattice((int)f + 1, seed);
        return a + (b - a) * t;
    }

    static float Lattice(int i, int seed)
    {
        unchecked
        {
            uint h = (uint)(i * 73856093 ^ seed * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (h & 0xffff) / 65535f;
        }
    }


    static void AddQuad(List<Vector2[]> pieces, Vector2 a, Vector2 b, float ta, float tb)
    {
        Vector2 d = b - a;
        if (d.sqrMagnitude < 1e-8f)
        {
            return;
        }
        d.Normalize();
        Vector2 n = new Vector2(-d.y, d.x);
        pieces.Add(new[] { a - n * Sink, b - n * Sink, b + n * tb, a + n * ta });
    }

    static void AddWedge(List<Vector2[]> pieces, Vector2 before, Vector2 corner, Vector2 after, float t)
    {
        Vector2 d1 = (corner - before).normalized;
        Vector2 d2 = (after - corner).normalized;
        float cross = d1.x * d2.y - d1.y * d2.x;
        if (cross >= -0.01f)
        {
            return;
        }
        Vector2 n1 = new Vector2(-d1.y, d1.x);
        Vector2 n2 = new Vector2(-d2.y, d2.x);
        pieces.Add(new[] { corner - (n1 + n2).normalized * Sink, corner + n1 * t, corner + n2 * t });
    }

    static void MakeMeshRenderer(GameObject go, List<Vector2[]> pieces, string textureName, string meshName, int order)
    {
        float tile = MapAssets.TextureWorldSize(textureName).x;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector2> uvs = new List<Vector2>();
        List<int> triangles = new List<int>();
        for (int p = 0; p < pieces.Count; p++)
        {
            Vector2[] poly = pieces[p];
            int first = vertices.Count;
            for (int i = 0; i < poly.Length; i++)
            {
                vertices.Add(new Vector3(poly[i].x, poly[i].y, 0f));
                uvs.Add(poly[i] / tile);
            }
            for (int i = 1; i + 1 < poly.Length; i++)
            {
                float cross = (poly[i].x - poly[0].x) * (poly[i + 1].y - poly[0].y) - (poly[i].y - poly[0].y) * (poly[i + 1].x - poly[0].x);
                triangles.Add(first);
                triangles.Add(first + (cross > 0f ? i + 1 : i));
                triangles.Add(first + (cross > 0f ? i : i + 1));
            }
        }
        Mesh mesh = new Mesh();
        mesh.name = meshName;
        mesh.indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        MeshFilter filter = go.AddComponent<MeshFilter>();
        filter.sharedMesh = MapAssets.SaveMesh(mesh, meshName);
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterial = MapAssets.GetTileMaterial(textureName);
        mr.sortingOrder = order;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }
}
