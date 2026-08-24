using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LayerLoadManager : MonoBehaviour
{
    public static LayerLoadManager Instance;

    private GameObject currentLayerInstance;
    public GameObject GetCurrentLayerInstance() => currentLayerInstance;
    private string currentlyLoadingLayerName; // Variable to remember the layer name

    // Singleton Pattern
    private void Awake() 
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void LoadLayer(string addressableName, Transform parentTranform)
    {
        Debug.Log($"[LayerLoadManager] LoadLayer called");
        Debug.Log($"[LayerLoadManager] Address = {addressableName}");
        Debug.Log($"[LayerLoadManager] Parent = {(parentTranform != null ? parentTranform.name : "NULL")}");

        if (currentLayerInstance != null)
        {
            Debug.Log("[LayerLoadManager] Releasing previous layer");

            Addressables.ReleaseInstance(currentLayerInstance);
            Destroy(currentLayerInstance);
        }

        currentlyLoadingLayerName = addressableName;

        Debug.Log("[LayerLoadManager] Calling Addressables.InstantiateAsync");

        Addressables.InstantiateAsync(addressableName, parentTranform)
            .Completed += OnLayerLoaded;
    }

    private void OnLayerLoaded(AsyncOperationHandle<GameObject> obj)
    {
        Debug.Log("[LayerLoadManager] OnLayerLoaded() called");
        Debug.Log($"[LayerLoadManager] Status = {obj.Status}");

        if (obj.OperationException != null)
        {
            Debug.LogError("[LayerLoadManager] Exception:\n" + obj.OperationException);
        }

        if (obj.Status == AsyncOperationStatus.Succeeded)
        {
            currentLayerInstance = obj.Result;

            Debug.Log($"[LayerLoadManager] Loaded Object = {currentLayerInstance.name}");
            Debug.Log($"[LayerLoadManager] World Position = {currentLayerInstance.transform.position}");
            Debug.Log($"[LayerLoadManager] Local Position = {currentLayerInstance.transform.localPosition}");
            Debug.Log($"[LayerLoadManager] Child Count = {currentLayerInstance.transform.childCount}");
            Debug.Log($"[LayerLoadManager] Active Self = {currentLayerInstance.activeSelf}");
            Debug.Log($"[LayerLoadManager] Active InHierarchy = {currentLayerInstance.activeInHierarchy}");

            Renderer[] renderers = currentLayerInstance.GetComponentsInChildren<Renderer>(true);
            Debug.Log($"[LayerLoadManager] Renderer Count = {renderers.Length}");

            foreach (Renderer renderer in renderers)
            {
                Debug.Log($"[LayerLoadManager] Renderer: {renderer.name}, Enabled = {renderer.enabled}");
            }

            // Reset the local transform to fix placement
            currentLayerInstance.transform.localPosition = Vector3.zero;
            currentLayerInstance.transform.localRotation = Quaternion.identity;
            currentLayerInstance.transform.localScale = Vector3.one;

            Debug.Log($"[LayerLoadManager] After Reset World Position = {currentLayerInstance.transform.position}");
            Debug.Log($"[LayerLoadManager] After Reset Local Position = {currentLayerInstance.transform.localPosition}");

            // Now that the layer is confirmed to be loaded, start its tutorial.
            if (FreeExpGoalManager.Instance != null)
            {
                Debug.Log("[LayerLoadManager] Starting Free Explore Tutorial");
                FreeExpGoalManager.Instance.StartFreeExploreTutorial(currentlyLoadingLayerName);
            }

            /* /* // Dynamically find MarineBuddy within the newly loaded layer instance
            // and inform goalManager and set TTSManager
            MarineBuddy buddy = currentLayerInstance.GetComponentInChildren<MarineBuddy>(true);
            if (buddy != null)
            {
                if (FreeExpGoalManager.Instance != null)
                {
                    FreeExpGoalManager.Instance.SetMarineBuddy(buddy);
                    // Assuming globalTTS is a singleton or accessible via FreeExpGoalManager
                    // You might need to add a public getter for globalTTS in FreeExpGoalManager
                    // buddy.SetTTSManager(FreeExpGoalManager.Instance.GetGlobalTTS()); 
                }
                // If CrossPlatformTTS is a singleton, you can set it directly
                // if (CrossPlatformTTS.Instance != null)
                // {
                //     buddy.SetTTSManager(CrossPlatformTTS.Instance);
                // }
            }
            else
            {
                Debug.LogWarning("[LayerLoadManager] MarineBuddy not found in the loaded Addressable layer: " + currentLayerInstance.name);
            }

            // Inform MarineBuddy and FreeExpGoalManager that a new layer is loaded
            // These calls should trigger reconnection logic in those singletons
            //if (MarineBuddy.Instance != null) MarineBuddy.Instance.OnNewLayerLoaded();
            if (FreeExpGoalManager.Instance != null) FreeExpGoalManager.Instance.OnNewLayerLoaded(); */
        }
        else
        {
            Debug.LogError("[LayerLoadManager] Failed to load Addressable layer.");
        }
    }
}
