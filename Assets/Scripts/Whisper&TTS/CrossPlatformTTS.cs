using UnityEngine;

public class CrossPlatformTTS : MonoBehaviour
{
    private AndroidTTS androidTTS;
    private iOSTTS iosTTS;

    public bool IsReady =>
#if UNITY_ANDROID && !UNITY_EDITOR
        androidTTS != null && androidTTS.IsInitialized;
#elif UNITY_IOS && !UNITY_EDITOR
        iosTTS != null && iosTTS.IsInitialized;
#else
        true;
#endif

    void Awake()
    {
        androidTTS = GetComponent<AndroidTTS>();
        iosTTS = GetComponent<iOSTTS>();
    }

    /// <summary>
    /// Speak with optional language. Defaults to English UK.
    /// </summary>
    public void Speak(string message, string languageCode = "en-GB")
    {
        if (!IsReady)
        {
            Debug.LogWarning("[TTS] Tried to speak before ready.");
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        androidTTS.SetLanguage(languageCode);
        androidTTS.Speak(message);
#elif UNITY_IOS && !UNITY_EDITOR
        iosTTS.Speak(message, languageCode);
#else
        Debug.Log($"[TTS] (Editor) Speaking '{message}' in '{languageCode}'");
#endif
    }

    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidTTS.Stop();
#elif UNITY_IOS && !UNITY_EDITOR
        iosTTS.Stop();
#else
        Debug.Log("[TTS] (Editor) Stop called");
#endif
    }
}
