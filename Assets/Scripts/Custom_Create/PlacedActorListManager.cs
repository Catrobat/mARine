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

    public GameObject qrPopupPanel;
    public Image qrImageDisplay;
    public Button shareButton;
    public Button closeButton;

    private EnvironmentData environmentData;
    private string currentModuleName;
    private string currentModulePath;

    private bool qrGenerated = false;
    private string generatedQRPath = "";

    void Start()
    {
        saveButton.onClick.AddListener(SaveModuleData);

        string newModuleName = PlayerPrefs.GetString("NewModuleName", "");
        string selectedModulePath = PlayerPrefs.GetString("SelectedModulePath", "");

        if (!string.IsNullOrEmpty(newModuleName) && string.IsNullOrEmpty(selectedModulePath))
        {
            generateQRButton.gameObject.SetActive(false);

            string json = PlayerPrefs.GetString(newModuleName, "");
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogError("Environment data not found: " + newModuleName);
                return;
            }

            environmentData = JsonUtility.FromJson<EnvironmentData>(json);
            currentModuleName = newModuleName;
            currentModulePath = ModuleSaveManager.GetModulePath(currentModuleName);

            environmentData.MigrateFromLegacy();

            // Assign unique IDs across all layers
            int total = environmentData.layerCount > 0 ? environmentData.layerCount : 5;
            for (int i = 0; i < total; i++)
            {
                foreach (var actor in environmentData.GetLayerActors(i))
                {
                    if (string.IsNullOrEmpty(actor.uniqueID))
                        actor.uniqueID = System.Guid.NewGuid().ToString();
                }
            }

            string updatedJson = JsonUtility.ToJson(environmentData);
            PlayerPrefs.SetString(newModuleName, updatedJson);
        }
        else if (!string.IsNullOrEmpty(selectedModulePath) && File.Exists(selectedModulePath))
        {
            string json = ModuleSaveManager.LoadModule(selectedModulePath);
            environmentData = JsonUtility.FromJson<EnvironmentData>(json);
            currentModuleName = environmentData.environmentName;
            currentModulePath = selectedModulePath;

            environmentData.MigrateFromLegacy();

            PlayerPrefs.SetString(currentModuleName, json);
            PlayerPrefs.SetString("SelectedEnvironmentKey", currentModuleName);
            PlayerPrefs.Save();

            generateQRButton.gameObject.SetActive(true);

            string folder = Path.Combine(Application.persistentDataPath, "QRCodeExports");
            string qrFilePath = Path.Combine(folder, currentModuleName + "_QR.png");

            if (File.Exists(qrFilePath))
            {
                qrGenerated = true;
                generatedQRPath = qrFilePath;
                generateQRButton.GetComponentInChildren<Text>().text = "View Generated QR";
            }
            else
            {
                generateQRButton.GetComponentInChildren<Text>().text = "Generate QR";
            }

            generateQRButton.onClick.RemoveAllListeners();
            generateQRButton.onClick.AddListener(() =>
            {
                if (qrGenerated && File.Exists(generatedQRPath))
                {
                    ShowQRPopup();
                    return;
                }

                string moduleJson = JsonUtility.ToJson(environmentData);
                string savedPath = QRCodeGenerator.GenerateAndSaveQRCode(
                    moduleJson, currentModuleName);

                if (!string.IsNullOrEmpty(savedPath))
                {
                    qrGenerated = true;
                    generatedQRPath = savedPath;
                    generateQRButton.GetComponentInChildren<Text>().text = "View Generated QR";
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

        // Get all actors across all layers
        List<PlacedActorData> allActors = EnvironmentDataCache.GetAllActors();

        if (allActors == null || allActors.Count == 0)
        {
            Debug.Log("No actors found.");
            return;
        }

        int layerTotal = environmentData.layerCount > 0 ? environmentData.layerCount : 3;
        Debug.Log($"Module: {currentModuleName} — {layerTotal} layers — " +
                  $"Total actors: {allActors.Count}");

        for (int i = 0; i < allActors.Count; i++)
        {
            PlacedActorData actorData = allActors[i];
            GameObject listItem = Instantiate(listItemPrefab, contentParent);
            listItem.transform.localScale = Vector3.one;

            Text actorNameText = listItem.transform.Find("ActorNameText").GetComponent<Text>();
            Button selectButton = listItem.transform.Find("SelectButton").GetComponent<Button>();

            // Show name and layer
            actorNameText.text = $"{actorData.prefabName} (Layer {actorData.layerIndex})";

            int capturedIndex = i;
            string selectedPrefabName = actorData.prefabName;
            string capturedUniqueID = actorData.uniqueID;
            int capturedLayerIndex = actorData.layerIndex;

            selectButton.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString("SelectedActorName", selectedPrefabName);
                PlayerPrefs.SetInt("SelectedActorIndex", capturedIndex);
                PlayerPrefs.SetString("SelectedActorUniqueID", capturedUniqueID);
                PlayerPrefs.SetInt("SelectedActorLayerIndex", capturedLayerIndex);
                PlayerPrefs.SetString("SelectedEnvironmentKey", currentModuleName);
                PlayerPrefs.SetInt("IsMainPlayer", 0);

                Debug.Log($"Selected: {selectedPrefabName} " +
                          $"Layer:{capturedLayerIndex} ID:{capturedUniqueID}");

                SceneManager.LoadScene("BehaviorScene");
            });
        }
    }

    void SaveModuleData()
    {
        if (string.IsNullOrEmpty(currentModuleName) ||
            EnvironmentDataCache.currentData == null)
        {
            Debug.LogWarning("No module to save.");
            return;
        }

        string json = JsonUtility.ToJson(EnvironmentDataCache.currentData);
        ModuleSaveManager.SaveModule(currentModuleName, json);

        PlayerPrefs.SetString("LastSavedEnvironment", currentModuleName);
        PlayerPrefs.SetString(currentModuleName, json);
        PlayerPrefs.SetString("SelectedModulePath",
            ModuleSaveManager.GetModulePath(currentModuleName));
        PlayerPrefs.Save();

        int total = EnvironmentDataCache.currentData.layerCount > 0
            ? EnvironmentDataCache.currentData.layerCount : 3;

        string log = $"Module '{currentModuleName}' saved — ";
        for (int i = 0; i < total; i++)
            log += $"L{i}:{EnvironmentDataCache.currentData.GetLayerActors(i).Count} ";
        Debug.Log(log);

        SceneManager.LoadScene("StartScreenScene");
    }

    void ShowQRPopup()
    {
        if (!File.Exists(generatedQRPath))
        {
            Debug.LogWarning("QR file not found: " + generatedQRPath);
            return;
        }

        byte[] imageData = File.ReadAllBytes(generatedQRPath);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(imageData);

        qrImageDisplay.sprite = Sprite.Create(
            tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f)
        );

        qrPopupPanel.SetActive(true);

        shareButton.onClick.RemoveAllListeners();
        shareButton.onClick.AddListener(() => ShareImage(generatedQRPath));

        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => qrPopupPanel.SetActive(false));
    }

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