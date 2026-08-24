using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the underwater tint and caustic light pattern overlay.
/// Rendered as a screen-space UI element covering the area below the water line.
/// Uses the FaceAR/UnderwaterTint shader for animated caustics and god rays.
/// </summary>
public class UnderwaterOverlay : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage overlayImage;

    [Header("Underwater Settings")]
    [SerializeField] private Color tintColor = new Color(0f, 0.22f, 0.5f, 0.3f);
    [SerializeField] private float causticScale = 6f;
    [SerializeField] private float causticSpeed = 0.4f;
    [SerializeField] private float causticIntensity = 0.12f;
    [SerializeField] private float godRayIntensity = 0.08f;

    private Material _underwaterMaterial;

    private static readonly int PropTintColor = Shader.PropertyToID("_TintColor");
    private static readonly int PropCausticScale = Shader.PropertyToID("_CausticScale");
    private static readonly int PropCausticSpeed = Shader.PropertyToID("_CausticSpeed");
    private static readonly int PropCausticIntensity = Shader.PropertyToID("_CausticIntensity");
    private static readonly int PropGodRayIntensity = Shader.PropertyToID("_GodRayIntensity");

    void Start()
    {
        if (overlayImage != null && overlayImage.material != null)
        {
            _underwaterMaterial = new Material(overlayImage.material);
            overlayImage.material = _underwaterMaterial;
            ApplyShaderProperties();
        }
    }

    /// <summary>Set the underwater tint color.</summary>
    public void SetTintColor(Color color)
    {
        tintColor = color;
        if (_underwaterMaterial != null)
            _underwaterMaterial.SetColor(PropTintColor, tintColor);
    }

    /// <summary>Set the caustic light pattern intensity.</summary>
    public void SetCausticIntensity(float intensity)
    {
        causticIntensity = Mathf.Clamp(intensity, 0f, 1f);
        if (_underwaterMaterial != null)
            _underwaterMaterial.SetFloat(PropCausticIntensity, causticIntensity);
    }

    /// <summary>Show or hide the underwater overlay (also handles parent canvas).</summary>
    public void SetActive(bool active)
    {
        if (overlayImage != null)
        {
            var canvas = overlayImage.GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.gameObject.SetActive(active);
            overlayImage.gameObject.SetActive(active);
        }
    }

    private void ApplyShaderProperties()
    {
        if (_underwaterMaterial == null) return;
        _underwaterMaterial.SetColor(PropTintColor, tintColor);
        _underwaterMaterial.SetFloat(PropCausticScale, causticScale);
        _underwaterMaterial.SetFloat(PropCausticSpeed, causticSpeed);
        _underwaterMaterial.SetFloat(PropCausticIntensity, causticIntensity);
        _underwaterMaterial.SetFloat(PropGodRayIntensity, godRayIntensity);
    }

    void OnDestroy()
    {
        if (_underwaterMaterial != null)
            Destroy(_underwaterMaterial);
    }
}
