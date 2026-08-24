using System;
using UnityEngine;

public sealed class AndroidSpeechRecognizerBridge : IDisposable
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string JavaClass =
        "AndroidSpeechRecognizerBridge";

    private AndroidJavaObject _bridge;
    private bool _disposed;

    public bool StartListening(
        string gameObjectName,
        string onResultMethod,
        string onErrorMethod,
        string languageTag,
        bool preferOffline,
        string[] biasingPhrases)
    {
        if (_disposed)
            return false;

        try
        {
            EnsureBridge();
            return _bridge.Call<bool>(
                "startListening",
                gameObjectName,
                onResultMethod,
                onErrorMethod,
                languageTag,
                preferOffline,
                biasingPhrases ?? Array.Empty<string>());
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[AndroidSpeechRecognizerBridge] Android speech bridge start failed: " +
                ex.Message);
            return false;
        }
    }

    public void StopListening()
    {
        if (_disposed || _bridge == null)
            return;

        try
        {
            _bridge.Call("stopListening");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[AndroidSpeechRecognizerBridge] Android speech bridge stop failed: " +
                ex.Message);
        }
    }

    public void Cancel()
    {
        if (_disposed || _bridge == null)
            return;

        try
        {
            _bridge.Call("cancel");
        }
        catch (Exception ex)
        {
            Debug.LogError(
                "[AndroidSpeechRecognizerBridge] Android speech bridge cancel failed: " +
                ex.Message);
        }
    }

    private void EnsureBridge()
    {
        if (_bridge != null)
            return;

        _bridge = new AndroidJavaObject(JavaClass);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        Cancel();
        _disposed = true;

        if (_bridge != null)
        {
            _bridge.Dispose();
            _bridge = null;
        }
    }
#else
    public bool StartListening(
        string gameObjectName,
        string onResultMethod,
        string onErrorMethod,
        string languageTag,
        bool preferOffline,
        string[] biasingPhrases)
    {
        return false;
    }

    public void StopListening()
    {
    }

    public void Cancel()
    {
    }

    public void Dispose()
    {
    }
#endif
}
