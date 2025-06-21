using UnityEngine;
using UnityEngine.Rendering;

public class UnderWaterDetector : MonoBehaviour
{
    [SerializeField] private Transform arCamera;
    [SerializeField] private Volume underWaterPostFX;
    [SerializeField] private AudioLowPassFilter lowPassFilter;
    [SerializeField] private Volume globalPostFX;

    private float _waterHeight = Mathf.NegativeInfinity;
    private OceanLayer _currentOceanLayer;

    public float waterHeight
    {
        get => _waterHeight;
        set => _waterHeight = value;
    }

    public void SetCurrentLayer(OceanLayer layer)
    {
        _currentOceanLayer = layer;
    }

    void Update()
    {
        if (float.IsNegativeInfinity(_waterHeight)) return;
        if (_currentOceanLayer == null) return;
        
        bool isUnderwater = arCamera.position.y < _waterHeight;

        if (underWaterPostFX && _currentOceanLayer.depthOffSet < 2)
        {
            underWaterPostFX.weight = isUnderwater ? 1f : 0f;
            
            if (globalPostFX)
                globalPostFX.weight = isUnderwater ? 0f : 1f;
        }
            

        if (lowPassFilter)
            lowPassFilter.enabled = isUnderwater;
    }
}