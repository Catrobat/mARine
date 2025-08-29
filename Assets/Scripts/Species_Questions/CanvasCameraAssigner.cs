using UnityEngine;

public class CanvasCameraAssigner : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera && canvas.worldCamera == null)
        {
            Camera arCamera = Camera.main;  // MainCamera tag
            if (arCamera != null)
            {
                canvas.worldCamera = arCamera;
            }
            else
            {
                Debug.LogWarning("No ARCamera found!");
            }
        }
    }
}
