using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls all Face AR UI buttons. Layout matches the reference image:
/// - Top: "Back to World AR" button
/// - Center bottom: "STOP AR" toggle
/// - Row 1: Quiz, Snap, Audio, Light (colored circular buttons)
/// - Row 2: Reset, Zoom-, Zoom+, Water (dark circular buttons)
/// - Right panel: Mask selector thumbnails
/// </summary>
public class FaceARUIController : MonoBehaviour
{
    [Header("Manager References")]
    [SerializeField] private FaceARManager faceManager;
    [SerializeField] private WaterSurfaceController waterController;
    [SerializeField] private UnderwaterOverlay underwaterOverlay;
    [SerializeField] private BubbleSpawner bubbleSpawner;
    [SerializeField] private ScreenshotCapture screenshotCapture;
    [SerializeField] private MaskSwitcher maskSwitcher;

    [Header("Navigation")]
    [SerializeField] private string worldARSceneName = "StartScene";

    [Header("Buttons")]
    [SerializeField] private Button backButton;
    [SerializeField] private Button stopARButton;
    [SerializeField] private Button quizButton;
    [SerializeField] private Button snapButton;
    [SerializeField] private Button audioButton;
    [SerializeField] private Button lightButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button zoomOutButton;
    [SerializeField] private Button zoomInButton;
    [SerializeField] private Button waterButton;

    [Header("Labels")]
    [SerializeField] private Text stopARLabel;

    [Header("Zoom Settings")]
    [SerializeField] private Camera arCamera;
    [SerializeField] private float zoomStep = 5f;
    [SerializeField] private float minFOV = 20f;
    [SerializeField] private float maxFOV = 80f;

    [Header("Light")]
    [SerializeField] private Light sceneLight;
    [SerializeField] private float lightOnIntensity = 1f;
    [SerializeField] private float lightOffIntensity = 0.2f;

    // UI state
    private bool _isARActive = true;
    private bool _isLightOn = true;
    private bool _isAudioOn = true;

    void Start()
    {
        WireButtons();
    }

    private void WireButtons()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackToWorldAR);
        if (stopARButton != null) stopARButton.onClick.AddListener(OnToggleAR);
        if (quizButton != null) quizButton.onClick.AddListener(OnQuiz);
        if (snapButton != null) snapButton.onClick.AddListener(OnSnap);
        if (audioButton != null) audioButton.onClick.AddListener(OnToggleAudio);
        if (lightButton != null) lightButton.onClick.AddListener(OnToggleLight);
        if (resetButton != null) resetButton.onClick.AddListener(OnReset);
        if (zoomOutButton != null) zoomOutButton.onClick.AddListener(OnZoomOut);
        if (zoomInButton != null) zoomInButton.onClick.AddListener(OnZoomIn);
        if (waterButton != null) waterButton.onClick.AddListener(OnToggleWater);
    }

    // ─── Button Handlers ──────────────────────────────────────

    private void OnBackToWorldAR()
    {
        Debug.Log("[FaceAR] Navigating back to World AR");
        if (faceManager != null)
            faceManager.StopAR();

        if (Application.CanStreamedLevelBeLoaded(worldARSceneName))
            SceneManager.LoadScene(worldARSceneName);
        else
            Debug.LogWarning($"[FaceAR] Scene '{worldARSceneName}' not found in build settings");
    }

    private void OnToggleAR()
    {
        _isARActive = !_isARActive;

        if (faceManager != null)
        {
            if (_isARActive) faceManager.StartAR();
            else faceManager.StopAR();
        }

        if (stopARLabel != null)
            stopARLabel.text = _isARActive ? "STOP AR" : "START AR";
    }

    private void OnQuiz()
    {
        Debug.Log("[FaceAR] Quiz button pressed — placeholder for quiz module");
        // TODO: Connect to quiz system when integrating with Marine_Biology_AR_Application
    }

    private void OnSnap()
    {
        if (screenshotCapture != null)
            screenshotCapture.CaptureScreenshot();
        else
            Debug.LogWarning("[FaceAR] ScreenshotCapture not assigned");
    }

    private void OnToggleAudio()
    {
        _isAudioOn = !_isAudioOn;
        AudioListener.volume = _isAudioOn ? 1f : 0f;
        Debug.Log($"[FaceAR] Audio {(_isAudioOn ? "ON" : "OFF")}");
    }

    private void OnToggleLight()
    {
        _isLightOn = !_isLightOn;
        if (sceneLight != null)
            sceneLight.intensity = _isLightOn ? lightOnIntensity : lightOffIntensity;
        Debug.Log($"[FaceAR] Light {(_isLightOn ? "ON" : "OFF")}");
    }

    private void OnReset()
    {
        if (faceManager != null)
            faceManager.ResetAR();

        // Reset zoom
        if (arCamera != null)
            arCamera.fieldOfView = 60f;

        // Re-enable water
        if (waterController != null)
            waterController.SetActive(true);
        if (underwaterOverlay != null)
            underwaterOverlay.SetActive(true);
        if (bubbleSpawner != null)
            bubbleSpawner.SetActive(true);

        _isARActive = true;
        _isLightOn = true;
        _isAudioOn = true;

        if (stopARLabel != null) stopARLabel.text = "STOP AR";
        if (sceneLight != null) sceneLight.intensity = lightOnIntensity;
        AudioListener.volume = 1f;

        Debug.Log("[FaceAR] Everything reset");
    }

    private void OnZoomIn()
    {
        if (arCamera != null)
            arCamera.fieldOfView = Mathf.Max(arCamera.fieldOfView - zoomStep, minFOV);
    }

    private void OnZoomOut()
    {
        if (arCamera != null)
            arCamera.fieldOfView = Mathf.Min(arCamera.fieldOfView + zoomStep, maxFOV);
    }

    private void OnToggleWater()
    {
        bool newState = waterController != null && !waterController.IsActive;

        if (waterController != null) waterController.SetActive(newState);
        if (underwaterOverlay != null) underwaterOverlay.SetActive(newState);
        if (bubbleSpawner != null) bubbleSpawner.SetActive(newState);

        Debug.Log($"[FaceAR] Water {(newState ? "ON" : "OFF")}");
    }
}
