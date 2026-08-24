using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LayerCountUI : MonoBehaviour
{
    [Header("References")]
    public SandboxSettings settings;
    public LayerManager layerManager;
    public CreateModeManager createModeManager;

    [Header("UI")]
    public GameObject layerCountPanel;
    public TMP_InputField layerCountInput;
    public Button confirmButton;
    public TMP_Text warningText;

    void Start()
    {
        layerCountPanel.SetActive(true);

        layerCountInput.text = settings.layerCount.ToString();
        layerCountInput.contentType = TMP_InputField.ContentType.IntegerNumber;

        if (warningText != null)
            warningText.text = "";

        confirmButton.onClick.AddListener(OnConfirm);
    }

    void OnConfirm()
    {
        if (!int.TryParse(layerCountInput.text, out int count))
        {
            ShowWarning("Please enter a valid number.");
            return;
        }

        if (count < 3 || count > 5)
        {
            ShowWarning("Layer count must be between 3 - 5.");
            return;
        }

        settings.layerCount = count;
        Debug.Log($"Layer count confirmed: {count}");

        layerCountPanel.SetActive(false);

        // InitializeLayers MUST come before CreateEnvironmentAndGrids
        layerManager.InitializeLayers(count);
        createModeManager.CreateEnvironmentAndGrids();
    }

    void ShowWarning(string message)
    {
        if (warningText != null)
            warningText.text = message;
        Debug.LogWarning(message);
    }
}
