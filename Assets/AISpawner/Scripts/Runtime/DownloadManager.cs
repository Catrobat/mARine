using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MarineAR.AISpawner.Models;
using MarineAR.AISpawner.Repository;
using MarineAR.AISpawner.Services;
using UnityEngine;

namespace MarineAR.AISpawner.Runtime
{
    /// <summary>
    /// Orchestrates fetching one organism's payload (GLB + facts) through the
    /// LRU cache. Only one download may be in flight at a time — starting a new
    /// one cancels the previous, which matches the single-active-placement rule.
    /// Plain C# class; owned and injected by <see cref="AISpawnerContext"/>.
    /// </summary>
    public sealed class DownloadManager
    {
        /// <summary>User-facing message required for any failed organism download.</summary>
        public const string FailureMessage =
            "Our servers currently have limited compute capacity.\n" +
            "Please try downloading this organism again later.";

        readonly DownloadService m_DownloadService;
        readonly CacheService m_CacheService;
        readonly MarineRepository m_Repository;

        CancellationTokenSource m_ActiveCts;

        public DownloadManager(DownloadService downloadService, CacheService cacheService, MarineRepository repository)
        {
            m_DownloadService = downloadService ?? throw new ArgumentNullException(nameof(downloadService));
            m_CacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            m_Repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        public bool IsDownloading => m_ActiveCts != null;

        /// <summary>
        /// Resolves an organism to local file paths, downloading when not cached.
        /// Progress is dominated by the model transfer; facts are a few hundred bytes.
        /// Returns null when this request was superseded or cancelled.
        /// Throws <see cref="OrganismDownloadException"/> for any transfer failure.
        /// </summary>
        public async Task<CacheService.CachedOrganism> FetchOrganismAsync(
            MarineOrganism organism,
            Action<float> progress = null)
        {
            if (organism == null)
                throw new ArgumentNullException(nameof(organism));

            CancelActive();
            var cts = new CancellationTokenSource();
            m_ActiveCts = cts;

            try
            {
                // Cache hit: reuse and refresh LRU order — no network traffic at all.
                CacheService.CachedOrganism cached = m_CacheService.Get(organism.id);
                if (cached != null)
                {
                    EnsureFactsInMemory(organism.id, cached.FactsPath);
                    progress?.Invoke(1f);
                    return cached;
                }

                string modelPath = m_CacheService.GetModelPath(organism.id);
                string factsPath = m_CacheService.GetFactsPath(organism.id);

                try
                {
                    // Model first (the big transfer drives the progress bar) …
                    await m_DownloadService.DownloadFileAsync(
                        organism.model, modelPath,
                        p => progress?.Invoke(p * 0.97f),
                        cts.Token);

                    // … then the facts JSON, when the manifest provides one.
                    if (!string.IsNullOrEmpty(organism.facts))
                    {
                        await m_DownloadService.DownloadFileAsync(
                            organism.facts, factsPath, null, cts.Token);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Superseded by a newer selection or scene exit — files already cleaned up.
                    return null;
                }
                catch (Exception e)
                {
                    // 404, timeout, DNS failure, storage full … Never crash and never
                    // keep partial payloads: scrub both destinations.
                    DownloadService.DeleteQuietly(modelPath);
                    DownloadService.DeleteQuietly(factsPath);
                    Debug.LogWarning($"[AISpawner] Download failed for '{organism.id}': {e.Message}");
                    throw new OrganismDownloadException(FailureMessage, e);
                }

                CacheService.CachedOrganism entry = m_CacheService.Add(organism.id, modelPath, factsPath);
                EnsureFactsInMemory(organism.id, factsPath);
                progress?.Invoke(1f);
                return entry;
            }
            finally
            {
                if (m_ActiveCts == cts)
                    m_ActiveCts = null;
                cts.Dispose();
            }
        }

        /// <summary>Cancels the in-flight download, if any.</summary>
        public void CancelActive()
        {
            m_ActiveCts?.Cancel();
            m_ActiveCts = null;
        }

        void EnsureFactsInMemory(string organismId, string factsPath)
        {
            if (m_Repository.GetFacts(organismId) != null)
                return;

            try
            {
                if (!string.IsNullOrEmpty(factsPath) && File.Exists(factsPath))
                    m_Repository.StoreFacts(organismId, FactsDocument.Parse(File.ReadAllText(factsPath)));
            }
            catch (Exception e)
            {
                // Facts are auxiliary content; their absence must never block placement.
                Debug.LogWarning($"[AISpawner] Could not read facts for '{organismId}': {e.Message}");
            }
        }
    }

    /// <summary>Raised when an organism payload could not be downloaded.</summary>
    public sealed class OrganismDownloadException : Exception
    {
        public OrganismDownloadException(string message, Exception inner) : base(message, inner) { }
    }
}
