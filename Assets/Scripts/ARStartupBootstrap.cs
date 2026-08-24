using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>
/// Ensures XR Management has initialized and started an AR loader before ARFoundation managers run.
/// This protects Android builds from stale XR Plug-in Management settings.
/// </summary>
public static class ARStartupBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureXRStarted()
    {
#if !UNITY_EDITOR
        var settings = XRGeneralSettings.Instance;
        var manager = settings != null ? settings.Manager : null;

        if (manager == null)
        {
            Debug.LogWarning("[FaceAR] XR Manager is not available. Check XR Plug-in Management settings.");
            return;
        }

        if (manager.activeLoader == null)
        {
            manager.InitializeLoaderSync();
        }

        if (manager.activeLoader != null)
        {
            manager.StartSubsystems();
            Debug.Log($"[FaceAR] XR loader active: {manager.activeLoader.name}");
        }
        else
        {
            Debug.LogWarning("[FaceAR] No XR loader became active. ARCore may be missing or unsupported on this device.");
        }
#endif
    }
}
