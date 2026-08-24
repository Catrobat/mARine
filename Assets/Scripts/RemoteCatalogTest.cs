using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement;

public class RemoteCatalogTest : MonoBehaviour
{
    async void Start()
    {
        Caching.ClearCache();

        ResourceManager.ExceptionHandler = (op, ex) =>
        {
            Debug.LogError("Addressables Exception: " + ex);
        };

        string catalogUrl =
            "https://raw.githubusercontent.com/joshi-p/marine-content/main/Android/catalog.bin";

        Debug.Log("Loading catalog...");

        var catalogHandle =
            Addressables.LoadContentCatalogAsync(catalogUrl);

        await catalogHandle.Task;

        Debug.Log("Catalog Loaded");

        var locationsHandle =
            Addressables.LoadResourceLocationsAsync("AR_Spawn");

        await locationsHandle.Task;

        Debug.Log("Locations Found: " + locationsHandle.Result.Count);

        foreach (IResourceLocation loc in locationsHandle.Result)
        {
            Debug.Log("Location: " + loc.InternalId);
        }

        // Check how much Addressables thinks it needs to download
        var sizeHandle =
            Addressables.GetDownloadSizeAsync("AR_Spawn");

        await sizeHandle.Task;

        Debug.Log("Download Size: " +
                  sizeHandle.Result +
                  " bytes");

        // Force dependency download
        Debug.Log("Downloading Dependencies...");

        var downloadHandle =
            Addressables.DownloadDependenciesAsync("AR_Spawn");

        while (!downloadHandle.IsDone)
        {
            var status = downloadHandle.GetDownloadStatus();

            if (status.TotalBytes > 0)
            {
                float progress =
                    (float)status.DownloadedBytes /
                    status.TotalBytes;

                Debug.Log(
                    $"Downloaded {status.DownloadedBytes / 1024f / 1024f:F2} MB / " +
                    $"{status.TotalBytes / 1024f / 1024f:F2} MB " +
                    $"({progress * 100f:F1}%)"
                );
            }
            else
            {
                Debug.Log("Preparing download...");
            }

            await System.Threading.Tasks.Task.Delay(500);
        }

        Debug.Log("Dependencies Downloaded");
        Debug.Log("Download Status: " +
                  downloadHandle.Status);

        if (downloadHandle.OperationException != null)
        {
            Debug.LogError(downloadHandle.OperationException);
        }

        Debug.Log("Loading Scene...");

        var sceneHandle =
            Addressables.LoadSceneAsync("AR_Spawn");

        await sceneHandle.Task;

        Debug.Log("Scene Loaded");
    }
}