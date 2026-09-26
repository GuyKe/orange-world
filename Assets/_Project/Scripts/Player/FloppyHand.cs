using UnityEngine;

namespace OrangeWorld
{
    // A physics hand pulled toward the tracked controller by an underdamped spring, so it lags, overshoots
    // and wobbles. Heavy held objects drag it down naturally because the spring force is shared through the joint.
    [RequireComponent(typeof(Rigidbody))]
    public class FloppyHand : MonoBehaviour
    {
        public Transform target;
        public Rigidbody playerBody;
        [Tooltip("Spring strength pulling the hand toward the controller.")]
        public float stiffness = 700f;
        [Tooltip("Critical damping is 2*sqrt(stiffness); lower values make the hand wobble more.")]
        public float damping = 22f;
        public float rotationFollow = 25f;
        [Tooltip("Beyond this distance the spring stiffens sharply, like a rubber band reaching its limit.")]
        public float stretchLimit = 1.2f;
        [Tooltip("Beyond this distance the hand teleports back (e.g. after respawn).")]
        public float snapDistance = 4f;

        public Rigidbody Body { get; private set; }
        public Vector3 TargetVelocity { get; private set; }

        Vector3 lastTargetPosition;

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Body.useGravity = false;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Body.maxAngularVelocity = 50f;
        }

        void Start()
        {
            SnapToTarget();
            if (playerBody == null) return;
            foreach (var mine in GetComponentsInChildren<Collider>())
                foreach (var theirs in playerBody.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(mine, theirs);
        }

        public void SnapToTarget()
        {
            Body.position = target.position;
            Body.rotation = target.rotation;
            if (!Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
            lastTargetPosition = target.position;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            TargetVelocity = (target.position - lastTargetPosition) / dt;
            lastTargetPosition = target.position;
            if (Body.isKinematic) return;

            Vector3 offset = target.position - Body.position;
            float distance = offset.magnitude;
            if (distance > snapDistance)
            {
                SnapToTarget();
                return;
            }

            float k = stiffness;
            if (distance > stretchLimit) k *= 1f + (distance - stretchLimit) * 8f;
            k = Mathf.Min(k, 0.4f / (dt * dt));
            Vector3 acceleration = offset * k + (TargetVelocity - Body.linearVelocity) * damping;
            Body.AddForce(acceleration, ForceMode.Acceleration);

            Quaternion delta = target.rotation * Quaternion.Inverse(Body.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            if (Mathf.Abs(angle) > 0.1f && float.IsFinite(axis.x))
            {
                Vector3 desired = axis * (angle * Mathf.Deg2Rad * rotationFollow);
                Body.angularVelocity = Vector3.Lerp(Body.angularVelocity, desired, 0.5f);
            }
        }
    }
}
