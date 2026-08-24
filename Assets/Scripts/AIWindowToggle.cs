using UnityEngine;

public class AIWindowToggle : MonoBehaviour
{
    public GameObject aiPanel;

    public void ToggleAI()
    {
        bool willBeActive = !aiPanel.activeSelf;
        aiPanel.SetActive(willBeActive);

        if (willBeActive)
            GetComponent<MarineDemoManager>()?.EnsureGeminiKeyLoaded();
    }
}