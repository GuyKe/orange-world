using UnityEngine;

namespace OrangeWorld
{
    public class Damageable : MonoBehaviour
    {
        public float maxHealth = 30f;
        public bool isPlayer;
        public bool destroyOnDeath = true;
        [Tooltip("Ignores further hits for this long, so one swing's multiple contacts count once.")]
        public float invulnerableSeconds = 0.15f;

        public float Health { get; private set; }
        public bool Dead { get; private set; }
        public float LastDamageTime { get; private set; } = -999f;

        public event System.Action<float, Vector3> Damaged;
        public event System.Action<Damageable> Died;

        MeshRenderer[] flashRenderers;
        Color[] baseColors;
        float flash;

        void Awake() => Health = maxHealth;

        void Start()
        {
            flashRenderers = GetComponentsInChildren<MeshRenderer>();
            baseColors = new Color[flashRenderers.Length];
            for (int i = 0; i < flashRenderers.Length; i++)
                baseColors[i] = flashRenderers[i].material.color;
        }

        public void SetMaxHealth(float value)
        {
            maxHealth = value;
            Health = value;
        }

        public void Heal(float amount)
        {
            if (!Dead) Health = Mathf.Min(maxHealth, Health + amount);
        }

        public void Revive()
        {
            Dead = false;
            Health = maxHealth;
        }

        public void TakeDamage(float amount, Vector3 point)
        {
            if (Dead || amount <= 0f || Time.time - LastDamageTime < invulnerableSeconds) return;

            LastDamageTime = Time.time;
            Health -= amount;
            flash = 1f;
            Damaged?.Invoke(amount, point);
            if (Health > 0f) return;

            Dead = true;
            flash = 0f;
            ApplyFlash(0f); // Clones spawned on death copy the current material colors.
            Died?.Invoke(this);
            if (destroyOnDeath) Destroy(gameObject);
        }

        void Update()
        {
            if (flash <= 0f) return;
            flash = Mathf.Max(0f, flash - Time.deltaTime * 5f);
            ApplyFlash(flash);
        }

        void ApplyFlash(float amount)
        {
            if (flashRenderers == null) return;
            for (int i = 0; i < flashRenderers.Length; i++)
                flashRenderers[i].material.color = Color.Lerp(baseColors[i], Color.white, amount);
        }
    }
}
