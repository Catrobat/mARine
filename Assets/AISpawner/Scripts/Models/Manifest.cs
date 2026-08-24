using System;
using System.Collections.Generic;
using UnityEngine;

namespace MarineAR.AISpawner.Models
{
    /// <summary>
    /// DTO matching the remote <c>manifest.json</c> exactly. The manifest is the single
    /// runtime source of truth for available organisms. Its schema is a contract:
    /// do not rename fields — they map 1:1 onto the JSON keys via <see cref="JsonUtility"/>.
    /// </summary>
    [Serializable]
    public class Manifest
    {
        public string dataset;
        public int version;
        public int count;
        public string model_format;
        public List<MarineOrganism> items = new List<MarineOrganism>();

        /// <summary>
        /// Parses raw manifest JSON. Returns null when the payload is empty or
        /// structurally unusable (no items).
        /// </summary>
        public static Manifest FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;

            Manifest manifest;
            try
            {
                manifest = JsonUtility.FromJson<Manifest>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AISpawner] Manifest parse failure: {e.Message}");
                return null;
            }

            if (manifest?.items == null || manifest.items.Count == 0)
                return null;

            // Defensive pass: drop entries that are unusable at runtime.
            manifest.items.RemoveAll(item =>
                item == null ||
                string.IsNullOrEmpty(item.id) ||
                string.IsNullOrEmpty(item.model));

            return manifest.items.Count > 0 ? manifest : null;
        }
    }

    /// <summary>
    /// A single organism entry inside the manifest. Field names mirror the JSON schema.
    /// </summary>
    [Serializable]
    public class MarineOrganism
    {
        public string id;
        public string common_name;
        public string scientific_name;
        public string model;
        public string facts;
        public long model_bytes;

        /// <summary>Human readable download size, e.g. "3.5 MB".</summary>
        public string ModelSizeLabel =>
            model_bytes <= 0 ? string.Empty : $"{model_bytes / 1024f / 1024f:F1} MB";

        /// <summary>Display name with a safe fallback to the id slug.</summary>
        public string DisplayName =>
            string.IsNullOrEmpty(common_name) ? id : common_name;
    }
}
