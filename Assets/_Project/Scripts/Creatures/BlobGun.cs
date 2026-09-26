using UnityEngine;

namespace OrangeWorld
{
    // Ranged blobs: lobs a glob at the player on a timer when in range with a clear line of sight.
    public class BlobGun : MonoBehaviour
    {
        public GameObject projectilePrefab;
        public Transform muzzle;
        public float range = 18f;
        public float fireInterval = 2f;
        public float projectileSpeed = 12f;
        public float damage = 8f;
        public float aimError = 0.08f;

        Transform player;
        float nextFire;

        void Start()
        {
            var walker = FindFirstObjectByType<PlanetWalker>();
            if (walker != null) player = walker.head;
            nextFire = Time.time + Random.Range(0.5f, fireInterval);
        }

        void Update()
        {
            if (player == null || projectilePrefab == null || Time.time < nextFire) return;

            Vector3 toPlayer = player.position - muzzle.position;
            float distance = toPlayer.magnitude;
            if (distance > range) return;

            Vector3 direction = toPlayer.normalized;
            bool blocked = Physics.Raycast(muzzle.position, direction, out var hit, distance)
                           && hit.collider.GetComponentInParent<Damageable>()?.isPlayer != true;
            if (blocked) return;

            Fire(direction);
            nextFire = Time.time + fireInterval * Random.Range(0.8f, 1.3f);
        }

        void Fire(Vector3 direction)
        {
            direction = (direction + Random.insideUnitSphere * aimError).normalized;
            var projectileGo = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
            var projectile = projectileGo.GetComponent<Projectile>();
            projectile.damage = damage;
            projectile.Launch(direction * projectileSpeed, transform.root.gameObject);
            Juice.Boing(muzzle.position, 0.3f);
        }
    }
}
