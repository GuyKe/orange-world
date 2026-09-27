using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

namespace OrangeWorld.EditorTools
{
    // Generates the playable prototype scene from code so the whole project stays text-diffable and regenerable.
    public static class PrototypeSceneBuilder
    {
        internal const string MaterialFolder = "Assets/_Project/Materials";
        internal const string PrefabFolder = "Assets/_Project/Prefabs";
        internal const string SceneFolder = "Assets/_Project/Scenes";
        const string ScenePath = SceneFolder + "/Prototype.unity";

        struct PlanetSpec
        {
            public string Name;
            public Vector3 Position;
            public float Radius;
            public Color Color;
        }

        static readonly PlanetSpec[] Planets =
        {
            new() { Name = "Orange Home", Position = Vector3.zero, Radius = 12f, Color = new Color(1f, 0.5f, 0.1f) },
            new() { Name = "Teal Moon", Position = new Vector3(0f, 8f, 42f), Radius = 7f, Color = new Color(0.1f, 0.8f, 0.75f) },
            new() { Name = "Magenta Lump", Position = new Vector3(-38f, 14f, -10f), Radius = 9f, Color = new Color(0.9f, 0.2f, 0.7f) },
            new() { Name = "Lime Pebble", Position = new Vector3(30f, -12f, -22f), Radius = 5f, Color = new Color(0.6f, 1f, 0.2f) },
            new() { Name = "Violet Crown", Position = new Vector3(10f, 36f, 10f), Radius = 6f, Color = new Color(0.5f, 0.3f, 1f) },
        };

        static readonly (int from, int to)[] JumpRoutes = { (0, 1), (1, 2), (2, 0), (0, 3), (3, 4), (4, 0), (0, 4) };

        static readonly Vector3 SpawnPoint = new(0f, 12.05f, 0f);

        const float ShopPlatformRadius = 6f;
        static readonly Vector3 ShopPosition = new(0f, -80f, 0f);

        internal struct BossSpec
        {
            public string Name;
            public Vector3 Position;
            public float Radius;
            public Color Color;
            public int RecommendedLevel;
            public bool Melee;
        }

        // Internal so SandboxSceneBuilder can summon copies of these same two bosses by the same stats.
        internal static readonly BossSpec[] BossPlanets =
        {
            new() { Name = "Crimson Titan's Lair", Position = new Vector3(0f, -25f, -68f), Radius = 20f,
                Color = new Color(0.55f, 0.04f, 0.04f), RecommendedLevel = 5, Melee = true },
            new() { Name = "Void Gunner Fortress", Position = new Vector3(55f, 18f, 48f), Radius = 18f,
                Color = new Color(0.14f, 0.04f, 0.3f), RecommendedLevel = 9, Melee = false },
        };

        internal class Palette
        {
            public Material Hand, Arm, EyeWhite, Pupil, Blob, Stalk, Metal, Mallet, Rock, ShroomCap, ShroomStalk, Slime;
            public Material[] Candy, Planet;
        }

        [MenuItem("Orange World/3. Build Prototype Scene", priority = 3)]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            QuestProjectSetup.EnsureUrp();
            QuestProjectSetup.EnsureFolder(MaterialFolder);
            QuestProjectSetup.EnsureFolder(PrefabFolder);
            QuestProjectSetup.EnsureFolder(SceneFolder);
            Random.InitState(1337);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var palette = CreatePalette();
            var slippery = PhysicsMat("Slippery", 0f, 0f, PhysicsMaterialCombine.Minimum, PhysicsMaterialCombine.Minimum);
            var bouncy = PhysicsMat("Bouncy", 0.4f, 0.7f, PhysicsMaterialCombine.Average, PhysicsMaterialCombine.Maximum);
            var projectile = BuildProjectilePrefab(palette);
            var blobling = BuildBloblingPrefab(palette, bouncy);
            var gunnerBlobling = BuildGunnerBloblingPrefab(palette, bouncy, projectile);
            var meleeBlobling = BuildMeleeBloblingPrefab(palette, bouncy);
            var meleeBossPrefab = BuildMeleeBossPrefab(palette, bouncy);
            var gunnerBossPrefab = BuildGunnerBossPrefab(palette, bouncy, projectile);
            var armoredBlobling = BuildArmoredBloblingPrefab(palette, bouncy);
            var jellyfishBlobling = BuildJellyfishPrefab(palette, bouncy);
            var flailPrefab = BuildFlailPrefab(palette);
            var malletPrefab = BuildMalletPrefab(palette);
            var bopperPrefab = BuildBopperPrefab(palette);
            var yoyoPrefab = BuildYoyoPrefab(palette);

            BuildLightingAndSky();
            var keepClear = Planets.Select(_ => new List<Vector3>()).ToArray();
            keepClear[0].Add(Vector3.up);
            BuildPlanets(palette);
            var jumpShroomParent = BuildJumpPads(palette, keepClear);
            BuildDecor(palette, keepClear);
            BuildSpaceJunk(palette);
            BuildPortals(palette);
            BuildBossPlanets(palette, meleeBossPrefab, gunnerBossPrefab, projectile, jumpShroomParent);
            Object.Instantiate(flailPrefab, SpawnPoint + new Vector3(0.6f, 1.1f, 0.9f), Quaternion.identity);
            Object.Instantiate(malletPrefab, SpawnPoint + new Vector3(-0.6f, 0.5f, 0.9f), Quaternion.identity);
            Object.Instantiate(bopperPrefab, SpawnPoint + new Vector3(1.3f, 0.4f, 0.4f), Quaternion.identity);
            Object.Instantiate(yoyoPrefab, SpawnPoint + new Vector3(-1.3f, 0.9f, 0.4f), Quaternion.identity);
            var shopStand = BuildShop(palette);
            var player = BuildPlayer(palette, slippery, shopStandPosition: shopStand);
            var spawner = new GameObject("Wave Spawner").AddComponent<WaveSpawner>();
            spawner.creaturePrefab = blobling;
            spawner.gunnerPrefab = gunnerBlobling;
            spawner.meleePrefab = meleeBlobling;
            spawner.flyerPrefab = jellyfishBlobling;
            spawner.armoredPrefab = armoredBlobling;

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterScene(ScenePath, isMenu: false);
            Selection.activeGameObject = player;
            Debug.Log($"[Orange World] Built {ScenePath}. Press Play (with Quest Link) or Build And Run to the headset.");
        }

