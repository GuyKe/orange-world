using UnityEngine;

namespace OrangeWorld
{
    // For eyes. Everything is watching you.
    public class LookAtCamera : MonoBehaviour
    {
        public float turnSpeed = 6f;

        static Transform cameraTransform;

        void LateUpdate()
        {
            if (cameraTransform == null)
            {
                var main = Camera.main;
                if (main == null) return;
                cameraTransform = main.transform;
            }

            Vector3 direction = cameraTransform.position - transform.position;
            if (direction.sqrMagnitude < 1e-4f) return;
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }
    }
}
