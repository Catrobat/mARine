/* using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartupFlowValidation : MonoBehaviour
{
    private static bool warningShown = false;

    [Header("UI References")]
    public GameObject safetyModal;
    public Button continueButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    [Header("Content (optional override)")]
    [TextArea(3, 6)]
    public string titleDefault = "Safety Warning";

    [TextArea(4, 8)]
    public string bodyDefault =
        "• Parental supervision: This AR experience may be unsuitable for young children without adult supervision.\n\n" +
        "• Be aware of your surroundings: Use caution and watch for real-world hazards (stairs, traffic, obstacles) while using AR.";

    private bool showingDownloadScreen = false;
    private bool packageInstalled = false;

    private TextMeshProUGUI buttonLabel;

    void Start()
    {
        Debug.Log("StartupFlowValidation: Start()");

        if (continueButton != null)
            buttonLabel = continueButton.GetComponentInChildren<TextMeshProUGUI>();

        if (continueButton == null)
            Debug.LogError("StartupFlowValidation: continueButton is NULL.");

        if (buttonLabel == null)
            Debug.LogError("StartupFlowValidation: Button label (TextMeshProUGUI) is NULL.");

        if (titleText == null)
            Debug.LogError("StartupFlowValidation: titleText is NULL.");

        if (bodyText == null)
            Debug.LogError("StartupFlowValidation: bodyText is NULL.");

        if (titleText != null)
            titleText.text = titleDefault;

        if (bodyText != null)
            bodyText.text = bodyDefault;

        if (warningShown)
        {
            if (safetyModal != null)
                safetyModal.SetActive(false);

            return;
        }

        if (safetyModal != null)
            safetyModal.SetActive(true);

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnContinueClicked);
    }

    private void OnContinueClicked()
    {
        Debug.Log("StartupFlowValidation: Continue clicked.");

        if (!showingDownloadScreen)
        {
            ShowDownloadPrompt();
            return;
        }

        if (packageInstalled)
        {
            warningShown = true;

            if (safetyModal != null)
                safetyModal.SetActive(false);
        }
    }

    private void ShowDownloadPrompt()
    {
        Debug.Log("StartupFlowValidation: Showing download prompt.");

        showingDownloadScreen = true;

        titleText.text =
            "Essential Resources Required";

        bodyText.text =
            "Marine Biology AR requires essential resources before use.\n\n" +
            "This package contains:\n" +
            "• AR Experiences\n" +
            "• 3D Models\n" +
            "• Learning Content\n\n" +
            "The required resources will be downloaded after you continue.";

        if (buttonLabel != null)
            buttonLabel.text = "Download";

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(StartPackageDownload);
    }

    private async void StartPackageDownload()
    {
        Debug.Log("========== StartPackageDownload ==========");

        Debug.Log($"continueButton = {continueButton}");
        Debug.Log($"buttonLabel = {buttonLabel}");
        Debug.Log($"titleText = {titleText}");
        Debug.Log($"bodyText = {bodyText}");
        Debug.Log($"PackageManager.Instance = {PackageManager.Instance}");

        if (continueButton == null)
        {
            Debug.LogError("Continue Button reference is NULL.");
            return;
        }

        if (buttonLabel == null)
        {
            Debug.LogError("Button label is NULL.");
            return;
        }

        if (titleText == null)
        {
            Debug.LogError("Title Text reference is NULL.");
            return;
        }

        if (bodyText == null)
        {
            Debug.LogError("Body Text reference is NULL.");
            return;
        }

        if (PackageManager.Instance == null)
        {
            Debug.LogError("PackageManager.Instance is NULL.");
            return;
        }

        // FOR TESTING ONLY
        // Remove later
        // Caching.ClearCache();

        buttonLabel.text = "Continue";

        continueButton.interactable = false;

        titleText.text =
            "Downloading Resources";

        bodyText.text =
            "Preparing download...";

        await System.Threading.Tasks.Task.Yield();

        try
        {
            Debug.Log("STEP 1: Initialize PackageManager");

            if (!await PackageManager.Instance.InitializeAsync())
            {
                Debug.LogError("Package Manager initialization failed.");
                return;
            }

            Debug.Log("STEP 2: Fetch Package Metadata");

            PackageMetadata package =
                await PackageManager.Instance.GetPackageMetadataAsync(
                    PackageIds.Essential);

            Debug.Log($"Package Metadata = {package}");

            if (package == null)
            {
                Debug.LogError("Package metadata not found.");
                return;
            }

            Debug.Log($"Package Name = {package.PackageName}");
            Debug.Log($"Catalog URL = {package.CatalogUrl}");
            Debug.Log($"Settings URL = {package.SettingsUrl}");

            string packageName = package.PackageName;

            // Download all assets tagged with the EssentialPackage label.
            string addressableKey = PackageIds.EssentialDownloadKey;

            Debug.Log($"Addressable Key = {addressableKey}");

            Debug.Log("STEP 3: Load Catalog");

            if (!await PackageManager.Instance.LoadCatalogAsync(package))
            {
                Debug.LogError("Failed to load content catalog.");
                return;
            }

            Debug.Log("STEP 4: Get Download Size");

            long downloadSize =
                await PackageManager.Instance.GetDownloadSizeAsync(addressableKey);

            if (downloadSize < 0)
            {
                Debug.LogError("Failed to determine download size.");
                return;
            }

            Debug.Log($"Download Size: {downloadSize} bytes");
            Debug.Log($"Download Size: {downloadSize / 1024f / 1024f:F2} MB");

            bodyText.text =
                $"{packageName}\n\n" +
                $"Download Size: {downloadSize / 1024f / 1024f:F2} MB";

            Debug.Log("STEP 5: Download Dependencies");

            bool downloadSucceeded =
                await PackageManager.Instance.DownloadDependenciesAsync(
                    addressableKey,
                    progress =>
                    {
                        bodyText.text =
                            $"{packageName}\n\n" +
                            $"Downloading... {progress:F0}%";
                    });

            if (!downloadSucceeded)
            {
                throw new System.Exception("Dependency download failed.");
            }

            Debug.Log("STEP 6: Dependencies Downloaded");

            packageInstalled = true;

            titleText.text =
                "Resources Installed";

            bodyText.text =
                $"{packageName}\n\n" +
                "All required resources have been installed successfully.";

            continueButton.interactable = true;

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnContinueClicked);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("========== DOWNLOAD FAILED ==========");
            Debug.LogError(ex);

            titleText.text =
                "Download Failed";

            bodyText.text =
                "Unable to download required resources.\n\nPlease try again.";

            continueButton.interactable = true;

            if (buttonLabel != null)
                buttonLabel.text = "Download";

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(StartPackageDownload);
        }
    }
}*/
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;

