using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;

public class BackNavigation : MonoBehaviour
{
    public ARSession arSession;
    public string previousSceneName = "StartScene";

    public GameObject infoCanvas;
    public GameObject quizCanvas;

    public CanvasToggleManager canvasToggleManager;

    public CrossPlatformTTS ttsManager;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Check if any UICanvasTag is active
            var allUICanvases = FindObjectsOfType<UICanvasTag>();
            foreach (var canvas in allUICanvases)
            {
                if (canvas.gameObject.activeSelf)
                {
                    if (ttsManager != null)
                    {
                        ttsManager.Stop(); // Stop any ongoing TTS before closing the canvas
                    }
                    canvas.gameObject.SetActive(false);

                    if (canvas.name == "AICanvas")
                        canvasToggleManager.HideAICanvas();
                    return;
                }
            }
            
            // No UI canvas open -> exit AR session
            Debug.Log("Back button detected in AR Session. Navigating to " + previousSceneName);
            if (arSession != null)
            {
                arSession.Reset(); // Or arSession.enabled = false;
            }
            SceneManager.LoadScene(previousSceneName);
        }
    }
}
