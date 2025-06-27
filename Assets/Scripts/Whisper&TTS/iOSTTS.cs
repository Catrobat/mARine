using System.Runtime.InteropServices;
using UnityEngine;

public class iOSTTS : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern void _InitTTS();
    [DllImport("__Internal")] private static extern bool _IsTTSReady();
    [DllImport("__Internal")] private static extern void _SpeakWithLanguage(string text, string langCode);
    [DllImport("__Internal")] private static extern void _StopTTS();
#endif

    private bool isInitialized = false;
    public bool IsInitialized => isInitialized;

    void Start()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _InitTTS();
        StartCoroutine(CheckInit());
#else
        isInitialized = true;
#endif
    }

    private System.Collections.IEnumerator CheckInit()
    {
        float timeout = 5f;
        float elapsed = 0f;
#if UNITY_IOS && !UNITY_EDITOR
        while (!_IsTTSReady() && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
        isInitialized = _IsTTSReady();
        Debug.Log($"[iOSTTS] TTS Ready: {isInitialized}");
#endif
        yield return null;
    }

    public void Speak(string text, string lang = "en-GB")
    {
#if UNITY_IOS && !UNITY_EDITOR
        if (isInitialized)
            _SpeakWithLanguage(text, lang);
        else
            Debug.LogWarning("[iOSTTS] Speak called before TTS ready.");
#else
        Debug.Log($"[iOSTTS] Would speak: {text} (lang={lang})");
#endif
    }

    public void Stop()
    {
#if UNITY_IOS && !UNITY_EDITOR
        _StopTTS();
#endif
    }
}
