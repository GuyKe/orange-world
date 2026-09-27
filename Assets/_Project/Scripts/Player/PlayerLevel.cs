using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

namespace OrangeWorld
{
    // Levels up from killing blobs. Each level needs more XP than the last (xpGrowthPerLevel compounds), and
    // grants a small permanent health bonus, so it's a slow, ever-steepening climb rather than a quick grind.
    [RequireComponent(typeof(Damageable))]
    public class PlayerLevel : MonoBehaviour
    {
        public Transform head;
        public float baseXPToLevel = 20f;
        [Tooltip("Each level needs this many times more XP than the one before it.")]
        public float xpGrowthPerLevel = 1.35f;
        public float xpPerNormalBlob = 4f;
        public float xpPerGunnerBlob = 7f;
        public float xpPerMeleeBlob = 6f;
        public float bonusHealthPerLevel = 5f;

        public float barWidth = 0.12f;
        public float barHeight = 0.03f;
        public Vector3 barOffset = new(-0.12f, 0.09f, 0.16f);
        [Tooltip("Alpha controls how much the level bar shows through into your view.")]
        public Color backgroundColor = new(0.02f, 0.18f, 0.22f, 0.35f);
        public Color fillColor = new(0.2f, 0.85f, 0.95f, 0.6f);
        public Color labelColor = new(0.4f, 0.9f, 1f, 0.7f);

        public int Level { get; private set; } = 1;
        public float XP { get; private set; }
        public float XPToNextLevel => baseXPToLevel * Mathf.Pow(xpGrowthPerLevel, Level - 1);

        Damageable health;
        Transform barFill;
        SpriteRenderer barFillRenderer;
        Text levelLabel;

        void Awake() => health = GetComponent<Damageable>();

        void OnEnable() => Damageable.AnyDied += OnAnyDied;
        void OnDisable() => Damageable.AnyDied -= OnAnyDied;

        void Start() => BuildBar();

        void OnAnyDied(Damageable dead)
        {
            // Only blobs (Blobling/GunnerBlobling/MeleeBlobling) carry a CreatureBrain, so this can't fire
            // from the player itself.
            if (dead.GetComponent<CreatureBrain>() == null) return;
            // Bosses pay out through BossReward instead, which is a lot more than any of the per-type values below.
            if (dead.GetComponent<BossReward>() != null) return;

            float gained = dead.GetComponent<BlobGun>() != null ? xpPerGunnerBlob
                : dead.GetComponent<MeleeBlob>() != null ? xpPerMeleeBlob
                : xpPerNormalBlob;
            GrantXP(gained);
        }

        public void GrantXP(float amount)
        {
            XP += amount;
            while (XP >= XPToNextLevel)
            {
                XP -= XPToNextLevel;
                LevelUp();
            }
            UpdateBar();
        }

        void LevelUp()
        {
            Level++;
            health.AddMaxHealth(bonusHealthPerLevel);
            Haptics.Pulse(XRNode.LeftHand, 0.7f, 0.2f);
            Haptics.Pulse(XRNode.RightHand, 0.7f, 0.2f);
            Juice.Boing(head != null ? head.position : transform.position, 0.8f);
        }

        // ---------- Head-locked HUD ----------

        void BuildBar()
        {
            if (head == null) return;

            HudSprite.Create("Level Bar Background", head, barOffset + Vector3.forward * 0.001f,
                new Vector2(barWidth, barHeight), backgroundColor);

            barFillRenderer = HudSprite.Create("Level Bar Fill", head, barOffset, new Vector2(0f, barHeight), fillColor);
            barFill = barFillRenderer.transform;

            levelLabel = WorldText.Create("Level Bar Label", head, barOffset + Vector3.up * 0.035f, new Vector2(barWidth, 0.025f), 0.018f);
            levelLabel.color = labelColor;
            UpdateBar();
        }

        void UpdateBar()
        {
            if (barFill == null) return;

            float fraction = Mathf.Clamp01(XP / XPToNextLevel);
            barFill.localScale = new Vector3(barWidth * fraction, barHeight, 1f);
            barFill.localPosition = barOffset + Vector3.left * (barWidth * (1f - fraction) * 0.5f);
            levelLabel.text = $"LEVEL {Level}";
        }
    }
}
