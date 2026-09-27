using UnityEngine;

namespace OrangeWorld
{
    // Boss-only kill reward, added by PrototypeSceneBuilder.SpawnBoss: a lump of Blob Bucks and XP, a full
    // ultimate charge, and a floating "defeated" readout. PlayerBucks/PlayerLevel skip their usual per-type
    // payout for a death that carries this component, so these numbers are the whole reward, not a bonus on top.
    [RequireComponent(typeof(Damageable))]
    public class BossReward : MonoBehaviour
    {
        public string bossName = "Boss";
        public int bucksReward = 50;
        public float xpReward = 80f;
        public float toastSeconds = 3f;
        public Color toastColor = new(1f, 0.85f, 0.3f, 0.95f);

        void Awake() => GetComponent<Damageable>().Died += OnDied;

        void OnDied(Damageable dead)
        {
            var bucks = FindFirstObjectByType<PlayerBucks>();
            if (bucks != null) bucks.AddBucks(bucksReward);

            var level = FindFirstObjectByType<PlayerLevel>();
            if (level != null) level.GrantXP(xpReward);

            var ultimate = FindFirstObjectByType<PlayerUltimate>();
            if (ultimate != null) ultimate.FillCharge();

            BuildToast(dead.transform.position);
        }

        void BuildToast(Vector3 position)
        {
            var label = WorldText.Create(bossName + " Defeated Toast", null, position + Vector3.up * 1.5f,
                new Vector2(6f, 1.6f), 0.5f);
            label.color = toastColor;
            label.text = $"{bossName} DEFEATED!\n+{bucksReward} Blob Bucks   +{Mathf.RoundToInt(xpReward)} XP";

            var root = label.transform.parent.gameObject;
            root.AddComponent<LookAtCamera>();
            Destroy(root, toastSeconds);
        }
    }
}
