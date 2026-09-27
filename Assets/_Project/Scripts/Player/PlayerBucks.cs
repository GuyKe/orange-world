using UnityEngine;
using UnityEngine.UI;

namespace OrangeWorld
{
    // Tracks Blob Bucks, earned by killing blobs, and shows the running total as a faded HUD counter.
    // A normal Blobling is worth less than a GunnerBlobling or MeleeBlobling, told apart by whether it carries
    // a BlobGun or a MeleeBlob marker.
    public class PlayerBucks : MonoBehaviour
    {
        public Transform head;
        public int normalBlobValue = 1;
        public int gunnerBlobValue = 2;
        public int meleeBlobValue = 2;

        public Vector2 counterSize = new(0.16f, 0.04f);
        public float counterFontHeight = 0.022f;
        public Vector3 counterOffset = new(0.12f, 0.09f, 0.16f);
        [Tooltip("Alpha controls how much the counter shows through into your view.")]
        public Color backgroundColor = new(0.3f, 0.28f, 0.02f, 0.35f);
        public Color textColor = new(1f, 0.92f, 0.15f, 0.9f);

        public int Bucks { get; private set; }

        Text counterText;

        void OnEnable() => Damageable.AnyDied += OnAnyDied;
        void OnDisable() => Damageable.AnyDied -= OnAnyDied;

        void Start() => BuildCounter();

        void OnAnyDied(Damageable dead)
        {
            // Only blobs (Blobling/GunnerBlobling/MeleeBlobling) carry a CreatureBrain, so this can't fire
            // from the player itself.
            if (dead.GetComponent<CreatureBrain>() == null) return;
            // Bosses pay out through BossReward instead, which is a lot more than any of the per-type values below.
            if (dead.GetComponent<BossReward>() != null) return;

            AddBucks(dead.GetComponent<BlobGun>() != null ? gunnerBlobValue
                : dead.GetComponent<MeleeBlob>() != null ? meleeBlobValue
                : normalBlobValue);
        }

        public void AddBucks(int amount)
        {
            Bucks += amount;
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
