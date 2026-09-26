using System.Collections.Generic;
using UnityEngine;

namespace OrangeWorld
{
    // Grab with one hand to hold it still; grab with both and pull apart to stretch it, Y-shaped, until it pops.
    [RequireComponent(typeof(Rigidbody), typeof(Damageable))]
    public class Stretchable : MonoBehaviour
    {
        public Transform visual;
        [Tooltip("Roughly the creature's resting diameter. Stretch is measured relative to this.")]
        public float restLength = 0.9f;
        [Tooltip("World distance between the two grabbing hands at which the creature pops.")]
        public float snapLength = 2.2f;
        public float pullStrength = 60f;
        public float pullDamping = 10f;

        public bool IsHeld => grips.Count > 0;

        readonly Dictionary<HandGrabber, Vector3> grips = new();
        readonly List<HandGrabber> handsScratch = new();
        Rigidbody body;
        Damageable damageable;
        Jiggle jiggle;
        Vector3 baseScale = Vector3.one;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            damageable = GetComponent<Damageable>();
            jiggle = GetComponent<Jiggle>();
            if (visual != null) baseScale = visual.localScale;
        }

        public bool TryGrab(HandGrabber hand)
        {
            if (grips.Count >= 2 || grips.ContainsKey(hand)) return false;
            grips[hand] = transform.InverseTransformPoint(hand.HandPosition);
            SetIgnoreCollision(hand, true);
            return true;
        }

        public void Release(HandGrabber hand)
        {
            if (grips.Remove(hand)) SetIgnoreCollision(hand, false);
        }

        void SetIgnoreCollision(HandGrabber hand, bool ignore)
        {
            foreach (var mine in GetComponentsInChildren<Collider>())
                foreach (var theirs in hand.GetComponentsInChildren<Collider>())
                    Physics.IgnoreCollision(mine, theirs, ignore);
        }

        void FixedUpdate()
        {
            if (grips.Count == 0)
            {
                if (jiggle != null) jiggle.ExternalOverride = false;
                return;
            }

            if (jiggle != null) jiggle.ExternalOverride = true;

            foreach (var grip in grips)
            {
                Vector3 anchor = transform.TransformPoint(grip.Value);
                Vector3 pointVelocity = body.GetPointVelocity(anchor);
                Vector3 pull = (grip.Key.HandPosition - anchor) * pullStrength - pointVelocity * pullDamping;
                body.AddForceAtPosition(pull, anchor, ForceMode.Acceleration);
            }

            if (grips.Count < 2)
            {
                if (visual != null) visual.localScale = baseScale;
                return;
            }

            handsScratch.Clear();
            handsScratch.AddRange(grips.Keys);
            Vector3 a = handsScratch[0].HandPosition;
            Vector3 b = handsScratch[1].HandPosition;
            float distance = Vector3.Distance(a, b);
            ApplyStretchVisual(a, b, distance);

            if (distance >= snapLength) Pop(Vector3.Lerp(a, b, 0.5f));
        }

        void ApplyStretchVisual(Vector3 a, Vector3 b, float distance)
        {
            if (visual == null) return;
            float stretch = Mathf.Max(1f, distance / restLength);
            float squash = 1f / Mathf.Sqrt(stretch);
            visual.rotation = Quaternion.FromToRotation(Vector3.up, (b - a).normalized);
            visual.localScale = new Vector3(baseScale.x * squash, baseScale.y * stretch, baseScale.z * squash);
        }

        void Pop(Vector3 point)
        {
            foreach (var hand in handsScratch)
                Haptics.Pulse(hand.Node, 1f, 0.2f);
            foreach (var hand in handsScratch)
                hand.ForceRelease();

            Juice.Boing(point, 1f);
            damageable.Kill(point);
        }
    }
}
