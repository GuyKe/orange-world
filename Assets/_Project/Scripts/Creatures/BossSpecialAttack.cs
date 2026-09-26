using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace OrangeWorld
{
    // A boss-only attack on top of its normal contact damage or gunfire, wired up by PrototypeSceneBuilder.
    // SpawnBoss: melee bosses ground-slam a radius around themselves, gunner bosses unleash a shotgun-style
    // barrage. Which one fires depends only on whether a projectilePrefab was assigned.
    [RequireComponent(typeof(GravityBody))]
    public class BossSpecialAttack : MonoBehaviour
    {
        public float cooldown = 6f;
        public float telegraphSeconds = 0.9f;
        public float range = 12f;

        [Header("Ground Slam (melee bosses)")]
        public float slamRadius = 6f;
        public float slamDamage = 30f;
        public float slamKnockback = 10f;

        [Header("Barrage (gunner bosses)")]
        public GameObject projectilePrefab;
        public Transform muzzle;
        public int barrageShots = 6;
        public float barrageSpreadDegrees = 30f;
        public float barrageProjectileSpeed = 14f;
        public float barrageDamagePerShot = 8f;

        GravityBody gravity;
        Jiggle jiggle;
        Transform player;
        float nextAttack;
        bool attacking;

        void Awake()
        {
            gravity = GetComponent<GravityBody>();
            jiggle = GetComponent<Jiggle>();
        }

        void Start()
        {
            var walker = FindFirstObjectByType<PlanetWalker>();
            if (walker != null) player = walker.transform;
            nextAttack = Time.time + Random.Range(2f, cooldown);
        }

        void Update()
        {
            if (attacking || player == null || Time.time < nextAttack) return;
            if ((player.position - transform.position).sqrMagnitude > range * range) return;

            nextAttack = Time.time + cooldown;
            StartCoroutine(projectilePrefab != null ? BarrageRoutine() : SlamRoutine());
        }

        IEnumerator SlamRoutine()
        {
            attacking = true;
            // Negative strength stretches the visual upward (a wind-up), the opposite of the squash a hit gives it.
            if (jiggle != null) jiggle.Punch(-2f);
            yield return new WaitForSeconds(telegraphSeconds);

            foreach (var hit in Physics.OverlapSphere(transform.position, slamRadius))
            {
                var target = hit.GetComponentInParent<Damageable>();
                if (target == null || !target.isPlayer) continue;
                target.TakeDamage(slamDamage, hit.transform.position);

                var body = hit.GetComponentInParent<Rigidbody>();
                if (body != null)
                {
                    Vector3 away = (body.position - transform.position).normalized;
                    body.AddForce(away * slamKnockback, ForceMode.VelocityChange);
                }
            }

            Haptics.Pulse(XRNode.LeftHand, 1f, 0.3f);
            Haptics.Pulse(XRNode.RightHand, 1f, 0.3f);
            Juice.Boing(transform.position, 1f);
            attacking = false;
        }

        IEnumerator BarrageRoutine()
        {
            attacking = true;
            if (jiggle != null) jiggle.Punch(-1.5f);
            yield return new WaitForSeconds(telegraphSeconds);

            if (player != null && muzzle != null)
            {
                Vector3 toPlayer = (player.position - muzzle.position).normalized;
                Vector3 axis = Vector3.Cross(toPlayer, gravity.Up);
                if (axis.sqrMagnitude < 0.01f) axis = Vector3.up;
                axis.Normalize();

                for (int i = 0; i < barrageShots; i++)
                {
                    float t = barrageShots > 1 ? (float)i / (barrageShots - 1) - 0.5f : 0f;
                    Vector3 direction = Quaternion.AngleAxis(t * barrageSpreadDegrees, axis) * toPlayer;
                    var projectileGo = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
                    var projectile = projectileGo.GetComponent<Projectile>();
                    projectile.damage = barrageDamagePerShot;
                    projectile.Launch(direction * barrageProjectileSpeed, transform.root.gameObject);
                }
                Juice.Boing(muzzle.position, 0.8f);
            }
            attacking = false;
        }
    }
}
