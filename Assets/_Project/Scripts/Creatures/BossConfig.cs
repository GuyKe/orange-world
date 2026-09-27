using UnityEngine;

namespace OrangeWorld
{
    // Turns a plain melee/gunner boss-variant blob instance into an actual boss: scales it up, scales its health
    // and contact damage to match, gives it a fixed body color, and wires up BossHealthBar, BossReward, and
    // BossSpecialAttack. Called right after Instantiate by PrototypeSceneBuilder.SpawnBoss (for a boss planet) or
    // SandboxBossSummonButton (for a sandbox-summoned one) - the same setup either way.
    //
    // A plain static method rather than a MonoBehaviour configured via AddComponent-then-assign: AddComponent runs
    // the new component's Awake immediately, before the caller's next line could ever assign a field on the
    // reference it returns, so there's no safe way to hand it per-boss numbers that way.
    public static class BossConfig
    {
        public static void Configure(GameObject instance, string bossName, float scale, Material bodyMaterial,
            GameObject projectilePrefab, int bucksReward, float xpReward)
        {
            instance.transform.localScale = Vector3.one * scale;

            var body = instance.GetComponent<Rigidbody>();
            if (body != null) body.mass *= scale * scale * scale;

            var health = instance.GetComponent<Damageable>();
            if (health != null) health.SetMaxHealth(health.maxHealth * scale * 2.5f);

            var brain = instance.GetComponent<CreatureBrain>();
            if (brain != null) brain.contactDamage *= 2f;

            var bodyRenderer = instance.transform.Find("Visual/Body")?.GetComponent<Renderer>();
            if (bodyRenderer != null && bodyMaterial != null) bodyRenderer.sharedMaterial = bodyMaterial;

            instance.AddComponent<BossHealthBar>().bossName = bossName;

            var reward = instance.AddComponent<BossReward>();
            reward.bossName = bossName;
            reward.bucksReward = bucksReward;
            reward.xpReward = xpReward;

            var special = instance.AddComponent<BossSpecialAttack>();
            var gun = instance.GetComponent<BlobGun>();
            if (gun != null)
            {
                special.projectilePrefab = projectilePrefab;
                special.muzzle = gun.muzzle;
                special.range = gun.range * 1.4f;
                special.barrageDamagePerShot = gun.damage;
            }
            else if (brain != null)
            {
                special.slamRadius = 2f + scale;
                special.range = special.slamRadius + 3f;
                special.slamDamage = brain.contactDamage;
            }
        }
    }
}
