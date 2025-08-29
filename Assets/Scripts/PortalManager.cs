using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct LayerSpawnConfig
{
    public string layerAddressableName;
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
}

public class PortalManager : MonoBehaviour
{
    public static PortalManager Instance;  // Singleton for easy access

    [Header("Component References")]
    [SerializeField] private Transform dynamicSpawnPoint; 
    // [SerializeField] private LayerLoadManager layerLoadManager;

    [Header("Layer Configuration")]
    [SerializeField] private List<LayerSpawnConfig> layerConfigs;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Public method that button will call
    public void RequestLayerLoad(string layerName)
    {
        // Find the config for the requested layer 
        LayerSpawnConfig? config = FindConfigForLayer(layerName);

        if (config.HasValue) {

            // Configure the dynamic spawn point with the correct transform.
            dynamicSpawnPoint.localPosition = config.Value.position;
            dynamicSpawnPoint.localEulerAngles = config.Value.rotation;
            dynamicSpawnPoint.localScale = config.Value.scale;

            // Tell the LayerLoadManager to load the layer at this configured point  
            LayerLoadManager.Instance.LoadLayer(config.Value.layerAddressableName, dynamicSpawnPoint);

            // Tell the GoalManager to start the tutorial for this layer.
            // FreeExpGoalManager.Instance.StartFreeExploreTutorial(config.Value.layerAddressableName);
        } 
        else
        {
            Debug.LogError("PortalManager: No configuration found for layer: " + layerName);

        }
    }

    private LayerSpawnConfig? FindConfigForLayer(string layerName)
    {
        foreach(var config in layerConfigs)
        {
            if (config.layerAddressableName == layerName)
            {
                return config;
            }
        }
        return null;
    }
}
