using UnityEngine;
using UnityEngine.Events;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

/// <summary>
/// Requests camera permission on Android at scene start.
/// Shows/hides UI based on permission result.
/// Pattern matches Marine_Biology_AR_Application PermissionManager.
/// </summary>
public class CameraPermissionHandler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Panel shown when permission is denied")]
    [SerializeField] private GameObject permissionDeniedPanel;

    [Tooltip("Root GameObject containing all AR content - disabled until permission granted")]
    [SerializeField] private GameObject arContentRoot;

    [Header("Events")]
    public UnityEvent OnPermissionGranted;
    public UnityEvent OnPermissionDenied;

    private bool _permissionGranted;

    public bool IsPermissionGranted => _permissionGranted;

    void Start()
    {
        if (permissionDeniedPanel != null)
            permissionDeniedPanel.SetActive(false);

        RequestCameraPermission();
    }

    /// <summary>
    /// Request camera permission. Can be called again from a "Retry" button.
    /// </summary>
    public void RequestCameraPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(Permission.Camera))
        {
            HandleGranted();
            return;
        }

        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ => HandleGranted();
        callbacks.PermissionDenied += _ => HandleDenied();
        callbacks.PermissionDeniedAndDontAskAgain += _ => HandleDenied();
        Permission.RequestUserPermission(Permission.Camera, callbacks);
#else
        // In Editor or non-Android, assume granted
        HandleGranted();
#endif
    }

    private void HandleGranted()
    {
        _permissionGranted = true;
        Debug.Log("[FaceAR] Camera permission granted");

        if (arContentRoot != null)
            arContentRoot.SetActive(true);
        if (permissionDeniedPanel != null)
            permissionDeniedPanel.SetActive(false);

        OnPermissionGranted?.Invoke();
    }

    private void HandleDenied()
    {
        _permissionGranted = false;
        Debug.LogWarning("[FaceAR] Camera permission denied");

        if (arContentRoot != null)
            arContentRoot.SetActive(false);
        if (permissionDeniedPanel != null)
            permissionDeniedPanel.SetActive(true);

        OnPermissionDenied?.Invoke();
    }
}
