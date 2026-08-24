/* using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public static class SceneLoaderBackend
{
    public static async Task<bool> LoadLocalSceneAsync(
        string sceneName,
        LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName, loadMode);

        if (operation == null)
        {
            Debug.LogError($"Failed to load local scene '{sceneName}'.");
            return false;
        }

        while (!operation.isDone)
            await Task.Yield();

        return true;
    }

    public static async Task<bool> LoadAddressableSceneAsync(
        string sceneName,
        LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        AsyncOperationHandle<SceneInstance> handle =
            Addressables.LoadSceneAsync(sceneName, loadMode);

        await handle.Task;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load Addressable scene '{sceneName}'.");
            return false;
        }

        return true;
    }
}*/
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

public static class SceneLoaderBackend
{
    // Central list — add a scene name here once, and every script
    // (BackNavigation, UIManager, anything else) picks it up automatically.
    private static readonly HashSet<string> AddressableScenes = new()
    {
        "AISpawnerScene",
        "FreeExploreConfig",
        "FreeExplore",
        "FreeExploreEndless",
        "CustomEnvBuilder",
    };

    // NEW — single entry point everything should call from now on
    public static Task<bool> LoadSceneAsync(string sceneName, LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        return AddressableScenes.Contains(sceneName)
            ? LoadAddressableSceneAsync(sceneName, loadMode)
            : LoadLocalSceneAsync(sceneName, loadMode);
    }

    public static async Task<bool> LoadLocalSceneAsync(
        string sceneName,
        LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName, loadMode);
        if (operation == null)
        {
            Debug.LogError($"Failed to load local scene '{sceneName}'.");
            return false;
        }
        while (!operation.isDone)
            await Task.Yield();
        return true;
    }

    public static async Task<bool> LoadAddressableSceneAsync(
        string sceneName,
        LoadSceneMode loadMode = LoadSceneMode.Single)
    {
        AsyncOperationHandle<SceneInstance> handle =
            Addressables.LoadSceneAsync(sceneName, loadMode);
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"Failed to load Addressable scene '{sceneName}'.");
            return false;
        }
        return true;
    }
}