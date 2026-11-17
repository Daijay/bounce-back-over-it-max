using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(50)]
public class SnowTrail : MonoBehaviour
{
    public float bodyRadius = 0.8f;
    public float headRadius = 0.4f;
    public float edgeWidth = 1.0f;
    public float squash = 0.5f;
    public float sinkSpeed = 10f;
    public float touchReach = 0.3f;
    public float recoverTime = 20f;
    public Color pressedTint = Color.white;

    class Piece
    {
        public SnowSurface surface;
        public Mesh mesh;
        public Vector3[] baseVertices;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] data;
        public Color32[] baseColours;
        public Color32[] colours;
        public float[] pressed;
        public Bounds bounds;
        public bool active;
    }

    Piece[] pieces;
    Collider2D bodyCollider;
    Collider2D headCollider;
    readonly Collider2D[] hits = new Collider2D[24];

    Vector2[] sourcePosition = new Vector2[2];
    float[] sourceRadius = new float[2];
    bool[] sourceActive = new bool[2];

    void OnEnable()
    {
        DestroyPieces();
        if (!Application.isPlaying)
        {
            return;
        }
        SnowSurface[] surfaces = FindObjectsByType<SnowSurface>();
        List<Piece> list = new List<Piece>(surfaces.Length);
        for (int s = 0; s < surfaces.Length; s++)
        {
            SnowSurface surface = surfaces[s];
            if (surface.sourceMesh == null || surface.filter == null)
            {
                continue;
            }
            Piece piece = new Piece();
            piece.surface = surface;
            piece.baseVertices = surface.sourceMesh.vertices;
            piece.normals = surface.sourceMesh.normals;
            piece.baseColours = surface.sourceMesh.colors32;
            Vector2[] uv2 = surface.sourceMesh.uv2;
            if (piece.baseVertices.Length == 0 || piece.normals.Length != piece.baseVertices.Length || uv2.Length != piece.baseVertices.Length)
            {
                continue;
            }
            if (piece.baseColours.Length != piece.baseVertices.Length)
            {
                piece.baseColours = new Color32[piece.baseVertices.Length];
                for (int i = 0; i < piece.baseColours.Length; i++)
                {
                    piece.baseColours[i] = new Color32(255, 255, 255, 255);
                }
            }
            piece.data = uv2;
            piece.vertices = new Vector3[piece.baseVertices.Length];
            piece.colours = new Color32[piece.baseVertices.Length];
            piece.pressed = new float[piece.baseVertices.Length];
            System.Array.Copy(piece.baseVertices, piece.vertices, piece.vertices.Length);
            System.Array.Copy(piece.baseColours, piece.colours, piece.colours.Length);
            piece.mesh = Instantiate(surface.sourceMesh);
            piece.mesh.name = surface.sourceMesh.name + " (squash)";
            surface.filter.sharedMesh = piece.mesh;
            MeshRenderer renderer = surface.filter.GetComponent<MeshRenderer>();
            piece.bounds = renderer != null ? renderer.bounds : new Bounds(surface.transform.position, Vector3.one * 100f);
            list.Add(piece);
        }
        pieces = list.ToArray();
        bodyCollider = null;
        headCollider = null;
    }

    void OnDisable()
    {
        DestroyPieces();
    }

    void DestroyPieces()
    {
        if (pieces == null)
        {
            return;
        }
        for (int i = 0; i < pieces.Length; i++)
        {
            Piece piece = pieces[i];
            if (piece.surface != null && piece.surface.filter != null)
            {
                piece.surface.filter.sharedMesh = piece.surface.sourceMesh;
            }
            if (piece.mesh != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(piece.mesh);
                }
                else
                {
                    DestroyImmediate(piece.mesh);
                }
            }
        }
        pieces = null;
    }

    void Update()
    {
        if (pieces == null)
        {
            return;
        }
        PlayerController player = PlayerController.Instance;
        sourceActive[0] = false;
        sourceActive[1] = false;
        if (player != null)
        {
            if (player.BodyRb != null)
            {
                if (bodyCollider == null)
                {
                    bodyCollider = player.BodyRb.GetComponent<Collider2D>();
                }
                Read(bodyCollider, 0, bodyRadius);
            }
            if (player.HammerRb != null)
            {
                if (headCollider == null)
                {
                    headCollider = player.HammerRb.GetComponent<Collider2D>();
                }
                Read(headCollider, 1, headRadius);
            }
        }

        float dt = Time.deltaTime;
        for (int i = 0; i < pieces.Length; i++)
        {
            Piece piece = pieces[i];
            if (piece.mesh == null)
            {
                continue;
            }
            bool near = false;
            for (int s = 0; s < 2 && !near; s++)
            {
                if (sourceActive[s])
                {
                    float reach = sourceRadius[s] + edgeWidth;
                    Vector3 p = new Vector3(sourcePosition[s].x, sourcePosition[s].y, piece.bounds.center.z);
                    near = piece.bounds.SqrDistance(p) < reach * reach;
                }
            }
            if (near || piece.active)
            {
                Step(piece, dt);
            }
        }
    }

    void Read(Collider2D part, int index, float radius)
    {
        if (part == null)
        {
            return;
        }
        Bounds bounds = part.bounds;
        float reach = Mathf.Max(bounds.extents.x, bounds.extents.y) + touchReach + 0.05f;
        int n = Physics2D.OverlapCircle(bounds.center, reach, ContactFilter2D.noFilter, hits);
        float best = touchReach;
        bool found = false;
        Vector2 point = Vector2.zero;
        for (int i = 0; i < n; i++)
        {
            SnowSurface surface;
            if (hits[i] == null || !hits[i].TryGetComponent(out surface))
            {
                continue;
            }
            ColliderDistance2D distance = part.Distance(hits[i]);
            if (distance.isValid && distance.distance < best)
            {
                best = distance.distance;
                point = distance.pointB;
                found = true;
            }
        }
        if (found)
        {
            sourceActive[index] = true;
            sourcePosition[index] = point;
            sourceRadius[index] = radius;
        }
    }
    void Step(Piece piece, float dt)
    {
        Vector3 offset = piece.surface.transform.position;
        float recover = dt / Mathf.Max(0.1f, recoverTime);
        bool any = false;
        Color tint = pressedTint;
        for (int v = 0; v < piece.baseVertices.Length; v++)
        {
            if (piece.data[v].y < 0.5f)
            {
                continue;
            }
            Vector3 b = piece.baseVertices[v];
            float target = 0f;
            for (int s = 0; s < 2; s++)
            {
                if (!sourceActive[s])
                {
                    continue;
                }
                float dx = b.x + offset.x - sourcePosition[s].x;
                float dy = b.y + offset.y - sourcePosition[s].y;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float w = 1f - Mathf.Clamp01((d - sourceRadius[s]) / Mathf.Max(0.05f, edgeWidth));
                w = w * w * (3f - 2f * w);
                if (w > target)
                {
                    target = w;
                }
            }
            float c = piece.pressed[v];
            if (target > c)
            {
                c = Mathf.Min(target, c + sinkSpeed * dt);
            }
            else if (c > 0f)
            {
                c = Mathf.Max(target, c - recover);
            }
            if (c <= 0.0005f)
            {
                c = 0f;
            }
            piece.pressed[v] = c;
            if (c > 0f)
            {
                any = true;
            }

            float lower = piece.data[v].x * squash * c;
            piece.vertices[v] = new Vector3(b.x - piece.normals[v].x * lower, b.y - piece.normals[v].y * lower, b.z);
            Color32 baseColour = piece.baseColours[v];
            float k = c * 0.9f;
            piece.colours[v] = new Color32(
                (byte)(baseColour.r * (1f - k + tint.r * k)),
                (byte)(baseColour.g * (1f - k + tint.g * k)),
                (byte)(baseColour.b * (1f - k + tint.b * k)),
                baseColour.a);
        }
        piece.mesh.vertices = piece.vertices;
        piece.mesh.colors32 = piece.colours;
        piece.active = any;
    }
}
