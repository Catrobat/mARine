using System;
using System.Threading;
using System.Threading.Tasks;
using MarineAR.AISpawner.Models;
using UnityEngine;
using UnityEngine.Networking;

namespace MarineAR.AISpawner.Services
{
    /// <summary>
    /// Downloads and parses the organisms manifest. The manifest is intentionally
    /// lightweight and is fetched fresh on every scene entry — never cached to disk —
    /// so organisms added to the GitHub repository appear without an app update.
    /// </summary>
    public sealed class ManifestService
    {
        const int k_TimeoutSeconds = 30;

        /// <summary>
        /// Fetches the manifest from <paramref name="manifestUrl"/> into memory and parses it.
        /// Throws on network failure or malformed payload.
        /// </summary>
        public async Task<Manifest> FetchManifestAsync(string manifestUrl, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(manifestUrl))
                throw new ArgumentException("Manifest URL is empty.", nameof(manifestUrl));

            using UnityWebRequest request = UnityWebRequest.Get(manifestUrl);
            request.timeout = k_TimeoutSeconds;

            UnityWebRequestAsyncOperation operation = request.SendWebRequest();
            while (!operation.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    request.Abort();
                    cancellationToken.ThrowIfCancellationRequested();
                }

                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
                throw new InvalidOperationException($"Manifest download failed ({request.responseCode}): {request.error}");

            Manifest manifest = Manifest.FromJson(request.downloadHandler.text);
            if (manifest == null)
                throw new InvalidOperationException("Manifest payload was empty or malformed.");

            Debug.Log($"[AISpawner] Manifest loaded: {manifest.items.Count} organisms (dataset '{manifest.dataset}', v{manifest.version}).");
            return manifest;
        }
    }
}
