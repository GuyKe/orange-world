using UnityEngine;

namespace OrangeWorld
{
    // Falls toward whichever planetoid pulls hardest. Between planetoids (no pull) it drifts, keeping its last "up".
    [RequireComponent(typeof(Rigidbody))]
    public class GravityBody : MonoBehaviour
    {
        public float gravityScale = 1f;

        public Vector3 Up { get; private set; } = Vector3.up;
        public GravityAttractor Attractor { get; private set; }

        Rigidbody body;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.useGravity = false;
        }

        void FixedUpdate()
        {
            GravityAttractor best = null;
            float bestGravity = 0f;
            foreach (var attractor in GravityAttractor.All)
            {
                float g = attractor.GravityAt(body.position);
                if (g > bestGravity)
                {
                    bestGravity = g;
                    best = attractor;
                }
            }

            Attractor = best;
            if (best == null) return;

            Up = best.UpAt(body.position);
            if (!body.isKinematic)
                body.AddForce(-Up * (bestGravity * gravityScale), ForceMode.Acceleration);
        }
    }
}
