using UnityEngine;

public class CrossPlatformTTS : MonoBehaviour
{
    private AndroidTTS androidTTS;
    private iOSTTS iosTTS;

    void Awake()
    {
        androidTTS = GetComponent<AndroidTTS>();
        iosTTS = GetComponent<iOSTTS>();
    }

    public void Speak(string message)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidTTS.Speak(message);
#elif UNITY_IOS && !UNITY_EDITOR
        iosTTS.Speak(message);
#else
        Debug.Log("TTS not supported on this platform.");
#endif
    }

    public void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        androidTTS.Stop();
#elif UNITY_IOS && !UNITY_EDITOR
        iosTTS.Stop();
#else
        Debug.Log("TTS stop not supported on this platform.");
#endif
    }
}