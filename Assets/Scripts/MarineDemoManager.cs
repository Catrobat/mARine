using System;
using System.Collections.Generic;
using System.Threading;
using System.Text.RegularExpressions;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MarineDemoManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button recordButton;
    [SerializeField] private TMP_Text transcriptionText;
    [SerializeField] private TMP_Text responseText;
    [SerializeField] private Button ttsButton;
    [SerializeField] private Toggle echoModeToggle;

    [Header("Android Speech Recognizer")]
    [SerializeField] private bool useAndroidSpeechRecognizer = true;
    [SerializeField] private bool preferAndroidOfflineRecognition;
    [SerializeField] private string[] androidSpeechBiasingPhrases =
    {
        "cephalopods",
        "mollusks",
        "arthropods",
        "echinoderms",
        "cnidarians",
        "plankton",
        "bioluminescence",
        "crustaceans",
        "coral reefs",
        "marine conservation",
        "oceanography"
    };

    [Header("Android TTS")]
    [SerializeField] private bool useAndroidTextToSpeech = true;

    [Header("Gemini")]
    [SerializeField] private bool useGemini = true;
    [SerializeField] private GeminiChatClient gemini = new();
    [TextArea(2, 4)]
    [SerializeField] private string geminiSystemPrompt =
        "You are Marina, an expert marine biology AI assistant.\nYou specialize in ocean ecosystems, marine species, coral reefs,\ndeep-sea biology, marine conservation, and oceanography.\nBy default, give a complete medium-length answer in about 4 to 6 sentences.\nIf the user asks for more detail, specific length, or explanation, follow that request.\nIf a question is not related to marine biology or oceans, politely redirect\nthe user back to marine topics.";

    private readonly CancellationTokenSource _destroyCts = new();
    private AndroidTextToSpeechBridge _tts = new();
    private AndroidSpeechRecognizerBridge _androidSpeechRecognizer = new();
    private string _currentLanguage = "en";
    private string _lastTranscription = "";
    private bool _isRecording;
    private bool _isInitializing;
    private bool _isTtsReady;
    private bool _isUsingAndroidSpeechRecognizer;

    private async void Start()
    {
        WireUi();
        SetStatus("Loading speech services...");
        try
        {
            _isInitializing = true;
            await InitializeSpeechAsync(_destroyCts.Token);
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetStatus("Initialization failed");
            Debug.LogException(ex);
        }
        finally
        {
            _isInitializing = false;
        }
    }

    public void EnsureGeminiKeyLoaded()
    {
        if (!useGemini || gemini.HasApiKey)
            return;

        FetchGeminiKeyAsync().Forget();
    }

    private async UniTask FetchGeminiKeyAsync()
    {
        string firestoreKey = await GeminiKeyProvider.FetchFromFirestoreAsync();
        if (!string.IsNullOrWhiteSpace(firestoreKey))
            gemini.ApiKey = firestoreKey;
    }

    private void WireUi()
    {
        if (languageDropdown != null)
        {
            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(new System.Collections.Generic.List<string>
            {
                "English",
                "German"
            });
            languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
        }

        if (recordButton != null)
            AddHoldEvents(recordButton.gameObject);

        if (ttsButton != null)
            ttsButton.onClick.AddListener(OnTtsButtonPressed);

        if (responseText != null)
            responseText.text = GetDefaultResponse();
    }

    private void AddHoldEvents(GameObject buttonObject)
    {
        var trigger = buttonObject.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = buttonObject.AddComponent<EventTrigger>();

        trigger.triggers.Clear();
        AddTrigger(trigger, EventTriggerType.PointerDown, _ => StartRecording());
        AddTrigger(trigger, EventTriggerType.PointerUp, _ => StopRecording());
        AddTrigger(trigger, EventTriggerType.PointerExit, _ => StopRecording());
    }

    private static void AddTrigger(
        EventTrigger trigger,
        EventTriggerType type,
        UnityEngine.Events.UnityAction<BaseEventData> callback)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private async UniTask InitializeSpeechAsync(CancellationToken ct)
    {
        DisposeSpeechServices();

        if (useAndroidTextToSpeech && Application.platform == RuntimePlatform.Android)
        {
            try
            {
                await _tts.InitializeAsync(GetSpeechLanguageTag(), ct);
                _isTtsReady = _tts.IsReady;
            }
            catch (Exception ex)
            {
                _isTtsReady = false;
                Debug.LogWarning("[MarineDemoManager] Android TTS init failed: " + ex.Message);
            }
        }
        else
        {
            _isTtsReady = false;
        }

        Debug.Log("[MarineDemoManager] Gemini key configured: " + gemini.HasApiKey);
        SetStatus(ShouldUseAndroidSpeechRecognizer()
            ? (preferAndroidOfflineRecognition
                ? "Ready for Android offline speech and TTS"
                : "Ready for Android speech and TTS")
            : "Android speech unavailable");
    }

    private void ApplyLanguageProfiles()
    {
        if (responseText != null && string.IsNullOrWhiteSpace(responseText.text))
            responseText.text = GetDefaultResponse();
    }

    private void OnLanguageChanged(int index)
    {
        _currentLanguage = index == 1 ? "de" : "en";
        if (_isRecording)
            StopRecording();

        SetStatus(_currentLanguage == "de" ? "Language: Deutsch" : "Language: English");
        ApplyLanguageProfiles();
        responseText.text = GetDefaultResponse();
        SetStatus("Ready");
    }

    private async void StartRecording()
    {
        if (_isRecording)
            return;

        if (_isInitializing)
        {
            SetStatus("Still initializing...");
            return;
        }

        if (!ShouldUseAndroidSpeechRecognizer())
        {
            SetStatus("Android speech unavailable");
            SetResponse("This build uses Android speech recognition. Run it on an Android device.");
            return;
        }

        if (!await RequestMicrophonePermissionAsync())
        {
            SetStatus("Microphone permission denied");
            SetResponse("Microphone permission was denied. Enable microphone permission for this app.");
            return;
        }

        _isRecording = true;
        _isUsingAndroidSpeechRecognizer = true;
        SetButtonLabel("Stop");
        SetStatus("Listening...");
        SetResponse("");
        transcriptionText.text = "";
        _lastTranscription = "";

        bool started = _androidSpeechRecognizer.StartListening(
            gameObject.name,
            nameof(OnAndroidSpeechRecognized),
            nameof(OnAndroidSpeechError),
            GetSpeechLanguageTag(),
            preferAndroidOfflineRecognition,
            androidSpeechBiasingPhrases);

        if (!started)
        {
            _isRecording = false;
            _isUsingAndroidSpeechRecognizer = false;
            SetButtonLabel("Hold to Speak");
            SetStatus("Speech recognizer unavailable");
            SetResponse("Android speech recognition is not available on this device.");
        }
    }

    private void StopRecording()
    {
        if (!_isRecording)
            return;

        _isRecording = false;
        SetButtonLabel("Hold to Speak");

        if (_isUsingAndroidSpeechRecognizer)
        {
            SetStatus("Processing...");
            _androidSpeechRecognizer.StopListening();
            return;
        }
    }

    private async UniTask ProcessRecognizedTextAsync(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
        {
            SetStatus("No speech detected");
            SetResponse("I did not hear speech clearly. Please hold the button and speak again.");
            return;
        }

        transcript = NormalizeMarineTerms(transcript);
        _lastTranscription = transcript;
        transcriptionText.text = transcript;

        if (useGemini && gemini.HasApiKey)
        {
            await AskGeminiAndSpeak(transcript);
            return;
        }

        string response = echoModeToggle != null && echoModeToggle.isOn
            ? transcript
            : BuildFallbackResponse(transcript);

        SetResponse(response);

        if (echoModeToggle != null && echoModeToggle.isOn)
            await Speak(response);

        SetStatus("Ready");
    }

    private void OnAndroidSpeechRecognized(string transcript)
    {
        _isUsingAndroidSpeechRecognizer = false;
        _isRecording = false;
        SetButtonLabel("Hold to Speak");
        ProcessRecognizedTextAsync(transcript).Forget();
    }

    private void OnAndroidSpeechError(string error)
    {
        _isUsingAndroidSpeechRecognizer = false;
        _isRecording = false;
        SetButtonLabel("Hold to Speak");
        if (!string.IsNullOrWhiteSpace(error)
            && (error.Contains("NO_MATCH") || error.Contains("SPEECH_TIMEOUT")))
        {
            SetStatus("No speech detected");
            SetResponse("I did not hear speech clearly. Please hold the button and speak again.");
        }
        else
        {
            SetStatus("Recognition error");
            SetResponse(string.IsNullOrWhiteSpace(error)
                ? "Android speech recognition failed."
                : "Android speech recognition failed: " + error);
        }
        Debug.LogError("[MarineDemoManager] Android speech recognition failed: " + error);
    }

    private async UniTask AskGeminiAndSpeak(string prompt)
    {
        Debug.Log($"[MarineDemoManager] AskGeminiAndSpeak start: len={prompt?.Length ?? 0}, hasKey={gemini.HasApiKey}, useGemini={useGemini}");
        try
        {
            SetStatus("Thinking...");
            string response = await gemini.GenerateResponseAsync(
                prompt,
                _currentLanguage,
                geminiSystemPrompt);

            Debug.Log(
                "[MarineDemoManager] Gemini raw response: " +
                (string.IsNullOrWhiteSpace(response) ? "<empty>" : response));

            if (string.IsNullOrWhiteSpace(response))
            {
                SetStatus("Gemini empty response");
                SetResponse("Gemini returned an empty response.");
                Debug.LogError("[MarineDemoManager] Gemini returned an empty response.");
                return;
            }

            SetResponse(response);
            await Speak(response);
        }
        catch (Exception ex)
        {
            SetStatus("Gemini error");
            SetResponse("Gemini failed: " + ex.Message);
            Debug.LogError("[MarineDemoManager] Gemini failed: " + ex.ToString());
            Debug.LogError("[MarineDemoManager] Prompt was: " + prompt);
            Debug.LogException(ex);
        }
    }

    private void OnTtsButtonPressed()
    {
        string text = responseText != null ? responseText.text : "";
        if (string.IsNullOrWhiteSpace(text))
            text = _lastTranscription;

        Speak(text).Forget();
    }

    private async UniTask Speak(string text)
    {
        if (_tts == null || string.IsNullOrWhiteSpace(text))
        {
            if (!string.IsNullOrWhiteSpace(text))
                SetStatus("TTS unavailable");
            return;
        }

        try
        {
            SetStatus("Speaking...");
            await _tts.SpeakAsync(text, GetSpeechLanguageTag(), _destroyCts.Token);
            _isTtsReady = _tts.IsReady;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            SetStatus("TTS error: " + ex.Message);
            Debug.LogException(ex);
            return;
        }

        SetStatus("Ready");
    }

    private string GetDefaultResponse()
    {
        return _currentLanguage == "de"
            ? "Ich habe dich verstanden."
            : "I heard you clearly.";
    }

    private string BuildFallbackResponse(string prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            return GetDefaultResponse();

        if (_currentLanguage == "de")
            return "Ich habe deine Frage gehoert, aber die Antwort konnte gerade nicht generiert werden.";

        return "I heard your question, but I could not generate the answer right now.";
    }

    private static string NormalizeMarineTerms(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return transcript;

        string result = transcript;
        result = ReplaceWholePhrase(result, "safe reports", "cephalopods");
        result = ReplaceWholePhrase(result, "safer pods", "cephalopods");
        result = ReplaceWholePhrase(result, "suffer pods", "cephalopods");
        result = ReplaceWholePhrase(result, "cefa pods", "cephalopods");
        result = ReplaceWholePhrase(result, "cephlopods", "cephalopods");
        result = ReplaceWholePhrase(result, "cephalo pods", "cephalopods");
        result = ReplaceWholePhrase(result, "molluscs", "mollusks");
        result = ReplaceWholePhrase(result, "arthro pods", "arthropods");
        result = ReplaceWholePhrase(result, "echino derms", "echinoderms");
        result = ReplaceWholePhrase(result, "nidarians", "cnidarians");
        result = ReplaceWholePhrase(result, "nidarian", "cnidarian");
        result = ReplaceWholePhrase(result, "bio luminescence", "bioluminescence");
        result = ReplaceWholePhrase(result, "crust station", "crustacean");
        result = ReplaceWholePhrase(result, "planktons", "plankton");
        return result;
    }

    private static string ReplaceWholePhrase(
        string value,
        string from,
        string to)
    {
        return Regex.Replace(
            value,
            $@"\b{Regex.Escape(from)}\b",
            to,
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private void SetStatus(string status)
    {
        if (statusText != null)
            statusText.text = status;
    }

    private void SetResponse(string response)
    {
        if (responseText != null)
            responseText.text = response;
    }

    private void SetButtonLabel(string label)
    {
        if (recordButton == null)
            return;

        var text = recordButton.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.text = label;
    }

    private void DisposeSpeechServices()
    {
        _androidSpeechRecognizer?.Cancel();

        _tts?.Dispose();
        _tts = new AndroidTextToSpeechBridge();
        _isTtsReady = false;
        _isUsingAndroidSpeechRecognizer = false;
    }

    private bool ShouldUseAndroidSpeechRecognizer()
    {
        return Application.platform == RuntimePlatform.Android
            && useAndroidSpeechRecognizer;
    }

    private async UniTask<bool> RequestMicrophonePermissionAsync()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            return true;

        var completion = new UniTaskCompletionSource<bool>();
        var callbacks = new PermissionCallbacks();
        callbacks.PermissionGranted += _ => completion.TrySetResult(true);
        callbacks.PermissionDenied += _ => completion.TrySetResult(false);
        callbacks.PermissionDeniedAndDontAskAgain += _ => completion.TrySetResult(false);

        Permission.RequestUserPermission(Permission.Microphone, callbacks);
        return await completion.Task.AttachExternalCancellation(_destroyCts.Token);
#else
        return true;
#endif
    }

    private string GetSpeechLanguageTag()
    {
        return _currentLanguage == "de" ? "de-DE" : "en-US";
    }

    private void OnDisable()
    {
        _tts?.Stop();
        _androidSpeechRecognizer?.Cancel();
    }

    private void OnDestroy()
    {
        _destroyCts.Cancel();
        DisposeSpeechServices();
        _androidSpeechRecognizer.Dispose();
        _destroyCts.Dispose();
    }
}
