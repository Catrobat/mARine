using UnityEngine;

public class AndroidTTS : MonoBehaviour
{
    private AndroidJavaObject ttsObject = null;
    private AndroidJavaObject unityActivity = null;

    void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            ttsObject = new AndroidJavaObject("android.speech.tts.TextToSpeech", unityActivity, null);
        }
#endif
    }

    public void Speak(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (ttsObject != null)
        {
            // 0 means QUEUE_FLUSH (replace with ongoing speech)
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