public class StartupFlowValidation : MonoBehaviour
{
    private const string PrefKeyPackageInstalled = "EssentialPackageInstalled_v1";
    private const string PrefKeyPackageVersion = "EssentialPackageVersion_v1";

    [Header("UI References")]
    public GameObject safetyModal;
    public Button continueButton;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI bodyText;

    [Header("Loading Bar")]
    public GameObject loadingBarContainer;      // parent object grouping bar + % text (optional)
    public Slider loadingBarSlider;             // Slider, Min Value = 0, Max Value = 1
    public TextMeshProUGUI loadingPercentText;   // shows "0%" -> "100%"

    [Header("Content (optional override)")]
    [TextArea(3, 6)]
    public string titleDefault = "Safety Warning";

    [TextArea(4, 8)]
    public string bodyDefault =
        "• Parental supervision: This AR experience may be unsuitable for young children without adult supervision.\n\n" +
        "• Be aware of your surroundings: Use caution and watch for real-world hazards (stairs, traffic, obstacles) while using AR.";

    [Header("Checking Animation")]
    [Tooltip("How long the simulated fill takes to approach ~90% while the metadata check is in flight. It always snaps to 100% the instant the real check resolves, so this only affects perceived smoothness, not actual timing.")]
    public float checkingAnimDuration = 0.8f;

    [Header("Editor Testing")]
#if UNITY_EDITOR
    [Tooltip("EDITOR ONLY: forces the download flow to run every time you press Play, ignoring any saved 'already installed' state. Has no effect in builds. If you're stuck always seeing the download screen, check whether this is still enabled.")]
    public bool forceResetOnPlay = false;
#endif

