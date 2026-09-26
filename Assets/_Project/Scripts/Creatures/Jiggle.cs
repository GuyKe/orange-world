using UnityEngine;

namespace OrangeWorld
{
    // Squash-and-stretch on a visual child: stretches along velocity, idles with a wobble, and squishes when hit.
    [RequireComponent(typeof(Rigidbody))]
    public class Jiggle : MonoBehaviour
    {
        public Transform visual;
        public float stretchPerSpeed = 0.05f;
        public float maxStretch = 0.5f;
        public float wobbleAmount = 0.06f;
        public float wobbleFrequency = 2.5f;
        public float punchStiffness = 250f;
        public float punchDamping = 9f;

        Rigidbody body;
        Vector3 baseScale;
        Vector3 stretchAxis = Vector3.up;
        float punch, punchVelocity, seed;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            baseScale = visual.localScale;
            seed = Random.value * 10f;
            var damageable = GetComponent<Damageable>();
            if (damageable != null)
                damageable.Damaged += (amount, _) => Punch(Mathf.Clamp(amount / 20f, 0.2f, 1.5f));
        }

        public void Punch(float strength) => punchVelocity -= strength * 8f;

        public void ResetVisual()
        {
            visual.localScale = baseScale;
            visual.localRotation = Quaternion.identity;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            punchVelocity += (-punch * punchStiffness - punchVelocity * punchDamping) * dt;
            punch += punchVelocity * dt;

            Vector3 velocity = body.linearVelocity;
            float speed = velocity.magnitude;
            if (speed > 0.5f)
                stretchAxis = Vector3.Slerp(stretchAxis, velocity / speed, 1f - Mathf.Exp(-10f * dt)).normalized;

            float wobble = Mathf.Sin((Time.time + seed) * wobbleFrequency * 2f * Mathf.PI) * wobbleAmount;
            float stretch = Mathf.Max(0.3f, 1f + Mathf.Min(speed * stretchPerSpeed, maxStretch) + punch + wobble);
            float squash = 1f / Mathf.Sqrt(stretch);

            visual.rotation = Quaternion.FromToRotation(Vector3.up, stretchAxis);
            visual.localScale = Vector3.Scale(baseScale, new Vector3(squash, stretch, squash));
        }
    }
}