        // ---------- Assets ----------

        static Palette CreatePalette()
        {
            var palette = CreateBasePalette();
            palette.Planet = Planets.Select(p => Mat("Planet_" + p.Name.Replace(" ", ""), p.Color, 0.15f)).ToArray();
            return palette;
        }

        // Shared with MainMenuSceneBuilder, which needs the same materials but not the per-planet ones above.
        internal static Palette CreateBasePalette()
        {
            var palette = new Palette
            {
                Hand = Mat("Hand", new Color(1f, 0.55f, 0.1f), 0.5f),
                Arm = Mat("Arm", new Color(1f, 0.7f, 0.3f), 0.6f),
                EyeWhite = Mat("EyeWhite", new Color(0.97f, 0.95f, 0.9f), 0.9f),
                Pupil = Mat("Pupil", new Color(0.02f, 0.02f, 0.04f), 0.95f),
                Blob = Mat("Blob", new Color(0.6f, 0.9f, 0.3f), 0.7f),
                Stalk = Mat("Stalk", new Color(0.95f, 0.85f, 0.75f), 0.2f),
                Metal = Mat("Metal", new Color(0.55f, 0.55f, 0.6f), 0.8f),
                Mallet = Mat("Mallet", new Color(1f, 0.3f, 0.6f), 0.6f),
                Rock = Mat("Rock", new Color(0.35f, 0.3f, 0.45f), 0.1f),
                ShroomCap = Mat("ShroomCap", new Color(1f, 0.2f, 0.35f), 0.5f, 0.6f),
                ShroomStalk = Mat("ShroomStalk", new Color(1f, 0.95f, 0.8f), 0.3f),
                Slime = Mat("Slime", new Color(0.75f, 1f, 0.2f), 0.8f, 0.5f),
            };

            Color[] candy =
            {
                new(1f, 0.25f, 0.55f), new(0.2f, 0.9f, 1f), new(1f, 0.9f, 0.1f),
                new(0.55f, 0.2f, 1f), new(0.3f, 1f, 0.5f), new(1f, 0.45f, 0.1f),
            };
            palette.Candy = candy.Select((c, i) => Mat("Candy" + i, c, 0.6f, i % 3 == 0 ? 0.8f : 0f)).ToArray();
            return palette;
        }

