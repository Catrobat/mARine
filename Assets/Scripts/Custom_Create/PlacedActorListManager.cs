using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

public class PlacedActorListManager : MonoBehaviour
{
    public Transform contentParent;
    public GameObject listItemPrefab;
    public Button saveButton;
    public Button generateQRButton; // Assign this in the Inspector

    private EnvironmentData environmentData;
    private string currentModuleName;
    private string currentModulePath;

    void Start()
    {
        saveButton.onClick.AddListener(SaveModuleData);

        string newModuleName = PlayerPrefs.GetString("NewModuleName", "");
        string selectedModulePath = PlayerPrefs.GetString("SelectedModulePath", "");

        // Case 1: New module being created
        if (!string.IsNullOrEmpty(newModuleName) && string.IsNullOrEmpty(selectedModulePath))
        {
            generateQRButton.gameObject.SetActive(false); // Hide QR for new modules

            string json = PlayerPrefs.GetString(newModuleName, "");
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Environment data not found for new module: " + newModuleName);
                return;
            }

            environmentData = JsonUtility.FromJson<EnvironmentData>(json);
            currentModuleName = newModuleName;
            currentModulePath = ModuleSaveManager.GetModulePath(currentModuleName);

            foreach (var actor in environmentData.placedActors)
            {
                if (string.IsNullOrEmpty(actor.uniqueID))
                    actor.uniqueID = System.Guid.NewGuid().ToString();
            }

            string updatedJson = JsonUtility.ToJson(environmentData);
            PlayerPrefs.SetString(newModuleName, updatedJson);
        }
        // Case 2: Existing saved module is opened
        else if (!string.IsNullOrEmpty(selectedModulePath) && File.Exists(selectedModulePath))
        {
            string json = ModuleSaveManager.LoadModule(selectedModulePath);
            environmentData = JsonUtility.FromJson<EnvironmentData>(json);
            currentModuleName = environmentData.environmentName;
            currentModulePath = selectedModulePath;

            PlayerPrefs.SetString(currentModuleName, json);
            PlayerPrefs.SetString("SelectedEnvironmentKey", currentModuleName);
            PlayerPrefs.Save();

            generateQRButton.gameObject.SetActive(true); // Show QR button for existing modules

            generateQRButton.onClick.AddListener(() =>
            {
                string folder = Path.Combine(Application.persistentDataPath, "QRCodeExports");
                string qrFilePath = Path.Combine(folder, currentModuleName + "_QR.png");

                if (File.Exists(qrFilePath))
                {
                    Debug.Log($"QR already exists at: {qrFilePath}");
                    return;
                }

                string moduleJson = JsonUtility.ToJson(environmentData);
                string savedPath = QRCodeGenerator.GenerateAndSaveQRCode(moduleJson, currentModuleName);

                if (!string.IsNullOrEmpty(savedPath))
                {
                    Debug.Log($"QR code generated at: {savedPath}");
                }
            });
        }
        else
        {
            Debug.LogError("No valid module found.");
            generateQRButton.gameObject.SetActive(false); // Hide by default
            return;
        }

        // Store in cache for edit/delete mode
        EnvironmentDataCache.SetData(environmentData);

        if (environmentData == null || environmentData.placedActors == null || environmentData.placedActors.Count == 0)
        {
            Debug.Log("No actors found.");
            return;
        }

        Debug.Log($"Loaded Module: {currentModuleName}, Total actors: {environmentData.placedActors.Count}");

        for (int i = 0; i < environmentData.placedActors.Count; i++)
        {
            PlacedActorData actorData = environmentData.placedActors[i];
            GameObject listItem = Instantiate(listItemPrefab, contentParent);
            listItem.transform.localScale = Vector3.one;

            Text actorNameText = listItem.transform.Find("ActorNameText").GetComponent<Text>();
            Button selectButton = listItem.transform.Find("SelectButton").GetComponent<Button>();

            actorNameText.text = actorData.prefabName;

            int capturedIndex = i;
            string selectedPrefabName = actorData.prefabName;

            selectButton.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString("SelectedActorName", selectedPrefabName);
                PlayerPrefs.SetInt("SelectedActorIndex", capturedIndex);
                PlayerPrefs.SetString("SelectedEnvironmentKey", currentModuleName);
                PlayerPrefs.SetInt("IsMainPlayer", 0);

                Debug.Log($"Selected Actor: {selectedPrefabName} (Index: {capturedIndex}), Env: {currentModuleName}");
                SceneManager.LoadScene("BehaviorScene");
            });
        }
    }

    void SaveModuleData()
    {
        if (string.IsNullOrEmpty(currentModuleName) || EnvironmentDataCache.currentData == null)
        {
            Debug.LogWarning("No module to save.");
            return;
        }

        string json = JsonUtility.ToJson(EnvironmentDataCache.currentData);
        ModuleSaveManager.SaveModule(currentModuleName, json);

        PlayerPrefs.SetString("LastSavedEnvironment", currentModuleName);
        PlayerPrefs.SetString(currentModuleName, json);
        PlayerPrefs.SetString("SelectedModulePath", ModuleSaveManager.GetModulePath(currentModuleName));
        PlayerPrefs.Save();

        Debug.Log($"Module '{currentModuleName}' saved to disk.");
        SceneManager.LoadScene("StartScreenScene");
    }
}
