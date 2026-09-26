using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace OrangeWorld
{
    // Grip grabs the nearest collider: dynamic bodies are held with a joint (swing them as weapons),
    // static geometry becomes a climbing anchor that pulls the player, so releasing mid-pull flings them.
    [DefaultExecutionOrder(-50)] // Climb pulls must be queued before PlanetWalker.FixedUpdate consumes them.
    [RequireComponent(typeof(FloppyHand))]
    public class HandGrabber : MonoBehaviour
    {
        public enum Side { Left, Right }

        public Side side;
        public PlanetWalker walker;
        public float grabRadius = 0.12f;
        public float breakForce = 6000f;
        public float releaseDistance = 1.5f;

        public XRNode Node => side == Side.Left ? XRNode.LeftHand : XRNode.RightHand;
        public bool Holding => joint != null || anchoredToWorld;

        FloppyHand hand;
        InputAction gripAction;
        FixedJoint joint;
        ImpactDamager heldDamager;
        Collider[] walkerColliders = new Collider[0];
        readonly List<Collider> ignoredColliders = new();
        readonly Collider[] overlaps = new Collider[16];
        bool anchoredToWorld;
        Vector3 anchorPoint;

        void Awake()
        {
            hand = GetComponent<FloppyHand>();
            string xrHand = side == Side.Left ? "LeftHand" : "RightHand";
            gripAction = new InputAction("Grip", InputActionType.Button, "<XRController>{" + xrHand + "}/gripButton");
            gripAction.AddBinding(side == Side.Left ? "<Keyboard>/q" : "<Keyboard>/e");

            var ownDamager = GetComponent<ImpactDamager>();
            if (ownDamager != null) ownDamager.HapticNode = Node;
            if (walker != null) walkerColliders = walker.GetComponentsInChildren<Collider>();
        }

        void OnEnable() => gripAction.Enable();
        void OnDisable() => gripAction.Disable();
        void OnDestroy() => gripAction.Dispose();

        void Update()
        {
            if (gripAction.WasPressedThisFrame()) Grab();
            else if (gripAction.WasReleasedThisFrame()) Release();
        }

        void FixedUpdate()
        {
            if (joint != null && joint.connectedBody == null)
            {
                Release();
                return;
            }

            if (!anchoredToWorld) return;

            hand.Body.MovePosition(anchorPoint);
            Vector3 pull = anchorPoint - hand.target.position;
            if (pull.magnitude > releaseDistance)
            {
                Release();
                return;
            }
            if (walker != null) walker.AddClimbPull(pull / Time.fixedDeltaTime);
        }

        void Grab()
        {
            if (Holding) return;

            Vector3 center = hand.Body.position;
            int count = Physics.OverlapSphereNonAlloc(center, grabRadius, overlaps, ~0, QueryTriggerInteraction.Ignore);
            Collider best = null;
            float bestSqr = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                var candidate = overlaps[i];
                var body = candidate.attachedRigidbody;
                if (body == hand.Body) continue;
                if (body != null && walker != null && body == walker.Body) continue;
                if (body != null && body.GetComponent<FloppyHand>() != null) continue;

                float sqr = (candidate.ClosestPoint(center) - center).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            if (best == null) return;
            if (best.attachedRigidbody != null) Hold(best.attachedRigidbody);
            else AnchorToWorld();
            Haptics.Pulse(Node, 0.3f, 0.05f);
        }

        void Hold(Rigidbody body)
        {
            joint = gameObject.AddComponent<FixedJoint>();
            joint.connectedBody = body;
            joint.breakForce = breakForce;
            joint.breakTorque = breakForce;

            heldDamager = body.GetComponent<ImpactDamager>();
            if (heldDamager != null) heldDamager.HapticNode = Node;

            // Ignore the held object's whole root hierarchy (e.g. every link of a flail) against the player body.
            foreach (var held in body.transform.root.GetComponentsInChildren<Collider>())
            {
                foreach (var own in walkerColliders)
                    Physics.IgnoreCollision(held, own, true);
                ignoredColliders.Add(held);
            }
        }

        void AnchorToWorld()
        {
            anchoredToWorld = true;
            anchorPoint = hand.Body.position;
            hand.Body.isKinematic = true;
        }

        void Release()
        {
            if (joint != null) Destroy(joint);
            joint = null;

            if (heldDamager != null) heldDamager.HapticNode = null;
            heldDamager = null;

            foreach (var held in ignoredColliders)
            {
                if (held == null) continue;
                foreach (var own in walkerColliders)
                    Physics.IgnoreCollision(held, own, false);
            }
            ignoredColliders.Clear();

            if (anchoredToWorld)
            {
                anchoredToWorld = false;
                hand.Body.isKinematic = false;
                if (walker != null) hand.Body.linearVelocity = walker.Body.linearVelocity;
            }
        }

        void OnJointBreak(float force) => Release();
    }
}
