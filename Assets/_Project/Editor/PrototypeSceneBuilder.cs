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
        const string MaterialFolder = "Assets/_Project/Materials";
        const string PrefabFolder = "Assets/_Project/Prefabs";
        const string SceneFolder = "Assets/_Project/Scenes";
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

        class Palette
        {
            public Material Hand, Arm, EyeWhite, Pupil, Blob, Stalk, Metal, Mallet, Rock, ShroomCap, ShroomStalk;
            public Material[] Candy, Planet;
        }

        [MenuItem("Orange World/2. Build Prototype Scene", priority = 2)]
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
            var blobling = BuildBloblingPrefab(palette, bouncy);

            BuildLightingAndSky(palette);
            var keepClear = Planets.Select(_ => new List<Vector3>()).ToArray();
            keepClear[0].Add(Vector3.up);
            BuildPlanets(palette);
            BuildJumpPads(palette, keepClear);
            BuildDecor(palette, keepClear);
            BuildSpaceJunk(palette);
            BuildFlail(SpawnPoint + new Vector3(0.6f, 1.1f, 0.9f), palette);
            BuildMallet(SpawnPoint + new Vector3(-0.6f, 0.5f, 0.9f), palette);
            var player = BuildPlayer(palette, slippery);
            new GameObject("Wave Spawner").AddComponent<WaveSpawner>().creaturePrefab = blobling;

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuild(ScenePath);
            Selection.activeGameObject = player;
            Debug.Log($"[Orange World] Built {ScenePath}. Press Play (with Quest Link) or Build And Run to the headset.");
        }

        // ---------- Assets ----------

        static Palette CreatePalette()
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
            };

            Color[] candy =
            {
                new(1f, 0.25f, 0.55f), new(0.2f, 0.9f, 1f), new(1f, 0.9f, 0.1f),
                new(0.55f, 0.2f, 1f), new(0.3f, 1f, 0.5f), new(1f, 0.45f, 0.1f),
            };
            palette.Candy = candy.Select((c, i) => Mat("Candy" + i, c, 0.6f, i % 3 == 0 ? 0.8f : 0f)).ToArray();
            palette.Planet = Planets.Select(p => Mat("Planet_" + p.Name.Replace(" ", ""), p.Color, 0.15f)).ToArray();
            return palette;
        }

        static Material Mat(string name, Color color, float smoothness, float emission = 0f)
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

        static PhysicsMaterial PhysicsMat(string name, float friction, float bounce,
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

        static GameObject BuildBloblingPrefab(Palette p, PhysicsMaterial bouncy)
        {
            var go = new GameObject("Blobling");
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
            go.AddComponent<Jiggle>().visual = visual;
            go.AddComponent<SplitOnDeath>();
            go.AddComponent<RandomTint>().targets = new[] { blob.GetComponent<Renderer>() };

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabFolder + "/Blobling.prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---------- World ----------

        static void BuildLightingAndSky(Palette p)
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

        static void BuildJumpPads(Palette p, List<Vector3>[] keepClear)
        {
            var parent = new GameObject("Jump Shrooms").transform;
            foreach (var (from, to) in JumpRoutes)
            {
                var a = Planets[from];
                var b = Planets[to];
                Vector3 direction = (b.Position - a.Position).normalized;
                float gap = Vector3.Distance(a.Position, b.Position) - a.Radius - b.Radius;

                var pad = new GameObject($"Shroom {a.Name} -> {b.Name}");
                pad.transform.SetParent(parent, false);
                pad.transform.SetPositionAndRotation(a.Position + direction * a.Radius, Quaternion.FromToRotation(Vector3.up, direction));
                Prim(PrimitiveType.Cylinder, "Stalk", pad.transform, new Vector3(0f, 0.15f, 0f), new Vector3(0.35f, 0.15f, 0.35f), p.ShroomStalk, collider: false);
                Prim(PrimitiveType.Sphere, "Cap", pad.transform, new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.35f, 1.6f), p.ShroomCap, collider: false);

                var trigger = pad.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.center = new Vector3(0f, 0.6f, 0f);
                trigger.radius = 0.8f;
                pad.AddComponent<JumpPad>().launchSpeed = Mathf.Min(26f, 9f + gap * 0.3f);
                keepClear[from].Add(direction);
            }
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

        // ---------- Weapons ----------

        static void BuildFlail(Vector3 handlePosition, Palette p)
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
        }

        static void BuildMallet(Vector3 position, Palette p)
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

        static GameObject BuildPlayer(Palette p, PhysicsMaterial slippery)
        {
            var player = new GameObject("Player");
            player.transform.SetPositionAndRotation(SpawnPoint, Quaternion.identity);

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
            var health = player.AddComponent<Damageable>();
            health.isPlayer = true;
            health.destroyOnDeath = false;
            health.maxHealth = 100f;
            health.invulnerableSeconds = 0.75f;
            var vitals = player.AddComponent<PlayerVitals>();

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

            var leftHand = BuildHand(HandGrabber.Side.Left, leftController, walker, cameraGo.transform, p);
            var rightHand = BuildHand(HandGrabber.Side.Right, rightController, walker, cameraGo.transform, p);
            vitals.handRenderers = leftHand.GetComponentsInChildren<MeshRenderer>()
                .Concat(rightHand.GetComponentsInChildren<MeshRenderer>())
                .Cast<Renderer>()
                .ToArray();

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
            driver.positionInput = new InputActionProperty(
                new InputAction(go.name + " Position", binding: positionBinding, expectedControlType: "Vector3"));
            driver.rotationInput = new InputActionProperty(
                new InputAction(go.name + " Rotation", binding: rotationBinding, expectedControlType: "Quaternion"));
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

        static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale,
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

        static Transform Eye(Transform parent, Vector3 localPosition, float size, Material iris, Palette p)
        {
            var eye = Prim(PrimitiveType.Sphere, "Eye", parent, localPosition, Vector3.one * size, p.EyeWhite, collider: false);
            eye.AddComponent<LookAtCamera>();
            Prim(PrimitiveType.Sphere, "Iris", eye.transform, new Vector3(0f, 0f, 0.38f), new Vector3(0.6f, 0.6f, 0.3f), iris, collider: false);
            Prim(PrimitiveType.Sphere, "Pupil", eye.transform, new Vector3(0f, 0f, 0.47f), new Vector3(0.3f, 0.3f, 0.12f), p.Pupil, collider: false);
            return eye.transform;
        }

        static Material Pick(Material[] materials) => materials[Random.Range(0, materials.Length)];

        static void AddSceneToBuild(string path)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != path).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
