using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace CreateEnv
{
    // Reads/writes environments. Built-ins come from code (BuiltInProfiles); user
    // environments are one .json file each under persistentDataPath/environments/.
    public static class EnvironmentRepository
    {
        public static string UserDir =>
            Path.Combine(Application.persistentDataPath, "environments");

        // ── Listing ──────────────────────────────────────────────────────────
        public static List<EnvironmentProfile> ListBuiltIns()
        {
            var list = new List<EnvironmentProfile>(BuiltInProfiles.All());
            return list;
        }

        public static List<EnvironmentProfile> ListUser()
        {
            var list = new List<EnvironmentProfile>();
            if (!Directory.Exists(UserDir)) return list;

            foreach (var path in Directory.GetFiles(UserDir, "*.json"))
            {
                var p = ReadFile(path);
                if (p != null) list.Add(p);
            }
            list.Sort((a, b) => string.Compare(a.displayName, b.displayName,
                                               System.StringComparison.OrdinalIgnoreCase));
            return list;
        }

        // ── Save / delete ────────────────────────────────────────────────────
        public static EnvironmentProfile Save(EnvironmentProfile profile)
        {
            if (profile == null) return null;
            profile.isBuiltIn = false; // user copies are always editable
            if (string.IsNullOrEmpty(profile.id) || profile.id.StartsWith("builtin-"))
                profile.id = MakeId(profile.displayName);

            EnvironmentBounds.Clamp(profile);

            Directory.CreateDirectory(UserDir);
            File.WriteAllText(FilePath(profile.id), JsonUtility.ToJson(profile, true));
            return profile;
        }

        public static void Delete(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            string path = FilePath(id);
            if (File.Exists(path)) File.Delete(path);
        }

        public static EnvironmentProfile Load(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (id.StartsWith("builtin-"))
                foreach (var b in BuiltInProfiles.All())
                    if (b.id == id) return b;
            return ReadFile(FilePath(id));
        }

        // ── Internals ────────────────────────────────────────────────────────
        static EnvironmentProfile ReadFile(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                var p = JsonUtility.FromJson<EnvironmentProfile>(json);
                if (p == null) return null;
                EnvironmentBounds.Clamp(p); // never trust the file
                return p;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[EnvironmentRepository] Skipping unreadable profile '{path}': {e.Message}");
                return null;
            }
        }

        static string FilePath(string id) => Path.Combine(UserDir, id + ".json");

        static string MakeId(string displayName)
        {
            var sb = new StringBuilder();
            foreach (char c in (displayName ?? "env").ToLowerInvariant())
                sb.Append(char.IsLetterOrDigit(c) ? c : '-');
            string slug = sb.ToString().Trim('-');
            if (slug.Length == 0) slug = "env";
            // Short unique suffix so two same-named envs don't collide.
            return slug + "-" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
        }
    }
}
