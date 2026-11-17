using UnityEngine;

[DefaultExecutionOrder(100)]
public class BackgroundParallax : MonoBehaviour
{
    [System.Serializable]
    public class Layer
    {
        public string name = "layer";
        public SpriteRenderer sprite;
        public float followFactor = 0.9f;
        public bool loop = true;
        public int sortingOrder = -100;
        public bool keepNearTop;
        public SpriteRenderer fillAbove;
        public SpriteRenderer fillBelow;
        public SpriteRenderer[] extraTiles;
        public float grassTopLocalY;

        [System.NonSerialized] public Transform[] tiles;
        [System.NonSerialized] public float pixelsPerUnit;
        [System.NonSerialized] public float tilePixels;
        [System.NonSerialized] public float heightPixels;
        [System.NonSerialized] public float pivotX;
        [System.NonSerialized] public float appliedScale;
    }

    public Camera targetCamera;
    public Layer[] layers;
    public float backgroundScale = 4.8f;
    public float groundTopY = 0f;
    public bool autoGroundTopY = true;
    public float verticalFollowMultiplier = 0.5f;
    public float maxAspect = 4f;
    public float fillHeight = 2000f;
    public float moonRiseRoom = 0.35f;
    public float moonSideRoom = 0.6f;
    public bool setCameraColor = true;
    public Color skyTopColor = new Color(15f / 255f, 26f / 255f, 38f / 255f, 1f);

    Camera cam;
    Transform camTransform;
    Vector3 startPosition;
    bool started;

