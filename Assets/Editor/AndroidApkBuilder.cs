using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class AndroidApkBuilder
{
    private const string DefaultOutputDirectory = "Builds/Android";
    private const string DefaultApkName = "MarineDemo.apk";

    [MenuItem("Tools/Marine Demo/Build Android APK")]
    public static void BuildAndroidApk()
    {
        MarineAndroidBuildConfigurator.Configure();

        string outputDirectory = Path.Combine(
            Directory.GetParent(Application.dataPath)!.FullName,
            DefaultOutputDirectory);
        Directory.CreateDirectory(outputDirectory);

        string outputPath = Path.Combine(outputDirectory, DefaultApkName);
        string[] scenes = GetEnabledScenes();
        if (scenes.Length == 0)
            throw new InvalidOperationException(
                "No enabled scenes found in Build Settings.");

        var buildPlayerOptions = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new InvalidOperationException(
                "Android APK build failed. Check the Unity Console for details.");
        }

        Debug.Log($"Android APK build completed: {outputPath}");
        EditorUtility.RevealInFinder(outputPath);
    }

    public static void BuildAndroidApkFromCommandLine()
    {
        BuildAndroidApk();
    }

    private static string[] GetEnabledScenes()
    {
        return Array.FindAll(
            EditorBuildSettings.scenes,
            scene => scene.enabled && !string.IsNullOrWhiteSpace(scene.path))
            is var scenes
            ? Array.ConvertAll(scenes, scene => scene.path)
            : Array.Empty<string>();
    }
}
