using UnityEngine;

namespace OrangeWorld
{
    // Floating health bar over a boss, added by PrototypeSceneBuilder.SpawnBoss. Kept in world space and
    // repositioned every frame instead of parented to the boss, so the boss's own (much larger) scale doesn't
    // stretch the bar - the same trap BuildBossPlanets avoids by keeping signage and jump pads in world space.
    [RequireComponent(typeof(Damageable))]
    public class BossHealthBar : MonoBehaviour
    {
        public string bossName = "Boss";
        public float heightAboveRadius = 1.2f;
        public float barWidth = 2.2f;
        public float barHeight = 0.22f;

        public Color backgroundColor = new(0.15f, 0.02f, 0.02f, 0.55f);
        public Color fillColor = new(1f, 0.15f, 0.1f, 0.85f);
        public Color labelColor = new(1f, 0.6f, 0.55f, 0.9f);

        Damageable health;
        SphereCollider sphere;
        GravityBody gravity;
        Transform root, fill;

        void Awake()
        {
            health = GetComponent<Damageable>();
            sphere = GetComponent<SphereCollider>();
            gravity = GetComponent<GravityBody>();
        }

        void Start()
        {
            root = new GameObject(bossName + " Health Bar").transform;

            HudSprite.Create("Background", root, Vector3.forward * 0.001f, new Vector2(barWidth, barHeight), backgroundColor);
            var fillRenderer = HudSprite.Create("Fill", root, Vector3.zero, new Vector2(barWidth, barHeight), fillColor);
            fill = fillRenderer.transform;

            var label = WorldText.Create("Label", root, Vector3.up * (barHeight * 1.8f), new Vector2(barWidth + 1f, barHeight * 1.6f), barHeight * 0.8f);
            label.color = labelColor;
            label.text = bossName;

            root.gameObject.AddComponent<LookAtCamera>();
            UpdateBar();
        }

        void LateUpdate()
        {
            if (root == null) return;

            float radius = sphere != null ? sphere.radius * transform.lossyScale.x : 1f;
            Vector3 up = gravity != null ? gravity.Up : Vector3.up;
            root.position = transform.position + up * (radius + heightAboveRadius);
            UpdateBar();
        }

        void UpdateBar()
        {
            if (fill == null) return;

            float fraction = Mathf.Clamp01(health.Health / health.maxHealth);
            fill.localScale = new Vector3(barWidth * fraction, barHeight, 1f);
            fill.localPosition = Vector3.left * (barWidth * (1f - fraction) * 0.5f);
        }

        // The bar lives outside the boss's hierarchy (see the class comment), so it has to be cleaned up by hand.
        void OnDestroy()
        {
            if (root != null) Destroy(root.gameObject);
        }
    }
}
