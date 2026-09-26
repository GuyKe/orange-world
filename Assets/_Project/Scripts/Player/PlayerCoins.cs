using UnityEngine;

namespace OrangeWorld
{
    // Tracks blob coins, earned by killing blobs, and shows progress toward the next spend as a faded HUD bar.
    // A normal Blobling is worth less than a GunnerBlobling, distinguished by whether it carries a BlobGun.
    public class PlayerCoins : MonoBehaviour
    {
        public Transform head;
        public int normalBlobValue = 1;
        public int gunnerBlobValue = 2;
        [Tooltip("How many coins the bar fills up to - matches the cost of the cheapest upgrade in the scene.")]
        public int barGoal = 25;

        public float barWidth = 0.12f;
        public float barHeight = 0.03f;
        public Vector3 barOffset = new(-0.09f, -0.04f, 0.16f);
        [Tooltip("Alpha controls how much the coin bar shows through into your view.")]
        public Color barBackgroundColor = new(0.05f, 0.05f, 0.05f, 0.35f);
        public Color barFillColor = new(1f, 0.85f, 0.1f, 0.6f);

        public int Coins { get; private set; }

        Transform barFill;
        SpriteRenderer barFillRenderer;

        void OnEnable() => Damageable.AnyDied += OnAnyDied;
        void OnDisable() => Damageable.AnyDied -= OnAnyDied;

        void Start() => BuildCoinBar();

        void Update() => UpdateCoinBar();

        void OnAnyDied(Damageable dead)
        {
            // Only blobs (Blobling/GunnerBlobling) carry a CreatureBrain, so this can't fire from the player itself.
            if (dead.GetComponent<CreatureBrain>() == null) return;
            Coins += dead.GetComponent<BlobGun>() != null ? gunnerBlobValue : normalBlobValue;
        }

        public bool TrySpend(int amount)
        {
            if (Coins < amount) return false;
            Coins -= amount;
            return true;
        }

        // ---------- Head-locked HUD ----------

        void BuildCoinBar()
        {
            if (head == null) return;

            HudSprite.Create("Coin Bar Background", head, barOffset + Vector3.forward * 0.001f,
                new Vector2(barWidth, barHeight), barBackgroundColor);

            barFillRenderer = HudSprite.Create("Coin Bar Fill", head, barOffset, new Vector2(0f, barHeight), barFillColor);
            barFill = barFillRenderer.transform;
        }

        void UpdateCoinBar()
        {
            if (barFill == null || barGoal <= 0) return;

            float fraction = Mathf.Clamp01(Coins / (float)barGoal);
            barFill.localScale = new Vector3(barWidth * fraction, barHeight, 1f);
            barFill.localPosition = barOffset + Vector3.left * (barWidth * (1f - fraction) * 0.5f);
        }
    }
}
