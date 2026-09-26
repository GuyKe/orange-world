using UnityEngine;

namespace OrangeWorld
{
    // On death, bursts into smaller copies of itself until they're too small to split again.
    [RequireComponent(typeof(Damageable))]
    public class SplitOnDeath : MonoBehaviour
    {
        public int pieces = 2;
        public float sizeFactor = 0.65f;
        public float minSize = 0.4f;
        public float scatterSpeed = 4f;

        void Awake() => GetComponent<Damageable>().Died += Split;

        void Split(Damageable dead)
        {
            float size = transform.localScale.x * sizeFactor;
            if (size < minSize) return;

            var jiggle = GetComponent<Jiggle>();
            if (jiggle != null) jiggle.ResetVisual();

            var body = GetComponent<Rigidbody>();
            var gravity = GetComponent<GravityBody>();
            Vector3 up = gravity != null ? gravity.Up : Vector3.up;
            float massFactor = sizeFactor * sizeFactor * sizeFactor;

            for (int i = 0; i < pieces; i++)
            {
                Vector3 offset = Random.onUnitSphere * (size * 0.5f);
                var clone = Instantiate(gameObject, transform.position + offset, Random.rotation);
                clone.name = gameObject.name;
                clone.transform.localScale = Vector3.one * size;

                var cloneBody = clone.GetComponent<Rigidbody>();
                cloneBody.mass = body.mass * massFactor;
                cloneBody.linearVelocity = (up + offset.normalized) * scatterSpeed;
                clone.GetComponent<Damageable>().SetMaxHealth(dead.maxHealth * sizeFactor);
            }
        }
    }
}
