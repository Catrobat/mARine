using UnityEngine;

public class TTSInitListener : AndroidJavaProxy
{
    private System.Action onInitialized;

    public TTSInitListener(System.Action callback) 
        : base("android.speech.tts.TextToSpeech$OnInitListener")
    {
        this.onInitialized = callback;
    }

    // Called by Android when TTS engine is initialized
    public void onInit(int status)
    {
        if (status == 0)  // SUCCESS
        {
            Debug.Log("[TTSInitListener] TTS initialized successfully.");
            onInitialized?.Invoke();
        }
        else
        {
            Debug.LogWarning("[TTSInitListener] TTS initialization failed with status: " + status);
        }
    }
}
