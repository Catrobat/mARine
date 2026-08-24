using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class AndroidTextToSpeechBridge : IDisposable
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private AndroidJavaObject _bridge;
    private bool _disposed;

    public bool IsReady
    {
        get
        {
            if (_bridge == null)
                return false;

            try
            {
                return _bridge.Call<bool>("isReady");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[AndroidTextToSpeechBridge] isReady failed: " + ex.Message);
                return false;
            }
        }
    }

    public UniTask InitializeAsync(
        string languageTag,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();
        EnsureBridge();

        bool initialized = _bridge.Call<bool>("initialize", languageTag);
        if (!initialized)
            throw new InvalidOperationException("Android TextToSpeech failed to initialize.");

        return UniTask.CompletedTask;
    }

    public UniTask SpeakAsync(
        string text,
        string languageTag,
        CancellationToken ct = default)
    {
        EnsureNotDisposed();

        if (string.IsNullOrWhiteSpace(text))
            return UniTask.CompletedTask;

        EnsureBridge();
        bool started = _bridge.Call<bool>("speak", text, languageTag);
        if (!started)
            Debug.LogWarning("Android TextToSpeech did not start speaking on this attempt.");

        return UniTask.CompletedTask;
    }

    public void Stop()
    {
        if (_bridge == null)
            return;

        try
        {
            _bridge.Call("stop");
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[AndroidTextToSpeechBridge] stop failed: " + ex.Message);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            _bridge?.Call("shutdown");
        }
        catch
        {
        }

        _bridge?.Dispose();
        _bridge = null;
    }

    private void EnsureBridge()
    {
        if (_bridge != null)
            return;

        _bridge = new AndroidJavaObject(
            "AndroidTextToSpeechBridge");
    }

    private void EnsureNotDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(AndroidTextToSpeechBridge));
    }
#else
    public bool IsReady => false;

    public UniTask InitializeAsync(
        string languageTag,
        CancellationToken ct = default)
    {
        return UniTask.CompletedTask;
    }

    public UniTask SpeakAsync(
        string text,
        string languageTag,
        CancellationToken ct = default)
    {
        return UniTask.CompletedTask;
    }

    public void Stop()
    {
    }

    public void Dispose()
    {
    }
#endif
}
