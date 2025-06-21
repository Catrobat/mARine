using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class APIManager : MonoBehaviour
{
    [SerializeField] private string gasUrl;
    [SerializeField] private TextMeshProUGUI prompt;
    [SerializeField] private TextMeshProUGUI responseText;
    [SerializeField] private CrossPlatformTTS ttsManager;
    [SerializeField] private GameObject spinner;
    [SerializeField] private Button sendButton;
    [SerializeField] private ARAppVoiceManager voiceManager;
    private bool fasle;

    public void SendPrompt()
    {
        // Clear the previous response immediately when the send button is pressed
        responseText.text = "";

        // Disable the send button while waiting
        if (sendButton != null)
            sendButton.interactable = false;

        StartCoroutine(SendDataToGas());
    }

    public void Record()
    {

        StartCoroutine("hi!");
    }

    private IEnumerator SendDataToGas()
    {
        // show spinner
        if (spinner != null)
            spinner.SetActive(true);

        // Use the transcription from ARAppVoiceManager 
        string userPrompt = "";
        if (voiceManager != null)
            userPrompt = voiceManager.GetLastTranscription();
        else if (prompt != null)
            userPrompt = prompt.text;

        WWWForm form = new WWWForm();
        form.AddField("parameter", userPrompt);
        UnityWebRequest www = UnityWebRequest.Post(gasUrl, form);
        
        yield return www.SendWebRequest();
        string response;

        if (www.result == UnityWebRequest.Result.Success)
        {
            response = www.downloadHandler.text;
        }
        else
        {
            response = "There was an error";
        }
        
        Debug.Log(response);
        responseText.text = response;

        // Speak the new AI response
        if (ttsManager != null && !string.IsNullOrEmpty(response))
        {
            ttsManager.Speak(response);
        }

        // Hide spinner
        if (spinner != null)
            spinner.SetActive(false);
    }
}
