using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/*
[System.Serializable]
public class OceanLayer
{
    public string name;
    public float depthOffset; // Vertical offset for each layer
    public VolumeProfile volumeProfile;
}
*/

public class OceanInPortalDepthController : MonoBehaviour
{
    private Transform highPolyPlane; // The water plane (HighPolyPlane)
    private Transform portal; // The portal (Potal_With_water)
    [SerializeField] private List<OceanLayer> oceanLayers; // List of ocean layers
    [SerializeField] private Volume postProcessVolume; // Post-processing volume (optional, can be set at runtime)
    [SerializeField] private TextMeshProUGUI textMeshProUGUI; // UI for layer name display (optional, can be set at runtime)

    private int _currentLayerIndex = 0;
    private UnderWaterDetector1 _underWaterDetector;
    private bool _isTransitioning = false;
    private Vector3 _initialPlaneLocalPos; // Initial local position of the water plane relative to the portal

    // Public setters for optional components that might be assigned at runtime
    public void SetPostProcessVolume(Volume volume)
    {
        postProcessVolume = volume;
    }

    public void SetTextMeshProUGUI(TextMeshProUGUI text)
    {
        textMeshProUGUI = text;
    }

    // Call this method after spawning the prefab to initialize references
    public void Initialize()
    {
        // Find the UnderWaterDetector in the scene
        _underWaterDetector = FindObjectOfType<UnderWaterDetector1>();
        if (!_underWaterDetector)
            Debug.LogWarning("UnderWaterDetector not found in the scene!");

        // Set the portal to this GameObject (Potal_With_water)
        portal = transform;

        // Find the HighPolyPlane as a child of the portal (search recursively)
        highPolyPlane = FindChildRecursive(transform, "HighPolyPlane");
        if (!highPolyPlane)
        {
            Debug.LogError($"HighPolyPlane not found in the hierarchy of {gameObject.name}! Ensure a child GameObject named 'HighPolyPlane' exists.");
            return;
        }

        // Store the initial local position of the water plane relative to the portal
        _initialPlaneLocalPos = portal.InverseTransformPoint(highPolyPlane.position);

        // Apply the first ocean layer if available
        if (oceanLayers.Count > 0)
            ApplyLayer(oceanLayers[_currentLayerIndex]);
        else
            Debug.LogWarning("No ocean layers defined in OceanInPortalDepthController!");
    }

    // Helper method to search for a child GameObject by name recursively
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform found = FindChildRecursive(child, childName);
            if (found != null)
                return found;
        }
        return null;
    }

    private void ApplyLayer(OceanLayer layer)
    {
        // Apply post-processing volume if available
        if (postProcessVolume && layer.volumeProfile)
        {
            postProcessVolume.profile = layer.volumeProfile;
            postProcessVolume.weight = 1f;
        }

        // Update the underwater detector with the current layer
        if (_underWaterDetector)
            _underWaterDetector.SetCurrentLayer(layer);

        // Update UI text if available
        if (textMeshProUGUI)
            textMeshProUGUI.text = $"{layer.name} Layer";

        // Update the water plane's position based on the layer's depth offset
        if (highPolyPlane != null)
        {
            Vector3 targetLocalPos = _initialPlaneLocalPos + Vector3.up * layer.depthOffSet;
            highPolyPlane.position = portal.TransformPoint(targetLocalPos);

            // Update the water height in the underwater detector
            if (_underWaterDetector)
                _underWaterDetector.UpdateWaterHeight(highPolyPlane.position.y);
        }
    }

    public void GoDeeper()
    {
        if (_isTransitioning || _currentLayerIndex >= oceanLayers.Count - 1) return;
        StartCoroutine(TransitionToLayer(_currentLayerIndex + 1));
        _currentLayerIndex++;
    }

    public void GoUpper()
    {
        if (_isTransitioning || _currentLayerIndex <= 0) return;
        StartCoroutine(TransitionToLayer(_currentLayerIndex - 1));
        _currentLayerIndex--;
    }

    private IEnumerator TransitionToLayer(int targetIndex)
    {
        if (highPolyPlane == null || portal == null)
        {
            Debug.LogError("Cannot transition: highPolyPlane or portal is null. Ensure Initialize() was called and HighPolyPlane exists.");
            yield break;
        }

        _isTransitioning = true;
        OceanLayer targetLayer = oceanLayers[targetIndex];

        // Calculate the start and target positions in world space
        Vector3 startWorldPos = highPolyPlane.position;
        Vector3 targetLocalPos = _initialPlaneLocalPos + Vector3.up * targetLayer.depthOffSet;
        Vector3 targetWorldPos = portal.TransformPoint(targetLocalPos);

        float duration = 1f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float smoothStep = Mathf.SmoothStep(0f, 1f, t);
            highPolyPlane.position = Vector3.Lerp(startWorldPos, targetWorldPos, smoothStep);

            // Update the water height in the underwater detector during transition
            if (_underWaterDetector)
                _underWaterDetector.UpdateWaterHeight(highPolyPlane.position.y);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Ensure the final position is set
        highPolyPlane.position = targetWorldPos;

        // Apply the new layer
        ApplyLayer(targetLayer);
        _isTransitioning = false;
    }
}
