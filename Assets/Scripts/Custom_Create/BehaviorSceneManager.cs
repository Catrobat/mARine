
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BehaviorSceneManager : MonoBehaviour
{
    public Toggle mainPlayerToggle;
    public Transform addedScriptsPanel;
    public GameObject scriptblockbutton;

    private string selectedActorUniqueID;
    private int selectedActorLayerIndex;
    private string selectedEnvKey;
    private EnvironmentData environmentData;
    private PlacedActorData selectedActor;

    void Start()
    {
        selectedActorUniqueID = PlayerPrefs.GetString("SelectedActorUniqueID", "");
        selectedActorLayerIndex = PlayerPrefs.GetInt("SelectedActorLayerIndex", 0);
        selectedEnvKey = PlayerPrefs.GetString("SelectedEnvironmentKey", "");

        if (string.IsNullOrEmpty(selectedActorUniqueID) ||
            string.IsNullOrEmpty(selectedEnvKey))
        {
            Debug.LogError("Missing actor unique ID or environment key.");
            return;
        }

        string json = PlayerPrefs.GetString(selectedEnvKey, "");
        environmentData = JsonUtility.FromJson<EnvironmentData>(json);

        if (environmentData == null)
        {
            Debug.LogError("Invalid environment data.");
            return;
        }

        environmentData.MigrateFromLegacy();

        selectedActor = FindActorByUniqueID(selectedActorUniqueID);

        if (selectedActor == null)
        {
            Debug.LogError($"Actor '{selectedActorUniqueID}' not found in any layer.");
            return;
        }

        Debug.Log($"Loaded actor: {selectedActor.prefabName} " +
                  $"on Layer {selectedActor.layerIndex}");

        mainPlayerToggle.isOn = selectedActor.isMainPlayer;
        mainPlayerToggle.onValueChanged.AddListener(OnMainPlayerToggleChanged);

        string pendingScript = PlayerPrefs.GetString("PendingScriptToAdd", "");
        if (!string.IsNullOrEmpty(pendingScript))
        {
            AddScriptToActor(pendingScript);

            if (pendingScript == "Food Consumption")
            {
                string pendingFoodTargetID = PlayerPrefs.GetString("PendingFoodTargetID", "");
                if (!string.IsNullOrEmpty(pendingFoodTargetID))
                {
                    PlacedActorData target = FindActorByUniqueID(pendingFoodTargetID);
                    if (target != null)
                    {
                        selectedActor.foodTargetUniqueID = pendingFoodTargetID;
                        Debug.Log($"Food target: {target.prefabName} " +
                                  $"(ID: {pendingFoodTargetID})");
                        SaveEnvironment();
                    }
                    else
                    {
                        Debug.LogWarning($"Food target not found: {pendingFoodTargetID}");
                    }

                    PlayerPrefs.DeleteKey("PendingFoodTargetID");
                }
            }

            PlayerPrefs.DeleteKey("PendingScriptToAdd");
        }

        RefreshScriptDisplay();
    }

    PlacedActorData FindActorByUniqueID(string uniqueID)
    {
        if (environmentData == null) return null;

        int total = environmentData.layerCount > 0 ? environmentData.layerCount : 5;
        for (int i = 0; i < total; i++)
        {
            var found = environmentData.GetLayerActors(i)
                                       .Find(a => a.uniqueID == uniqueID);
            if (found != null) return found;
        }

        return null;
    }

    void OnMainPlayerToggleChanged(bool isOn)
    {
        // Clear main player across all layers
        int total = environmentData.layerCount > 0 ? environmentData.layerCount : 5;
        for (int i = 0; i < total; i++)
        {
            foreach (var actor in environmentData.GetLayerActors(i))
                actor.isMainPlayer = false;
        }

        if (isOn)
        {
            selectedActor.isMainPlayer = true;
            Debug.Log($"Main player: {selectedActor.prefabName}");
        }

        SaveEnvironment();
    }

    void AddScriptToActor(string scriptName)
    {
        if (selectedActor.addedScripts == null)
            selectedActor.addedScripts = new List<string>();

        if (!selectedActor.addedScripts.Contains(scriptName))
        {
            selectedActor.addedScripts.Add(scriptName);
            Debug.Log($"Script '{scriptName}' added to {selectedActor.prefabName}");
            SaveEnvironment();
        }
    }

    void RefreshScriptDisplay()
    {
        foreach (Transform child in addedScriptsPanel)
            Destroy(child.gameObject);

        if (selectedActor.addedScripts == null) return;

        foreach (string scriptName in selectedActor.addedScripts)
        {
            GameObject scriptVisual = Instantiate(scriptblockbutton, addedScriptsPanel);
            Text txt = scriptVisual.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = scriptName;
            else
                Debug.LogError("Text not found in scriptblockbutton prefab!");
        }
    }

    void SaveEnvironment()
    {
        string updatedJson = JsonUtility.ToJson(environmentData);

        // Save to PlayerPrefs (fast in-memory access)
        PlayerPrefs.SetString(selectedEnvKey, updatedJson);
        PlayerPrefs.Save();

        // Also save to file so PlacedActorListManager doesn't overwrite
        // PlayerPrefs with stale data from disk on next load
        ModuleSaveManager.SaveModule(selectedEnvKey, updatedJson);

        Debug.Log($"[BehaviorScene] Saved environment '{selectedEnvKey}' " +
                  $"with {environmentData.GetLayerActors(0).Count + environmentData.GetLayerActors(1).Count + environmentData.GetLayerActors(2).Count} actors.");
    }

    public void OnAddScriptsButtonPressed()
    {
        SceneManager.LoadScene("AddScriptsScene");
    }
}