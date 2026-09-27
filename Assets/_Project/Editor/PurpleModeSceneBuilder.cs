using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeWorld.EditorTools
{
    // A deliberately minimal stub scene: a purple baseplate, a title, the player, and a way back to the main
    // menu. Nothing else lives here on purpose - this is a blank canvas for a future game mode, not a feature.
    public static class PurpleModeSceneBuilder
    {
        const string ScenePath = PrototypeSceneBuilder.SceneFolder + "/PurpleMode.unity";

        const float BaseplateRadius = 25f;
        static readonly Vector3 BaseplatePosition = Vector3.zero;
        static readonly Vector3 SpawnPoint = new(0f, BaseplateRadius + 0.05f, 0f);

        [MenuItem("Orange World/5. Build Purple Mode Scene", priority = 5)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            QuestProjectSetup.EnsureUrp();
            QuestProjectSetup.EnsureFolder(PrototypeSceneBuilder.MaterialFolder);
            QuestProjectSetup.EnsureFolder(PrototypeSceneBuilder.PrefabFolder);
            QuestProjectSetup.EnsureFolder(PrototypeSceneBuilder.SceneFolder);
            Random.InitState(9001);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var palette = PrototypeSceneBuilder.CreateBasePalette();
            var slippery = PrototypeSceneBuilder.PhysicsMat("Slippery", 0f, 0f, PhysicsMaterialCombine.Minimum, PhysicsMaterialCombine.Minimum);

            PrototypeSceneBuilder.BuildLightingAndSky();
            BuildBaseplate();
            BuildTitle();

            var player = PrototypeSceneBuilder.BuildPlayer(palette, slippery, addVitals: true, spawnPosition: SpawnPoint);
            BuildBackButton(palette);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PrototypeSceneBuilder.RegisterScene(ScenePath, isMenu: false);
            Selection.activeGameObject = player;
            Debug.Log($"[Orange World] Built {ScenePath}. It's an empty purple stub - build your game mode here.");
        }

        static void BuildBaseplate()
        {
            var mat = PrototypeSceneBuilder.Mat("PurpleModeBaseplate", new Color(0.45f, 0.15f, 0.75f), 0.25f, 0.15f);
            var baseplate = PrototypeSceneBuilder.Prim(PrimitiveType.Sphere, "Purple Mode Baseplate", null,
                BaseplatePosition, Vector3.one * BaseplateRadius * 2f, mat);
            baseplate.AddComponent<GravityAttractor>();
        }

        static void BuildTitle()
        {
            Vector3 position = SpawnPoint + Vector3.forward * 1.6f + Vector3.up * 1.8f;
            WorldText.Create("Purple Mode Title", null, position, new Vector2(4f, 0.6f), 0.4f).text = "PURPLE MODE";
        }

        static void BuildBackButton(PrototypeSceneBuilder.Palette p)
        {
            Vector3 position = SpawnPoint + Vector3.forward * 1.6f + Vector3.up * 1.1f;
            var button = PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Back To Menu Button", null, position,
                new Vector3(0.5f, 0.5f, 0.25f), p.Metal);

            var label = WorldText.Create("Back To Menu Label", button.transform, Vector3.back * 0.14f, new Vector2(0.6f, 0.25f), 0.1f);
            label.text = "BACK TO\nMENU";

            var menuButton = button.AddComponent<MenuButton>();
            menuButton.sceneName = "MainMenu";
            menuButton.visual = button.transform;
        }
    }
}
