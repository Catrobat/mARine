using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MarineAR.AISpawner.Services
{
    /// <summary>
    /// Session-scoped LRU cache for downloaded organism files (GLB model + facts JSON),
    /// stored under <see cref="Application.temporaryCachePath"/>. Holds at most
    /// <see cref="k_Capacity"/> organisms: caching a third evicts the least recently
    /// used one and deletes its files. <see cref="ClearAll"/> wipes the entire cache
    /// directory and is called when the user exits the scene — cache lifetime is the
    /// current scene session only.
    /// </summary>
    public sealed class CacheService
    {
        /// <summary>Maximum number of organisms kept on disk simultaneously.</summary>
        const int k_Capacity = 2;

        const string k_RootFolder = "aispawner";

        public sealed class CachedOrganism
        {
            public string Id;
            public string ModelPath;
            public string FactsPath;
        }

        // Most recently used at the end of the list; count is tiny so a list is ideal.
        readonly List<CachedOrganism> m_Lru = new List<CachedOrganism>(k_Capacity + 1);

        readonly string m_Root;

        public CacheService()
        {
            m_Root = Path.Combine(Application.temporaryCachePath, k_RootFolder);
        }

        /// <summary>Absolute cache directory root.</summary>
        public string RootDirectory => m_Root;

        /// <summary>Builds the destination path for an organism's GLB file.</summary>
        public string GetModelPath(string organismId) =>
            Path.Combine(m_Root, "models", organismId + ".glb");

        /// <summary>Builds the destination path for an organism's facts file.</summary>
        public string GetFactsPath(string organismId) =>
            Path.Combine(m_Root, "facts", organismId + ".json");

        /// <summary>Whether the organism is fully cached (both files present).</summary>
        public bool Contains(string organismId)
        {
            CachedOrganism entry = Find(organismId);
            if (entry == null)
                return false;

            if (!File.Exists(entry.ModelPath))
            {
                // Disk state diverged (e.g. OS reclaimed temp storage) — drop the entry.
                Remove(organismId);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Returns the cached entry and marks it most-recently-used, or null when absent.
        /// </summary>
        public CachedOrganism Get(string organismId)
        {
            if (!Contains(organismId))
                return null;

            CachedOrganism entry = Find(organismId);
            m_Lru.Remove(entry);
            m_Lru.Add(entry); // touch — most recently used
            return entry;
        }

        /// <summary>
        /// Registers freshly downloaded files for an organism. Evicts (and deletes the
        /// files of) the least recently used organism when capacity is exceeded.
        /// </summary>
        public CachedOrganism Add(string organismId, string modelPath, string factsPath)
        {
            Remove(organismId); // replace any stale entry without double-tracking

            var entry = new CachedOrganism
            {
                Id = organismId,
                ModelPath = modelPath,
                FactsPath = factsPath,
            };
            m_Lru.Add(entry);

            while (m_Lru.Count > k_Capacity)
            {
                CachedOrganism evicted = m_Lru[0];
                m_Lru.RemoveAt(0);
                DeleteEntryFiles(evicted);
                Debug.Log($"[AISpawner] Cache evicted '{evicted.Id}' (LRU, capacity {k_Capacity}).");
            }

            return entry;
        }

        /// <summary>Removes a single organism from the cache and deletes its files.</summary>
        public void Remove(string organismId)
        {
            CachedOrganism entry = Find(organismId);
            if (entry == null)
                return;

            m_Lru.Remove(entry);
            DeleteEntryFiles(entry);
        }

        /// <summary>
        /// Deletes every cached model and facts file. Called on scene exit so a later
        /// visit starts from a clean slate and re-downloads on demand.
        /// </summary>
        public void ClearAll()
        {
            m_Lru.Clear();
            try
            {
                if (Directory.Exists(m_Root))
                    Directory.Delete(m_Root, recursive: true);
            }
            catch (IOException e)
            {
                Debug.LogWarning($"[AISpawner] Cache cleanup incomplete: {e.Message}");
            }
        }

        CachedOrganism Find(string organismId)
        {
            foreach (CachedOrganism entry in m_Lru)
            {
                if (entry.Id == organismId)
                    return entry;
            }

            return null;
        }

        static void DeleteEntryFiles(CachedOrganism entry)
        {
            DownloadService.DeleteQuietly(entry.ModelPath);
            DownloadService.DeleteQuietly(entry.FactsPath);
        }
    }
}
