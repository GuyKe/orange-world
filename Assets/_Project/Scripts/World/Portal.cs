using System.Collections.Generic;
using UnityEngine;

namespace OrangeWorld
{
    // A weird, funky little rift: pulls any nearby Rigidbody toward its core, then teleports it out of its linked
    // partner once it gets close enough, launching it back out along the partner's up axis - the same idea as a
    // JumpPad, just triggered by proximity instead of a trigger volume, and it works both ways.
    public class Portal : MonoBehaviour
    {
        public Portal linkedPortal;
        public float pullRadius = 8f;
        public float pullStrength = 6f;
        public float suckRadius = 1.1f;
        public float ejectSpeed = 10f;
        [Tooltip("Ignores a body that just arrived (at either end) for this long, so it can't immediately suck itself back in.")]
        public float arrivalCooldown = 1f;

        readonly Dictionary<Rigidbody, float> arrivedAt = new();

        void FixedUpdate()
        {
            if (linkedPortal == null) return;

            foreach (var hit in Physics.OverlapSphere(transform.position, pullRadius))
            {
                var body = hit.attachedRigidbody;
                if (body == null || body.isKinematic) continue;
                if (arrivedAt.TryGetValue(body, out float time) && Time.time - time < arrivalCooldown) continue;

                Vector3 toCenter = transform.position - body.position;
                float distance = toCenter.magnitude;
                if (distance < 0.05f) continue;

                if (distance <= suckRadius)
                {
                    Teleport(body);
                    continue;
                }

                Vector3 direction = toCenter / distance;
                float pull = pullStrength * Mathf.InverseLerp(pullRadius, suckRadius, distance);
                // A little tangential swirl on top of the inward pull, so anything caught spirals in rather than
                // flying straight at the core - fits the "weird funky vortex" look better than a plain magnet would.
                Vector3 swirl = Vector3.Cross(direction, transform.up) * (pull * 0.4f);
                body.AddForce(direction * pull + swirl, ForceMode.Acceleration);
            }
        }

        void Teleport(Rigidbody body)
        {
            Vector3 exitPoint = linkedPortal.transform.position + linkedPortal.transform.up * 0.6f;
            Vector3 exitVelocity = linkedPortal.transform.up * ejectSpeed;

            var walker = body.GetComponent<PlanetWalker>();
            if (walker != null)
            {
                walker.Teleport(exitPoint, body.rotation);
                walker.Launch(exitVelocity);
            }
            else
            {
                body.position = exitPoint;
                body.linearVelocity = exitVelocity;
            }

            // Cooldown at both ends: the body lands right next to (and well within suck range of) the linked
            // portal, so without this it would just get immediately sucked back in.
            arrivedAt[body] = Time.time;
            linkedPortal.arrivedAt[body] = Time.time;
            Juice.Boing(exitPoint, 1f);
        }
    }
}
