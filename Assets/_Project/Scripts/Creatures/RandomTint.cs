using UnityEngine;

namespace OrangeWorld
{
    public class RandomTint : MonoBehaviour
    {
        public Renderer[] targets;
        public float saturation = 0.75f;
        public float value = 1f;

        void Awake()
        {
            Color color = Color.HSVToRGB(Random.value, saturation, value);
            foreach (var target in targets)
                if (target != null) target.material.color = color;
        }
    }
}
