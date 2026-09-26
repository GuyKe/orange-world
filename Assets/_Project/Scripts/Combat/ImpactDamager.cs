using UnityEngine;
using UnityEngine.XR;

namespace OrangeWorld
{
    // Momentum-based damage: harder, heavier hits deal more. Slow contact deals nothing, so you must swing.
    [RequireComponent(typeof(Rigidbody))]
    public class ImpactDamager : MonoBehaviour
    {
        // Scales every hit in the scene, e.g. while PlayerUltimate is active. Reset to 1 when it ends.
        public static float GlobalDamageMultiplier = 1f;

        public float minImpactSpeed = 2.5f;
        public float damagePerSpeed = 5f;
        [Tooltip("Extra cartoon knockback on top of real physics, as a velocity change per m/s of impact.")]
        public float knockback = 0.25f;
        [Tooltip("If above 0, this object hurts itself when it slams into anything faster than this.")]
        public float selfDamageMinSpeed = 0f;
        public float selfDamagePerSpeed = 3f;

        public XRNode? HapticNode { get; set; }

        Rigidbody body;
        Damageable self;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            self = GetComponent<Damageable>();
        }

        void OnCollisionEnter(Collision collision)
        {
            float speed = collision.relativeVelocity.magnitude;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

            if (self != null && selfDamageMinSpeed > 0f && speed > selfDamageMinSpeed)
                self.TakeDamage((speed - selfDamageMinSpeed) * selfDamagePerSpeed, point);

            if (speed < minImpactSpeed) return;

            if (HapticNode.HasValue)
                Haptics.Pulse(HapticNode.Value, Mathf.InverseLerp(minImpactSpeed, 15f, speed), 0.06f);

            var target = collision.collider.GetComponentInParent<Damageable>();
            if (target == null || target.isPlayer || target == self) return;

            float damage = (speed - minImpactSpeed) * damagePerSpeed * Mathf.Sqrt(body.mass) * GlobalDamageMultiplier;
            target.TakeDamage(damage, point);
            Juice.Boing(point, Mathf.InverseLerp(minImpactSpeed, minImpactSpeed * 5f, speed));

            var other = collision.rigidbody;
            if (other != null && !other.isKinematic)
            {
                Vector3 direction = (other.worldCenterOfMass - body.worldCenterOfMass).normalized;
                other.AddForce(direction * (speed * knockback), ForceMode.VelocityChange);
            }
        }
    }
}
