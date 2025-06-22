using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class AddScriptsSceneManager : MonoBehaviour
{
    public Transform scriptListContainer;      // Assign: ScriptList GameObject
    public GameObject scriptblockbuttonPrefab; // Assign: Prefab with Text, Dropdown (optional), Button

    private List<string> availableScripts = new List<string>
    {
        "Food Consumption"
    };

    void Start()
    {
        string selectedEnvKey = PlayerPrefs.GetString("SelectedEnvironmentKey", "");
        if (string.IsNullOrEmpty(selectedEnvKey)) { Debug.LogError("Missing environment key!"); return; }

        string json = PlayerPrefs.GetString(selectedEnvKey, "");
        EnvironmentData environmentData = JsonUtility.FromJson<EnvironmentData>(json);
        if (environmentData == null || environmentData.placedActors == null) { Debug.LogError("Invalid environment data!"); return; }

        int actorIndex = PlayerPrefs.GetInt("SelectedActorIndex", -1);
        if (actorIndex < 0 || actorIndex >= environmentData.placedActors.Count) { Debug.LogError("Invalid actor index!"); return; }

        string currentActorId = environmentData.placedActors[actorIndex].uniqueID;

        foreach (string scriptName in availableScripts)
        {
            GameObject buttonObj = Instantiate(scriptblockbuttonPrefab, scriptListContainer);

            // Set the label
            Text label = buttonObj.GetComponentInChildren<Text>();
            if (label != null) label.text = scriptName;

            Dropdown dropdown = buttonObj.GetComponentInChildren<Dropdown>(true); // Even if disabled

            if (scriptName == "Food Consumption" && dropdown != null)
            {
                dropdown.gameObject.SetActive(true);

                dropdown.ClearOptions();
                List<string> actorDisplayNames = new List<string>();
                Dictionary<string, string> displayNameToIdMap = new Dictionary<string, string>();
                Dictionary<string, int> prefabCounts = new Dictionary<string, int>();

                foreach (var actor in environmentData.placedActors)
                {
                    if (actor.uniqueID == currentActorId) continue;

                    if (!prefabCounts.ContainsKey(actor.prefabName))
                        prefabCounts[actor.prefabName] = 1;
                    else
                        prefabCounts[actor.prefabName]++;

                    string displayName = $"{actor.prefabName} ({prefabCounts[actor.prefabName]})";
                    actorDisplayNames.Add(displayName);
                    displayNameToIdMap[displayName] = actor.uniqueID;
                }

                dropdown.AddOptions(actorDisplayNames);

                // Automatically store the first (default) option
                if (actorDisplayNames.Count > 0)
                {
                    string defaultName = actorDisplayNames[0];
                    string defaultID = displayNameToIdMap[defaultName];
                    PlayerPrefs.SetString("PendingFoodTargetID", defaultID);
                    PlayerPrefs.Save();
                    Debug.Log($"Default food target saved: {defaultName} => {defaultID}");
                }

                // Store new selection on change
                dropdown.onValueChanged.AddListener((int index) =>
                {
                    string selectedName = actorDisplayNames[index];
                    string selectedID = displayNameToIdMap[selectedName];
                    PlayerPrefs.SetString("PendingFoodTargetID", selectedID);
                    PlayerPrefs.Save();
                    Debug.Log($"PendingFoodTargetID set to: {selectedName} => {selectedID}");
                });
            }

            // Button logic to set pending script and load scene
            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    PlayerPrefs.SetString("PendingScriptToAdd", scriptName);
                    PlayerPrefs.Save();
                    SceneManager.LoadScene("BehaviorScene");
                });
            }
            else
            {
                Debug.LogError("No Button component found in scriptblockbuttonPrefab!");
            }
        }
    }
}