    private bool packageInstalled = false;
    private long installedVersion = -1;

    private PackageMetadata cachedPackage;

    private TextMeshProUGUI buttonLabel;

    void Start()
    {
        if (continueButton != null)
            buttonLabel = continueButton.GetComponentInChildren<TextMeshProUGUI>();

        if (continueButton == null)
            Debug.LogError("StartupFlowValidation: continueButton is NULL.");

        if (buttonLabel == null)
            Debug.LogError("StartupFlowValidation: Button label (TextMeshProUGUI) is NULL.");

        if (titleText == null)
            Debug.LogError("StartupFlowValidation: titleText is NULL.");

        if (bodyText == null)
            Debug.LogError("StartupFlowValidation: bodyText is NULL.");

        if (titleText != null)
            titleText.text = titleDefault;

        if (bodyText != null)
            bodyText.text = bodyDefault;

        SetLoadingBar(false);

        cachedPackage = null;

#if UNITY_EDITOR
        if (forceResetOnPlay)
        {
            Debug.LogWarning("StartupFlowValidation: forceResetOnPlay is ON — clearing saved install state this run.");
            PlayerPrefs.DeleteKey(PrefKeyPackageInstalled);
            PlayerPrefs.DeleteKey(PrefKeyPackageVersion);
        }
#endif

        packageInstalled = PlayerPrefs.GetInt(PrefKeyPackageInstalled, 0) == 1;

        string storedVersionStr = PlayerPrefs.GetString(PrefKeyPackageVersion, "");
        if (!long.TryParse(storedVersionStr, out installedVersion))
            installedVersion = -1;

        Debug.Log($"StartupFlowValidation: Start() — packageInstalled={packageInstalled}, installedVersion={installedVersion}");

        if (safetyModal != null)
            safetyModal.SetActive(true);

        if (buttonLabel != null)
            buttonLabel.text = "Continue";

        continueButton.interactable = true;

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnSafetyAcknowledged);
    }

    private void SetLoadingBar(bool show, float progress01 = 0f)
    {
        if (loadingBarContainer != null)
            loadingBarContainer.SetActive(show);

        if (loadingBarSlider != null)
            loadingBarSlider.value = progress01; // Min Value 0 / Max Value 1 in Inspector

        if (loadingPercentText != null)
            loadingPercentText.text = Mathf.RoundToInt(progress01 * 100f) + "%";
    }

    private void CompleteFlow()
    {
        if (safetyModal != null)
            safetyModal.SetActive(false);
    }

    private void OnSafetyAcknowledged()
    {
        Debug.Log("StartupFlowValidation: Safety acknowledged, starting update check.");
        RunUpdateCheckAndProceed();
    }

    // Runs the whole checking -> (skip or download) sequence with no
    // required taps in between. The loading bar stays visible and
    // continuous across both phases.
    private async void RunUpdateCheckAndProceed()
    {
        if (PackageManager.Instance == null)
        {
            Debug.LogError("PackageManager.Instance is NULL.");
            ShowCheckFailed();
            return;
        }

        continueButton.interactable = false;
        if (buttonLabel != null)
            buttonLabel.text = "Continue";

        titleText.text = "Checking for Updates";
        bodyText.text = "Looking for the latest content...";

        SetLoadingBar(true, 0f);

        // Simulated fill while the real metadata check is in flight, since
        // a small Firestore lookup has no measurable byte progress. Caps
        // at 90% and never claims completion until the real check returns.
        bool checkFinished = false;
        var animTask = AnimateCheckingBar(() => checkFinished);

        try
        {
            if (!await PackageManager.Instance.InitializeAsync())
                throw new System.Exception("Package Manager initialization failed.");

            cachedPackage =
                await PackageManager.Instance.GetPackageMetadataAsync(PackageIds.Essential);

            if (cachedPackage == null)
                throw new System.Exception("Package metadata not found.");

            checkFinished = true;
            await animTask; // let the bar visibly reach 100% before continuing

            Debug.Log(
                $"StartupFlowValidation: Check complete. installedVersion={installedVersion}, " +
                $"serverVersion={cachedPackage.Version}, packageInstalled={packageInstalled}");

            bool alreadyLatest =
                packageInstalled && installedVersion == cachedPackage.Version;

            if (alreadyLatest)
            {
                Debug.Log("StartupFlowValidation: Already on latest version — closing panel.");
                SetLoadingBar(false);
                CompleteFlow();
            }
            else
            {
                Debug.Log("StartupFlowValidation: Update/download needed — proceeding automatically.");
                await StartPackageDownload();
            }
        }
        catch (System.Exception ex)
        {
            checkFinished = true;
            Debug.LogError("StartupFlowValidation: Update check failed — " + ex);
            ShowCheckFailed();
        }
    }

    private async Task AnimateCheckingBar(System.Func<bool> isDone)
    {
        float t = 0f;
        const float cap = 0.9f;

        while (!isDone() && t < checkingAnimDuration)
        {
            t += Time.deltaTime;
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / checkingAnimDuration), 2f);
            SetLoadingBar(true, eased * cap);
            await Task.Yield();
        }

        // Hold near the cap if the real check is still pending after the
        // animation window elapses, rather than sitting frozen.
        while (!isDone())
        {
            await Task.Yield();
        }

        SetLoadingBar(true, 1f);
        await Task.Delay(150);
    }

    private void ShowCheckFailed()
    {
        SetLoadingBar(false);

        titleText.text = "Unable to Check for Updates";
        bodyText.text = "Please check your connection and try again.";

        continueButton.interactable = true;

        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(OnSafetyAcknowledged);
    }

    private async Task StartPackageDownload()
    {
        Debug.Log("========== StartPackageDownload ==========");

        if (cachedPackage == null)
        {
            Debug.LogError("No cached package metadata — update check must run before download.");
            ShowCheckFailed();
            return;
        }

        titleText.text = "Downloading Resources";
        bodyText.text = "Preparing download...";

        SetLoadingBar(true, 0f);

        try
        {
            PackageMetadata package = cachedPackage;
            string packageName = package.PackageName;

            // Download all assets tagged with the EssentialPackage label.
            string addressableKey = PackageIds.EssentialDownloadKey;

            Debug.Log("STEP 1: Load Catalog");

            if (!await PackageManager.Instance.LoadCatalogAsync(package))
            {
                Debug.LogError("Failed to load content catalog.");
                throw new System.Exception("Failed to load content catalog.");
            }

            Debug.Log("STEP 2: Get Download Size");

            long downloadSize =
                await PackageManager.Instance.GetDownloadSizeAsync(addressableKey);

            if (downloadSize < 0)
            {
                Debug.LogError("Failed to determine download size.");
                throw new System.Exception("Failed to determine download size.");
            }

            Debug.Log($"Download Size: {downloadSize / 1024f / 1024f:F2} MB");

            bodyText.text = $"{packageName}";

            Debug.Log("STEP 3: Download Dependencies");

            bool downloadSucceeded =
                await PackageManager.Instance.DownloadDependenciesAsync(
                    addressableKey,
                    progress =>
                    {
                        // Reports 0-100.
                        SetLoadingBar(true, Mathf.Clamp01(progress / 100f));
                    });

            if (!downloadSucceeded)
            {
                throw new System.Exception("Dependency download failed.");
            }

            Debug.Log("STEP 4: Dependencies Downloaded");

            PlayerPrefs.SetInt(PrefKeyPackageInstalled, 1);
            PlayerPrefs.SetString(PrefKeyPackageVersion, package.Version.ToString());
            PlayerPrefs.Save();

            packageInstalled = true;
            installedVersion = package.Version;

            SetLoadingBar(false);

            titleText.text = "Resources Installed";

            bodyText.text =
                $"{packageName}\n\n" +
                "All required resources have been installed successfully.";

            continueButton.interactable = true;

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(CompleteFlow);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("========== DOWNLOAD FAILED ==========");
            Debug.LogError(ex);

            SetLoadingBar(false);

            titleText.text = "Download Failed";

            bodyText.text =
                "Unable to download required resources.\n\nPlease try again.";

            continueButton.interactable = true;

            if (buttonLabel != null)
                buttonLabel.text = "Retry";

            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(async () => await StartPackageDownload());
        }
    }
}