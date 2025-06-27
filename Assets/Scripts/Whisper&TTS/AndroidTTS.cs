using UnityEngine;

public class AndroidTTS : MonoBehaviour
{
    private AndroidJavaObject ttsObject = null;
    private AndroidJavaObject unityActivity = null;
    private bool isInitialized = false;

    public bool IsInitialized => isInitialized;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            // Create listener
            var listener = new TTSInitListener(OnTTSInitialized);

            // Instantiate TTS with listener
            ttsObject = new AndroidJavaObject("android.speech.tts.TextToSpeech", unityActivity, listener);
        }
#endif
    }

    private void OnTTSInitialized()
    {
        isInitialized = true;
        SetLanguage("en-GB");  // Set language safely after initialization
    }

    public void SetLanguage(string languageTag = "en-GB")
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsObject != null && isInitialized)
        {
            using (var locale = new AndroidJavaObject("java.util.Locale", languageTag))
            {
                int result = ttsObject.Call<int>("setLanguage", locale);
                if (result < 0)
                    Debug.LogWarning("TTS: Language not supported or missing data: " + languageTag);
            }
        }
#endif
    }

    public void Speak(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsObject != null && isInitialized)
        {
            ttsObject.Call<int>("speak", message, 0, null, null);
        }
#endif
    }

    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsObject != null)
        {
            ttsObject.Call<int>("stop");
        }
#endif
    }
}
