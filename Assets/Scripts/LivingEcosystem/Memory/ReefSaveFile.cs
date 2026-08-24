using System;
using System.IO;
using UnityEngine;

namespace CreateEnv.Ecosystem.Memory
{
    // Where a reef lives between sessions.
    //
    // One small file per environment, on the device. No server and no account
    // (Design Document 8.3). Sits beside the environment profiles the builder
    // already writes, so a learner's saved worlds and their saved reefs are in the
    // same place.
    public static class ReefSaveFile
    {
        public const int BudgetBytes = 4096;

        public static string Folder =>
            Path.Combine(Application.persistentDataPath, "reefs");

        public static string PathFor(string environmentId)
        {
            string id = string.IsNullOrEmpty(environmentId) ? "default" : environmentId;
            return Path.Combine(Folder, id + ".json");
        }

        public static bool Exists(string environmentId) => File.Exists(PathFor(environmentId));

        public static void Write(ReefSave save)
        {
            if (save == null) return;

            try
            {
                Directory.CreateDirectory(Folder);
                string json = JsonUtility.ToJson(save);
                File.WriteAllText(PathFor(save.environmentId), json);

                // The budget is a design constraint, not a hard limit, so exceeding it
                // is a warning rather than a failure — but it should never pass
                // unnoticed, because the packing exists precisely to stay under it.
                int bytes = System.Text.Encoding.UTF8.GetByteCount(json);
                if (bytes > BudgetBytes)
                    Debug.LogWarning($"[ReefSave] '{save.environmentId}' wrote {bytes} bytes, " +
                                     $"over the {BudgetBytes} byte budget.");
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ReefSave] Could not write the reef: " + e.Message);
            }
        }

        public static ReefSave Read(string environmentId)
        {
            string path = PathFor(environmentId);
            if (!File.Exists(path)) return null;

            try
            {
                var save = JsonUtility.FromJson<ReefSave>(File.ReadAllText(path));
                if (save == null) return null;

                if (save.format != ReefSave.Format)
                {
                    Debug.Log($"[ReefSave] Ignoring '{environmentId}': written in format " +
                              $"{save.format}, this build reads {ReefSave.Format}.");
                    return null;
                }

                // The species order is the wire format for every index in the file.
                // A save written against a different roster would put one species'
                // numbers into another's pool.
                if (save.rosterVersion != SpeciesLibrary.RosterVersion)
                {
                    Debug.Log($"[ReefSave] Ignoring '{environmentId}': written against roster " +
                              $"version {save.rosterVersion}, this build uses " +
                              $"{SpeciesLibrary.RosterVersion}. The reef will start fresh.");
                    return null;
                }

                return save;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ReefSave] Could not read the reef: " + e.Message);
                return null;
            }
        }

        public static void Delete(string environmentId)
        {
            try
            {
                string path = PathFor(environmentId);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ReefSave] Could not delete the reef: " + e.Message);
            }
        }

        public static int SizeOf(string environmentId)
        {
            string path = PathFor(environmentId);
            return File.Exists(path) ? (int)new FileInfo(path).Length : 0;
        }
    }

    // How much time passed in the ocean while the learner was elsewhere.
    //
    // "An hour away is a day in your ocean, up to two weeks." That sentence is all a
    // learner ever needs to know about time (Design Document 8.1), and the cap is
    // what stops someone returning after a month to a reef that starved to death
    // without them.
    //
    // This reads wall-clock time from the save file's timestamp. It has nothing to do
    // with the in-scene clock, which counts Time.deltaTime while the app is running —
    // the two never interact, so away-time can never disturb a live session.
    public static class TimeAway
    {
        public const double RealHoursPerDay = 1.0;
        public const int MaximumDays = 14;

        // Under an hour away is not worth a card (Design Document 8.2).
        public const double MinimumHoursToReport = 1.0;

        public struct Result
        {
            public double hoursAway;
            public int daysToRun;      // after the cap
            public int uncappedDays;   // what it would have been
            public bool wasCapped;
            public bool worthReporting;
        }

        public static Result Measure(double hoursAway)
        {
            hoursAway = Math.Max(0.0, hoursAway);
            int uncapped = (int)Math.Floor(hoursAway / RealHoursPerDay);
            int capped = Mathf.Clamp(uncapped, 0, MaximumDays);

            return new Result
            {
                hoursAway = hoursAway,
                daysToRun = capped,
                uncappedDays = uncapped,
                wasCapped = uncapped > MaximumDays,
                worthReporting = hoursAway >= MinimumHoursToReport && capped > 0,
            };
        }
    }
}
