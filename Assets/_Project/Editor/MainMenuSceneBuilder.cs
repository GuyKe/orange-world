using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeWorld.EditorTools
{
    // Generates the main menu: a small platform with a punchable Play button, standing in the middle of a
    // few blobs hopping around on their own - the "menu background" is just the actual game running quietly.
    public static class MainMenuSceneBuilder
    {
        internal const string ScenePath = PrototypeSceneBuilder.SceneFolder + "/MainMenu.unity";

        const float PlatformRadius = 6f;
        static readonly Vector3 PlatformPosition = Vector3.zero;
        static readonly Vector3 SpawnPoint = new(0f, PlatformRadius + 0.05f, 0f);

        [MenuItem("Orange World/2. Build Main Menu Scene", priority = 2)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            QuestProjectSetup.EnsureUrp();
            QuestProjectSetup.EnsureFolder(PrototypeSceneBuilder.MaterialFolder);
            QuestProjectSetup.EnsureFolder(PrototypeSceneBuilder.PrefabFolder);
            QuestProjectSetup.EnsureFolder(PrototypeSceneBuilder.SceneFolder);
            Random.InitState(4242);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var palette = PrototypeSceneBuilder.CreateBasePalette();
            var slippery = PrototypeSceneBuilder.PhysicsMat("Slippery", 0f, 0f, PhysicsMaterialCombine.Minimum, PhysicsMaterialCombine.Minimum);
            var bouncy = PrototypeSceneBuilder.PhysicsMat("Bouncy", 0.4f, 0.7f, PhysicsMaterialCombine.Average, PhysicsMaterialCombine.Maximum);

            PrototypeSceneBuilder.BuildLightingAndSky();
            BuildPlatform();
            BuildBackdropPlanetoids(palette);
            SpawnDemoBlobs(palette, bouncy);
            BuildTitle();
            BuildPlayButton(palette);
            BuildSandboxButton(palette);
            BuildPurpleModeButton();
            var player = PrototypeSceneBuilder.BuildPlayer(palette, slippery, addVitals: false, spawnPosition: SpawnPoint);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PrototypeSceneBuilder.RegisterScene(ScenePath, isMenu: true);
            Selection.activeGameObject = player;
            Debug.Log($"[Orange World] Built {ScenePath}. It's now first in Build Settings, so Build And Run opens here.");
        }

        [MenuItem("Orange World/6. Build All Scenes", priority = 6)]
        public static void BuildAll()
        {
            Build();
            PrototypeSceneBuilder.Build();
            SandboxSceneBuilder.Build();
            PurpleModeSceneBuilder.Build();
        }

        static void BuildPlatform()
        {
            var platformMat = PrototypeSceneBuilder.Mat("MenuPlatform", new Color(1f, 0.5f, 0.1f), 0.15f);
            var platform = PrototypeSceneBuilder.Prim(PrimitiveType.Sphere, "Menu Platform", null,
                PlatformPosition, Vector3.one * PlatformRadius * 2f, platformMat);
            platform.AddComponent<GravityAttractor>();
        }

        static void BuildBackdropPlanetoids(PrototypeSceneBuilder.Palette p)
        {
            var parent = new GameObject("Backdrop").transform;
            Vector3[] positions = { new(-16f, 8f, 32f), new(20f, -10f, 28f), new(-6f, -18f, -34f) };
            foreach (var position in positions)
            {
                var planetoid = PrototypeSceneBuilder.Prim(PrimitiveType.Sphere, "Backdrop Planetoid", parent,
                    position, Vector3.one * Random.Range(6f, 11f), PrototypeSceneBuilder.Pick(p.Candy), collider: false);
                var orbiter = planetoid.AddComponent<Orbiter>();
                orbiter.axis = Random.onUnitSphere;
                orbiter.degreesPerSecond = Random.Range(1f, 3f);
                orbiter.spin = Random.insideUnitSphere * 8f;
            }
        }

        static void SpawnDemoBlobs(PrototypeSceneBuilder.Palette p, PhysicsMaterial bouncy)
        {
            var blobling = PrototypeSceneBuilder.BuildBloblingPrefab(p, bouncy);
            int spawned = 0;
            for (int attempt = 0; attempt < 20 && spawned < 4; attempt++)
            {
                Vector3 point = PlatformPosition + Random.onUnitSphere * (PlatformRadius + 0.5f);
                if (Vector3.Distance(point, SpawnPoint) < 3f) continue;
                Object.Instantiate(blobling, point, Random.rotation);
                spawned++;
            }
        }

        static void BuildTitle()
        {
            Vector3 basePosition = SpawnPoint + Vector3.forward * 2.6f;
            WorldText.Create("Title", null, basePosition + Vector3.up * 1.85f, new Vector2(3f, 0.7f), 0.5f).text = "ORANGE WORLD";
            WorldText.Create("Subtitle", null, basePosition + Vector3.up * 1.35f, new Vector2(3f, 0.3f), 0.16f).text = "a fever-dream physics RPG";
        }

        static void BuildPlayButton(PrototypeSceneBuilder.Palette p)
        {
            Vector3 position = SpawnPoint + Vector3.forward * 1.6f + Vector3.up * 1.1f;
            var button = PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Play Button", null, position,
                new Vector3(0.5f, 0.5f, 0.25f), p.Candy[4]);

            var label = WorldText.Create("Play Label", button.transform, Vector3.back * 0.14f, new Vector2(0.6f, 0.25f), 0.14f);
            label.text = "PLAY";

            var menuButton = button.AddComponent<MenuButton>();
            menuButton.sceneName = "Prototype";
            menuButton.visual = button.transform;
        }

        static void BuildSandboxButton(PrototypeSceneBuilder.Palette p)
        {
            Vector3 position = SpawnPoint + Vector3.forward * 1.6f + Vector3.right * 0.9f + Vector3.up * 1.1f;
            var button = PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Sandbox Button", null, position,
                new Vector3(0.5f, 0.5f, 0.25f), p.Candy[1]);

            var label = WorldText.Create("Sandbox Label", button.transform, Vector3.back * 0.14f, new Vector2(0.6f, 0.25f), 0.12f);
            label.text = "SANDBOX";

            var menuButton = button.AddComponent<MenuButton>();
            menuButton.sceneName = "Sandbox";
            menuButton.visual = button.transform;
        }

        // Loads PurpleModeSceneBuilder's scene: a deliberately empty purple stub, not a finished feature.
        static void BuildPurpleModeButton()
        {
            var purple = PrototypeSceneBuilder.Mat("PurpleModeButton", new Color(0.55f, 0.2f, 1f), 0.6f, 0.3f);
            Vector3 position = SpawnPoint + Vector3.forward * 1.6f + Vector3.left * 0.9f + Vector3.up * 1.1f;
            var button = PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Purple Mode Button", null, position,
                new Vector3(0.5f, 0.5f, 0.25f), purple);

            var label = WorldText.Create("Purple Mode Label", button.transform, Vector3.back * 0.14f, new Vector2(0.6f, 0.25f), 0.1f);
            label.text = "PURPLE\nMODE";

            var menuButton = button.AddComponent<MenuButton>();
            menuButton.sceneName = "PurpleMode";
            menuButton.visual = button.transform;
        }
    }
}
