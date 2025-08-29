using UnityEngine;
using TMPro;
using Whisper.Utils;
using UnityEngine.UI;

public class ARAppVoiceManager : MonoBehaviour
{

    [Tooltip("Reference to the main VoiceController in the scene.")]
    public VoiceController voiceController;

    [Tooltip("Reference to the MicrophoneRecord component to know when recording starts/stops.")]
    public MicrophoneRecord microphoneRecord;

    [Tooltip("Reference to TTS component for text-to-speech functionality.")]
    public CrossPlatformTTS ttsManager;

    [Header("UI Elements")]
    [Tooltip("The button that starts the recording process.")]
    public Button recordButton;

    [Tooltip("Button for sending the transcribed prompt to the AI.")]
    public Button sendButton;

    [Tooltip("The button's text to indicate the current state.")]
    public TextMeshProUGUI buttonText;
    
    [Tooltip("A UI Text element to show the transcription result.")]
    public TextMeshProUGUI outputText;

    private string lastTranscription = "";

    private void Start()
    {
        // Make sure all references are set
        if (voiceController == null || microphoneRecord == null || recordButton == null || sendButton == null || buttonText == null || outputText == null)
        {
            Debug.LogError("VoiceUIManager: A reference is missing! Please assign all fields in the Inspector.");
            return;
        }

        // Subscribe to the events we need
        voiceController.OnTranscriptionReceived += OnTranscriptionReceived;
        microphoneRecord.OnRecordStop += (audioChunk) => OnRecordingStopped(); // We don't need the audio chunk here, just the event

        // Set up the button's click listener
        recordButton.onClick.AddListener(OnRecordButtonPressed);

        // Disable the send button initially
        sendButton.interactable = false;

        // Set the initial state
        ResetUI();
    }

    private void OnDestroy()
    {
        // Always unsubscribe to prevent errors
        if (voiceController != null)
        {
            voiceController.OnTranscriptionReceived -= OnTranscriptionReceived;
        }
        if (microphoneRecord != null)
        {
            microphoneRecord.OnRecordStop -= (audioChunk) => OnRecordingStopped();
        }
    }

    /// <summary>
    /// This method is called by the Button's OnClick event.
    /// </summary>
    private void OnRecordButtonPressed()
    {
        if (!microphoneRecord.IsRecording)
        {
            ttsManager.Stop(); // Stop any ongoing TTS before starting recording
            // --- Start Recording ---
            voiceController.StartListening();

            // Update UI to show we are listening
            buttonText.text = "Listening...";
            recordButton.interactable = false; // Disable button while listening
            sendButton.interactable = false;  // Disable send while recording
            outputText.text = "Listening for your voice...";
            lastTranscription = "";
        }
    }

    /// <summary>
    /// This method is called by the VoiceController's event when a result is ready.
    /// </summary>
    private void OnTranscriptionReceived(string transcription)
    {
        lastTranscription = transcription;

        // Display the final text
        outputText.text = transcription;

        // Reset the UI to its initial state
        ResetUI();

        // Enable send button only if the transcription is not empty/whitespace
        sendButton.interactable = !string.IsNullOrWhiteSpace(transcription);

    }

    /// <summary>
    /// This method is called by the MicrophoneRecord's event when recording stops for any reason.
    /// </summary>
    private void OnRecordingStopped()
    {
        // If the transcription hasn't already reset the UI, this will catch it.
        // This is useful if the recording stops due to VAD timeout without a result.
        if (!recordButton.interactable)
        {
            ResetUI();
        }
    }

    /// <summary>
    /// Resets the UI elements to their default state.
    /// </summary>
    private void ResetUI()
    {
        buttonText.text = "Speak";
        recordButton.interactable = true;
    }

    public string GetLastTranscription()
    {
        return lastTranscription;
    }
}
