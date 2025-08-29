using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModuleStatusButton : MonoBehaviour
{
    [System.Serializable]
    public class ModuleButton
    {
        public string moduleName;
        public Button button;
    }

    public ModuleButton[] moduleButtons;
    public string availableModule = "Module-5";  // Only this module is unlocked
    public GameObject toastPanel;                // UI Panel for toast
    public TMP_Text toastText;                       // Text to show in toast

    void Start()
    {
        foreach (var module in moduleButtons)
        {
            if (module.moduleName != availableModule)
            {
                module.button.onClick.RemoveAllListeners();
                module.button.onClick.AddListener(() => ShowToast("Only Module Defense is available right now."));
                module.button.interactable = true;  // Keep it interactable to catch clicks
                ColorBlock cb = module.button.colors;
                cb.normalColor = new Color(0.7f, 0.7f, 0.7f);  // Grey out visually
                module.button.colors = cb;
            }
        }
    }

    void ShowToast(string message)
    {
        StopAllCoroutines();
        StartCoroutine(ShowToastRoutine(message));
    }

    System.Collections.IEnumerator ShowToastRoutine(string message)
    {
        toastText.text = message;
        toastPanel.SetActive(true);
        yield return new WaitForSeconds(2f);
        toastPanel.SetActive(false);
    }
}
