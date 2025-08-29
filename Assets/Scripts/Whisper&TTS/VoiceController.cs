using UnityEngine;
using System.Threading.Tasks;

// Required namespaces from the Whisper package
using Whisper;
using Whisper.Utils;

public class VoiceController : MonoBehaviour
{
    // --- Public Events ---
    // Other scripts can subscribe to these events to get results
    public event System.Action<string> OnTranscriptionReceived;

    // --- Inspector References ---
    [Tooltip("The WhisperManager component for transcription.")]
    public WhisperManager whisperManager;
    
    [Tooltip("The MicrophoneRecord component for recording audio.")]
    public MicrophoneRecord microphoneRecord;

    private void Awake()
    {
        // Subscribe to the event that fires when the microphone has finished recording
        microphoneRecord.OnRecordStop += HandleRecordingStopped;
    }

    private void OnDestroy()
    {
        // Always unsubscribe from events when the object is destroyed
        microphoneRecord.OnRecordStop -= HandleRecordingStopped;
    }

    /// <summary>
    /// Call this method from other scripts to start listening for voice input.
    /// </summary>
    public void StartListening()
    {
        if (!microphoneRecord.IsRecording)
        {
            Debug.Log("VoiceController: Starting to listen...");
            microphoneRecord.StartRecord();
        }
    }

    /// <summary>
    /// Call this method from other scripts to manually stop listening.
    /// </summary>
    public void StopListening()
    {
        if (microphoneRecord.IsRecording)
        {
            Debug.Log("VoiceController: Manually stopping listening.");
            microphoneRecord.StopRecord();
        }
    }

    /// <summary>
    /// This method is called automatically when the MicrophoneRecord stops.
    /// </summary>
    private async void HandleRecordingStopped(AudioChunk recordedAudio)
    {
        Debug.Log("VoiceController: Recording stopped. Processing transcription...");

        // Asynchronously get the transcription from Whisper
        var result = await whisperManager.GetTextAsync(
            recordedAudio.Data, 
            recordedAudio.Frequency, 
            recordedAudio.Channels);

        if (result != null)
        {
            string transcription = result.Result;
            Debug.Log($"VoiceController: Transcription received: \"{transcription}\"");

            // --- THIS IS THE KEY PART ---
            // Fire the event with the final transcription text.
            // Any other script subscribed to this will now receive the text.
            OnTranscriptionReceived?.Invoke(transcription);
            
            // --- BONUS: AI INTEGRATION POINT ---
            // Now that you have the user's text, you can send it to an AI.
            // await GetAnswerFromAI(transcription);
        }
        else
        {
            Debug.LogError("VoiceController: Transcription failed.");
        }
    }

    // --- BONUS: AI and TTS Placeholder ---
    /*
    private async Task GetAnswerFromAI(string userQuestion)
    {
        // 1. Send 'userQuestion' to your OpenAI Chat API wrapper.
        // string aiResponse = await openAIChat.Ask(userQuestion);

        // 2. Log the response.
        // Debug.Log($"AI Response: {aiResponse}");

        // 3. Send the AI response to your TTS script to be spoken aloud.
        // openAITTS.StartTTS(aiResponse);
    }
    */
}
