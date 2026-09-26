using UnityEngine;
using UnityEngine.XR;

namespace OrangeWorld
{
    // Player health feedback: hands shift from orange to red as health drops, hits knock you back, death respawns.
    [RequireComponent(typeof(Damageable), typeof(PlanetWalker))]
    public class PlayerVitals : MonoBehaviour
    {
        public Renderer[] handRenderers;
        public Color healthyColor = new(1f, 0.55f, 0.1f);
        public Color hurtColor = new(0.9f, 0.05f, 0.2f);
        public float regenDelay = 4f;
        public float regenPerSecond = 8f;
        public float hitKnockback = 4f;

        Damageable health;
        PlanetWalker walker;

        void Awake()
        {
            health = GetComponent<Damageable>();
            walker = GetComponent<PlanetWalker>();
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        void Start() => Tint();

        void Update()
        {
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
    }
}
