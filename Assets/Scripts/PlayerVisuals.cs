using UnityEngine;

[System.Serializable]
public class SpritePart
{
    public Sprite sprite;
    [Tooltip("1 = the art fits its collider, bigger or smaller to adjust by eye")]
    public float visualScale = 1f;
    [Tooltip("rig units, moves the art without moving the collider")]
    public Vector2 visualOffset = Vector2.zero;
    [Tooltip("degrees, use it if the art is drawn pointing another way")]
    public float rotationOffset = 0f;
    public int sortingOrder = 10;
    [Tooltip("size in pixels of the drawn art inside the image (the scene builder measures it); 0 = whole image")]
    public Vector2 artPixels = Vector2.zero;
}

public class PlayerVisuals : MonoBehaviour
{
    public VisualSettings visual = new VisualSettings();

    [Header("Sprite art (empty = keep the primitive)")]
    public SpritePart bodyArt = new SpritePart { sortingOrder = 10 };
    public bool bodyArtFacesLeft = true;
    public bool bodyArtAlignBottom = true;
    public float faceSwitchDistance = 0.1f;
    public bool handsFromArtMiddle = true;
    public float artHandSpread = 0.04f;

    [Header("Arms (visuals only)")]
    public Color armColor = new Color(0.55f, 0.55f, 0.55f);
    public int armSortingOrder = 13;
    public float leftShoulderOffset = -0.17f;
    public float rightShoulderOffset = 0.17f;
    public float shoulderHeight = 0.05f;

    [Header("Dizzy wobble (visual only)")]
    public float wobbleDegrees = 7f;
    public float wobbleSpeed = 16f;

    [Header("Rat height (visual only, no collider or physics change)")]
    public float heightStretch = 1.3333f;

    public SpritePart hammerHeadArt = new SpritePart { sortingOrder = 15 };
    public SpritePart handleArt = new SpritePart { sortingOrder = 14 };

    public Transform hammer;
    public Transform pot;
    public Transform torso;
    public Transform head;
    public Transform leftArm;
    public Transform rightArm;
    public Transform shaft;
    public Transform hammerHead;
    public Transform bodyArtTransform;
    public Transform hammerHeadArtTransform;
    public Transform handleArtTransform;

    float scale = 1f;
    PlayerController owner;
    SpriteRenderer bodyRenderer;
    bool bodyArtActive;
    bool handleArtActive;
    bool faceRight = true;
    float handleSpriteLength = 1f;
    float handleSpriteHeight = 1f;
    SpriteRenderer leftArmRenderer;
    SpriteRenderer rightArmRenderer;
    float appliedBurn;
    Vector2 headTopLocal;
    float bodyBottomLocalY;
    bool wobbling;

    public void Layout(float rigScale)
    {
        scale = rigScale;
        owner = GetComponent<PlayerController>();
        VisualSettings v = visual;

        Place(pot, v.potOffset * scale, v.potSize * scale);
        Place(torso, v.torsoOffset * scale, v.torsoSize * scale);
        Place(head, v.headOffset * scale, Vector2.one * (v.headDiameter * scale));
        Place(shaft, new Vector2(-v.shaftCenterBack * scale, 0f), new Vector2(v.shaftLength, v.shaftThickness) * scale);
        Place(hammerHead, Vector2.zero, v.hammerHeadSize * scale);
        FillSprite(pot, SpriteFactory.White, v.potColor, 10);
        FillSprite(torso, SpriteFactory.White, v.torsoColor, 11);
        FillSprite(head, SpriteFactory.Circle, v.headColor, 12);
        FillSprite(leftArm, SpriteFactory.White, armColor, armSortingOrder);
        FillSprite(rightArm, SpriteFactory.White, armColor, armSortingOrder);
        leftArmRenderer = leftArm != null ? leftArm.GetComponent<SpriteRenderer>() : null;
        rightArmRenderer = rightArm != null ? rightArm.GetComponent<SpriteRenderer>() : null;
        FillSprite(shaft, SpriteFactory.White, v.shaftColor, 14);
        FillSprite(hammerHead, SpriteFactory.White, v.hammerHeadColor, 15);

        headTopLocal = new Vector2(v.headOffset.x * scale, (v.headOffset.y + v.headDiameter * 0.5f) * scale);
        bodyBottomLocalY = (v.potOffset.y - v.potSize.y * 0.5f) * scale;
        LayoutBodyArt();
        LayoutHammerHeadArt();
        LayoutHandleArt();
        PlaceArms();
    }

