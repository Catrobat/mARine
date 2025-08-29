using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.EventSystems;
#endif

public class TemperatureController : MonoBehaviour
{
    [Header("Temperature (°C)")]
    [Range(0f, 40f)] public float temperature = 25f;

    [Header("pH Level")]
    [Range(7.5f, 8.4f)] public float pH = 8.1f;

    [Header("UI Controls")]
    public Slider temperatureSlider;
    public Text temperatureLabel;
    public Slider phSlider;
    public Text phLabel;

    [Header("Coral Materials")]
    [SerializeField] private List<Material> coralMaterials = new List<Material>();

    [Header("Directional Light")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Color lightColdColor = new Color(0.65f, 0.83f, 1f);
    [SerializeField] private Color lightNormalColor = new Color(1f, 0.95f, 0.85f);
    [SerializeField] private Color lightHotColor = new Color(1f, 0.8f, 0.6f);

    [Header("Fog Settings")]
    [SerializeField] private Color fogColdColor = new Color(0.30f, 0.66f, 0.62f);
    [SerializeField] private Color fogNormalColor = new Color(0.44f, 1f, 0.92f);
    [SerializeField] private Color fogHotColor = new Color(0.87f, 1f, 1f);
    [SerializeField] private float fogDensity = 0.02f;

    private Dictionary<Material, Color> materialBaseColors = new Dictionary<Material, Color>();

    public float BubbleStrength { get; private set; }

    private void OnEnable() => CacheOriginalColors();
    private void OnDisable() => RestoreOriginalColors();

    private void Start()
    {
        // Initialize slider values and listeners
        if (temperatureSlider != null)
        {
            temperatureSlider.minValue = 0f;
            temperatureSlider.maxValue = 40f;
            temperatureSlider.value = temperature;
            temperatureSlider.onValueChanged.AddListener(OnTemperatureSliderChanged);
        }

        if (phSlider != null)
        {
            phSlider.minValue = 7.5f;
            phSlider.maxValue = 8.4f;
            phSlider.value = pH;
            phSlider.onValueChanged.AddListener(OnPHSliderChanged);
        }

        UpdateLabels();
    }

    private void Update()
    {
        float effectiveTemp = GetEffectiveTemperature(temperature, pH);
        UpdateCoralColors(effectiveTemp);
        UpdateDirectionalLight(effectiveTemp);
        UpdateFog(effectiveTemp);
        BubbleStrength = GetBubbleStrength(temperature, pH);
    }

    private void OnTemperatureSliderChanged(float value)
    {
        temperature = value;
        UpdateLabels();
    }

    private void OnPHSliderChanged(float value)
    {
        pH = value;
        UpdateLabels();
    }

    private void UpdateLabels()
    {
        if (temperatureLabel != null)
            temperatureLabel.text = $"Temperature: {temperature:F1}°C";
        if (phLabel != null)
            phLabel.text = $"pH Level: {pH:F2}";
    }

    private void CacheOriginalColors()
    {
        materialBaseColors.Clear();
        foreach (Material mat in coralMaterials)
        {
            if (mat != null && mat.HasProperty("_Color"))
                materialBaseColors[mat] = mat.color;
        }
    }

    private void RestoreOriginalColors()
    {
        foreach (var kvp in materialBaseColors)
        {
            if (kvp.Key != null)
                kvp.Key.color = kvp.Value;
        }
    }

    private void UpdateCoralColors(float effectiveTemp)
    {
        foreach (Material mat in coralMaterials)
        {
            if (mat != null && mat.HasProperty("_Color"))
            {
                if (!materialBaseColors.ContainsKey(mat))
                    materialBaseColors[mat] = mat.color;

                Color normal = materialBaseColors[mat];
                Color cold = GenerateColdColor(normal);
                Color hot = GenerateHotColor(normal);

                Color targetColor = EvaluateTemperatureColor(effectiveTemp, cold, normal, hot);
                mat.color = targetColor;
            }
        }
    }

    private void UpdateDirectionalLight(float effectiveTemp)
    {
        if (directionalLight == null) return;
        Color targetLightColor = EvaluateTemperatureColor(effectiveTemp, lightColdColor, lightNormalColor, lightHotColor);
        directionalLight.color = targetLightColor;
    }

    private void UpdateFog(float effectiveTemp)
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = fogDensity;

        Color fogColor = EvaluateTemperatureColor(effectiveTemp, fogColdColor, fogNormalColor, fogHotColor);
        RenderSettings.fogColor = fogColor;
    }

    private Color EvaluateTemperatureColor(float temp, Color cold, Color normal, Color hot)
    {
        if (temp < 25f)
        {
            float t = Mathf.InverseLerp(0f, 25f, temp);
            return Color.Lerp(cold, normal, t);
        }
        else
        {
            float t = Mathf.InverseLerp(25f, 40f, temp);
            return Color.Lerp(normal, hot, t);
        }
    }

    private float GetEffectiveTemperature(float temp, float ph)
    {
        float deviation = ph - 8.1f;
        float amplification = deviation < 0f
            ? 1f + Mathf.Abs(deviation) * 2f
            : 1f - (deviation * 0.5f);

        float tempDeviation = temp - 25f;
        return 25f + tempDeviation * amplification;
    }

    private float GetBubbleStrength(float temp, float ph)
    {
        float tempFactor = Mathf.Exp(-Mathf.Pow((temp - 26f) / 6f, 2));
        float phFactor = Mathf.Exp(-Mathf.Pow((ph - 8.1f) / 0.25f, 2));
        float boosted = tempFactor * phFactor * 1.5f;
        return Mathf.Clamp01(boosted);
    }

    private Color GenerateColdColor(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        h = (h + 0.2f) % 1f;
        s = Mathf.Clamp01(s * 0.4f);
        v = Mathf.Clamp01(v * 0.5f);
        return Color.HSVToRGB(h, s, v);
    }

    private Color GenerateHotColor(Color baseColor)
    {
        return Color.Lerp(baseColor, Color.white, 0.8f);
    }

    public float GetModulatedBubbleStrength()
    {
        float noise = Mathf.PerlinNoise(Time.time * 0.5f, 0f);
        float baseStrength = GetBubbleStrength(temperature, pH);
        return Mathf.Clamp01(baseStrength * Mathf.Lerp(0.8f, 1.2f, noise));
    }
}
