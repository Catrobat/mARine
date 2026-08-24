using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;

namespace MarineAR.AISpawner.Services
{
    /// <summary>
    /// Remote configuration for the AI content feature, read from
    /// Firestore <c>aiContent/marine</c>. The manifest URL is never hardcoded —
    /// it always comes from this document.
    /// </summary>
    public sealed class AiContentConfig
    {
        public string ManifestUrl;
        public string Dataset;
        public long Version;
        public long Count;
        public bool Enabled;
    }

    /// <summary>
    /// Reads the AI content configuration from Firestore. Follows the same
    /// dependency-check + snapshot pattern as the existing essential-package flow
    /// (<c>StartupFlowValidation</c>) so both features behave identically on devices
    /// with broken Google Play services.
    /// </summary>
    public sealed class FirestoreService
    {
        const string k_Collection = "aiContent";
        const string k_Document = "marine";

        /// <summary>
        /// Fetches <c>aiContent/marine</c>. Throws on infrastructure failure
        /// (no Firebase, no network); returns a disabled config when the document
        /// is missing so the feature degrades gracefully instead of crashing.
        /// </summary>
        public async Task<AiContentConfig> FetchAiContentConfigAsync()
        {
            var status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
                throw new InvalidOperationException($"Firebase dependencies unavailable: {status}");

            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

            DocumentSnapshot snap = await db.Collection(k_Collection)
                                            .Document(k_Document)
                                            .GetSnapshotAsync();

            if (!snap.Exists)
            {
                Debug.LogWarning($"[AISpawner] Firestore document {k_Collection}/{k_Document} not found — AI content disabled.");
                return new AiContentConfig { Enabled = false };
            }

            var config = new AiContentConfig
            {
                ManifestUrl = GetString(snap, "manifestUrl"),
                Dataset = GetString(snap, "dataset"),
                Version = GetLong(snap, "version"),
                Count = GetLong(snap, "count"),
                Enabled = GetBool(snap, "enabled"),
            };

            if (config.Enabled && string.IsNullOrEmpty(config.ManifestUrl))
            {
                Debug.LogWarning("[AISpawner] aiContent/marine is enabled but has no manifestUrl — treating as disabled.");
                config.Enabled = false;
            }

            return config;
        }

        static string GetString(DocumentSnapshot snap, string field)
        {
            return snap.TryGetValue(field, out string value) ? value : null;
        }

        static long GetLong(DocumentSnapshot snap, string field)
        {
            return snap.TryGetValue(field, out long value) ? value : 0L;
        }

        static bool GetBool(DocumentSnapshot snap, string field)
        {
            return snap.TryGetValue(field, out bool value) && value;
        }
    }
}
