using UnityEngine;

namespace OrangeWorld
{
    // Keeps a growing population of creatures alive, biased toward the player's current planetoid.
    public class WaveSpawner : MonoBehaviour
    {
        public GameObject creaturePrefab;
        public GameObject gunnerPrefab;
        [Range(0f, 1f)] public float gunnerChance = 0.25f;
        public int startingCount = 8;
        public int maxCount = 30;
        public float secondsPerExtraCreature = 15f;
        public float spawnInterval = 1.5f;
        public float minDistanceFromPlayer = 10f;
        public Vector2 sizeRange = new(0.8f, 1.5f);
        [Range(0f, 1f)] public float homePlanetBias = 0.6f;

        PlanetWalker player;
        float nextSpawn;

        void Start() => player = FindFirstObjectByType<PlanetWalker>();

        void Update()
        {
            int target = Mathf.Min(maxCount, startingCount + Mathf.FloorToInt(Time.timeSinceLevelLoad / secondsPerExtraCreature));
            if (CreatureBrain.All.Count >= target || Time.time < nextSpawn) return;
            nextSpawn = Time.time + spawnInterval;
            TrySpawn();
        }

        void TrySpawn()
        {
            var planets = GravityAttractor.All;
            if (planets.Count == 0 || creaturePrefab == null) return;

            bool spawnGunner = gunnerPrefab != null && Random.value < gunnerChance;
            var prefab = spawnGunner ? gunnerPrefab : creaturePrefab;

            for (int attempt = 0; attempt < 6; attempt++)
            {
                var planet = planets[Random.Range(0, planets.Count)];
                if (player != null && player.Gravity.Attractor != null && Random.value < homePlanetBias)
                    planet = player.Gravity.Attractor;

                float size = Random.Range(sizeRange.x, sizeRange.y);
                Vector3 point = planet.transform.position + Random.onUnitSphere * (planet.Radius + size + 0.5f);
                if (player != null && (point - player.transform.position).sqrMagnitude < minDistanceFromPlayer * minDistanceFromPlayer)
                    continue;

                var creature = Instantiate(prefab, point, Random.rotation);
                creature.transform.localScale = Vector3.one * size;
                creature.GetComponent<Rigidbody>().mass *= size * size * size;
                var health = creature.GetComponent<Damageable>();
                health.SetMaxHealth(health.maxHealth * size);
                return;
            }
        }
    }
}
