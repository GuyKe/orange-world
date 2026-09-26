using UnityEngine;
using UnityEngine.UI;

namespace OrangeWorld
{
    // Tracks Blob Bucks, earned by killing blobs, and shows the running total as a faded HUD counter.
    // A normal Blobling is worth less than a GunnerBlobling, distinguished by whether it carries a BlobGun.
    public class PlayerBucks : MonoBehaviour
    {
        public Transform head;
        public int normalBlobValue = 1;
        public int gunnerBlobValue = 2;

        public Vector2 counterSize = new(0.16f, 0.04f);
        public float counterFontHeight = 0.022f;
        public Vector3 counterOffset = new(-0.09f, -0.04f, 0.16f);
        [Tooltip("Alpha controls how much the counter shows through into your view.")]
        public Color backgroundColor = new(0.05f, 0.05f, 0.05f, 0.35f);
        public Color textColor = new(1f, 0.85f, 0.1f, 0.85f);

        public int Bucks { get; private set; }

        Text counterText;

        void OnEnable() => Damageable.AnyDied += OnAnyDied;
        void OnDisable() => Damageable.AnyDied -= OnAnyDied;

        void Start() => BuildCounter();

        void OnAnyDied(Damageable dead)
        {
            // Only blobs (Blobling/GunnerBlobling) carry a CreatureBrain, so this can't fire from the player itself.
            if (dead.GetComponent<CreatureBrain>() == null) return;
            Bucks += dead.GetComponent<BlobGun>() != null ? gunnerBlobValue : normalBlobValue;
            UpdateCounter();
        }

        public bool TrySpend(int amount)
        {
            if (Bucks < amount) return false;
            Bucks -= amount;
            UpdateCounter();
            return true;
        }

        // ---------- Head-locked HUD ----------

        void BuildCounter()
        {
            if (head == null) return;

            HudSprite.Create("Blob Bucks Background", head, counterOffset + Vector3.forward * 0.001f, counterSize, backgroundColor);

            counterText = WorldText.Create("Blob Bucks Counter", head, counterOffset, counterSize, counterFontHeight);
            counterText.color = textColor;
            UpdateCounter();
        }

        void UpdateCounter()
        {
            if (counterText != null) counterText.text = $"Blob Bucks: {Bucks}";
        }
    }
}
