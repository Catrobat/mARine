using UnityEngine;

public class ModuleSelector : MonoBehaviour
{
    public GameObject arSessionOrigin;
    public GameObject arSession;
    public GameObject uiCanvas;
    public Camera arCamera;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (arSessionOrigin != null)
        {
            arSessionOrigin.SetActive(false);
        }
        
        if (arSession != null)
        {
            arSession.SetActive(false);
        }

        if (uiCanvas != null)
        {
            uiCanvas.SetActive(true);
        }
        
    }

    public void EnableAR()
    {
        if (arSession != null)
        {
            arSession.SetActive(true);
        }

        if (arSessionOrigin != null)
        {
            arSessionOrigin.SetActive(true);
        }

        // Hide UI Canvas
        if (uiCanvas != null)
        {
            uiCanvas.SetActive(false);
        }

        if (arCamera != null)
        {
            arCamera.enabled = true;
        }
    }
}