        internal static Material Mat(string name, Color color, float smoothness, float emission = 0f)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (emission > 0f)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", color * emission);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static PhysicsMaterial PhysicsMat(string name, float friction, float bounce,
            PhysicsMaterialCombine frictionCombine, PhysicsMaterialCombine bounceCombine)
        {
            string path = $"{MaterialFolder}/{name}.asset";
            var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(path);
            if (material == null)
            {
                material = new PhysicsMaterial(name);
                AssetDatabase.CreateAsset(material, path);
            }

            material.dynamicFriction = friction;
            material.staticFriction = friction;
            material.bounciness = bounce;
            material.frictionCombine = frictionCombine;
            material.bounceCombine = bounceCombine;
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static GameObject BuildBloblingPrefab(Palette p, PhysicsMaterial bouncy)
        {
            var go = CreateBloblingBase("Blobling", p, bouncy);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/Blobling.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        internal static GameObject BuildGunnerBloblingPrefab(Palette p, PhysicsMaterial bouncy, GameObject projectilePrefab)
        {
            var go = CreateBloblingBase("Gunner Blobling", p, bouncy);
            AddGun(go, p, projectilePrefab);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/GunnerBlobling.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        internal static GameObject BuildMeleeBloblingPrefab(Palette p, PhysicsMaterial bouncy)
        {
            var go = CreateBloblingBase("Melee Blobling", p, bouncy);
            AddClub(go, p);
            go.AddComponent<MeleeBlob>();

            var brain = go.GetComponent<CreatureBrain>();
            brain.contactDamage = 24f;
            brain.lungeSpeed = 9f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/MeleeBlobling.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        // Separate prefabs from the regular blobs, rather than just scaling one up at spawn time, because a boss
        // permanently needs two components stripped (see below) - and that's only safe to do before anything has
        // ever subscribed to their events, i.e. at prefab-creation time, not on some already-playing instance.
        internal static GameObject BuildMeleeBossPrefab(Palette p, PhysicsMaterial bouncy)
        {
            var go = CreateBloblingBase("Melee Boss Blobling", p, bouncy);
            AddClub(go, p);
            go.AddComponent<MeleeBlob>();

            var brain = go.GetComponent<CreatureBrain>();
            brain.contactDamage = 24f;
            brain.lungeSpeed = 9f;

            // A boss never splits and gets a fixed body color from BossConfig instead of the usual random tint.
            Object.DestroyImmediate(go.GetComponent<SplitOnDeath>());
            Object.DestroyImmediate(go.GetComponent<RandomTint>());

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/MeleeBossBlobling.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        internal static GameObject BuildGunnerBossPrefab(Palette p, PhysicsMaterial bouncy, GameObject projectilePrefab)
        {
            var go = CreateBloblingBase("Gunner Boss Blobling", p, bouncy);
            AddGun(go, p, projectilePrefab);

            Object.DestroyImmediate(go.GetComponent<SplitOnDeath>());
            Object.DestroyImmediate(go.GetComponent<RandomTint>());

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/GunnerBossBlobling.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject CreateBloblingBase(string name, Palette p, PhysicsMaterial bouncy)
        {
            var go = new GameObject(name);
            var body = go.AddComponent<Rigidbody>();
            body.mass = 3f;
            body.angularDamping = 1.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var collider = go.AddComponent<SphereCollider>();
            collider.radius = 0.5f;
            collider.sharedMaterial = bouncy;
            go.AddComponent<GravityBody>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var blob = Prim(PrimitiveType.Sphere, "Body", visual, Vector3.zero, Vector3.one, p.Blob, collider: false);
            Eye(visual, new Vector3(-0.18f, 0.2f, 0.4f), 0.28f, p.Candy[1], p);
            Eye(visual, new Vector3(0.2f, 0.25f, 0.38f), 0.22f, p.Candy[2], p);
            Eye(visual, new Vector3(0.02f, 0.42f, 0.22f), 0.16f, p.Candy[0], p);

            go.AddComponent<Damageable>().maxHealth = 30f;
            var damager = go.AddComponent<ImpactDamager>();
            damager.minImpactSpeed = 6f;
            damager.damagePerSpeed = 2.5f;
            damager.knockback = 0.2f;
            damager.selfDamageMinSpeed = 8f;
            damager.selfDamagePerSpeed = 4f;
            go.AddComponent<CreatureBrain>();
            var jiggle = go.AddComponent<Jiggle>();
            jiggle.visual = visual;
            go.AddComponent<SplitOnDeath>();
            go.AddComponent<RandomTint>().targets = new[] { blob.GetComponent<Renderer>() };

            var stretch = go.AddComponent<Stretchable>();
            stretch.visual = visual;
            stretch.restLength = 0.9f;
            stretch.snapLength = 1.6f;

            return go;
        }

        static void AddGun(GameObject go, Palette p, GameObject projectilePrefab)
        {
            Vector3 gunDirection = new(0.4f, 0.05f, 0.9f);
            var gun = new GameObject("Gun").transform;
            gun.SetParent(go.transform, false);
            gun.localPosition = gunDirection.normalized * 0.45f;
            gun.localRotation = Quaternion.LookRotation(gunDirection.normalized);

            Prim(PrimitiveType.Cylinder, "Barrel", gun, new Vector3(0f, 0f, 0.22f), new Vector3(0.09f, 0.22f, 0.09f), p.Metal, collider: false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(gun, false);
            muzzle.localPosition = new Vector3(0f, 0f, 0.5f);

            var blobGun = go.AddComponent<BlobGun>();
            blobGun.projectilePrefab = projectilePrefab;
            blobGun.muzzle = muzzle;
        }

        static void AddClub(GameObject go, Palette p)
        {
            Vector3 armDirection = new(0.4f, -0.1f, 0.9f);
            var arm = new GameObject("Club Arm").transform;
            arm.SetParent(go.transform, false);
            arm.localPosition = armDirection.normalized * 0.4f;
            arm.localRotation = Quaternion.LookRotation(armDirection.normalized);

            Prim(PrimitiveType.Cylinder, "Club Handle", arm, new Vector3(0f, 0f, 0.18f), new Vector3(0.06f, 0.18f, 0.06f), p.Stalk, collider: false)
                .transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Prim(PrimitiveType.Sphere, "Club Head", arm, new Vector3(0f, 0f, 0.4f), Vector3.one * 0.24f, p.Metal, collider: false);
        }

        // A tankier, slower blob wearing a hard shell: ArmoredHide makes ImpactDamager ignore a bare hand hitting
        // it, so you need to swing a weapon, throw something, or grab-and-stretch it instead of just punching.
        internal static GameObject BuildArmoredBloblingPrefab(Palette p, PhysicsMaterial bouncy)
        {
            var go = CreateBloblingBase("Armored Blobling", p, bouncy);
            AddShell(go, p);
            go.AddComponent<ArmoredHide>();

            var brain = go.GetComponent<CreatureBrain>();
            brain.hopSpeed = 2f;
            brain.lungeSpeed = 4f;
            brain.contactDamage = 10f;
            go.GetComponent<Damageable>().maxHealth = 60f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/ArmoredBlobling.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void AddShell(GameObject go, Palette p)
        {
            var visual = go.transform.Find("Visual");
            Prim(PrimitiveType.Sphere, "Shell", visual, new Vector3(0f, 0.22f, -0.15f), new Vector3(0.8f, 0.5f, 0.8f), p.Rock, collider: false);
        }

        // A flying blob with no CreatureBrain at all - Flyer replaces the usual hop/chase/lunge with hovering,
        // drifting, and diving. Tentacles hang from a flattened dome instead of the usual round body.
        internal static GameObject BuildJellyfishPrefab(Palette p, PhysicsMaterial bouncy)
        {
            var go = new GameObject("Jellyfish Blobling");
            var body = go.AddComponent<Rigidbody>();
            body.mass = 2f;
            body.angularDamping = 2f;
            body.linearDamping = 0.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            var collider = go.AddComponent<SphereCollider>();
            collider.radius = 0.45f;
            collider.sharedMaterial = bouncy;
            go.AddComponent<GravityBody>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var dome = Prim(PrimitiveType.Sphere, "Body", visual, Vector3.zero, new Vector3(1f, 0.6f, 1f), p.Blob, collider: false);
            Eye(visual, new Vector3(-0.16f, 0.12f, 0.36f), 0.22f, p.Candy[1], p);
            Eye(visual, new Vector3(0.18f, 0.15f, 0.34f), 0.2f, p.Candy[2], p);

            Vector2[] tentacleSpread = { new(-0.25f, 0.15f), new(0.25f, 0.15f), new(0f, -0.28f), new(-0.15f, -0.2f) };
            foreach (var spread in tentacleSpread)
                Prim(PrimitiveType.Capsule, "Tentacle", visual, new Vector3(spread.x, -0.45f, spread.y),
                    new Vector3(0.07f, 0.3f, 0.07f), Pick(p.Candy), collider: false);

            go.AddComponent<Damageable>().maxHealth = 22f;
            var damager = go.AddComponent<ImpactDamager>();
            damager.minImpactSpeed = 5f;
            damager.damagePerSpeed = 2f;
            damager.knockback = 0.15f;

            go.AddComponent<Flyer>();
            var jiggle = go.AddComponent<Jiggle>();
            jiggle.visual = visual;
            go.AddComponent<SplitOnDeath>().pieces = 2;
            go.AddComponent<RandomTint>().targets = new[] { dome.GetComponent<Renderer>() };

            var stretch = go.AddComponent<Stretchable>();
            stretch.visual = visual;
            stretch.restLength = 0.8f;
            stretch.snapLength = 1.5f;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/JellyfishBlobling.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        internal static GameObject BuildProjectilePrefab(Palette p)
        {
            var go = Prim(PrimitiveType.Sphere, "Glob", null, Vector3.zero, Vector3.one * 0.14f, p.Slime);
            var body = go.AddComponent<Rigidbody>();
            body.mass = 0.3f;
            go.AddComponent<Projectile>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/Glob.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------- World ----------

        internal static void BuildLightingAndSky()
        {
            var sun = new GameObject("Weird Sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.85f, 0.95f);
            sun.intensity = 1.3f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            RenderSettings.sun = sun;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.6f, 0.4f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.9f, 0.5f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.2f, 0.6f, 0.6f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = new Color(0.75f, 0.45f, 0.8f);
            RenderSettings.fogDensity = 0.004f;

            string skyPath = MaterialFolder + "/FeverSky.mat";
            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }
            sky.SetColor("_SkyTint", new Color(1f, 0.3f, 0.8f));
            sky.SetColor("_GroundColor", new Color(0.1f, 0.5f, 0.55f));
            sky.SetFloat("_AtmosphereThickness", 2.2f);
            sky.SetFloat("_Exposure", 1.1f);
            sky.SetFloat("_SunSize", 0.12f);
            EditorUtility.SetDirty(sky);
            RenderSettings.skybox = sky;
        }

        static void BuildPlanets(Palette p)
        {
            for (int i = 0; i < Planets.Length; i++)
            {
                var spec = Planets[i];
                var planet = Prim(PrimitiveType.Sphere, spec.Name, null, spec.Position, Vector3.one * spec.Radius * 2f, p.Planet[i]);
                planet.AddComponent<GravityAttractor>();
            }
        }

        static Transform BuildJumpPads(Palette p, List<Vector3>[] keepClear)
        {
            var parent = new GameObject("Jump Shrooms").transform;
            foreach (var (from, to) in JumpRoutes)
            {
                var a = Planets[from];
                var b = Planets[to];
                Vector3 direction = BuildJumpPad(parent, a.Position, a.Radius, a.Name, b.Position, b.Radius, b.Name, p);
                keepClear[from].Add(direction);
            }
            return parent;
        }

        // Returns the launch direction (from -> to), so the caller can keep decor clear of the landing spot.
        static Vector3 BuildJumpPad(Transform parent, Vector3 fromPosition, float fromRadius, string fromName,
            Vector3 toPosition, float toRadius, string toName, Palette p)
        {
            Vector3 direction = (toPosition - fromPosition).normalized;
            float gap = Vector3.Distance(fromPosition, toPosition) - fromRadius - toRadius;

            var pad = new GameObject($"Shroom {fromName} -> {toName}");
            pad.transform.SetParent(parent, false);
            pad.transform.SetPositionAndRotation(fromPosition + direction * fromRadius, Quaternion.FromToRotation(Vector3.up, direction));
            Prim(PrimitiveType.Cylinder, "Stalk", pad.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.35f, 0.15f, 0.35f), p.ShroomStalk, collider: false);
            Prim(PrimitiveType.Sphere, "Cap", pad.transform, new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.35f, 1.6f), p.ShroomCap, collider: false);

            var trigger = pad.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 0.6f, 0f);
            trigger.radius = 0.8f;
            pad.AddComponent<JumpPad>().launchSpeed = Mathf.Min(26f, 9f + gap * 0.3f);
            return direction;
        }

        static void BuildDecor(Palette p, List<Vector3>[] keepClear)
        {
            var parent = new GameObject("Decor").transform;
            for (int i = 0; i < Planets.Length; i++)
            {
                var spec = Planets[i];
                int count = Mathf.RoundToInt(spec.Radius * 2f);
                for (int n = 0; n < count; n++)
                {
                    Vector3 direction = Random.onUnitSphere;
                    if (keepClear[i].Any(c => Vector3.Angle(c, direction) < 15f)) continue;

                    Vector3 surface = spec.Position + direction * spec.Radius;
                    Quaternion rotation = Quaternion.FromToRotation(Vector3.up, direction) * Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                    float roll = Random.value;
                    if (roll < 0.45f) NoodleTree(parent, surface, rotation, p);
                    else if (roll < 0.7f) Crystal(parent, surface, rotation, p);
                    else if (roll < 0.88f) EyeStalk(parent, surface, rotation, p);
                    else Boulder(surface + direction * 0.6f, p);
                }
            }
        }

        static void NoodleTree(Transform parent, Vector3 position, Quaternion rotation, Palette p)
        {
            var root = new GameObject("Noodle Tree").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, rotation);
            float height = Random.Range(1.5f, 4.5f);
            float width = Random.Range(0.15f, 0.35f);
            float crown = Random.Range(1f, 2.6f);
            Prim(PrimitiveType.Cylinder, "Trunk", root, new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height * 0.5f, width), p.Stalk);
            Prim(PrimitiveType.Sphere, "Crown", root, new Vector3(0f, height, 0f), new Vector3(crown, crown * Random.Range(0.5f, 1.3f), crown), Pick(p.Candy));
        }

        static void Crystal(Transform parent, Vector3 position, Quaternion rotation, Palette p)
        {
            float height = Random.Range(1f, 3f);
            var crystal = Prim(PrimitiveType.Cube, "Crystal", parent, position + rotation * Vector3.up * (height * 0.35f),
                new Vector3(0.5f, height, 0.5f), Pick(p.Candy));
            crystal.transform.rotation = rotation * Quaternion.Euler(Random.Range(-25f, 25f), 45f, Random.Range(-25f, 25f));
        }

        static void EyeStalk(Transform parent, Vector3 position, Quaternion rotation, Palette p)
        {
            var root = new GameObject("Eye Stalk").transform;
            root.SetParent(parent, false);
            root.SetPositionAndRotation(position, rotation);
            float height = Random.Range(1.2f, 3f);
            float size = Random.Range(0.6f, 1.4f);
            Prim(PrimitiveType.Cylinder, "Stalk", root, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.12f, height * 0.5f, 0.12f), p.Stalk);
            Eye(root, new Vector3(0f, height + size * 0.4f, 0f), size, Pick(p.Candy), p);
        }

        static void Boulder(Vector3 position, Palette p)
        {
            float size = Random.Range(0.3f, 0.6f);
            var rock = Prim(PrimitiveType.Cube, "Boulder", null, position, Vector3.one * size, p.Rock);
            rock.transform.rotation = Random.rotation;
            var body = rock.AddComponent<Rigidbody>();
            body.mass = size * 10f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            rock.AddComponent<GravityBody>();
            var damager = rock.AddComponent<ImpactDamager>();
            damager.minImpactSpeed = 3f;
            damager.damagePerSpeed = 4f;
        }

        static void BuildSpaceJunk(Palette p)
        {
            var parent = new GameObject("Space Junk").transform;
            for (int i = 0; i < 14; i++)
            {
                var type = Random.value < 0.5f ? PrimitiveType.Cube : PrimitiveType.Sphere;
                var junk = Prim(type, "Drifting Thing", parent, Random.onUnitSphere * Random.Range(90f, 160f),
                    Vector3.one * Random.Range(4f, 14f), Pick(p.Candy), collider: false);
                var orbiter = junk.AddComponent<Orbiter>();
                orbiter.axis = Random.onUnitSphere;
                orbiter.degreesPerSecond = Random.Range(1f, 4f);
                orbiter.spin = Random.insideUnitSphere * 30f;
            }
            Eye(parent, new Vector3(0f, 70f, -140f), 40f, p.Candy[3], p).name = "The Watcher";
        }

        // ---------- Portals ----------

        // A single pair of weird, funky little rifts that suck in anything nearby and spit it out of the other
        // one - deliberately just one pair, so it stays a fun surprise rather than replacing the Jump Shroom
        // network as the main way to get around.
        static void BuildPortals(Palette p)
        {
            var home = Planets[0];
            var magentaLump = Planets[2];

            Vector3 homeDir = new Vector3(-0.7f, 0.45f, -0.6f).normalized;
            Vector3 lumpDir = new Vector3(-0.2f, 0.9f, 0.4f).normalized;

            var portalA = BuildPortal("Cyan", home.Position + homeDir * (home.Radius + 0.7f), homeDir, p, new Color(0.25f, 1f, 0.9f));
            var portalB = BuildPortal("Magenta", magentaLump.Position + lumpDir * (magentaLump.Radius + 0.7f), lumpDir, p, new Color(1f, 0.25f, 0.85f));

            var linkA = portalA.GetComponent<Portal>();
            var linkB = portalB.GetComponent<Portal>();
            linkA.linkedPortal = linkB;
            linkB.linkedPortal = linkA;
        }

        // A pulsing, off-kilter core with a ring of tumbling shards orbiting it at odd angles - primitives only,
        // but asymmetric and constantly moving so it reads as "alien machine" rather than "regular decoration".
        static GameObject BuildPortal(string id, Vector3 position, Vector3 outDirection, Palette p, Color tint)
        {
            var root = new GameObject(id + " Portal").transform;
            root.SetPositionAndRotation(position, Quaternion.FromToRotation(Vector3.up, outDirection));

            var coreMat = Mat("Portal" + id + "Core", tint, 0.9f, 1.3f);
            var core = Prim(PrimitiveType.Sphere, "Core", root, Vector3.zero, Vector3.one * 0.45f, coreMat, collider: false);
            var coreSpin = core.AddComponent<Orbiter>();
            coreSpin.degreesPerSecond = 0f; // spin in place, not orbit around the world origin
            coreSpin.spin = new Vector3(50f, 90f, 30f);

            const int shardCount = 7;
            for (int i = 0; i < shardCount; i++)
            {
                float angle = i * (360f / shardCount) + Random.Range(-12f, 12f);
                float radius = Random.Range(0.55f, 0.9f);
                float height = Random.Range(-0.25f, 0.25f);
                Quaternion around = Quaternion.AngleAxis(angle, Vector3.up);

                var shard = Prim(PrimitiveType.Cube, "Shard", root, around * new Vector3(radius, height, 0f),
                    new Vector3(0.07f, Random.Range(0.3f, 0.65f), 0.07f), Pick(p.Candy), collider: false);
                shard.transform.localRotation = around * Quaternion.Euler(Random.Range(-35f, 35f), Random.Range(0f, 360f), Random.Range(-35f, 35f));

                var orbiter = shard.AddComponent<Orbiter>();
                orbiter.center = position;
                orbiter.axis = outDirection;
                orbiter.degreesPerSecond = Random.Range(20f, 55f) * (Random.value < 0.5f ? 1f : -1f);
                orbiter.spin = new Vector3(Random.Range(30f, 90f), Random.Range(30f, 90f), Random.Range(30f, 90f));
            }

            root.gameObject.AddComponent<Portal>();
            return root.gameObject;
        }

        // Big, bare arena planets, each connected to Home by its own Jump Shroom and signed with a recommended
        // level, holding one scaled-up blob (melee or gunner) as the boss. No decor here - just the fight.
        static void BuildBossPlanets(Palette p, GameObject meleePrefab, GameObject gunnerPrefab, GameObject projectilePrefab, Transform jumpShroomParent)
        {
            var home = Planets[0];
            foreach (var boss in BossPlanets)
            {
                var planetMat = Mat("Boss_" + boss.Name.Replace(" ", "").Replace("'", ""), boss.Color, 0.2f, 0.15f);
                var planet = Prim(PrimitiveType.Sphere, boss.Name, null, boss.Position, Vector3.one * boss.Radius * 2f, planetMat);
                planet.AddComponent<GravityAttractor>();

                BuildJumpPad(jumpShroomParent, home.Position, home.Radius, home.Name, boss.Position, boss.Radius, boss.Name, p);

                WorldText.Create(boss.Name + " Sign", null, boss.Position + Vector3.up * (boss.Radius + 4f),
                    new Vector2(6f, 1.6f), 0.6f).text = $"{boss.Name}\nRecommended Level {boss.RecommendedLevel}";

                SpawnBoss(boss, meleePrefab, gunnerPrefab, projectilePrefab);
            }
        }

        // Shared with SandboxBossSummonButton's setup (via SandboxSceneBuilder) so a summoned boss matches its
        // planet-dwelling counterpart exactly.
        internal static float BossScale(BossSpec boss) => 3f + boss.RecommendedLevel * 0.4f;
        internal static int BossBucksReward(BossSpec boss) => 30 + boss.RecommendedLevel * 8;
        internal static float BossXpReward(BossSpec boss) => 50f + boss.RecommendedLevel * 15f;

        internal static Material BossBodyMaterial(BossSpec boss) =>
            Mat("BossBody_" + boss.Name.Replace(" ", "").Replace("'", ""), boss.Color, 0.6f, 0.35f);

        static void SpawnBoss(BossSpec boss, GameObject meleePrefab, GameObject gunnerPrefab, GameObject projectilePrefab)
        {
            var prefab = boss.Melee ? meleePrefab : gunnerPrefab;
            float scale = BossScale(boss);
            Vector3 point = boss.Position + Vector3.up * (boss.Radius + scale * 0.5f + 0.1f);

            var instance = Object.Instantiate(prefab, point, Quaternion.identity);
            instance.name = boss.Name + " Boss";

            // The rest of the boss's setup (scale, health, damage, color, health bar, reward, special attack) is
            // the same call SandboxBossSummonButton makes for a sandbox-summoned boss, so both produce an identical boss.
            BossConfig.Configure(instance, boss.Name, scale, BossBodyMaterial(boss), projectilePrefab,
                BossBucksReward(boss), BossXpReward(boss));
        }

        // ---------- Weapons ----------

        static GameObject BuildFlail(Vector3 handlePosition, Palette p)
        {
            const float spacing = 0.09f;
            const int linkCount = 5;
            const float headRadius = 0.13f;
            var root = new GameObject("Eyeball Flail").transform;

            var handle = Part("Handle", root, handlePosition, 0.6f);
            Prim(PrimitiveType.Capsule, "Grip", handle.transform, Vector3.zero, new Vector3(0.07f, 0.2f, 0.07f), p.Mallet);

            Rigidbody previous = handle;
            Vector3 cursor = handlePosition + Vector3.down * 0.2f;
            for (int i = 0; i < linkCount; i++)
            {
                var link = Part("Link " + i, root, cursor + Vector3.down * (spacing * 0.5f), 0.3f);
                Prim(PrimitiveType.Sphere, "Bead", link.transform, Vector3.zero, Vector3.one * 0.06f, p.Metal);
                BallJoint(link, previous, new Vector3(0f, spacing * 0.5f, 0f));
                previous = link;
                cursor += Vector3.down * spacing;
            }

            var head = Part("Eyeball", root, cursor + Vector3.down * headRadius, 2f);
            Prim(PrimitiveType.Sphere, "Ball", head.transform, Vector3.zero, Vector3.one * headRadius * 2f, p.EyeWhite);
            var gaze = new GameObject("Gaze");
            gaze.transform.SetParent(head.transform, false);
            gaze.AddComponent<LookAtCamera>();
            Prim(PrimitiveType.Sphere, "Iris", gaze.transform, new Vector3(0f, 0f, headRadius * 0.85f), new Vector3(0.13f, 0.13f, 0.05f), p.Candy[4], collider: false);
            Prim(PrimitiveType.Sphere, "Pupil", gaze.transform, new Vector3(0f, 0f, headRadius * 0.97f), new Vector3(0.06f, 0.06f, 0.03f), p.Pupil, collider: false);

            Vector3[] spikeDirections = { Vector3.left, Vector3.right, Vector3.forward, Vector3.back, Vector3.down };
            foreach (var direction in spikeDirections)
            {
                var spike = Prim(PrimitiveType.Cube, "Spike", head.transform, direction * headRadius, new Vector3(0.04f, 0.04f, 0.14f), p.Metal);
                spike.transform.localRotation = Quaternion.FromToRotation(Vector3.forward, direction);
            }

            BallJoint(head, previous, new Vector3(0f, headRadius, 0f));
            var damager = head.gameObject.AddComponent<ImpactDamager>();
            damager.minImpactSpeed = 2.5f;
            damager.damagePerSpeed = 9f;
            damager.knockback = 0.35f;

            return root.gameObject;
        }

        internal static GameObject BuildFlailPrefab(Palette p)
        {
            var go = BuildFlail(Vector3.zero, p);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/EyeballFlail.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildMallet(Vector3 position, Palette p)
        {
            var mallet = new GameObject("Squeaky Mallet");
            mallet.transform.position = position;
            var body = mallet.AddComponent<Rigidbody>();
            body.mass = 1.5f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            mallet.AddComponent<GravityBody>();
            var damager = mallet.AddComponent<ImpactDamager>();
            damager.minImpactSpeed = 2.5f;
            damager.damagePerSpeed = 7f;
            damager.knockback = 0.4f;

            Prim(PrimitiveType.Cylinder, "Handle", mallet.transform, Vector3.zero, new Vector3(0.06f, 0.28f, 0.06f), p.Stalk);
            var head = Prim(PrimitiveType.Cylinder, "Head", mallet.transform, new Vector3(0f, 0.32f, 0f), new Vector3(0.26f, 0.2f, 0.26f), p.Mallet);
            head.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            return mallet;
        }

        internal static GameObject BuildMalletPrefab(Palette p)
        {
            var go = BuildMallet(Vector3.zero, p);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/SqueakyMallet.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildBopper(Vector3 position, Palette p)
        {
            // A light, floppy boxing-glove club: low mass and low damage per swing, but a big cartoon knockback.
            var bopper = new GameObject("Boxing Glove Bopper");
            bopper.transform.position = position;
            var body = bopper.AddComponent<Rigidbody>();
            body.mass = 0.8f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            bopper.AddComponent<GravityBody>();
            var damager = bopper.AddComponent<ImpactDamager>();
            damager.minImpactSpeed = 2f;
            damager.damagePerSpeed = 5f;
            damager.knockback = 0.65f;

            Prim(PrimitiveType.Cylinder, "Handle", bopper.transform, Vector3.zero, new Vector3(0.05f, 0.22f, 0.05f), p.Stalk);
            Prim(PrimitiveType.Sphere, "Glove", bopper.transform, new Vector3(0f, 0.32f, 0f), new Vector3(0.28f, 0.24f, 0.28f), Pick(p.Candy));

            return bopper;
        }

        internal static GameObject BuildBopperPrefab(Palette p)
        {
            var go = BuildBopper(Vector3.zero, p);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/BoxingGloveBopper.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static GameObject BuildYoyo(Vector3 position, Palette p)
        {
            // A ball on a SpringJoint instead of the flail's rigid chain: it stretches and snaps back, unpredictable.
            // Both parts share a root (unlike a bare pair of top-level objects) so Instantiate-ing the whole thing
            // as a prefab correctly remaps the SpringJoint's connectedBody to the clone's own handle, not the original's.
            var root = new GameObject("Yo-yo").transform;

            var handle = Part("Yo-yo Handle", root, position, 0.5f);
            Prim(PrimitiveType.Capsule, "Grip", handle.transform, Vector3.zero, new Vector3(0.06f, 0.18f, 0.06f), p.Mallet);

            var ball = Part("Yo-yo Ball", root, position + Vector3.down * 0.5f, 1.2f);
            Prim(PrimitiveType.Sphere, "Ball", ball.transform, Vector3.zero, Vector3.one * 0.22f, Pick(p.Candy));

            var spring = ball.gameObject.AddComponent<SpringJoint>();
            spring.connectedBody = handle;
            spring.autoConfigureConnectedAnchor = false;
            spring.anchor = Vector3.zero;
            spring.connectedAnchor = Vector3.zero;
            spring.spring = 300f;
            spring.damper = 8f;
            spring.minDistance = 0.15f;
            spring.maxDistance = 0.9f;

            var damager = ball.gameObject.AddComponent<ImpactDamager>();
            damager.minImpactSpeed = 2f;
            damager.damagePerSpeed = 8f;
            damager.knockback = 0.3f;

            return root.gameObject;
        }

        internal static GameObject BuildYoyoPrefab(Palette p)
        {
            var go = BuildYoyo(Vector3.zero, p);
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/Yoyo.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void BuildHealthShrine(Vector3 position, Palette p)
        {
            var gold = Mat("Gold", new Color(1f, 0.85f, 0.1f), 0.7f, 0.5f);

            var shrine = new GameObject("Health Shrine");
            shrine.transform.position = position;
            var collider = shrine.AddComponent<CapsuleCollider>();
            collider.height = 1f;
            collider.radius = 0.3f;
            collider.center = new Vector3(0f, 0.5f, 0f);
            shrine.AddComponent<HealthShrine>();

            Prim(PrimitiveType.Cylinder, "Pedestal", shrine.transform, new Vector3(0f, 0.25f, 0f), new Vector3(0.3f, 0.25f, 0.3f), p.Metal, collider: false);
            var orb = Prim(PrimitiveType.Sphere, "Orb", shrine.transform, new Vector3(0f, 0.65f, 0f), Vector3.one * 0.3f, gold, collider: false);
            var spinner = orb.AddComponent<Orbiter>();
            spinner.degreesPerSecond = 0f; // spin in place, not orbit around the world origin
            spinner.spin = new Vector3(0f, 60f, 0f);

            WorldText.Create("Health Shrine Label", shrine.transform, new Vector3(0f, 1.1f, 0f), new Vector2(1.4f, 0.3f), 0.16f).text =
                "+10 Max HP\n25 Blob Bucks";
        }

        // A small, out-of-the-way platform only reached by ShopTeleport (click B), so upgrades never clutter
        // the main play area. Everything here is placed in world space (not parented under the platform sphere),
        // since that sphere's own scale would otherwise blow up any child sized in local units. Returns where
        // the player should stand after warping here.
        static Vector3 BuildShop(Palette p)
        {
            var platformMat = Mat("ShopPlatform", new Color(0.3f, 0.3f, 0.4f), 0.2f);
            var platform = Prim(PrimitiveType.Sphere, "Shop Platform", null,
                ShopPosition, Vector3.one * ShopPlatformRadius * 2f, platformMat);
            platform.AddComponent<GravityAttractor>();

            Vector3 standPosition = ShopPosition + Vector3.up * (ShopPlatformRadius + 0.05f);
            BuildHealthShrine(standPosition + Vector3.forward * 1f, p);

            WorldText.Create("Shop Title", null, standPosition + Vector3.up * 2f + Vector3.forward * 1f,
                new Vector2(2f, 0.4f), 0.3f).text = "BLOB BUCKS SHOP";

            return standPosition;
        }

        static Rigidbody Part(string name, Transform parent, Vector3 position, float mass)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var body = go.AddComponent<Rigidbody>();
            body.mass = mass;
            body.solverIterations = 20;
            body.solverVelocityIterations = 8;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            go.AddComponent<GravityBody>();
            return body;
        }

        static void BallJoint(Rigidbody body, Rigidbody connectedTo, Vector3 anchor)
        {
            var joint = body.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = connectedTo;
            joint.anchor = anchor;
            joint.autoConfigureConnectedAnchor = true;
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            joint.angularXMotion = ConfigurableJointMotion.Free;
            joint.angularYMotion = ConfigurableJointMotion.Free;
            joint.angularZMotion = ConfigurableJointMotion.Free;
        }

        // ---------- Player ----------

        internal static GameObject BuildPlayer(Palette p, PhysicsMaterial slippery, bool addVitals = true,
            Vector3? spawnPosition = null, Vector3? shopStandPosition = null)
        {
            var player = new GameObject("Player");
            player.transform.SetPositionAndRotation(spawnPosition ?? SpawnPoint, Quaternion.identity);

            var body = player.AddComponent<Rigidbody>();
            body.mass = 70f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;

            var capsule = player.AddComponent<CapsuleCollider>();
            capsule.radius = 0.25f;
            capsule.height = 1.6f;
            capsule.center = new Vector3(0f, 0.8f, 0f);
            capsule.sharedMaterial = slippery;

            player.AddComponent<GravityBody>();
            var walker = player.AddComponent<PlanetWalker>();
            PlayerVitals vitals = null;
            PlayerUltimate ultimate = null;
            PlayerBucks bucks = null;
            PlayerLevel level = null;
            if (addVitals)
            {
                var health = player.AddComponent<Damageable>();
                health.isPlayer = true;
                health.destroyOnDeath = false;
                health.maxHealth = 100f;
                health.invulnerableSeconds = 0.75f;
                vitals = player.AddComponent<PlayerVitals>();
                ultimate = player.AddComponent<PlayerUltimate>();
                bucks = player.AddComponent<PlayerBucks>();
                level = player.AddComponent<PlayerLevel>();

                if (shopStandPosition.HasValue)
                {
                    var teleport = player.AddComponent<ShopTeleport>();
                    teleport.walker = walker;
                    teleport.shopPosition = shopStandPosition.Value;
                }
            }

            var originGo = new GameObject("XR Origin");
            originGo.transform.SetParent(player.transform, false);
            var origin = originGo.AddComponent<XROrigin>();
            var offset = new GameObject("Camera Offset").transform;
            offset.SetParent(originGo.transform, false);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            cameraGo.transform.SetParent(offset, false);
            var camera = cameraGo.AddComponent<Camera>();
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 500f;
            cameraGo.AddComponent<AudioListener>();
            AddPoseDriver(cameraGo, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation");

            origin.Camera = camera;
            origin.CameraFloorOffsetObject = offset.gameObject;
            origin.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;

            var leftController = Controller("Left Controller", offset, "LeftHand", -0.2f);
            var rightController = Controller("Right Controller", offset, "RightHand", 0.2f);
            walker.head = cameraGo.transform;
            if (ultimate != null) ultimate.head = cameraGo.transform;
            if (vitals != null) vitals.head = cameraGo.transform;
            if (bucks != null) bucks.head = cameraGo.transform;
            if (level != null) level.head = cameraGo.transform;

            var leftHand = BuildHand(HandGrabber.Side.Left, leftController, walker, cameraGo.transform, p);
            var rightHand = BuildHand(HandGrabber.Side.Right, rightController, walker, cameraGo.transform, p);
            if (vitals != null)
            {
                vitals.handRenderers = leftHand.GetComponentsInChildren<MeshRenderer>()
                    .Concat(rightHand.GetComponentsInChildren<MeshRenderer>())
                    .Cast<Renderer>()
                    .ToArray();
            }

            var debugRig = player.AddComponent<DesktopDebugRig>();
            debugRig.cameraTransform = cameraGo.transform;
            debugRig.leftController = leftController;
            debugRig.rightController = rightController;
            return player;
        }

        static Transform Controller(string name, Transform parent, string xrHand, float x)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x, 1.1f, 0.3f);
            AddPoseDriver(go, "<XRController>{" + xrHand + "}/devicePosition", "<XRController>{" + xrHand + "}/deviceRotation");
            return go.transform;
        }

        static void AddPoseDriver(GameObject go, string positionBinding, string rotationBinding)
        {
            var driver = go.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();
            var positionAction = new InputAction(go.name + " Position", binding: positionBinding, expectedControlType: "Vector3");
            var rotationAction = new InputAction(go.name + " Rotation", binding: rotationBinding, expectedControlType: "Quaternion");
            driver.positionInput = new InputActionProperty(positionAction);
            driver.rotationInput = new InputActionProperty(rotationAction);
            // A loose InputAction (not part of an enabled action map) never reads live device values
            // until Enable() is called - without this the driver just leaves the transform at its
            // default local pose, which is why the camera/hands never actually tracked at all.
            positionAction.Enable();
            rotationAction.Enable();
        }

        static GameObject BuildHand(HandGrabber.Side side, Transform controller, PlanetWalker walker, Transform head, Palette p)
        {
            bool left = side == HandGrabber.Side.Left;
            var go = new GameObject(side + " Hand");
            go.transform.SetPositionAndRotation(controller.position, controller.rotation);

            var body = go.AddComponent<Rigidbody>();
            body.mass = 1f;
            body.angularDamping = 1f;
            go.AddComponent<SphereCollider>().radius = 0.065f;
            Prim(PrimitiveType.Sphere, "Mitten", go.transform, Vector3.zero, new Vector3(0.12f, 0.1f, 0.15f), p.Hand, collider: false);
            Prim(PrimitiveType.Sphere, "Thumb", go.transform, new Vector3(left ? 0.06f : -0.06f, 0.02f, 0.02f), Vector3.one * 0.05f, p.Hand, collider: false);

            var hand = go.AddComponent<FloppyHand>();
            hand.target = controller;
            hand.playerBody = walker.GetComponent<Rigidbody>();

            var damager = go.AddComponent<ImpactDamager>();
            damager.minImpactSpeed = 2f;
            damager.damagePerSpeed = 4f;
            damager.knockback = 0.3f;

            var grabber = go.AddComponent<HandGrabber>();
            grabber.side = side;
            grabber.walker = walker;

            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = p.Arm;
            line.numCapVertices = 4;
            line.numCornerVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            var arm = go.AddComponent<ElasticArm>();
            arm.head = head;
            arm.walker = walker;
            arm.side = left ? -1f : 1f;
            return go;
        }

        // ---------- Helpers ----------

        internal static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale,
            Material material, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        internal static Transform Eye(Transform parent, Vector3 localPosition, float size, Material iris, Palette p)
        {
            var eye = Prim(PrimitiveType.Sphere, "Eye", parent, localPosition, Vector3.one * size, p.EyeWhite, collider: false);
            eye.AddComponent<LookAtCamera>();
            Prim(PrimitiveType.Sphere, "Iris", eye.transform, new Vector3(0f, 0f, 0.38f), new Vector3(0.6f, 0.6f, 0.3f), iris, collider: false);
            Prim(PrimitiveType.Sphere, "Pupil", eye.transform, new Vector3(0f, 0f, 0.47f), new Vector3(0.3f, 0.3f, 0.12f), p.Pupil, collider: false);
            return eye.transform;
        }

        internal static Material Pick(Material[] materials) => materials[Random.Range(0, materials.Length)];

        // Keeps the main menu first in Build Settings and this scene right after it, however the two
        // builders happen to be run.
        internal static void RegisterScene(string path, bool isMenu)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            int index = isMenu ? 0 : scenes.Any(s => s.path == MainMenuSceneBuilder.ScenePath) ? 1 : 0;
            index = Mathf.Min(index, scenes.Count);
            scenes.Insert(index, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
