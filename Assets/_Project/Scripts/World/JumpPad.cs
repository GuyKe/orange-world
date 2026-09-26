using System.Collections.Generic;
using UnityEngine;

namespace OrangeWorld
{
    // Launches any rigidbody along this pad's up axis. Placed facing a neighboring planetoid to hop between worlds.
    public class JumpPad : MonoBehaviour
    {
        public float launchSpeed = 16f;
        public float cooldown = 0.5f;

        readonly Dictionary<Rigidbody, float> lastLaunch = new();

        void OnTriggerEnter(Collider other)
        {
            var body = other.attachedRigidbody;
            if (body == null || body.isKinematic) return;
            if (lastLaunch.TryGetValue(body, out float time) && Time.time - time < cooldown) return;
            lastLaunch[body] = Time.time;

            Vector3 velocity = transform.up * launchSpeed;
            var walker = body.GetComponent<PlanetWalker>();
            if (walker != null) walker.Launch(velocity);
            else body.linearVelocity = velocity;
            Juice.Boing(transform.position, 1f);
        }
    }
}
