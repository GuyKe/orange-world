using UnityEngine;

namespace OrangeWorld
{
    // Punch this with either hand to spend blob bucks on a permanent max-health upgrade, if you can afford it.
    public class HealthShrine : MonoBehaviour
    {
        public int cost = 25;
        public float healthBonus = 10f;
        public float minImpactSpeed = 1.2f;
        public float cooldownSeconds = 0.5f;

        float nextAllowed;

        void OnCollisionEnter(Collision collision)
        {
            if (Time.time < nextAllowed || collision.relativeVelocity.magnitude < minImpactSpeed) return;

            var hand = collision.collider.GetComponentInParent<HandGrabber>();
            if (hand == null || hand.walker == null) return;

            var bucks = hand.walker.GetComponent<PlayerBucks>();
            var health = hand.walker.GetComponent<Damageable>();
            if (bucks == null || health == null) return;

            nextAllowed = Time.time + cooldownSeconds;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;

            if (bucks.TrySpend(cost))
            {
                health.AddMaxHealth(healthBonus);
                Haptics.Pulse(hand.Node, 0.8f, 0.2f);
                Juice.Boing(point, 1f);
            }
            else
            {
                Haptics.Pulse(hand.Node, 0.2f, 0.08f);
                Juice.Boing(point, 0.15f);
            }
        }
    }
}
