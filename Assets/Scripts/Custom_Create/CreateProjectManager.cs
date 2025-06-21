using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class CreateProjectManager : MonoBehaviour
{
    [System.Serializable]
    public class EnvironmentOption
    {
        public string name;                 // Display name + prefab name
        public GameObject prefab;          // Assign this in the Inspector
    }

    [Header("UI References")]
    public InputField moduleNameInput;
    public Transform environmentListContainer;
    public GameObject environmentButtonPrefab;
    public Button nextButton;

    [Header("Environment Options")]
    public List<EnvironmentOption> environmentOptions; // Fill in Inspector

    private string selectedEnvironmentName;

    void Start()
    {
        PopulateEnvironmentButtons();
        nextButton.onClick.AddListener(OnNextClicked);
    }

    void PopulateEnvironmentButtons()
    {
        foreach (Transform child in environmentListContainer)
            Destroy(child.gameObject);

        foreach (EnvironmentOption env in environmentOptions)
        {
            GameObject btnObj = Instantiate(environmentButtonPrefab, environmentListContainer);
            EnvironmentButton envBtn = btnObj.GetComponent<EnvironmentButton>();
            envBtn.SetData(env.name, OnEnvironmentSelected, false);
        }
    }

    void OnEnvironmentSelected(string selectedName)
    {
        selectedEnvironmentName = selectedName;

        // Update button visuals
        foreach (Transform child in environmentListContainer)
        {
            EnvironmentButton btn = child.GetComponent<EnvironmentButton>();
            btn.background.color = (btn.prefabName == selectedName) ? Color.green : Color.white;
        }
    }

    void OnNextClicked()
    {
        string moduleName = moduleNameInput.text.Trim();

        if (string.IsNullOrEmpty(moduleName))
        {
            Debug.LogWarning("Module name is required.");
            return;
        }

        if (string.IsNullOrEmpty(selectedEnvironmentName))
        {
            Debug.LogWarning("Environment must be selected.");
            return;
        }

        // Find selected EnvironmentOption to get actual prefab name
        EnvironmentOption selectedOption = environmentOptions.Find(env => env.name == selectedEnvironmentName);
        if (selectedOption == null || selectedOption.prefab == null)
        {
            Debug.LogWarning("Selected environment prefab not found.");
            return;
        }

        string actualPrefabName = selectedOption.prefab.name;

        PlayerPrefs.SetString("NewModuleName", moduleName);
        PlayerPrefs.SetString("SelectedEnvironmentPrefabName", actualPrefabName); // Save actual prefab name
        PlayerPrefs.Save();

        SceneManager.LoadScene("TestScene2");
    }


    // Used from other scripts to get prefab by name
    public GameObject GetEnvironmentPrefabByName(string name)
    {
        foreach (var option in environmentOptions)
        {
            if (option.name == name)
                return option.prefab;
        }
        return null;
    }
}
