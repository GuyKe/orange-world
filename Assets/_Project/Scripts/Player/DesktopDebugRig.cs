using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace OrangeWorld
{
    // Lets you poke at the game in the Editor without a headset. Disables itself when an XR device is active.
    public class DesktopDebugRig : MonoBehaviour
    {
        public Transform cameraTransform;
        public Transform leftController;
        public Transform rightController;
        public float eyeHeight = 1.6f;
        public float lookSensitivity = 0.12f;
        public float windmillSpeed = 14f;

        float yaw, pitch, windmillAngle, leftPunch;

        void Start()
        {
            if (XRSettings.isDeviceActive) enabled = false;
        }

        void Update()
        {
            // XR Origin applies its own fallback height to the camera offset when no headset is present.
            float offsetHeight = cameraTransform.parent != null ? cameraTransform.parent.localPosition.y : 0f;
            cameraTransform.localPosition = new Vector3(0f, eyeHeight - offsetHeight, 0f);

            var mouse = Mouse.current;
            var keyboard = Keyboard.current;

            if (mouse != null && mouse.rightButton.isPressed)
            {
                Vector2 delta = mouse.delta.ReadValue() * lookSensitivity;
                yaw += delta.x;
                pitch = Mathf.Clamp(pitch - delta.y, -85f, 85f);
            }

            Quaternion look = Quaternion.Euler(pitch, yaw, 0f);
            Quaternion bodyYaw = Quaternion.Euler(0f, yaw, 0f);
            cameraTransform.localRotation = look;
            Vector3 eye = cameraTransform.localPosition;

            bool windmill = mouse != null && mouse.leftButton.isPressed;
            windmillAngle = windmill ? windmillAngle + windmillSpeed * Time.deltaTime : 0f;
            Vector3 rightOffset = windmill
                ? new Vector3(0.25f, -0.25f + Mathf.Sin(windmillAngle) * 0.55f, 0.3f + Mathf.Cos(windmillAngle) * 0.55f)
                : new Vector3(0.25f, -0.35f, 0.35f);

            bool punching = keyboard != null && keyboard.fKey.isPressed;
            leftPunch = Mathf.MoveTowards(leftPunch, punching ? 1f : 0f, Time.deltaTime * 8f);
            Vector3 leftOffset = new(-0.25f, -0.35f + leftPunch * 0.2f, 0.35f + leftPunch * 0.4f);

            rightController.localPosition = eye + bodyYaw * rightOffset;
            rightController.localRotation = look;
            leftController.localPosition = eye + bodyYaw * leftOffset;
            leftController.localRotation = look;
        }
    }
}
