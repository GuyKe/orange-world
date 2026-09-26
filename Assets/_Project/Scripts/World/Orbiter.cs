using UnityEngine;

namespace OrangeWorld
{
    public class Orbiter : MonoBehaviour
    {
        public Vector3 center;
        public Vector3 axis = Vector3.up;
        public float degreesPerSecond = 3f;
        public Vector3 spin = new(10f, 20f, 0f);

        void Update()
        {
            transform.RotateAround(center, axis, degreesPerSecond * Time.deltaTime);
            transform.Rotate(spin * Time.deltaTime, Space.Self);
        }
    }
}
