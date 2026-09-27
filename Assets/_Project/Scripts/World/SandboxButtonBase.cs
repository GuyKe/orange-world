using System.Collections;
using UnityEngine;

namespace OrangeWorld
{
    // Shared punch-and-press-animation base for every interactive button in the sandbox (summon, god mode).
    // Unlike MenuButton, pressing one doesn't load a scene and isn't one-shot - it calls OnPressed and resets.
    public abstract class SandboxButtonBase : MonoBehaviour
    {
        public Transform visual;
        public float pressDepth = 0.06f;
        public float pressDuration = 0.3f;
        public float minImpactSpeed = 1.2f;

        Vector3 restLocalPosition;
        bool pressing;

        protected virtual void Awake()
        {
            if (visual != null) restLocalPosition = visual.localPosition;
        }

        protected abstract void OnPressed(HandGrabber hand);

        void OnCollisionEnter(Collision collision)
        {
            if (pressing || collision.relativeVelocity.magnitude < minImpactSpeed) return;
            var hand = collision.collider.GetComponentInParent<HandGrabber>();
            if (hand == null) return;

            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Haptics.Pulse(hand.Node, 0.6f, 0.15f);
            Juice.Boing(point, 1f);
            OnPressed(hand);
            StartCoroutine(PressAndRelease());
        }

        IEnumerator PressAndRelease()
        {
            pressing = true;
            if (visual != null)
            {
                float half = pressDuration * 0.5f;
                Vector3 pressedLocalPosition = restLocalPosition - Vector3.forward * pressDepth;
                for (float t = 0f; t < half; t += Time.deltaTime)
                {
                    visual.localPosition = Vector3.Lerp(restLocalPosition, pressedLocalPosition, t / half);
                    yield return null;
                }
                for (float t = 0f; t < half; t += Time.deltaTime)
                {
                    visual.localPosition = Vector3.Lerp(pressedLocalPosition, restLocalPosition, t / half);
                    yield return null;
                }
                visual.localPosition = restLocalPosition;
            }
            else
            {
                yield return new WaitForSeconds(pressDuration);
            }
            pressing = false;
        }
    }
}
