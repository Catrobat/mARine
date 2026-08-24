using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the animated water surface overlay rendered in screen space.
/// Uses a fullscreen UI RawImage with the FaceAR/WaterSurface shader.
/// The shader handles multi-layer sine wave animation for the water line.
/// </summary>
public class WaterSurfaceController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage waterImage;

    [Header("Water Settings")]
    [SerializeField] private Color waterColor = new Color(0f, 0.45f, 0.75f, 0.55f);
    [SerializeField] private Color foamColor = new Color(0.85f, 0.95f, 1f, 0.8f);
    [SerializeField] private float waveSpeed = 1.5f;
    [SerializeField] private float waveAmplitude = 0.025f;
    [SerializeField] private float waveFrequency = 4f;

    [Tooltip("Water line position (0 = bottom, 1 = top, 0.5 = middle)")]
    [SerializeField] private float waterLineY = 0.5f;
    [SerializeField] private float foamWidth = 0.015f;

    private Material _waterMaterial;
    private bool _isActive = true;

    // Shader property IDs (cached for performance)
    private static readonly int PropWaterColor = Shader.PropertyToID("_WaterColor");
    private static readonly int PropFoamColor = Shader.PropertyToID("_FoamColor");
    private static readonly int PropWaveAmplitude = Shader.PropertyToID("_WaveAmplitude");
    private static readonly int PropWaveFrequency = Shader.PropertyToID("_WaveFrequency");
    private static readonly int PropWaveSpeed = Shader.PropertyToID("_WaveSpeed");
    private static readonly int PropWaterLineY = Shader.PropertyToID("_WaterLineY");
    private static readonly int PropFoamWidth = Shader.PropertyToID("_FoamWidth");

    void Start()
    {
        if (waterImage != null && waterImage.material != null)
        {
            // Create material instance to avoid modifying the shared asset
            _waterMaterial = new Material(waterImage.material);
            waterImage.material = _waterMaterial;
            ApplyShaderProperties();
        }
    }

    /// <summary>Toggle the water effect on/off.</summary>
    public void ToggleWater()
    {
        SetActive(!_isActive);
    }

    /// <summary>Enable or disable the water surface (also enables parent canvas).</summary>
    public void SetActive(bool active)
    {
        _isActive = active;

        // Enable the parent canvas so child objects become visible
        if (waterImage != null)
        {
            // Walk up to find the Canvas root and activate it too
            var canvas = waterImage.GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.gameObject.SetActive(active);

            waterImage.gameObject.SetActive(active);
        }
    }

    /// <summary>Set the water line height (0-1 normalized screen height).</summary>
    public void SetWaterLine(float normalizedY)
    {
        waterLineY = Mathf.Clamp01(normalizedY);
        if (_waterMaterial != null)
            _waterMaterial.SetFloat(PropWaterLineY, waterLineY);
    }

    public bool IsActive => _isActive;

    private void ApplyShaderProperties()
    {
        if (_waterMaterial == null) return;
        _waterMaterial.SetColor(PropWaterColor, waterColor);
        _waterMaterial.SetColor(PropFoamColor, foamColor);
        _waterMaterial.SetFloat(PropWaveAmplitude, waveAmplitude);
        _waterMaterial.SetFloat(PropWaveFrequency, waveFrequency);
        _waterMaterial.SetFloat(PropWaveSpeed, waveSpeed);
        _waterMaterial.SetFloat(PropWaterLineY, waterLineY);
        _waterMaterial.SetFloat(PropFoamWidth, foamWidth);
    }

    void OnDestroy()
    {
        if (_waterMaterial != null)
            Destroy(_waterMaterial);
    }
}
