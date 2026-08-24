using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class MarineAndroidBuildConfigurator
{
    [MenuItem("Tools/Marine Demo/Configure Android Build")]
    public static void Configure()
    {
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Android,
            BuildTarget.Android);

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.demo.marine");
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.High);
        PlayerSettings.stripEngineCode = true;

        Debug.Log("Configured Android build: min API 26, IL2CPP, ARM64, high stripping, custom manifest.");
    }
}
