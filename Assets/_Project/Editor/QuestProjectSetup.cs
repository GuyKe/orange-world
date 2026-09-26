using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OrangeWorld.EditorTools
{
    public static class QuestProjectSetup
    {
        const string SettingsFolder = "Assets/_Project/Settings";

        [MenuItem("Orange World/1. Configure Project for Quest", priority = 1)]
        public static void Configure()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            PlayerSettings.companyName = "Orange World";
            PlayerSettings.productName = "Orange World";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.orangeworld.game");
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            EnsureUrp();
            AssetDatabase.SaveAssets();

            Debug.Log("[Orange World] Quest player settings applied. Next: in XR Plug-in Management enable OpenXR on the " +
                      "Android tab, add the Meta Quest Support feature and the Oculus Touch Controller Profile (see README).");
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
        }

        public static void EnsureUrp()
        {
            if (GraphicsSettings.defaultRenderPipeline != null) return;

            EnsureFolder(SettingsFolder);
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, SettingsFolder + "/OrangeWorld_Renderer.asset");

            var pipeline = UniversalRenderPipelineAsset.Create(rendererData);
            pipeline.msaaSampleCount = 4;
            pipeline.supportsHDR = false; // HDR costs a full-screen copy per eye on mobile GPUs.
            pipeline.shadowDistance = 30f;
            AssetDatabase.CreateAsset(pipeline, SettingsFolder + "/OrangeWorld_URP.asset");

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            AssetDatabase.SaveAssets();
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
