using UnityEngine;
using UnityEngine.InputSystem;

namespace OrangeWorld
{
    // Rig-root locomotion on spherical planetoids: head-relative thumbstick movement, snap turn, jump,
    // hand-anchored climbing, and launches. Keeps the rig's up aligned with local gravity.
    [RequireComponent(typeof(Rigidbody), typeof(GravityBody), typeof(CapsuleCollider))]
    public class PlanetWalker : MonoBehaviour
    {
        public Transform head;
        public float moveSpeed = 3f;
        public float acceleration = 15f;
        public float airControl = 0.25f;
        public float jumpSpeed = 5f;
        public float snapTurnDegrees = 45f;
        public float alignSpeed = 6f;
        public float maxClimbSpeed = 14f;
        public float lostInSpaceSeconds = 8f;

        public Rigidbody Body { get; private set; }
        public GravityBody Gravity { get; private set; }
        public bool Grounded { get; private set; }
        public bool Climbing { get; private set; }

        InputAction moveAction, turnAction, jumpAction;
        CapsuleCollider capsule;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        Vector3 climbVelocitySum;
        int climbAnchors;
        float pendingTurn;
        bool turnArmed = true;
        bool jumpQueued;
        float launchGraceUntil;
        float timeLost;
        readonly RaycastHit[] groundHits = new RaycastHit[8];

        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            Gravity = GetComponent<GravityBody>();
            capsule = GetComponent<CapsuleCollider>();
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;

            moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");
            moveAction.AddBinding("<XRController>{LeftHand}/{Primary2DAxis}");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            turnAction = new InputAction("Turn", InputActionType.Value, "<XRController>{RightHand}/{Primary2DAxis}", expectedControlType: "Vector2");

            jumpAction = new InputAction("Jump", InputActionType.Button, "<XRController>{RightHand}/{PrimaryButton}");
            jumpAction.AddBinding("<Keyboard>/space");
        }

        void OnEnable()
        {
            moveAction.Enable();
            turnAction.Enable();
            jumpAction.Enable();
        }

        void OnDisable()
        {
            moveAction.Disable();
            turnAction.Disable();
            jumpAction.Disable();
        }

        void OnDestroy()
        {
            moveAction.Dispose();
            turnAction.Dispose();
            jumpAction.Dispose();
        }

        public void AddClimbPull(Vector3 velocity)
        {
            climbVelocitySum += velocity;
            climbAnchors++;
        }

        public void Launch(Vector3 velocity)
        {
            Body.linearVelocity = velocity;
            launchGraceUntil = Time.time + 0.4f;
            Grounded = false;
        }

        public void Respawn()
        {
            Body.position = spawnPosition;
            Body.rotation = spawnRotation;
            Body.linearVelocity = Vector3.zero;
            timeLost = 0f;
        }

        void Update()
        {
            float turn = turnAction.ReadValue<Vector2>().x;
            if (turnArmed && Mathf.Abs(turn) > 0.7f)
            {
                pendingTurn += Mathf.Sign(turn) * snapTurnDegrees;
                turnArmed = false;
            }
            else if (Mathf.Abs(turn) < 0.3f)
            {
                turnArmed = true;
            }

            if (jumpAction.WasPressedThisFrame()) jumpQueued = true;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            Vector3 up = Gravity.Up;

            if (TrackLostInSpace(dt)) return;
            FitCapsuleToHead();
            ApplySnapTurn(up);
            AlignToGravity(up, dt);
            Grounded = CheckGrounded(up);

            if (climbAnchors > 0)
            {
                Climbing = true;
                Gravity.gravityScale = 0f;
                Body.linearVelocity = Vector3.ClampMagnitude(climbVelocitySum / climbAnchors, maxClimbSpeed);
                climbVelocitySum = Vector3.zero;
                climbAnchors = 0;
                jumpQueued = false;
                return;
            }

            Climbing = false;
            Gravity.gravityScale = 1f;

            Vector3 velocity = Body.linearVelocity;
            Vector3 vertical = Vector3.Project(velocity, up);
            Vector3 planar = velocity - vertical;
            Vector2 input = Vector2.ClampMagnitude(moveAction.ReadValue<Vector2>(), 1f);
            Vector3 desired = HeadRelative(input, up) * moveSpeed;
            bool launched = Time.time < launchGraceUntil;

            if (Grounded && !launched)
            {
                planar = Vector3.MoveTowards(planar, desired, acceleration * dt);
            }
            else if (input.sqrMagnitude > 0.01f)
            {
                // Air control can steer but never brakes a fling.
                float cap = Mathf.Max(planar.magnitude, moveSpeed);
                planar = Vector3.ClampMagnitude(planar + desired * (airControl * acceleration * dt / moveSpeed), cap);
            }

            if (jumpQueued && Grounded) vertical = up * jumpSpeed;
            jumpQueued = false;

            Body.linearVelocity = planar + vertical;
        }

        Vector3 HeadRelative(Vector2 input, Vector3 up)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, up);
            if (forward.sqrMagnitude < 1e-4f) forward = Vector3.ProjectOnPlane(head.up, up);
            forward.Normalize();
            Vector3 right = Vector3.Cross(up, forward);
            return forward * input.y + right * input.x;
        }

        void FitCapsuleToHead()
        {
            Vector3 local = transform.InverseTransformPoint(head.position);
            float height = Mathf.Clamp(local.y, 0.8f, 2.2f);
            capsule.height = height;
            capsule.center = new Vector3(local.x, height * 0.5f, local.z);
        }

        void ApplySnapTurn(Vector3 up)
        {
            if (pendingTurn == 0f) return;
            Quaternion turn = Quaternion.AngleAxis(pendingTurn, up);
            Vector3 pivot = head.position;
            Body.position = pivot + turn * (Body.position - pivot);
            Body.rotation = turn * Body.rotation;
            pendingTurn = 0f;
        }

        void AlignToGravity(Vector3 up, float dt)
        {
            Quaternion target = Quaternion.FromToRotation(Body.rotation * Vector3.up, up) * Body.rotation;
            Body.MoveRotation(Quaternion.Slerp(Body.rotation, target, 1f - Mathf.Exp(-alignSpeed * dt)));
        }

        bool CheckGrounded(Vector3 up)
        {
            float radius = capsule.radius * 0.9f;
            Vector3 footOffset = Body.rotation * new Vector3(capsule.center.x, 0f, capsule.center.z);
            Vector3 origin = Body.position + footOffset + up * (radius + 0.05f);
            int count = Physics.SphereCastNonAlloc(origin, radius, -up, groundHits, 0.15f, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var hitBody = groundHits[i].collider.attachedRigidbody;
                if (hitBody == Body) continue;
                if (hitBody != null && hitBody.GetComponent<FloppyHand>() != null) continue;
                return true;
            }
            return false;
        }

        bool TrackLostInSpace(float dt)
        {
            if (Gravity.Attractor != null)
            {
                timeLost = 0f;
                return false;
            }

            timeLost += dt;
            if (timeLost < lostInSpaceSeconds) return false;
            Respawn();
            return true;
        }
    }
}
