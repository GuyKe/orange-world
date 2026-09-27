using UnityEngine;

namespace OrangeWorld
{
    // Like SandboxSpawnButton, but for bosses: also calls BossConfig.Configure on the clone, the same way
    // PrototypeSceneBuilder.SpawnBoss sets up a boss planet's occupant.
    public class SandboxBossSummonButton : SandboxButtonBase
    {
        public GameObject bossPrefab;
        public Transform spawnPoint;
        public string bossName = "Boss";
        public float scale = 5f;
        public Material bodyMaterial;
        public GameObject projectilePrefab;
        public int bucksReward = 50;
        public float xpReward = 100f;

        protected override void OnPressed(HandGrabber hand)
        {
            if (bossPrefab == null || spawnPoint == null) return;

            var instance = Instantiate(bossPrefab, spawnPoint.position, spawnPoint.rotation);
            instance.name = bossName + " Boss";
            BossConfig.Configure(instance, bossName, scale, bodyMaterial, projectilePrefab, bucksReward, xpReward);
        }
    }
}
