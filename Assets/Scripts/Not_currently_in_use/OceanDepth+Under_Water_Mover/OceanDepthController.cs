using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

[System.Serializable]
public class OceanLayer
{
    public string name;
    public float depthOffSet;
    public VolumeProfile volumeProfile;
}

public class OceanDepthController : MonoBehaviour
{
    public Volume postProcessVolume;
    public List<OceanLayer> oceanLayers;
    public TextMeshProUGUI textMeshProUGUI;

    private int _currentLayerIndex = 0;
    private UnderWaterDetector _underWaterDetector;

    private void Start()
    {
        _underWaterDetector = FindAnyObjectByType<UnderWaterDetector>();
        if (!_underWaterDetector)
            Debug.LogWarning("UnderWaterDetector not found in the scene!");

        if (oceanLayers.Count > 0)
            ApplyLayer(oceanLayers[_currentLayerIndex]);
    }

    public void ApplyLayer(OceanLayer layer)
    {
        if (postProcessVolume)
        {
            postProcessVolume.profile = layer.volumeProfile;
            postProcessVolume.weight = 1f;
        }

        if (_underWaterDetector)
            _underWaterDetector.SetCurrentLayer(layer);

        if (textMeshProUGUI)
            textMeshProUGUI.text = $"{layer.name} layer";
    }

    public void GoDeeper()
    {
        if (_currentLayerIndex >= oceanLayers.Count - 1) return;
        _currentLayerIndex++;
        ApplyLayer(oceanLayers[_currentLayerIndex]);
    }

    public void GoUpper()
    {
        if (_currentLayerIndex <= 0) return;
        _currentLayerIndex--;
        ApplyLayer(oceanLayers[_currentLayerIndex]);
    }
}