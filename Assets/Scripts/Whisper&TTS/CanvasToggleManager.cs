using UnityEngine;

public class CanvasToggleManager : MonoBehaviour
{
    [Tooltip("The main UI canvas to show/hide.")]
    public GameObject mainCanvas;

    [Tooltip("The AI canvas to show/hide.")]
    public GameObject aiCanvas;

    public void ShowAICanvas()
    {
        if (mainCanvas != null) mainCanvas.SetActive(false);
        if (aiCanvas != null) aiCanvas.SetActive(true);
    }

    public void HideAICanvas()
    {
        if (mainCanvas != null) mainCanvas.SetActive(true);
        if (aiCanvas != null) aiCanvas.SetActive(false);
    }
}
