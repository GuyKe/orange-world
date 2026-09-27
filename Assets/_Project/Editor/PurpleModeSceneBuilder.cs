using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OrangeWorld.EditorTools
{
    // A deliberately minimal stub scene: you spawn inside a boxy, low-poly house sitting in a big grass field,
    // with a way back to the main menu. No creatures, no rules - a blank canvas for a future game mode, not a
    // finished feature.
    public static class PurpleModeSceneBuilder
    {
        const string ScenePath = PrototypeSceneBuilder.SceneFolder + "/PurpleMode.unity";

        const float FieldRadius = 45f;
        static readonly Vector3 FieldPosition = Vector3.zero;
        // The point on the grass field's surface the house sits on - also doubles as the house's local floor origin.
        static readonly Vector3 HouseFloor = new(0f, FieldRadius + 0.05f, 0f);

        const float HouseWidth = 10f;
        const float HouseDepth = 8f;
        const float HouseHeight = 4f;
        const float WallThickness = 0.3f;

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
            BuildGrassField();
            BuildHouse();

            // A little back from center and facing +Z (the default spawn rotation), so you spawn looking toward
            // the front door and the field beyond it.
            Vector3 playerSpawn = HouseFloor + Vector3.back * 1.8f;
            var player = PrototypeSceneBuilder.BuildPlayer(palette, slippery, addVitals: true, spawnPosition: playerSpawn);
            BuildBackButton(palette);

            EditorSceneManager.SaveScene(scene, ScenePath);
            PrototypeSceneBuilder.RegisterScene(ScenePath, isMenu: false);
            Selection.activeGameObject = player;
            Debug.Log($"[Orange World] Built {ScenePath}. It's an empty stub (a house in a grass field) - build your game mode here.");
        }

        static void BuildGrassField()
        {
            var mat = PrototypeSceneBuilder.Mat("PurpleModeGrass", new Color(0.16f, 0.5f, 0.12f), 0.1f);
            var field = PrototypeSceneBuilder.Prim(PrimitiveType.Sphere, "Grass Field", null,
                FieldPosition, Vector3.one * FieldRadius * 2f, mat);
            field.AddComponent<GravityAttractor>();
        }

        // A boxy, low-poly house built from separate wall panels rather than a solid box with holes carved into
        // it - like a lot of original PlayStation-era levels, the windows and doorway are real gaps between
        // panels, not something cut out of one piece, so you can actually see and walk through them.
        static void BuildHouse()
        {
            var wallMat = PrototypeSceneBuilder.Mat("PurpleModeWall", new Color(0.55f, 0.07f, 0.07f), 0.1f);
            var roofMat = PrototypeSceneBuilder.Mat("PurpleModeRoof", new Color(0.05f, 0.05f, 0.07f), 0.15f);
            var floorMat = PrototypeSceneBuilder.Mat("PurpleModeFloor", new Color(0.22f, 0.1f, 0.07f), 0.1f);

            var root = new GameObject("Purple Mode House").transform;
            root.position = HouseFloor;

            PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Floor", root, new Vector3(0f, -0.05f, 0f),
                new Vector3(HouseWidth, 0.1f, HouseDepth), floorMat);

            // Flat and overhanging on every side, sitting right on top of the walls.
            PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Roof", root, new Vector3(0f, HouseHeight + 0.2f, 0f),
                new Vector3(HouseWidth + 1f, 0.4f, HouseDepth + 1f), roofMat);

            BuildFrontWall(root, wallMat);
            PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Back Wall", root, new Vector3(0f, HouseHeight * 0.5f, -HouseDepth * 0.5f),
                new Vector3(HouseWidth, HouseHeight, WallThickness), wallMat);
            PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Left Wall", root, new Vector3(-HouseWidth * 0.5f, HouseHeight * 0.5f, 0f),
                new Vector3(WallThickness, HouseHeight, HouseDepth), wallMat);
            PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Right Wall", root, new Vector3(HouseWidth * 0.5f, HouseHeight * 0.5f, 0f),
                new Vector3(WallThickness, HouseHeight, HouseDepth), wallMat);
        }

        // Five square windows in a row plus a doorway on the right, all real gaps between wall panels.
        static void BuildFrontWall(Transform root, Material mat)
        {
            const float z = HouseDepth * 0.5f;
            const float windowBottom = 1.7f, windowTop = 2.7f, doorTop = 2.6f;

            // (x0, x1, y0, y1) solid panels in a frame where x runs [0, HouseWidth] and y runs [0, HouseHeight] -
            // each gets re-centered on the house root below.
            (float x0, float x1, float y0, float y1)[] panels =
            {
                (0f, 1.5f, 0f, HouseHeight),           // left margin pillar
                (1.5f, 8.5f, 0f, windowBottom),        // strip below all five windows
                (1.5f, 8.5f, windowTop, HouseHeight),  // strip above all five windows
                (2.5f, 3.0f, windowBottom, windowTop), // pillar between windows 1 and 2
                (4.0f, 4.5f, windowBottom, windowTop), // pillar between windows 2 and 3
                (5.5f, 6.0f, windowBottom, windowTop), // pillar between windows 3 and 4
                (7.0f, 7.5f, windowBottom, windowTop), // pillar between windows 4 and 5
                (8.5f, 10f, doorTop, HouseHeight),     // above the doorway
                (8.5f, 8.6f, 0f, doorTop),             // door frame, left sliver
                (9.9f, 10f, 0f, doorTop),               // door frame, right sliver
            };

            foreach (var (x0, x1, y0, y1) in panels)
            {
                Vector3 size = new(x1 - x0, y1 - y0, WallThickness);
                Vector3 position = new((x0 + x1) * 0.5f - HouseWidth * 0.5f, (y0 + y1) * 0.5f, z);
                PrototypeSceneBuilder.Prim(PrimitiveType.Cube, "Front Wall Panel", root, position, size, mat);
            }
        }

        static void BuildBackButton(PrototypeSceneBuilder.Palette p)
        {
            Vector3 position = HouseFloor + Vector3.back * 1.8f + Vector3.right * 2.5f + Vector3.up * 1.1f;
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
