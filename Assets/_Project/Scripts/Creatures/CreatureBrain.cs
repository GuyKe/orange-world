using System.Collections.Generic;
using UnityEngine;

namespace OrangeWorld
{
    // Hops around its planetoid, chases the player when near, and lunges at close range.
    [RequireComponent(typeof(Rigidbody), typeof(GravityBody))]
    public class CreatureBrain : MonoBehaviour
    {
        public static readonly List<CreatureBrain> All = new();

        public float hopInterval = 1.4f;
        public float hopSpeed = 3f;
        public float hopHeight = 4f;
        public float noticeRange = 30f;
        public float lungeRange = 3.5f;
        public float lungeSpeed = 7f;
        public float contactDamage = 12f;

        Rigidbody body;
        GravityBody gravity;
        SphereCollider sphere;
        Transform player;
        float nextHop;

        float Radius => sphere != null ? sphere.radius * transform.lossyScale.x : 0.5f;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            gravity = GetComponent<GravityBody>();
            sphere = GetComponent<SphereCollider>();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            var walker = FindFirstObjectByType<PlanetWalker>();
            if (walker != null) player = walker.transform;
            nextHop = Time.time + Random.Range(0.2f, hopInterval);
        }

        void FixedUpdate()
        {
            if (Time.time < nextHop || !IsGrounded()) return;

            Vector3 up = gravity.Up;
            Vector3 toPlayer = player != null ? player.position - body.position : Vector3.zero;
            bool chasing = player != null && toPlayer.sqrMagnitude < noticeRange * noticeRange;
            bool lunging = chasing && toPlayer.sqrMagnitude < lungeRange * lungeRange;

            Vector3 direction = Vector3.ProjectOnPlane(chasing ? toPlayer : Random.onUnitSphere, up).normalized;
            body.linearVelocity = lunging
                ? direction * lungeSpeed + up * 2.5f
                : direction * hopSpeed + up * hopHeight;
            body.angularVelocity = Random.insideUnitSphere * 6f;

            nextHop = Time.time + hopInterval * Random.Range(0.7f, 1.3f) * (lunging ? 1.6f : 1f);
        }

        bool IsGrounded() =>
            Physics.Raycast(body.position, -gravity.Up, Radius + 0.1f, ~0, QueryTriggerInteraction.Ignore);

        void OnCollisionEnter(Collision collision)
        {
            var target = collision.collider.GetComponentInParent<Damageable>();
            if (target == null || !target.isPlayer) return;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            target.TakeDamage(contactDamage, point);
        }
    }
}