    void Awake()
    {
        cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogWarning("BackgroundParallax: no camera found, the background stays still.");
            enabled = false;
            return;
        }
        camTransform = cam.transform;
        if (setCameraColor)
        {
            cam.backgroundColor = skyTopColor;
        }

        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            MakeTiles(layers[i]);
        }
    }

    void MakeTiles(Layer layer)
    {
        if (!CacheSprite(layer))
        {
            return;
        }

        layer.sprite.sortingOrder = layer.sortingOrder;
        SetFillOrder(layer);

        if (layer.extraTiles != null && layer.extraTiles.Length > 0)
        {
            AdoptExtraTiles(layer);
            return;
        }

        int count = 1;
        if (layer.loop)
        {
            count = TileCount(layer.tilePixels, ReferenceHeight(), backgroundScale, maxAspect);
        }

        layer.tiles = new Transform[count];
        layer.tiles[0] = layer.sprite.transform;
        for (int i = 1; i < count; i++)
        {
            GameObject copy = new GameObject(layer.name + " Tile " + i);
            copy.transform.SetParent(layer.sprite.transform.parent, false);
            SpriteRenderer renderer = copy.AddComponent<SpriteRenderer>();
            renderer.sprite = layer.sprite.sprite;
            renderer.sharedMaterial = layer.sprite.sharedMaterial;
            renderer.sortingLayerID = layer.sprite.sortingLayerID;
            renderer.sortingOrder = layer.sortingOrder;
            layer.tiles[i] = copy.transform;
        }
    }

    public static int TileCount(float tilePixels, float referenceHeight, float scale, float aspect)
    {
        float needed = aspect * referenceHeight / (Mathf.Max(0.01f, scale) * tilePixels);
        return Mathf.CeilToInt(needed) + 2;
    }

    void AdoptExtraTiles(Layer layer)
    {
        int count = 1 + layer.extraTiles.Length;
        layer.tiles = new Transform[count];
        layer.tiles[0] = layer.sprite.transform;
        for (int i = 0; i < layer.extraTiles.Length; i++)
        {
            layer.extraTiles[i].sortingOrder = layer.sortingOrder;
            layer.tiles[i + 1] = layer.extraTiles[i].transform;
        }
    }

    float ReferenceHeight()
    {
        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            if (layers[i] != null && layers[i].sprite != null && layers[i].sprite.sprite != null)
            {
                return layers[i].sprite.sprite.rect.height;
            }
        }
        return 240f;
    }

    bool CacheSprite(Layer layer)
    {
        if (layer == null || layer.sprite == null || layer.sprite.sprite == null)
        {
            return false;
        }
        Sprite sprite = layer.sprite.sprite;
        layer.pixelsPerUnit = sprite.pixelsPerUnit;
        layer.tilePixels = sprite.rect.width;
        layer.heightPixels = sprite.rect.height;
        layer.pivotX = sprite.pivot.x / sprite.rect.width;
        return true;
    }

    void SetFillOrder(Layer layer)
    {
        if (layer.fillAbove != null)
        {
            layer.fillAbove.sortingOrder = layer.sortingOrder - 1;
        }
        if (layer.fillBelow != null)
        {
            layer.fillBelow.sortingOrder = layer.sortingOrder - 1;
        }
    }

    void LateUpdate()
    {
        if (cam == null)
        {
            return;
        }

        Vector3 cameraPosition = camTransform.position;
        if (!started)
        {
            startPosition = cameraPosition;
            started = true;
            if (autoGroundTopY)
            {
                FindGroundTop();
            }
        }
        Place(cameraPosition, startPosition, cam.orthographicSize, cam.aspect);
    }

    void FindGroundTop()
    {
        Vector2 origin;
        GameObject spawn = GameObject.Find("PlayerStart");
        if (spawn != null)
        {
            origin = spawn.transform.position;
        }
        else if (PlayerController.Instance != null)
        {
            origin = PlayerController.Instance.transform.position;
        }
        else
        {
            return;
        }

        RaycastHit2D[] hits = new RaycastHit2D[16];
        int count = Physics2D.Raycast(origin, Vector2.down, ContactFilter2D.noFilter, hits, 500f);
        float bestDistance = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = hits[i].collider;
            if (hit == null || hit.GetComponentInParent<PlayerController>() != null || hit.GetComponent<HammerContact>() != null)
            {
                continue;
            }
            if (hits[i].distance < bestDistance)
            {
                bestDistance = hits[i].distance;
                groundTopY = hits[i].point.y;
            }
        }
    }

    public void PlaceAtCamera(Vector3 cameraPosition, float orthographicSize, float aspect)
    {
        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            if (CacheSprite(layers[i]))
            {
                layers[i].sprite.sortingOrder = layers[i].sortingOrder;
                SetFillOrder(layers[i]);
                if (layers[i].tiles == null && layers[i].extraTiles != null && layers[i].extraTiles.Length > 0)
                {
                    AdoptExtraTiles(layers[i]);
                }
            }
        }
        Place(cameraPosition, cameraPosition, orthographicSize, aspect);
    }

    void Place(Vector3 cameraPosition, Vector3 start, float size, float aspect)
    {
        float halfHeight = size;
        float halfWidth = size * aspect;
        float z = transform.position.z;

        float grassLocal = 0f;
        bool hasGrass = false;
        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            if (layers[i] != null && layers[i].grassTopLocalY != 0f)
            {
                grassLocal = layers[i].grassTopLocalY;
                hasGrass = true;
            }
        }
        float referenceHeight = ReferenceHeight();

        for (int i = 0; layers != null && i < layers.Length; i++)
        {
            Layer layer = layers[i];
            if (layer == null || layer.sprite == null)
            {
                continue;
            }
            if (layer.pixelsPerUnit <= 0f && !CacheSprite(layer))
            {
                continue;
            }

            float pixel = 2f * halfHeight * Mathf.Max(0.01f, backgroundScale) / referenceHeight;
            float scale = pixel * layer.pixelsPerUnit;

            float horizontal = layer.followFactor;
            float vertical = layer.followFactor * verticalFollowMultiplier;
            float x = start.x + (cameraPosition.x - start.x) * horizontal;
            float ownGrass = layer.grassTopLocalY != 0f ? layer.grassTopLocalY : grassLocal;
            float baseY = hasGrass ? groundTopY - ownGrass * scale : start.y;
            float y = baseY + (cameraPosition.y - start.y) * vertical;

            if (layer.keepNearTop)
            {
                float topEdge = y + 0.5f * layer.heightPixels * pixel;
                float viewTop = cameraPosition.y + halfHeight;
                y += Mathf.Clamp(topEdge, viewTop, viewTop + moonRiseRoom * halfHeight) - topEdge;
                x = Mathf.Clamp(x, cameraPosition.x - moonSideRoom * halfWidth, cameraPosition.x + moonSideRoom * halfWidth);
            }

            bool scaleChanged = layer.appliedScale != scale;
            layer.appliedScale = scale;

            if (layer.tiles == null)
            {
                Transform only = layer.sprite.transform;
                only.localScale = new Vector3(scale, scale, 1f);
                only.position = new Vector3(x, y, z);
            }
            else
            {
                float tileWidth = layer.tilePixels * pixel;
                int first = 0;
                if (layer.loop)
                {
                    first = Mathf.FloorToInt((cameraPosition.x - halfWidth - x) / tileWidth - (1f - layer.pivotX)) + 1;
                }
                for (int t = 0; t < layer.tiles.Length; t++)
                {
                    Transform tile = layer.tiles[t];
                    if (scaleChanged)
                    {
                        tile.localScale = new Vector3(scale, scale, 1f);
                    }
                    tile.position = new Vector3(x + (first + t) * tileWidth, y, z);
                }
            }

            float fillWidth = 2f * halfWidth + 2f;
            if (layer.fillAbove != null)
            {
                float edge = y + 0.5f * layer.heightPixels * pixel - pixel;
                Transform fill = layer.fillAbove.transform;
                fill.localScale = new Vector3(fillWidth, fillHeight, 1f);
                fill.position = new Vector3(cameraPosition.x, edge + 0.5f * fillHeight, z);
            }
            if (layer.fillBelow != null)
            {
                float edge = y - 0.5f * layer.heightPixels * pixel + pixel;
                Transform fill = layer.fillBelow.transform;
                fill.localScale = new Vector3(fillWidth, fillHeight, 1f);
                fill.position = new Vector3(cameraPosition.x, edge - 0.5f * fillHeight, z);
            }
        }
    }
}
