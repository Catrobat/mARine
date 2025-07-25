using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;

public class HumanDetectionUI : MonoBehaviour
{
    [Header("AR Human Detection")]
    public ARHumanBodyManager humanBodyManager;

    [Header("UI Elements")]
    public Text statusText;

    private bool wasDetected = false;

    void Start()
    {
        if (statusText != null)
        {
            statusText.text = "Not Detected";
            statusText.color = Color.red;
        }
    }

    void Update()
    {
        if (humanBodyManager == null || statusText == null)
            return;

        // Check if any human bodies are currently tracked
        bool isDetected = false;
        foreach (var body in humanBodyManager.trackables)
        {
            if (body.trackingState == UnityEngine.XR.ARSubsystems.TrackingState.Tracking)
            {
                isDetected = true;
                break;
            }
        }

        if (isDetected != wasDetected)
        {
            wasDetected = isDetected;

            statusText.text = isDetected ? "Detected" : "Not Detected";
            statusText.color = isDetected ? Color.green : Color.red;
        }
    }
}
