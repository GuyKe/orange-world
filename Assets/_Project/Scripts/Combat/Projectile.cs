using UnityEngine;

namespace OrangeWorld
{
    // A lobbed glob fired by a BlobGun. Hurts the player on contact and pops on anything else.
    [RequireComponent(typeof(Rigidbody))]
    public class Projectile : MonoBehaviour
    {
        public float damage = 8f;
        public float lifetime = 6f;

        Rigidbody body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }

        public void Launch(Vector3 velocity, GameObject shooter)
        {
            body.linearVelocity = velocity;
            if (velocity.sqrMagnitude > 0.001f) transform.rotation = Quaternion.LookRotation(velocity.normalized);

            if (shooter != null)
                foreach (var mine in GetComponentsInChildren<Collider>())
                    foreach (var theirs in shooter.GetComponentsInChildren<Collider>())
                        Physics.IgnoreCollision(mine, theirs);

            Destroy(gameObject, lifetime);
        }

        void OnCollisionEnter(Collision collision)
        {
            var target = collision.collider.GetComponentInParent<Damageable>();
            if (target != null && target.isPlayer)
            {
                Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
                target.TakeDamage(damage, point);
                Juice.Boing(point, 0.5f);
            }
            Destroy(gameObject);
        }
    }
}
