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
    public Button generateQRButton;

    // UI references for QR popup
    public GameObject qrPopupPanel;
    public Image qrImageDisplay;
    public Button shareButton;
    public Button closeButton;

    private EnvironmentData environmentData;
    private string currentModuleName;
    private string currentModulePath;

    // Track if QR has been generated
    private bool qrGenerated = false;
    private string generatedQRPath = "";

    void Start()
    {
        saveButton.onClick.AddListener(SaveModuleData);

        string newModuleName = PlayerPrefs.GetString("NewModuleName", "");
        string selectedModulePath = PlayerPrefs.GetString("SelectedModulePath", "");

        // Handle New Module
        if (!string.IsNullOrEmpty(newModuleName) && string.IsNullOrEmpty(selectedModulePath))
        {
            generateQRButton.gameObject.SetActive(false);

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

        // Handle Existing Module
        else if (!string.IsNullOrEmpty(selectedModulePath) && File.Exists(selectedModulePath))
        {
            string json = ModuleSaveManager.LoadModule(selectedModulePath);
            environmentData = JsonUtility.FromJson<EnvironmentData>(json);
            currentModuleName = environmentData.environmentName;
            currentModulePath = selectedModulePath;

            PlayerPrefs.SetString(currentModuleName, json);
            PlayerPrefs.SetString("SelectedEnvironmentKey", currentModuleName);
            PlayerPrefs.Save();

            generateQRButton.gameObject.SetActive(true);

            //Check if QR already exists
            string folder = Path.Combine(Application.persistentDataPath, "QRCodeExports");
            string qrFilePath = Path.Combine(folder, currentModuleName + "_QR.png");

            if (File.Exists(qrFilePath))
            {
                qrGenerated = true;
                generatedQRPath = qrFilePath;
                generateQRButton.GetComponentInChildren<Text>().text = "View Generated QR"; //Updated button text
            }
            else
            {
                generateQRButton.GetComponentInChildren<Text>().text = "Generate QR";
            }

            //Listener logic
            generateQRButton.onClick.RemoveAllListeners();
            generateQRButton.onClick.AddListener(() =>
            {
                if (qrGenerated && File.Exists(generatedQRPath))
                {
                    ShowQRPopup(); //Only show popup when user clicks "View Generated QR"
                    return;
                }

                // Generate new QR
                string moduleJson = JsonUtility.ToJson(environmentData);
                string savedPath = QRCodeGenerator.GenerateAndSaveQRCode(moduleJson, currentModuleName);

                if (!string.IsNullOrEmpty(savedPath))
                {
                    qrGenerated = true;
                    generatedQRPath = savedPath;
                    generateQRButton.GetComponentInChildren<Text>().text = "View Generated QR"; //Change button text after generation
                }
            });
        }
        else
        {
            Debug.LogError("No valid module found.");
            generateQRButton.gameObject.SetActive(false);
            return;
        }

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

    // Show QR popup
    void ShowQRPopup()
    {
        if (!File.Exists(generatedQRPath))
        {
            Debug.LogWarning("QR file not found at: " + generatedQRPath);
            return;
        }

        byte[] imageData = File.ReadAllBytes(generatedQRPath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageData);

        qrImageDisplay.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        qrPopupPanel.SetActive(true);

        shareButton.onClick.RemoveAllListeners();
        shareButton.onClick.AddListener(() => ShareImage(generatedQRPath));

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => qrPopupPanel.SetActive(false));
    }

    // Share QR using NativeShare
    void ShareImage(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Debug.LogError("File to share not found.");
            return;
        }

        new NativeShare()
            .AddFile(filePath)
            .SetSubject("Check out this AR Module QR!")
            .SetText("Scan this QR to load a MarineAR module!")
            .SetTitle("Share QR Code")
            .Share();
    }
}
