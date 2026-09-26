using UnityEngine;
using UnityEngine.XR;

namespace OrangeWorld
{
    public static class Haptics
    {
        public static void Pulse(XRNode node, float amplitude, float duration)
        {
            var device = InputDevices.GetDeviceAtXRNode(node);
            if (device.isValid)
                device.SendHapticImpulse(0, Mathf.Clamp01(amplitude), duration);
        }
    }
}
