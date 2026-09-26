using UnityEngine;
using UnityEngine.XR;

namespace OrangeWorld
{
    // Player health feedback: hands shift from orange to red as health drops, hits knock you back, death respawns.
    [RequireComponent(typeof(Damageable), typeof(PlanetWalker))]
    public class PlayerVitals : MonoBehaviour
    {
        public Transform head;
        public Renderer[] handRenderers;
        public Color healthyColor = new(1f, 0.55f, 0.1f);
        public Color hurtColor = new(0.9f, 0.05f, 0.2f);
        public float regenDelay = 4f;
        public float regenPerSecond = 8f;
        public float hitKnockback = 4f;

        public float barWidth = 0.12f;
        public float barHeight = 0.03f;
        public Vector3 barOffset = new(-0.12f, -0.09f, 0.16f);
        [Tooltip("Alpha controls how much the health bar shows through into your view.")]
        public Color barBackgroundColor = new(0.25f, 0.02f, 0.02f, 0.35f);
        public Color barLowColor = new(0.5f, 0.02f, 0.02f, 0.6f);
        public Color barFullColor = new(1f, 0.15f, 0.1f, 0.6f);
        public Color labelColor = new(1f, 0.3f, 0.25f, 0.7f);

        Damageable health;
        PlanetWalker walker;
        Transform barFill;
        SpriteRenderer barFillRenderer;

        void Awake()
        {
            health = GetComponent<Damageable>();
            walker = GetComponent<PlanetWalker>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void Start()
        {
            Tint();
            BuildHealthBar();
        }

        void Update()
        {
            UpdateHealthBar();

            if (health.Dead || health.Health >= health.maxHealth) return;
            if (Time.time - health.LastDamageTime < regenDelay) return;
            health.Heal(regenPerSecond * Time.deltaTime);
            Tint();
        }

        void OnDamaged(float amount, Vector3 point)
        {
            Haptics.Pulse(XRNode.LeftHand, 0.9f, 0.15f);
            Haptics.Pulse(XRNode.RightHand, 0.9f, 0.15f);
            Juice.Boing(point, 1f);

            Vector3 up = walker.Gravity.Up;
            Vector3 away = Vector3.ProjectOnPlane(transform.position - point, up).normalized;
            walker.Launch(walker.Body.linearVelocity + (away + up * 0.5f) * hitKnockback);
            Tint();
        }

        void OnDied(Damageable _)
        {
            walker.Respawn();
            health.Revive();
            Tint();
        }

        void Tint()
        {
            Color color = Color.Lerp(hurtColor, healthyColor, health.Health / health.maxHealth);
            foreach (var r in handRenderers)
                if (r != null) r.material.color = color;
        }

        // ---------- Head-locked HUD ----------

        void BuildHealthBar()
        {
            if (head == null) return;

            HudSprite.Create("Health Bar Background", head, barOffset + Vector3.forward * 0.001f,
                new Vector2(barWidth, barHeight), barBackgroundColor);

            barFillRenderer = HudSprite.Create("Health Bar Fill", head, barOffset, new Vector2(0f, barHeight), barFullColor);
            barFill = barFillRenderer.transform;

            var label = WorldText.Create("Health Bar Label", head, barOffset + Vector3.up * 0.035f, new Vector2(barWidth, 0.025f), 0.018f);
            label.text = "HEALTH";
            label.color = labelColor;
        }

        void UpdateHealthBar()
        {
            if (barFill == null) return;

            float fraction = Mathf.Clamp01(health.Health / health.maxHealth);
            barFill.localScale = new Vector3(barWidth * fraction, barHeight, 1f);
            barFill.localPosition = barOffset + Vector3.left * (barWidth * (1f - fraction) * 0.5f);
            barFillRenderer.color = Color.Lerp(barLowColor, barFullColor, fraction);
        }
    }
}
