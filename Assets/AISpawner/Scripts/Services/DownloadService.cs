using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace MarineAR.AISpawner.Services
{
    /// <summary>
    /// Streams remote files to disk with progress reporting and guaranteed cleanup of
    /// partial downloads. Files are first written to a <c>.part</c> path and only moved
    /// to their final path once the transfer completed successfully, so a failed or
    /// cancelled download can never leave a corrupt file that later gets mistaken
    /// for a valid cached model.
    /// </summary>
    public sealed class DownloadService
    {
        const int k_TimeoutSeconds = 180;
        const string k_PartialSuffix = ".part";

        /// <summary>
        /// Downloads <paramref name="url"/> to <paramref name="destinationPath"/>.
        /// <paramref name="progress"/> receives values in [0, 1].
        /// Throws <see cref="OperationCanceledException"/> on cancellation and
        /// <see cref="InvalidOperationException"/> on transfer failure — in both cases
        /// no partial file remains on disk.
        /// </summary>
        public async Task DownloadFileAsync(
            string url,
            string destinationPath,
            Action<float> progress = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Download URL is empty.", nameof(url));
            if (string.IsNullOrEmpty(destinationPath))
                throw new ArgumentException("Destination path is empty.", nameof(destinationPath));

            string directory = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string partialPath = destinationPath + k_PartialSuffix;
            DeleteQuietly(partialPath);

            using UnityWebRequest request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbGET);
            request.downloadHandler = new DownloadHandlerFile(partialPath) { removeFileOnAbort = true };
            request.timeout = k_TimeoutSeconds;

            try
            {
                UnityWebRequestAsyncOperation operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        request.Abort();
                        // Let the abort settle so the file handle is released before cleanup.
                        while (!operation.isDone)
                            await Task.Yield();

                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    progress?.Invoke(request.downloadProgress);
                    await Task.Yield();
                }

                if (request.result != UnityWebRequest.Result.Success)
                    throw new InvalidOperationException($"Download failed ({request.responseCode}): {request.error} [{url}]");

                progress?.Invoke(1f);

                DeleteQuietly(destinationPath);
                File.Move(partialPath, destinationPath);
            }
            finally
            {
                // Success moved the file away; on any failure path remove the partial.
                DeleteQuietly(partialPath);
            }
        }

        /// <summary>Deletes a file, ignoring the error when it does not exist or is locked.</summary>
        public static void DeleteQuietly(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
                // A locked temp file is not worth crashing over; it lives in
                // temporaryCachePath and the OS reclaims it eventually.
            }
        }
    }
}
