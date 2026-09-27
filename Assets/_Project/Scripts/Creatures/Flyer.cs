using System.Collections.Generic;
using UnityEngine;

namespace OrangeWorld
{
    // Floats at a set height above whichever planetoid it's near, drifting sideways and diving at the player when
    // close, then rising back to cruising height. No hopping, no ground contact - a jellyfish, not a blob.
    [RequireComponent(typeof(Rigidbody), typeof(GravityBody))]
    public class Flyer : MonoBehaviour
    {
        // A separate registry from CreatureBrain.All, since a flyer doesn't carry one - WaveSpawner sums both
        // so its population cap actually counts everything alive.
        public static readonly List<Flyer> All = new();

        public float hoverHeight = 6f;
        public float diveHeight = 1.5f;
        public float driftSpeed = 2f;
        public float noticeRange = 25f;
        public float diveRange = 10f;
        public float diveSpeed = 8f;
        public float contactDamage = 10f;

        Rigidbody body;
        GravityBody gravity;
        Transform player;
        Vector3 driftDirection;
        float nextDriftChange;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            gravity = GetComponent<GravityBody>();
            gravity.gravityScale = 0f; // it flies - the pull toward the planet is fought, not felt
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Start()
        {
            var walker = FindFirstObjectByType<PlanetWalker>();
            if (walker != null) player = walker.transform;
            PickNewDrift();
        }

        void FixedUpdate()
        {
            if (gravity.Attractor == null) return;

            Vector3 up = gravity.Up;
            float surfaceDistance = Vector3.Distance(body.position, gravity.Attractor.transform.position) - gravity.Attractor.Radius;

            bool inRange = player != null && (player.position - body.position).sqrMagnitude < noticeRange * noticeRange;
            bool diving = inRange && (player.position - body.position).sqrMagnitude < diveRange * diveRange;

            float targetHeight = diving ? diveHeight : hoverHeight;
            float heightError = targetHeight - surfaceDistance;
            Vector3 vertical = up * Mathf.Clamp(heightError * 2f, -4f, 4f);

            Vector3 horizontal;
            if (diving)
            {
                horizontal = Vector3.ProjectOnPlane(player.position - body.position, up).normalized * diveSpeed;
            }
            else
            {
                if (Time.time > nextDriftChange) PickNewDrift();
                horizontal = driftDirection * driftSpeed;
            }

            // Blended rather than hard-set, so a knockback hit or a split's scatter kick still shows for a moment
            // instead of being instantly overwritten every physics step.
            Vector3 desired = horizontal + vertical;
            body.linearVelocity = Vector3.Lerp(body.linearVelocity, desired, 0.15f);
        }

        void PickNewDrift()
        {
            driftDirection = Vector3.ProjectOnPlane(Random.onUnitSphere, gravity.Up).normalized;
            nextDriftChange = Time.time + Random.Range(2f, 5f);
        }

        void OnCollisionEnter(Collision collision)
        {
            var target = collision.collider.GetComponentInParent<Damageable>();
            if (target == null || !target.isPlayer) return;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            target.TakeDamage(contactDamage, point);
        }
    }
}
