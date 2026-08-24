using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class BuildEnvInjector :
    IPreprocessBuildWithReport,
    IPostprocessBuildWithReport
{
    private const string RootEnvFileName = ".env";
    private const string BuildEnvResourceRelativePath = "Assets/Resources/BuildEnv.txt";

    public int callbackOrder => 10;

    public void OnPreprocessBuild(BuildReport report)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        string sourcePath = Path.Combine(projectRoot, RootEnvFileName);
        string destinationPath = Path.Combine(projectRoot, BuildEnvResourceRelativePath);

        if (!File.Exists(sourcePath))
        {
            Debug.LogWarning(
                "[BuildEnvInjector] No root .env found. " +
                "Android build will not include API keys.");
            return;
        }

        ValidateRequiredKeys(sourcePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        File.Copy(sourcePath, destinationPath, true);
        AssetDatabase.Refresh();

        Debug.Log("[BuildEnvInjector] Copied root .env into Resources for build.");
    }

    public void OnPostprocessBuild(BuildReport report)
    {
        CleanupStreamingAssetsEnv();
    }

    [MenuItem("Tools/Marine Demo/Cleanup Build .env")]
    public static void CleanupStreamingAssetsEnv()
    {
        string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
        string envPath = Path.Combine(projectRoot, BuildEnvResourceRelativePath);
        string metaPath = envPath + ".meta";

        bool changed = false;

        if (File.Exists(envPath))
        {
            File.Delete(envPath);
            changed = true;
        }

        if (File.Exists(metaPath))
        {
            File.Delete(metaPath);
            changed = true;
        }

        if (changed)
        {
            AssetDatabase.Refresh();
            Debug.Log("[BuildEnvInjector] Removed temporary build env resource.");
        }
    }

    private static void ValidateRequiredKeys(string envPath)
    {
        string content = File.ReadAllText(envPath);
        string[] requiredKeys =
        {
            "GEMINI_API_KEY="
        };

        foreach (string key in requiredKeys)
        {
            if (!content.Contains(key))
            {
                Debug.LogWarning(
                    $"[BuildEnvInjector] Missing {key.TrimEnd('=')} in root .env.");
            }
        }
    }
}
