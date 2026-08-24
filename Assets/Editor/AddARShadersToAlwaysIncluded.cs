using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// One-shot menu utility that registers the AR background shaders in
/// Project Settings → Graphics → Always Included Shaders.
///
/// Required because AR_Spawn now lives in an Addressables bundle, so the
/// build-time shader stripper can't see the AR Camera Background material
/// and strips the ARCore/ARKit background shaders from the player. Result:
/// black AR camera on device, fine in editor.
///
/// Naming changed across AR Foundation versions, so this version SCANS every
/// shader available in the project and adds any whose name looks like an AR
/// camera background shader.
///
/// Run via: Tools ▸ mARine ▸ Fix AR Background Shader Stripping
/// </summary>
public static class AddARShadersToAlwaysIncluded
{
    // A shader is considered an "AR background" candidate if its name (case-insensitive)
    // contains BOTH a platform tag AND a "background" / "camera" tag, or matches one of
    // the known historical names.
    static readonly string[] HardNames =
    {
        "Unlit/ARCoreBackground",
        "Unlit/ARKitBackground",
        "Unlit/ARCameraBackground",
        "Hidden/AR Foundation/CameraBackground",
        "Hidden/ARCore/CameraBackground",
        "Hidden/ARKit/CameraBackground",
    };

    static readonly string[] PlatformTags = { "arcore", "arkit", "arfoundation", "ar foundation" };
    static readonly string[] PurposeTags = { "background", "camerabackground", "camera background" };

    [MenuItem("Tools/mARine/Fix AR Background Shader Stripping")]
    public static void Run()
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
        if (assets == null || assets.Length == 0)
        {
            EditorUtility.DisplayDialog("AR shader fix",
                "Could not load ProjectSettings/GraphicsSettings.asset.", "OK");
            return;
        }

        var so = new SerializedObject(assets[0]);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");
        if (arr == null)
        {
            EditorUtility.DisplayDialog("AR shader fix",
                "Could not find m_AlwaysIncludedShaders property.", "OK");
            return;
        }

        // Build the candidate list.
        var candidates = new HashSet<Shader>();

        // 1. Hard-coded historical names.
        foreach (var name in HardNames)
        {
            var s = Shader.Find(name);
            if (s != null) candidates.Add(s);
        }

        // 2. Scan every shader the project knows about and keep AR-flavoured ones.
        var allGuids = AssetDatabase.FindAssets("t:Shader");
        foreach (var guid in allGuids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var s = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (s == null) continue;
            if (LooksLikeARBackground(s.name) || LooksLikeARBackground(path))
                candidates.Add(s);
        }

        if (candidates.Count == 0)
        {
            EditorUtility.DisplayDialog("AR shader fix",
                "No AR background shaders were found in the project.\n\n" +
                "Check that com.unity.xr.arcore (and com.unity.xr.arkit on iOS) are imported, " +
                "and the active build target is Android or iOS. Then re-run this tool.", "OK");
            return;
        }

        // Diff against existing entries.
        var existing = new HashSet<Object>();
        for (int i = 0; i < arr.arraySize; i++)
        {
            var obj = arr.GetArrayElementAtIndex(i).objectReferenceValue;
            if (obj != null) existing.Add(obj);
        }

        var added = new List<string>();
        var skipped = new List<string>();
        foreach (var shader in candidates)
        {
            if (existing.Contains(shader))
            {
                skipped.Add(shader.name);
                continue;
            }
            arr.arraySize++;
            arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
            added.Add(shader.name);
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();

        var msg = new StringBuilder();
        msg.AppendLine($"Candidates found: {candidates.Count}");
        msg.AppendLine($"Added:   {added.Count}");
        msg.AppendLine($"Already present: {skipped.Count}");
        msg.AppendLine();
        if (added.Count > 0)
        {
            msg.AppendLine("ADDED:");
            foreach (var n in added) msg.AppendLine("  • " + n);
            msg.AppendLine();
        }
        if (skipped.Count > 0)
        {
            msg.AppendLine("ALREADY PRESENT:");
            foreach (var n in skipped) msg.AppendLine("  • " + n);
        }
        msg.AppendLine();
        msg.AppendLine("Rebuild the APK now.");

        Debug.Log("[AR shader fix] " + msg);
        EditorUtility.DisplayDialog("AR shader fix", msg.ToString(), "OK");
    }

    static bool LooksLikeARBackground(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        var low = s.ToLowerInvariant();
        bool hasPlatform = PlatformTags.Any(t => low.Contains(t));
        bool hasPurpose = PurposeTags.Any(t => low.Contains(t));
        return hasPlatform && hasPurpose;
    }
}
