using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class LayerLoadManager : MonoBehaviour
{
    public static LayerLoadManager Instance;

    private GameObject currentLayerInstance;

    // Singleton Pattern
    private void Awake() 
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void LoadLayer(string addressableName, Transform parentTranform)
    {
        // Handle the dynamically loaded layer
        if (currentLayerInstance != null)
        {
            Addressables.ReleaseInstance(currentLayerInstance);
            Destroy(currentLayerInstance);
        }

        Addressables.InstantiateAsync(addressableName, parentTranform).Completed += OnLayerLoaded;
    }

    private void OnLayerLoaded(AsyncOperationHandle<GameObject> obj)
    {
        if (obj.Status == AsyncOperationStatus.Succeeded) {
            currentLayerInstance = obj.Result;

            // Reset the local transform to fix placement
            currentLayerInstance.transform.localPosition = Vector3.zero;
            currentLayerInstance.transform.localRotation = Quaternion.identity;
            currentLayerInstance.transform.localScale = Vector3.one;

            // Dynamically find MarineBuddy within the newly loaded layer instance
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
            if (MarineBuddy.Instance != null) MarineBuddy.Instance.OnNewLayerLoaded();
            if (FreeExpGoalManager.Instance != null) FreeExpGoalManager.Instance.OnNewLayerLoaded();
        }
    }
}
