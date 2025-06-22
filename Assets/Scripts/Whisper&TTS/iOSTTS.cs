using UnityEngine;
using System.Runtime.InteropServices;

public class iOSTTS : MonoBehaviour
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void _Speak(string text);

    [DllImport("__Internal")]
    private static extern void _StopTTS();

    public void Speak(string message)
    {
        _Speak(message);
    }

    public void Stop()
    {
        _StopTTS();
    }
#else
    public void Speak(string message)
    {
        Debug.Log("TTS not supported in editor.");
    }

    public void Stop()
    {
        Debug.Log("TTS stop not supported in editor.");
    }
#endif
}
