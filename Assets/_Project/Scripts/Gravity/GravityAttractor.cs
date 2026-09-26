using System.Collections.Generic;
using UnityEngine;

namespace OrangeWorld
{
    public class GravityAttractor : MonoBehaviour
    {
        public static readonly List<GravityAttractor> All = new();

        [Tooltip("Acceleration at the surface, in m/s².")]
        public float surfaceGravity = 9.81f;
        [Tooltip("Gravity reaches out to this many radii from the center, falling off with the inverse square.")]
        public float influenceRadii = 3f;

        public float Radius { get; private set; }

        void Awake() => Radius = ComputeRadius();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public float GravityAt(Vector3 point)
        {
            float distance = Vector3.Distance(point, transform.position);
            if (distance > Radius * influenceRadii) return 0f;
            float r = Mathf.Max(distance, Radius);
            return surfaceGravity * Radius * Radius / (r * r);
        }

        public Vector3 UpAt(Vector3 point) => (point - transform.position).normalized;

        float ComputeRadius()
        {
            Vector3 scale = transform.lossyScale;
            float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            var sphere = GetComponent<SphereCollider>();
            return sphere != null ? sphere.radius * maxScale : maxScale * 0.5f;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, ComputeRadius() * influenceRadii);
        }
    }
}
