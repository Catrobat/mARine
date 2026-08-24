using System.IO;
using UnityEditor;
using UnityEngine;

namespace CreateEnv.Ecosystem.EditorTools
{
    // Writes a SpeciesLibrary asset pre-filled with the built-in roster, into a
    // Resources folder where the simulation will find it.
    //
    // After this, every rate is an inspector field: balancing needs no rebuild, and
    // no code change, which is what the milestone document asks for. Deleting the
    // asset falls straight back to the built-in roster.
    public static class SpeciesLibraryAssetCreator
    {
        const string Folder = "Assets/Resources";
        const string AssetPath = Folder + "/SpeciesLibrary.asset";

        [MenuItem("Tools/Living Ecosystem/Create Species Library Asset")]
        static void Create()
        {
            if (File.Exists(AssetPath))
            {
                bool replace = EditorUtility.DisplayDialog(
                    "Species Library already exists",
                    "A SpeciesLibrary asset is already present. Replacing it will discard any " +
                    "balancing you have done in it.\n\nReplace it?",
                    "Replace", "Cancel");
                if (!replace) return;
            }

            if (!Directory.Exists(Folder)) Directory.CreateDirectory(Folder);

            var asset = ScriptableObject.CreateInstance<SpeciesLibraryAsset>();
            // Stamped so a later build can tell this was written before any fields it
            // now expects, and ignore it loudly rather than override the code with zeros.
            asset.rosterVersion = SpeciesLibrary.RosterVersion;

            // Clone the built-ins so the asset owns its own copies and editing it
            // cannot mutate the code defaults for the rest of the session.
            var source = SpeciesLibrary.All;
            asset.species = new SpeciesDefinition[source.Length];
            for (int i = 0; i < source.Length; i++) asset.species[i] = source[i].Clone();

            var web = SpeciesLibrary.Web;
            asset.web = new FoodLink[web.Length];
            for (int i = 0; i < web.Length; i++)
                asset.web[i] = new FoodLink(web[i].predator, web[i].prey,
                                            web[i].preference, web[i].halfSaturation);

            AssetDatabase.CreateAsset(asset, AssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SpeciesLibrary.Invalidate();

            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            Debug.Log($"[LivingReef] Created {AssetPath}. Every rate is now editable without a rebuild. " +
                      "Re-run Tools > Living Ecosystem > Balance Report after changing anything.");
        }
    }
}
