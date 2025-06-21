using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class BehaviorSceneManager : MonoBehaviour
{
    public Toggle mainPlayerToggle;
    public Transform addedScriptsPanel;
    public GameObject scriptblockbutton;

    private int selectedActorIndex;
    private string selectedEnvKey;
    private EnvironmentData environmentData;

    void Start()
    {
        selectedActorIndex = PlayerPrefs.GetInt("SelectedActorIndex", -1);
        selectedEnvKey = PlayerPrefs.GetString("SelectedEnvironmentKey", "");

        if (selectedActorIndex < 0 || string.IsNullOrEmpty(selectedEnvKey))
        {
            Debug.LogError("Missing actor index or environment key.");
            return;
        }

        string json = PlayerPrefs.GetString(selectedEnvKey, "");
        environmentData = JsonUtility.FromJson<EnvironmentData>(json);

        if (environmentData == null || selectedActorIndex >= environmentData.placedActors.Count)
        {
            Debug.LogError("Invalid environment data or actor index.");
            return;
        }

        PlacedActorData actor = environmentData.placedActors[selectedActorIndex];
        mainPlayerToggle.isOn = actor.isMainPlayer;
        mainPlayerToggle.onValueChanged.AddListener(OnMainPlayerToggleChanged);

        // Handle script just added
        string pendingScript = PlayerPrefs.GetString("PendingScriptToAdd", "");
        if (!string.IsNullOrEmpty(pendingScript))
        {
            AddScriptToActor(pendingScript);

            if (pendingScript == "Food Consumption")
            {
                string pendingFoodTargetID = PlayerPrefs.GetString("PendingFoodTargetID", "");

                if (!string.IsNullOrEmpty(pendingFoodTargetID))
                {
                    var target = environmentData.placedActors.Find(a => a.uniqueID == pendingFoodTargetID);
                    if (target != null)
                    {
                        actor.foodTargetUniqueID = pendingFoodTargetID;

                        Debug.Log($"Food target assigned: {target.prefabName} (ID: {pendingFoodTargetID})");
                        SaveEnvironment();
                    }
                    else
                    {
                        Debug.LogWarning($"No actor found with uniqueID: {pendingFoodTargetID}");
                    }

                    PlayerPrefs.DeleteKey("PendingFoodTargetID");
                }
            }

            PlayerPrefs.DeleteKey("PendingScriptToAdd");
        }

        RefreshScriptDisplay();
    }

    void OnMainPlayerToggleChanged(bool isOn)
    {
        for (int i = 0; i < environmentData.placedActors.Count; i++)
        {
            environmentData.placedActors[i].isMainPlayer = false;
        }

        if (isOn)
        {
            environmentData.placedActors[selectedActorIndex].isMainPlayer = true;
            Debug.Log($"Actor '{environmentData.placedActors[selectedActorIndex].prefabName}' set as Main Player.");
        }
        else
        {
            Debug.Log("No main player selected.");
        }

        SaveEnvironment();
    }

    void AddScriptToActor(string scriptName)
    {
        PlacedActorData actor = environmentData.placedActors[selectedActorIndex];

        if (actor.addedScripts == null)
            actor.addedScripts = new List<string>();

        if (!actor.addedScripts.Contains(scriptName))
        {
            actor.addedScripts.Add(scriptName);
            Debug.Log($"Script '{scriptName}' added to actor: {actor.prefabName} (ID: {actor.uniqueID})");
            SaveEnvironment();
        }
    }

    void RefreshScriptDisplay()
    {
        foreach (Transform child in addedScriptsPanel)
        {
            Destroy(child.gameObject);
        }

        PlacedActorData actor = environmentData.placedActors[selectedActorIndex];

        if (actor.addedScripts == null) return;

        foreach (string scriptName in actor.addedScripts)
        {
            GameObject scriptVisual = Instantiate(scriptblockbutton, addedScriptsPanel);
            Debug.Log($"Instantiated visual for script: {scriptName}");

            Text txt = scriptVisual.GetComponentInChildren<Text>();
            if (txt != null)
                txt.text = scriptName;
            else
                Debug.LogError("Text component not found in scriptblockbutton prefab!");
        }
    }

    void SaveEnvironment()
    {
        string updatedJson = JsonUtility.ToJson(environmentData);
        PlayerPrefs.SetString(selectedEnvKey, updatedJson);
        PlayerPrefs.Save();
    }

    public void OnAddScriptsButtonPressed()
    {
        SceneManager.LoadScene("AddScriptsScene");
    }
}
