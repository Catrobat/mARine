using UnityEngine;
using UnityEngine.Rendering;

public class UnderWaterDetector1 : MonoBehaviour
{
    private Transform arCamera; // AR camera
    [SerializeField] private Volume underWaterPostFX; // Underwater post-processing volume
    [SerializeField] private Volume globalPostFX; // Global post-processing volume
    [SerializeField] private AudioLowPassFilter lowPassFilter; // Audio low-pass filter
    [SerializeField] private float depthThreshold = 2f; // Depth threshold for applying effects

    private float _waterHeight = Mathf.NegativeInfinity;
    private OceanLayer _currentOceanLayer;

    private void Start()
    {
        // Dynamically find the AR camera
        arCamera = Camera.main.transform;
        if (!arCamera)
        {
            Debug.LogError("AR Camera not found! Ensure the main camera is tagged as 'MainCamera'.");
        }
    }

    public void UpdateWaterHeight(float height)
    {
        _waterHeight = height;
    }

    public void SetCurrentLayer(OceanLayer layer)
    {
        _currentOceanLayer = layer;
    }

    private void Update()
    {
        if (float.IsNegativeInfinity(_waterHeight) || _currentOceanLayer == null || !arCamera)
            return;

        bool isUnderwater = arCamera.position.y < _waterHeight;

        // Apply post-processing effects based on underwater state and depth
        if (underWaterPostFX && _currentOceanLayer.depthOffSet < depthThreshold)
        {
            underWaterPostFX.weight = isUnderwater ? 1f : 0f;
            if (globalPostFX)
                globalPostFX.weight = isUnderwater ? 0f : 1f;
        }
        else
        {
            underWaterPostFX.weight = 0f;
            if (globalPostFX)
                globalPostFX.weight = 1f;
        }

        // Apply audio low-pass filter
        if (lowPassFilter)
            lowPassFilter.enabled = isUnderwater;
    }
}