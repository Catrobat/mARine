using System.IO;
using UnityEngine;

/// <summary>
/// Captures screenshots and saves to Android gallery.
/// Connected to the "Snap" button in FaceARUIController.
/// </summary>
public class ScreenshotCapture : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string filenamePrefix = "FaceAR";
    [SerializeField] private int superSize = 1; // 1 = normal resolution

    /// <summary>
    /// Capture a screenshot and save to persistent storage.
    /// On Android, refreshes the media gallery so it shows up in Photos.
    /// </summary>
    public void CaptureScreenshot()
    {
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string filename = $"{filenamePrefix}_{timestamp}.png";

#if UNITY_ANDROID && !UNITY_EDITOR
        // Save to Pictures folder on Android
        string picturesPath = "/storage/emulated/0/Pictures/FaceAR";
        if (!Directory.Exists(picturesPath))
            Directory.CreateDirectory(picturesPath);
        string filepath = Path.Combine(picturesPath, filename);
#else
        string filepath = Path.Combine(Application.persistentDataPath, filename);
#endif

        ScreenCapture.CaptureScreenshot(filepath, superSize);
        Debug.Log($"[FaceAR] Screenshot saved: {filepath}");

#if UNITY_ANDROID && !UNITY_EDITOR
        // Refresh Android media scanner so the image shows in gallery
        RefreshAndroidGallery(filepath);
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void RefreshAndroidGallery(string path)
    {
        try
        {
            using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            using var mediaScannerConnection = new AndroidJavaClass("android.media.MediaScannerConnection");
            mediaScannerConnection.CallStatic("scanFile", currentActivity,
                new string[] { path }, null, null);
            Debug.Log("[FaceAR] Gallery refreshed for: " + path);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[FaceAR] Failed to refresh gallery: " + ex.Message);
        }
    }
#endif
}
