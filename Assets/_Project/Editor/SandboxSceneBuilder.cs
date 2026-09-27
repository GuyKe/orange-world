using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeWorld.EditorTools
{
    // Generates the Sandbox scene: a big flat-feeling baseplate with no wave spawner or shop, just a row of
    // punchable buttons that summon blobs, bosses, and weapons on demand, plus a God Mode toggle. Everything here
    // is built the same way as the Prototype scene's own creatures/weapons/bosses, just triggered by a button
    // press at runtime instead of being placed once when the scene is generated.
    public static class SandboxSceneBuilder
    {
        const string ScenePath = PrototypeSceneBuilder.SceneFolder + "/Sandbox.unity";

        const float BaseplateRadius = 25f;
        static readonly Vector3 BaseplatePosition = Vector3.zero;
        static readonly Vector3 SpawnPoint = new(0f, BaseplateRadius + 0.05f, 0f);

        [MenuItem("Orange World/4. Build Sandbox Scene", priority = 4)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            QuestProjectSetup.EnsureUrp();
            QuestProjectSetup.EnsureFolder(PrototypeSceneBuilder.MaterialFolder);
            QuestProjectSetup.EnsureFolder(PrototypeSceneBuilder.PrefabFolder);
            QuestProjectSetup.EnsureFolder(PrototypeSceneBuilder.SceneFolder);
            Random.InitState(7777);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var palette = PrototypeSceneBuilder.CreateBasePalette();
            var slippery = PrototypeSceneBuilder.PhysicsMat("Slippery", 0f, 0f, PhysicsMaterialCombine.Minimum, PhysicsMaterialCombine.Minimum);
            var bouncy = PrototypeSceneBuilder.PhysicsMat("Bouncy", 0.4f, 0.7f, PhysicsMaterialCombine.Average, PhysicsMaterialCombine.Maximum);

            var projectile = PrototypeSceneBuilder.BuildProjectilePrefab(palette);
            var blobling = PrototypeSceneBuilder.BuildBloblingPrefab(palette, bouncy);
            var gunnerBlobling = PrototypeSceneBuilder.BuildGunnerBloblingPrefab(palette, bouncy, projectile);
            var meleeBlobling = PrototypeSceneBuilder.BuildMeleeBloblingPrefab(palette, bouncy);
            var meleeBossPrefab = PrototypeSceneBuilder.BuildMeleeBossPrefab(palette, bouncy);
            var gunnerBossPrefab = PrototypeSceneBuilder.BuildGunnerBossPrefab(palette, bouncy, projectile);
            var flailPrefab = PrototypeSceneBuilder.BuildFlailPrefab(palette);
            var malletPrefab = PrototypeSceneBuilder.BuildMalletPrefab(palette);
            var bopperPrefab = PrototypeSceneBuilder.BuildBopperPrefab(palette);
            var yoyoPrefab = PrototypeSceneBuilder.BuildYoyoPrefab(palette);

            PrototypeSceneBuilder.BuildLightingAndSky();
            BuildBaseplate(palette);
            BuildTitle();

            var player = PrototypeSceneBuilder.BuildPlayer(palette, slippery, addVitals: true, spawnPosition: SpawnPoint);
            var playerHealth = player.GetComponent<Damageable>();

            BuildSpawnButton("Blob", new Vector3(-2.6f, 0f, 3.5f), palette.Candy[4], blobling,
                new Vector3(0f, 1.2f, 1f));
            BuildSpawnButton("Gunner Blob", new Vector3(-1.3f, 0f, 3.5f), palette.Candy[1], gunnerBlobling,
                new Vector3(0f, 1.2f, 1f));
            BuildSpawnButton("Melee Blob", new Vector3(0f, 0f, 3.5f), palette.Candy[5], meleeBlobling,
                new Vector3(0f, 1.2f, 1f));

            var meleeBoss = PrototypeSceneBuilder.BossPlanets[0];
            var gunnerBoss = PrototypeSceneBuilder.BossPlanets[1];
            BuildBossSummonButton("Melee Boss", new Vector3(1.3f, 0f, 3.5f), meleeBoss, meleeBossPrefab, null);
            BuildBossSummonButton("Gunner Boss", new Vector3(2.6f, 0f, 3.5f), gunnerBoss, gunnerBossPrefab, projectile);

            BuildSpawnButton("Flail", new Vector3(-3.25f, 0f, 5f), palette.Mallet, flailPrefab,
                new Vector3(0f, 1f, 0.8f));
            BuildSpawnButton("Mallet", new Vector3(-1.95f, 0f, 5f), palette.Stalk, malletPrefab,
                new Vector3(0f, 1f, 0.8f));
            BuildSpawnButton("Bopper", new Vector3(-0.65f, 0f, 5f), palette.Candy[0], bopperPrefab,
                new Vector3(0f, 1f, 0.8f));
            BuildSpawnButton("Yoyo", new Vector3(0.65f, 0f, 5f), palette.Candy[3], yoyoPrefab,
                new Vector3(0f, 1f, 0.8f));

            BuildGodModeButton(new Vector3(1.95f, 0f, 5f), playerHealth);
            BuildBackButton(new Vector3(3.25f, 0f, 5f), palette);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PrototypeSceneBuilder.RegisterScene(ScenePath, isMenu: false);
            Selection.activeGameObject = player;
            Debug.Log($"[Orange World] Built {ScenePath}.");
        }

        static void BuildBaseplate(PrototypeSceneBuilder.Palette p)
        {
            var mat = PrototypeSceneBuilder.Mat("SandboxBaseplate", new Color(0.4f, 0.45f, 0.5f), 0.2f);
            var baseplate = PrototypeSceneBuilder.Prim(PrimitiveType.Sphere, "Sandbox Baseplate", null,
                BaseplatePosition, Vector3.one * BaseplateRadius * 2f, mat);
            baseplate.AddComponent<GravityAttractor>();
        }

        static void BuildTitle()
        {
            Vector3 basePosition = SpawnPoint + Vector3.forward * 1.6f + Vector3.up * 2.2f;
            WorldText.Create("Sandbox Title", null, basePosition, new Vector2(4f, 0.6f), 0.4f).text = "SANDBOX";
        }

        // A cube button plus its floating label, at the given local offset from SpawnPoint.
        static GameObject BuildButtonVisual(string name, Vector3 localOffset, Vector2 size, Material material, string labelText, float fontHeight = 0.09f)
        {
            var button = PrototypeSceneBuilder.Prim(PrimitiveType.Cube, name + " Button", null,
                SpawnPoint + localOffset, new Vector3(size.x, size.y, 0.25f), material);

            var label = WorldText.Create(name + " Label", button.transform, Vector3.back * 0.14f,
                new Vector2(size.x + 0.15f, 0.3f), fontHeight);
            label.text = labelText;

            return button;
        }

        static Transform BuildSpawnPoint(string name, Vector3 localOffset)
        {
            var marker = new GameObject(name + " Spawn Point").transform;
            marker.position = SpawnPoint + localOffset;
            return marker;
        }

        static void BuildSpawnButton(string label, Vector3 buttonOffset, Material material, GameObject template, Vector3 spawnOffsetFromButton)
        {
            var button = BuildButtonVisual(label, buttonOffset, new Vector2(0.5f, 0.5f), material, "SUMMON\n" + label.ToUpperInvariant());
            var spawnPoint = BuildSpawnPoint(label, buttonOffset + spawnOffsetFromButton);

            var spawner = button.AddComponent<SandboxSpawnButton>();
            spawner.visual = button.transform;
            spawner.template = template;
            spawner.spawnPoint = spawnPoint;
        }

        static void BuildBossSummonButton(string label, Vector3 buttonOffset, PrototypeSceneBuilder.BossSpec boss, GameObject bossPrefab, GameObject projectilePrefab)
        {
            var bodyMaterial = PrototypeSceneBuilder.BossBodyMaterial(boss);
            var button = BuildButtonVisual(label, buttonOffset, new Vector2(0.5f, 0.5f), bodyMaterial, "SUMMON\n" + label.ToUpperInvariant());
            var spawnPoint = BuildSpawnPoint(label, buttonOffset + new Vector3(0f, 4f, 3f));

            var summon = button.AddComponent<SandboxBossSummonButton>();
            summon.visual = button.transform;
            summon.bossPrefab = bossPrefab;
            summon.spawnPoint = spawnPoint;
            summon.bossName = boss.Name;
            summon.scale = PrototypeSceneBuilder.BossScale(boss);
            summon.bodyMaterial = bodyMaterial;
            summon.projectilePrefab = projectilePrefab;
            summon.bucksReward = PrototypeSceneBuilder.BossBucksReward(boss);
            summon.xpReward = PrototypeSceneBuilder.BossXpReward(boss);
        }

        static void BuildGodModeButton(Vector3 buttonOffset, Damageable playerHealth)
        {
            var material = PrototypeSceneBuilder.Mat("GodModeButton", new Color(1f, 0.85f, 0.2f), 0.7f, 0.4f);
            var button = BuildButtonVisual("God Mode", buttonOffset, new Vector2(0.5f, 0.5f), material, "GOD MODE\nOFF");

            var toggle = button.AddComponent<SandboxGodModeButton>();
            toggle.visual = button.transform;
            toggle.playerHealth = playerHealth;
            toggle.label = button.GetComponentInChildren<UnityEngine.UI.Text>();
        }

        static void BuildBackButton(Vector3 buttonOffset, PrototypeSceneBuilder.Palette p)
        {
            var button = BuildButtonVisual("Back To Menu", buttonOffset, new Vector2(0.5f, 0.5f), p.Candy[2], "BACK TO\nMENU");

            var menuButton = button.AddComponent<MenuButton>();
            menuButton.sceneName = "MainMenu";
            menuButton.visual = button.transform;
        }
    }
}
