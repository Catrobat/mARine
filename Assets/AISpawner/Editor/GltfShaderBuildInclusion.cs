using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MarineAR.AISpawner.EditorTools
{
    /// <summary>
    /// Guarantees the glTFast URP shader graphs ship inside the player build.
    ///
    /// Why this exists: AISpawner loads GLB models at runtime, so no built asset
    /// references glTFast's shader graphs and Unity strips them from the APK.
    /// In the Editor glTFast loads its shaders by GUID through the AssetDatabase
    /// (always available), but in a player it falls back to
    /// <c>Shader.Find("Shader Graphs/glTF-…")</c> — which returns null once the
    /// shader was stripped. Result: "ShaderMissing" in logcat and pink models on
    /// device while everything looks fine in the Editor.
    ///
    /// The fix is to append the shader graphs to Graphics Settings →
    /// "Always Included Shaders". The settings UI's object picker cannot browse
    /// package assets, so this tool assigns them via script instead, resolving each
    /// shader by the same asset GUIDs glTFast uses internally (stable across
    /// package versions) with a name-based lookup as fallback.
    ///
    /// Runs automatically before every player build (additive and idempotent), and
    /// manually via: Marine AR → AI Spawner → Include glTFast Shaders in Build.
    /// </summary>
    public static class GltfShaderBuildInclusion
    {
        const string k_GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        const string k_AlwaysIncludedProperty = "m_AlwaysIncludedShaders";

        /// <summary>
        /// GUID + runtime name of every shader glTFast's URP material generator can
        /// request via Shader.Find in a player build. GUIDs are taken from
        /// ShaderGraphMaterialGenerator / UniversalRPMaterialGenerator in the package.
        /// </summary>
        static readonly (string guid, string name)[] k_RequiredShaders =
        {
            ("b9d29dfa1474148e792ac720cbd45122", "Shader Graphs/glTF-pbrMetallicRoughness"),
            ("c87047c884d9843f5b0f4cce282aa760", "Shader Graphs/glTF-unlit"),
            ("9a07dad0f3c4e43ff8312e3b5fa42300", "Shader Graphs/glTF-pbrSpecularGlossiness"),
            ("c18c97ae1ce021b4980c5d19a54f0d3c", "Shader Graphs/glTF-pbrMetallicRoughness-Clearcoat"),
        };

        [MenuItem("Marine AR/AI Spawner/Include glTFast Shaders in Build")]
        public static void IncludeShadersMenu()
        {
            int added = EnsureIncluded();
            string message = added > 0
                ? $"Added {added} glTFast shader(s) to Graphics Settings → Always Included Shaders.\n\nRebuild the APK for the change to take effect (no Addressables rebuild needed)."
                : "All glTFast shaders are already in Always Included Shaders.\n\nIf models are still pink on device, rebuild the APK — the change only applies to new player builds.";

            EditorUtility.DisplayDialog("glTFast Shader Inclusion", message, "OK");
        }

        /// <summary>
        /// Appends any missing glTFast shaders to the Always Included Shaders list.
        /// Additive and idempotent — existing entries are never touched.
        /// </summary>
        /// <returns>Number of shaders newly added.</returns>
        public static int EnsureIncluded()
        {
            Object[] settingsAssets = AssetDatabase.LoadAllAssetsAtPath(k_GraphicsSettingsPath);
            if (settingsAssets == null || settingsAssets.Length == 0)
            {
                Debug.LogError("[AISpawner] Could not open GraphicsSettings.asset — add the glTFast shaders to Always Included Shaders manually.");
                return 0;
            }

            var serializedSettings = new SerializedObject(settingsAssets[0]);
            SerializedProperty list = serializedSettings.FindProperty(k_AlwaysIncludedProperty);
            if (list == null || !list.isArray)
            {
                Debug.LogError($"[AISpawner] Property '{k_AlwaysIncludedProperty}' not found in GraphicsSettings.");
                return 0;
            }

            var alreadyIncluded = new HashSet<Object>();
            for (int i = 0; i < list.arraySize; i++)
            {
                Object entry = list.GetArrayElementAtIndex(i).objectReferenceValue;
                if (entry != null)
                    alreadyIncluded.Add(entry);
            }

            int added = 0;
            foreach ((string guid, string name) in k_RequiredShaders)
            {
                Shader shader = Resolve(guid, name);
                if (shader == null)
                {
                    // The clearcoat graph only exists on some URP versions; glTFast
                    // falls back to the base metallic shader when it is absent.
                    Debug.LogWarning($"[AISpawner] glTFast shader '{name}' not found in this project — skipped.");
                    continue;
                }

                if (alreadyIncluded.Contains(shader))
                    continue;

                int index = list.arraySize;
                list.InsertArrayElementAtIndex(index);
                list.GetArrayElementAtIndex(index).objectReferenceValue = shader;
                added++;
                Debug.Log($"[AISpawner] Always Included Shaders += '{name}'.");
            }

            if (added > 0)
            {
                serializedSettings.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
            }

            return added;
        }

        static Shader Resolve(string guid, string name)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
                if (shader != null)
                    return shader;
            }

            // Fallback for future package versions that changed asset GUIDs.
            return Shader.Find(name);
        }
    }

    /// <summary>
    /// Safety net: re-checks shader inclusion before every player build, so the fix
    /// survives fresh checkouts and settings resets without anyone remembering it.
    /// </summary>
    sealed class GltfShaderBuildPreprocessor : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            int added = GltfShaderBuildInclusion.EnsureIncluded();
            if (added > 0)
                Debug.Log($"[AISpawner] Build preprocessor added {added} glTFast shader(s) to Always Included Shaders.");
        }
    }
}
