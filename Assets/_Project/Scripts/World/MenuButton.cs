using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OrangeWorld
{
    // A big punchable button: any hand that hits it hard enough plays a short press animation, then loads a scene.
    public class MenuButton : MonoBehaviour
    {
        public string sceneName = "Prototype";
        public Transform visual;
        public float pressDepth = 0.06f;
        public float pressDuration = 0.4f;
        public float minImpactSpeed = 1.2f;

        Vector3 restLocalPosition;
        bool pressed;

        void Awake()
        {
            if (visual != null) restLocalPosition = visual.localPosition;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (pressed || collision.relativeVelocity.magnitude < minImpactSpeed) return;

            var hand = collision.collider.GetComponentInParent<HandGrabber>();
            if (hand == null) return;

            pressed = true;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Haptics.Pulse(hand.Node, 0.6f, 0.15f);
            Juice.Boing(point, 1f);
            StartCoroutine(PressAndLoad());
        }

        IEnumerator PressAndLoad()
        {
            if (visual != null)
            {
                float half = pressDuration * 0.5f;
                Vector3 pressedLocalPosition = restLocalPosition - Vector3.forward * pressDepth;
                for (float t = 0f; t < half; t += Time.deltaTime)
                {
                    visual.localPosition = Vector3.Lerp(restLocalPosition, pressedLocalPosition, t / half);
                    yield return null;
                }
            }

            yield return new WaitForSeconds(pressDuration * 0.5f);
            SceneManager.LoadScene(sceneName);
        }
    }
}