    void LayoutBodyArt()
    {
        bodyArtActive = bodyArt.sprite != null && bodyArtTransform != null && owner != null;
        SetActive(pot, !bodyArtActive);
        SetActive(torso, !bodyArtActive);
        SetActive(head, !bodyArtActive);
        if (bodyArtTransform != null)
        {
            bodyArtTransform.gameObject.SetActive(bodyArtActive);
        }
        if (!bodyArtActive)
        {
            bodyRenderer = null;
            return;
        }

        bodyRenderer = ShowArt(bodyArtTransform, bodyArt);
        appliedBurn = 0f;
        Vector2 artWorld = ArtWorldSize(bodyArt);
        Vector2 collider = owner.body.bodySize * scale;
        Vector2 colliderCenter = owner.body.bodyOffset * scale;
        float fit = Mathf.Min(collider.x / artWorld.x, collider.y / artWorld.y) * bodyArt.visualScale;

        float centerY = colliderCenter.y;
        if (bodyArtAlignBottom)
        {
            centerY = colliderCenter.y - collider.y * 0.5f + artWorld.y * fit * heightStretch * 0.5f;
        }
        bodyArtTransform.localPosition = new Vector3(colliderCenter.x + bodyArt.visualOffset.x * scale, centerY + bodyArt.visualOffset.y * scale, 0f);
        bodyArtTransform.localRotation = Quaternion.Euler(0f, 0f, bodyArt.rotationOffset);
        bodyArtTransform.localScale = new Vector3(fit, fit * heightStretch, 1f);
        float drawnHalf = artWorld.y * fit * heightStretch * 0.5f;
        headTopLocal = new Vector2(colliderCenter.x + bodyArt.visualOffset.x * scale, centerY + bodyArt.visualOffset.y * scale + drawnHalf);
        bodyBottomLocalY = centerY + bodyArt.visualOffset.y * scale - drawnHalf;
    }

    public float BodyHeight { get { return Mathf.Max(0.1f, headTopLocal.y - bodyBottomLocalY); } }

    public float BodyWidth { get { return owner != null ? owner.body.bodySize.x * scale : BodyHeight * 0.4f; } }

    public Vector3 BodyCenterWorld()
    {
        return transform.TransformPoint(new Vector3(headTopLocal.x, (headTopLocal.y + bodyBottomLocalY) * 0.5f, 0f));
    }

    public Vector3 HeadTopWorld()
    {
        return transform.TransformPoint(new Vector3(headTopLocal.x, headTopLocal.y, 0f));
    }

    void LayoutHammerHeadArt()
    {
        bool active = hammerHeadArt.sprite != null && hammerHeadArtTransform != null && owner != null;
        SetActive(hammerHead, !active);
        if (hammerHeadArtTransform != null)
        {
            hammerHeadArtTransform.gameObject.SetActive(active);
        }
        if (!active)
        {
            return;
        }

        ShowArt(hammerHeadArtTransform, hammerHeadArt);
        Vector2 artWorld = ArtWorldSize(hammerHeadArt);
        float diameter = owner.hammer.headRadius * 2f * scale;
        float fit = Mathf.Min(diameter / artWorld.x, diameter / artWorld.y) * hammerHeadArt.visualScale;
        hammerHeadArtTransform.localPosition = new Vector3(hammerHeadArt.visualOffset.x * scale, hammerHeadArt.visualOffset.y * scale, 0f);
        hammerHeadArtTransform.localRotation = Quaternion.Euler(0f, 0f, hammerHeadArt.rotationOffset);
        hammerHeadArtTransform.localScale = new Vector3(fit, fit, 1f);
    }

    void LayoutHandleArt()
    {
        handleArtActive = handleArt.sprite != null && handleArtTransform != null;
        SetActive(shaft, !handleArtActive);
        if (handleArtTransform != null)
        {
            handleArtTransform.gameObject.SetActive(handleArtActive);
        }
        if (!handleArtActive)
        {
            return;
        }

        ShowArt(handleArtTransform, handleArt);
        Vector2 artWorld = ArtWorldSize(handleArt);
        handleSpriteLength = artWorld.x;
        handleSpriteHeight = artWorld.y;
    }

    void LateUpdate()
    {
        PlaceArms();
        PlaceHandleArt();
        UpdateFacing();
        UpdateWobble();
        UpdateBurnTint();
    }

    void UpdateBurnTint()
    {
        if (!bodyArtActive || bodyRenderer == null || owner == null)
        {
            return;
        }
        float p = owner.LavaProgress;
        if (Mathf.Approximately(p, appliedBurn))
        {
            return;
        }
        appliedBurn = p;
        bodyRenderer.color = Color.Lerp(Color.white, owner.lavaDeath.burnTint, p);
    }

    void UpdateWobble()
    {
        if (!bodyArtActive || bodyArtTransform == null || owner == null)
        {
            return;
        }
        bool dizzy = owner.Dizzy;
        if (!dizzy && !wobbling)
        {
            return;
        }
        float angle = bodyArt.rotationOffset;
        if (dizzy)
        {
            float fade = Mathf.Clamp01(owner.DizzyLeft / 0.25f);
            angle += Mathf.Sin(Time.time * wobbleSpeed) * wobbleDegrees * fade;
        }
        bodyArtTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        wobbling = dizzy;
    }

