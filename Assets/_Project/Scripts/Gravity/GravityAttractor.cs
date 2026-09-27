using System.Collections.Generic;
using UnityEngine;

namespace OrangeWorld
{
    public class GravityAttractor : MonoBehaviour
    {
        public static readonly List<GravityAttractor> All = new();

        [Tooltip("Acceleration at the surface, in m/s².")]
        public float surfaceGravity = 9.81f;
        [Tooltip("Gravity reaches out to this many radii from the center, falling off with the inverse square. Ignored when planar.")]
        public float influenceRadii = 3f;
        [Tooltip("Treats this as a flat plane (up = this transform's up, pull is straight down everywhere) instead of a " +
                 "sphere pulling toward its center - for a rectangular baseplate rather than a planetoid.")]
        public bool planar;
        [Tooltip("Only used when planar: how far above (or below) the surface gravity still applies before dropping to zero.")]
        public float planarHeight = 40f;

        public float Radius { get; private set; }

        void Awake() => Radius = ComputeRadius();
        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public float GravityAt(Vector3 point)
        {
            if (planar)
            {
                float height = Mathf.Abs(Vector3.Dot(point - transform.position, transform.up));
                return height <= planarHeight ? surfaceGravity : 0f;
            }

            float distance = Vector3.Distance(point, transform.position);
            if (distance > Radius * influenceRadii) return 0f;
            float r = Mathf.Max(distance, Radius);
            return surfaceGravity * Radius * Radius / (r * r);
        }

        public Vector3 UpAt(Vector3 point) => planar ? transform.up : (point - transform.position).normalized;

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
            if (planar) Gizmos.DrawWireCube(transform.position, new Vector3(ComputeRadius() * 2f, planarHeight * 2f, ComputeRadius() * 2f));
            else Gizmos.DrawWireSphere(transform.position, ComputeRadius() * influenceRadii);
        }
    }
}
