using UnityEngine;

namespace OrangeWorld
{
    // Draws a sagging, wiggling noodle arm from a virtual shoulder to this hand. It thins as it stretches.
    [RequireComponent(typeof(LineRenderer))]
    public class ElasticArm : MonoBehaviour
    {
        public Transform head;
        public PlanetWalker walker;
        [Tooltip("-1 for the left shoulder, +1 for the right.")]
        public float side = 1f;
        public int segments = 14;
        public float restLength = 0.6f;
        public float thickness = 0.06f;
        public float sag = 0.12f;
        public float wiggle = 0.025f;

        LineRenderer line;

        void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = segments + 1;
        }

        void LateUpdate()
        {
            Vector3 up = walker != null ? walker.transform.up : Vector3.up;
            Vector3 right = Vector3.ProjectOnPlane(head.right, up).normalized;
            Vector3 shoulder = head.position - up * 0.25f + right * (0.17f * side);
            Vector3 hand = transform.position;
            Vector3 span = hand - shoulder;
            float length = span.magnitude;

            float slack = Mathf.Max(0f, restLength - length);
            Vector3 control = (shoulder + hand) * 0.5f - up * (sag + slack);
            Vector3 bendAxis = Vector3.Cross(span, up);
            bendAxis = bendAxis.sqrMagnitude > 1e-6f ? bendAxis.normalized : right;
            float phase = Time.time * 9f;

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float u = 1f - t;
                Vector3 point = u * u * shoulder + 2f * u * t * control + t * t * hand;
                point += bendAxis * (Mathf.Sin(t * 3f * Mathf.PI + phase) * wiggle * Mathf.Sin(t * Mathf.PI));
                line.SetPosition(i, point);
            }

            float thinning = Mathf.Sqrt(restLength / Mathf.Max(length, 0.01f));
            line.widthMultiplier = thickness * Mathf.Clamp(thinning, 0.3f, 1.5f);
        }
    }
}
