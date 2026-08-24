/*using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PackageManager : MonoBehaviour
{
    public static PackageManager Instance { get; private set; }

    private FirebaseFirestore firestore;

    private bool isInitialized;
    private bool isCatalogLoaded;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> InitializeAsync()
    {
        if (isInitialized)
            return true;

        Debug.Log("Initializing Package Manager...");

        var status = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"Firebase dependency issue: {status}");
            return false;
        }

        firestore = FirebaseFirestore.DefaultInstance;

        isInitialized = true;

        Debug.Log("Package Manager initialized.");

        return true;
    }

    public async Task<PackageMetadata> GetPackageMetadataAsync(string packageId)
    {
        if (!isInitialized)
        {
            Debug.LogError("PackageManager is not initialized.");
            return null;
        }

        var snapshot = await firestore
            .Collection("packages")
            .Document(packageId)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError($"Package '{packageId}' not found.");
            return null;
        }

        return new PackageMetadata
        {
            PackageId = snapshot.GetValue<string>("packageId"),
            PackageName = snapshot.GetValue<string>("packageName"),
            Mandatory = snapshot.GetValue<bool>("mandatory"),
            Version = snapshot.GetValue<long>("version"),
            CatalogUrl = snapshot.GetValue<string>("catalogUrl"),
            CatalogHashUrl = snapshot.GetValue<string>("catalogHashUrl"),
            SettingsUrl = snapshot.GetValue<string>("settingsUrl")
        };
    }

    public async Task<bool> LoadCatalogAsync(PackageMetadata package)
    {
        if (package == null)
        {
            Debug.LogError("Package metadata is null.");
            return false;
        }

        if (isCatalogLoaded)
        {
            Debug.Log("Content catalog already loaded.");
            return true;
        }

        Debug.Log($"Loading catalog: {package.CatalogUrl}");

        AsyncOperationHandle<IResourceLocator> handle =
            Addressables.LoadContentCatalogAsync(package.CatalogUrl);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Failed to load content catalog.");
            return false;
        }

        isCatalogLoaded = true;

        Debug.Log("Content catalog loaded successfully.");

        Addressables.Release(handle);

        return true;
    }

    public async Task<long> GetDownloadSizeAsync(object key)
    {
        var handle = Addressables.GetDownloadSizeAsync(key);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Failed to calculate download size.");
            return -1;
        }

        long size = handle.Result;

        Addressables.Release(handle);

        return size;
    }

    public async Task<bool> DownloadDependenciesAsync(
        object key,
        Action<float> onProgress = null)
    {
        var handle = Addressables.DownloadDependenciesAsync(key);

        while (!handle.IsDone)
        {
            var status = handle.GetDownloadStatus();

            if (status.TotalBytes > 0)
            {
                float progress =
                    (float)status.DownloadedBytes /
                    status.TotalBytes;

                onProgress?.Invoke(progress * 100f);
            }

            await Task.Delay(250);
        }

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Failed to download dependencies.");
            return false;
        }

        onProgress?.Invoke(100f);

        Addressables.Release(handle);

        return true;
    }
}*/

using System;
using System.Threading.Tasks;
using Firebase;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PackageManager : MonoBehaviour
{
    private static PackageManager _instance;

    public static PackageManager Instance
    {
        get
        {
            if (_instance == null)
            {
                // Try to find one already in the scene first.
                _instance = FindObjectOfType<PackageManager>();

                // If none exists anywhere, create one on the spot.
              
                if (_instance == null)
                {
                    var go = new GameObject("PackageManager");
                    _instance = go.AddComponent<PackageManager>();
                }
            }

            return _instance;
        }
    }

    private FirebaseFirestore firestore;

    private bool isInitialized;
    private bool isCatalogLoaded;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public async Task<bool> InitializeAsync()
    {
        if (isInitialized)
            return true;

        Debug.Log("Initializing Package Manager...");

        var status = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"Firebase dependency issue: {status}");
            return false;
        }

        firestore = FirebaseFirestore.DefaultInstance;

        isInitialized = true;

        Debug.Log("Package Manager initialized.");

        return true;
    }

    public async Task<PackageMetadata> GetPackageMetadataAsync(string packageId)
    {
        if (!isInitialized)
        {
            Debug.LogError("PackageManager is not initialized.");
            return null;
        }

        var snapshot = await firestore
            .Collection("packages")
            .Document(packageId)
            .GetSnapshotAsync();

        if (!snapshot.Exists)
        {
            Debug.LogError($"Package '{packageId}' not found.");
            return null;
        }

        return new PackageMetadata
        {
            PackageId = snapshot.GetValue<string>("packageId"),
            PackageName = snapshot.GetValue<string>("packageName"),
            Mandatory = snapshot.GetValue<bool>("mandatory"),
            Version = snapshot.GetValue<long>("version"),
            CatalogUrl = snapshot.GetValue<string>("catalogUrl"),
            CatalogHashUrl = snapshot.GetValue<string>("catalogHashUrl"),
            SettingsUrl = snapshot.GetValue<string>("settingsUrl")
        };
    }

    public async Task<bool> LoadCatalogAsync(PackageMetadata package)
    {
        if (package == null)
        {
            Debug.LogError("Package metadata is null.");
            return false;
        }

        if (isCatalogLoaded)
        {
            Debug.Log("Content catalog already loaded.");
            return true;
        }

        Debug.Log($"Loading catalog: {package.CatalogUrl}");

        AsyncOperationHandle<IResourceLocator> handle =
            Addressables.LoadContentCatalogAsync(package.CatalogUrl);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Failed to load content catalog.");
            return false;
        }

        isCatalogLoaded = true;

        Debug.Log("Content catalog loaded successfully.");

        Addressables.Release(handle);

        return true;
    }

    public async Task<long> GetDownloadSizeAsync(object key)
    {
        var handle = Addressables.GetDownloadSizeAsync(key);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Failed to calculate download size.");
            return -1;
        }

        long size = handle.Result;

        Addressables.Release(handle);

        return size;
    }

    public async Task<bool> DownloadDependenciesAsync(
        object key,
        Action<float> onProgress = null)
    {
        var handle = Addressables.DownloadDependenciesAsync(key);

        while (!handle.IsDone)
        {
            var status = handle.GetDownloadStatus();

            if (status.TotalBytes > 0)
            {
                float progress =
                    (float)status.DownloadedBytes /
                    status.TotalBytes;

                // Reports 0-100. StartupFlowValidation's loading bar
                // expects this range and normalizes it to 0-1 itself.
                onProgress?.Invoke(progress * 100f);
            }

            await Task.Delay(250);
        }

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("Failed to download dependencies.");
            return false;
        }

        onProgress?.Invoke(100f);

        Addressables.Release(handle);

        return true;
    }
}