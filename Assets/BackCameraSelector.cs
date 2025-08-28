using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using System.Collections;
using UnityEngine;

public class BackCameraSelector : MonoBehaviour
{
    IEnumerator Start()
    {
        // --- CAMERA PERMISSION (Android/iOS) ---
#if UNITY_ANDROID
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
            while (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
                yield return null; // wait for user
        }
#elif UNITY_IOS
        if (!Application.HasUserAuthorization(UserAuthorization.WebCam))
        {
            yield return Application.RequestUserAuthorization(UserAuthorization.WebCam);
        }
#endif

        // Wait until Bootstrap created the ImageSource
        yield return new WaitUntil(() => ImageSourceProvider.ImageSource != null);
        var src = ImageSourceProvider.ImageSource;

        // Pick the first non-front-facing device (fallback index 0)
        var devices = WebCamTexture.devices;
        int backIdx = -1;
        for (int i = 0; i < devices.Length; i++)
            if (!devices[i].isFrontFacing) { backIdx = i; break; }
        if (backIdx < 0) backIdx = 0;

        // If already running, stop it first
        if (src.isPrepared)
        {
            src.Stop();  // no yield, just call it
        }

        // Select the back camera and play
        src.SelectSource(backIdx);
        yield return src.Play();  // Play() does return IEnumerator
    }
}
