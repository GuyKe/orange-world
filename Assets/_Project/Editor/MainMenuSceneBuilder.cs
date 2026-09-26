using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace OrangeWorld.EditorTools
{
    // Generates the main menu: a small platform with a punchable Play button, standing in the middle of a
    // few blobs hopping around on their own - the "menu background" is just the actual game running quietly.
    public static class MainMenuSceneBuilder
    {
        internal const string ScenePath = PrototypeSceneBuilder.SceneFolder + "/MainMenu.unity";

        const float PlatformRadius = 6f;
        const float CanvasScale = 0.01f;
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
            var player = PrototypeSceneBuilder.BuildPlayer(palette, slippery, addVitals: false, spawnPosition: SpawnPoint);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PrototypeSceneBuilder.RegisterScene(ScenePath, isMenu: true);
            Selection.activeGameObject = player;
            Debug.Log($"[Orange World] Built {ScenePath}. It's now first in Build Settings, so Build And Run opens here.");
        }

        [MenuItem("Orange World/4. Build All Scenes", priority = 4)]
        public static void BuildAll()
        {
            Build();
            PrototypeSceneBuilder.Build();
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
            BuildWorldText("Title", basePosition + Vector3.up * 1.85f, "ORANGE WORLD", 0.5f, new Vector2(3f, 0.7f));
            BuildWorldText("Subtitle", basePosition + Vector3.up * 1.35f, "a fever-dream physics RPG", 0.16f, new Vector2(3f, 0.3f));
        }

        static void BuildPlayButton(PrototypeSceneBuilder.Palette p)
        {
            Vector3 position = SpawnPoint + Vector3.forward * 1.6f + Vector3.up * 1.1f;
            var button = PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Play Button", null, position,
                new Vector3(0.5f, 0.5f, 0.25f), p.Candy[4]);

            var label = BuildWorldText("Play Label", position + Vector3.back * 0.14f, "PLAY", 0.14f, new Vector2(0.6f, 0.25f));
            label.transform.SetParent(button.transform, true);

            var menuButton = button.AddComponent<MenuButton>();
            menuButton.sceneName = "Prototype";
            menuButton.visual = button.transform;
        }

        static GameObject BuildWorldText(string name, Vector3 position, string text, float worldFontHeight, Vector2 worldSize)
        {
            var go = new GameObject(name);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));
            go.transform.localScale = Vector3.one * CanvasScale;

            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = worldSize / CanvasScale;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textRect = textGo.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            var uiText = textGo.AddComponent<Text>();
            uiText.text = text;
            uiText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uiText.fontSize = Mathf.RoundToInt(worldFontHeight / CanvasScale);
            uiText.alignment = TextAnchor.MiddleCenter;
            uiText.color = Color.white;

            return go;
        }
    }
}
