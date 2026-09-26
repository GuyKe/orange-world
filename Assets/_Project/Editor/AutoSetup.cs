using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using UnityEngine.XR.OpenXR.Features.MetaQuestSupport;

namespace OrangeWorld.EditorTools
{
    public static class AutoSetup
    {
        public static void ConfigureXR()
        {
            Debug.Log("[AutoSetup] Enabling OpenXR loader for Android via XR Plug-in Management...");
            XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
            foreach (var guid in AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget"))
            {
                buildTargetSettings = AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(guid));
                if (buildTargetSettings != null) break;
            }
            if (buildTargetSettings == null)
            {
                buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                System.IO.Directory.CreateDirectory("Assets/XR/Settings");
                AssetDatabase.CreateAsset(buildTargetSettings, "Assets/XR/Settings/XRGeneralSettingsPerBuildTarget.asset");
                AssetDatabase.SaveAssets();
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, buildTargetSettings, true);
            }

            if (!buildTargetSettings.HasSettingsForBuildTarget(BuildTargetGroup.Android))
                buildTargetSettings.CreateDefaultSettingsForBuildTarget(BuildTargetGroup.Android);

            if (!buildTargetSettings.HasManagerSettingsForBuildTarget(BuildTargetGroup.Android))
                buildTargetSettings.CreateDefaultManagerSettingsForBuildTarget(BuildTargetGroup.Android);

            var androidXrSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            bool assigned = XRPackageMetadataStore.AssignLoader(androidXrSettings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", BuildTargetGroup.Android);
            Debug.Log($"[AutoSetup] OpenXR loader assigned for Android: {assigned}");
            androidXrSettings.InitManagerOnStart = true;
            EditorUtility.SetDirty(androidXrSettings.Manager);
            EditorUtility.SetDirty(androidXrSettings);
            EditorUtility.SetDirty(buildTargetSettings);

            Debug.Log("[AutoSetup] Enabling Meta Quest Support + Oculus Touch Controller Profile OpenXR features...");
            FeatureHelpers.RefreshFeatures(BuildTargetGroup.Android);
            var androidOpenXrSettings = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (androidOpenXrSettings == null)
            {
                Debug.LogError("[AutoSetup] No OpenXR settings found for Android build target group.");
            }
            else
            {
                var metaQuestFeature = androidOpenXrSettings.GetFeature<MetaQuestFeature>();
                if (metaQuestFeature != null) { metaQuestFeature.enabled = true; Debug.Log("[AutoSetup] Enabled MetaQuestFeature"); }
                else Debug.LogError("[AutoSetup] MetaQuestFeature not found");

                var touchProfile = androidOpenXrSettings.GetFeature<OculusTouchControllerProfile>();
                if (touchProfile != null) { touchProfile.enabled = true; Debug.Log("[AutoSetup] Enabled OculusTouchControllerProfile"); }
                else Debug.LogError("[AutoSetup] OculusTouchControllerProfile not found");

                EditorUtility.SetDirty(androidOpenXrSettings);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[AutoSetup] XR configuration DONE");
        }

        public static void BuildApk()
        {
            Debug.Log("[AutoSetup] Starting Android build...");
            var scenes = EditorBuildSettings.scenes;
            var buildPath = "Builds/Android/OrangeWorld.apk";
            System.IO.Directory.CreateDirectory("Builds/Android");

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = System.Array.ConvertAll(scenes, s => s.path),
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = BuildOptions.None,
            });

            Debug.Log($"[AutoSetup] Build result: {report.summary.result}, total errors: {report.summary.totalErrors}, size: {report.summary.totalSize} bytes, output: {buildPath}");
        }
    }
}
