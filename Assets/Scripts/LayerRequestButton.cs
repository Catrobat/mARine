using UnityEngine;

// This script is very simple. Its only job is to tell the PortalManager what to load.
public class LayerRequestButton : MonoBehaviour
{
    // This public method will be called by the button's OnClick() event.
    public void RequestTargetLayer(string layerAddressableName)
    {
        // Check if the PortalManager instance exists before trying to use it.
        if (PortalManager.Instance != null)
        {
            // Use the static singleton instance to call the public method.
            PortalManager.Instance.RequestLayerLoad(layerAddressableName);
        }
        else
        {
            Debug.LogError("PortalManager instance not found! Make sure it's in your scene.");
        }
    }
}