    void PlaceHandleArt()
    {
        if (!handleArtActive || hammer == null)
        {
            return;
        }
        Vector3 from = transform.position;
        Vector3 to = hammer.position;
        float dx = to.x - from.x;
        float dy = to.y - from.y;
        float length = Mathf.Sqrt(dx * dx + dy * dy);
        if (Safe.Bad(length) || length < 0.0001f)
        {
            return;
        }
        float angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
        float rad = angle * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);
        float ox = (handleArt.visualOffset.x * cos - handleArt.visualOffset.y * sin) * scale;
        float oy = (handleArt.visualOffset.x * sin + handleArt.visualOffset.y * cos) * scale;

        handleArtTransform.position = new Vector3(from.x + ox, from.y + oy, handleArtTransform.position.z);
        handleArtTransform.rotation = Quaternion.Euler(0f, 0f, angle + handleArt.rotationOffset);
        float thickness = visual.shaftThickness * scale * handleArt.visualScale;
        handleArtTransform.localScale = new Vector3(length / handleSpriteLength, thickness / handleSpriteHeight, 1f);
    }

    void UpdateFacing()
    {
        if (!bodyArtActive || bodyRenderer == null || owner == null || owner.Aim == null)
        {
            return;
        }
        float x = owner.Aim.Offset.x;
        float dead = faceSwitchDistance * scale;
        if (x > dead)
        {
            faceRight = true;
        }
        else if (x < -dead)
        {
            faceRight = false;
        }
        bodyRenderer.flipX = faceRight == bodyArtFacesLeft;
    }

    void PlaceArms()
    {
        if (hammer == null)
        {
            return;
        }
        Vector3 handle = hammer.TransformPoint(new Vector3(-visual.handleBack * scale, 0f, 0f));

        Vector3 middle = bodyArtActive && bodyArtTransform != null ? bodyArtTransform.position : transform.position;
        float shoulderY = middle.y + shoulderHeight * heightStretch * scale;
        Vector3 leftShoulder = new Vector3(middle.x + leftShoulderOffset * scale, shoulderY, middle.z);
        Vector3 rightShoulder = new Vector3(middle.x + rightShoulderOffset * scale, shoulderY, middle.z);

        StretchArm(leftArm, leftShoulder, handle);
        StretchArm(rightArm, rightShoulder, handle);
        ApplyArmLook();
    }

    void ApplyArmLook()
    {
        SyncArm(leftArmRenderer);
        SyncArm(rightArmRenderer);
    }

    void SyncArm(SpriteRenderer sr)
    {
        if (sr == null)
        {
            return;
        }
        if (sr.color != armColor)
        {
            sr.color = armColor;
        }
        if (sr.sortingOrder != armSortingOrder)
        {
            sr.sortingOrder = armSortingOrder;
        }
    }

    void StretchArm(Transform arm, Vector3 shoulder, Vector3 handle)
    {
        if (arm == null)
        {
            return;
        }
        Vector2 d = handle - shoulder;
        float length = d.magnitude;
        if (Safe.Bad(length))
        {
            return;
        }
        arm.position = new Vector3((shoulder.x + handle.x) * 0.5f, (shoulder.y + handle.y) * 0.5f, arm.position.z);
        arm.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        arm.localScale = new Vector3(Mathf.Max(0.001f, length), visual.armThickness * scale, 1f);
    }

    static Vector2 ArtWorldSize(SpritePart part)
    {
        float ppu = Mathf.Max(0.01f, part.sprite.pixelsPerUnit);
        Vector2 pixels = part.artPixels;
        if (pixels.x < 1f || pixels.y < 1f)
        {
            pixels = part.sprite.rect.size;
        }
        return new Vector2(Mathf.Max(0.0001f, pixels.x / ppu), Mathf.Max(0.0001f, pixels.y / ppu));
    }

    static SpriteRenderer ShowArt(Transform t, SpritePart part)
    {
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = part.sprite;
            sr.color = Color.white;
            sr.sortingOrder = part.sortingOrder;
        }
        return sr;
    }

    static void SetActive(Transform t, bool active)
    {
        if (t != null && t.gameObject.activeSelf != active)
        {
            t.gameObject.SetActive(active);
        }
    }

    static void Place(Transform t, Vector2 position, Vector2 size)
    {
        if (t == null)
        {
            return;
        }
        t.localPosition = new Vector3(position.x, position.y, 0f);
        t.localScale = new Vector3(size.x, size.y, 1f);
    }

    static void FillSprite(Transform t, Sprite fallback, Color color, int order)
    {
        if (t == null)
        {
            return;
        }
        SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            return;
        }
        if (sr.sprite == null)
        {
            sr.sprite = fallback;
        }
        sr.color = color;
        sr.sortingOrder = order;
    }
}
