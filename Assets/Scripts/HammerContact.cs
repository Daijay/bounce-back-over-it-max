using UnityEngine;

public class HammerContact : MonoBehaviour
{
    public PlayerController owner;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contactCount == 0)
        {
            return;
        }
        ContactPoint2D contact = collision.GetContact(0);
        float speed = Mathf.Abs(Vector2.Dot(collision.relativeVelocity, contact.normal));
        FxBursts.Sparks(contact.point, contact.normal, speed);
    }
}
