using UnityEngine;

public class ARActivator : MonoBehaviour
{
    public GameObject arSession;
    public GameObject arSessionOrigin;
    public GameObject uiCanvas;
    public Camera uiCamera;

    public void ActivateAR()
    {
        // Enable AR components
        arSession.SetActive(true);
        arSessionOrigin.SetActive(true);
        
        // Hide UI Canvas
        uiCanvas.SetActive(false);
        
        // Disable temporary UI camera
        uiCamera.gameObject.SetActive(false);
        uiCamera.gameObject.SetActive(false);
    }
}
