using System.Collections;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class AuthLoadingController : MonoBehaviour
{
    [Header("Existing UI Manager (just drag the one already in your scene)")]
    public AuthUIManager authUIManager;

    [Header("New Loading UI (create once, drag in)")]
    public GameObject loadingPanel;
    public Slider loadingBar;          // Min 0, Max 1
    public TextMeshProUGUI loadingText; // optional, shows "0%" -> "100%"

    [Header("Safety Timeout")]
    [Tooltip("If Firebase hasn't responded within this many seconds, stop waiting and treat as logged out.")]
    public float maxWaitSeconds = 8f;

    private bool authCheckComplete = false;
    private bool userIsLoggedIn = false;

    private void Awake()
    {
        if (authUIManager != null)
        {
            if (authUIManager.landingPanel != null) authUIManager.landingPanel.SetActive(false);
            if (authUIManager.loginPanel != null) authUIManager.loginPanel.SetActive(false);
            if (authUIManager.registerPanel != null) authUIManager.registerPanel.SetActive(false);
            if (authUIManager.forgotPasswordPanel != null) authUIManager.forgotPasswordPanel.SetActive(false);
        }

        if (loadingPanel != null) loadingPanel.SetActive(true);
        if (loadingBar != null) loadingBar.value = 0f;
        if (loadingText != null) loadingText.text = "0%";
    }

    private void Start()
    {
        StartCoroutine(LoadingBarRoutine());
        StartCoroutine(TimeoutWatchdog());

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            try
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Firebase dependency check faulted: " + task.Exception);
                    userIsLoggedIn = false;
                    return;
                }

                if (task.IsCanceled)
                {
                    Debug.LogWarning("Firebase dependency check was canceled.");
                    userIsLoggedIn = false;
                    return;
                }

                if (task.Result == DependencyStatus.Available)
                {
                    userIsLoggedIn = FirebaseAuth.DefaultInstance.CurrentUser != null;
                }
                else
                {
                    Debug.LogError("Firebase dependency status: " + task.Result);
                    userIsLoggedIn = false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Exception during Firebase dependency check: " + e);
                userIsLoggedIn = false;
            }
            finally
            {
                // This is the key fix - guaranteed to run no matter what happened above.
                authCheckComplete = true;
            }
        });
    }

    /// <summary>
    /// Hard safety net. If Firebase never calls back at all (e.g. task hangs,
    /// no internet, device-specific edge case), this forces the app to move on
    /// after maxWaitSeconds instead of getting stuck on the loading screen forever.
    /// </summary>
    private IEnumerator TimeoutWatchdog()
    {
        yield return new WaitForSeconds(maxWaitSeconds);

        if (!authCheckComplete)
        {
            Debug.LogWarning($"Firebase auth check did not complete within {maxWaitSeconds}s - proceeding as logged out.");
            userIsLoggedIn = false;
            authCheckComplete = true;
        }
    }

    private IEnumerator LoadingBarRoutine()
    {
        float fakeProgress = 0f;
        float fillSpeed = 0.6f;

        while (!authCheckComplete && fakeProgress < 0.9f)
        {
            fakeProgress += Time.deltaTime * fillSpeed;
            fakeProgress = Mathf.Min(fakeProgress, 0.9f);
            SetBar(fakeProgress);
            yield return null;
        }

        while (!authCheckComplete)
            yield return null;

        float t = 0f;
        float start = loadingBar != null ? loadingBar.value : fakeProgress;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            SetBar(Mathf.Lerp(start, 1f, t));
            yield return null;
        }
        SetBar(1f);

        yield return new WaitForSeconds(0.2f);

        if (userIsLoggedIn)
        {
            SceneManager.LoadScene("StartScene");
        }
        else
        {
            if (loadingPanel != null) loadingPanel.SetActive(false);
            if (authUIManager != null && authUIManager.landingPanel != null)
                authUIManager.landingPanel.SetActive(true);
        }
    }

    private void SetBar(float value)
    {
        if (loadingBar != null) loadingBar.value = value;
        if (loadingText != null) loadingText.text = Mathf.RoundToInt(value * 100f) + "%";
    }
}