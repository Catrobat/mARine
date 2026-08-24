
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;


public class AddScriptsSceneManager : MonoBehaviour
{
    [Header("References")]
    public Transform scriptListContainer;
    public GameObject scriptblockbuttonPrefab;
    public FoodChainConfig foodChainConfig;     // assign in Inspector

    // Tier-based behaviour names — these are the strings saved in addedScripts
    // and read by ARPlacementController to attach components
    private const string BEHAVIOUR_HUNT = "Hunt Prey";
    private const string BEHAVIOUR_FLEE = "Flee Predators";

    private EnvironmentData environmentData;
    private PlacedActorData currentActor;
    private List<PlacedActorData> allActors;
    private string selectedEnvKey;

    void Start()
    {
        selectedEnvKey = PlayerPrefs.GetString("SelectedEnvironmentKey", "");
        if (string.IsNullOrEmpty(selectedEnvKey))
        {
            Debug.LogError("[AddScripts] Missing environment key!");
            return;
        }

        string json = PlayerPrefs.GetString(selectedEnvKey, "");
        environmentData = JsonUtility.FromJson<EnvironmentData>(json);
        if (environmentData == null)
        {
            Debug.LogError("[AddScripts] Invalid environment data!");
            return;
        }

        environmentData.MigrateFromLegacy();

        string currentActorUniqueID = PlayerPrefs.GetString("SelectedActorUniqueID", "");
        if (string.IsNullOrEmpty(currentActorUniqueID))
        {
            Debug.LogError("[AddScripts] Missing selected actor unique ID!");
            return;
        }

        // Find current actor across all layers
        int total = environmentData.layerCount > 0 ? environmentData.layerCount : 5;
        for (int i = 0; i < total; i++)
        {
            currentActor = environmentData.GetLayerActors(i)
                                          .Find(a => a.uniqueID == currentActorUniqueID);
            if (currentActor != null) break;
        }

        if (currentActor == null)
        {
            Debug.LogError($"[AddScripts] Actor '{currentActorUniqueID}' not found!");
            return;
        }

        // Collect all other actors (used for legacy food target dropdown fallback)
        allActors = new List<PlacedActorData>();
        for (int i = 0; i < total; i++)
            allActors.AddRange(environmentData.GetLayerActors(i));

        Debug.Log($"[AddScripts] Actor: {currentActor.prefabName} " +
                  $"Tier: {currentActor.foodChainTier} Layer: {currentActor.layerIndex}");

        BuildBehaviourList();
    }

    void BuildBehaviourList()
    {
        List<string> availableBehaviours = new List<string>();

        int myTier = currentActor.foodChainTier;

        if (foodChainConfig != null)
        {
            // Can this actor hunt anything in the current environment?
            bool canHunt = allActors.Exists(a =>
                a.uniqueID != currentActor.uniqueID &&
                foodChainConfig.ShouldHunt(myTier, a.foodChainTier));

            // Can this actor be hunted by anything?
            bool canFlee = allActors.Exists(a =>
                a.uniqueID != currentActor.uniqueID &&
                foodChainConfig.ShouldFlee(myTier, a.foodChainTier));

            if (canHunt) availableBehaviours.Add(BEHAVIOUR_HUNT);
            if (canFlee) availableBehaviours.Add(BEHAVIOUR_FLEE);

            // Add species-specific additional behaviours
            List<string> extras = foodChainConfig.GetAdditionalBehaviours(currentActor.prefabName);
            foreach (string extra in extras)
            {
                if (!availableBehaviours.Contains(extra))
                    availableBehaviours.Add(extra);
            }
        }
        else
        {
            // Fallback if no config assigned: show legacy Food Consumption option
            Debug.LogWarning("[AddScripts] FoodChainConfig not assigned. " +
                             "Falling back to legacy 'Food Consumption' behaviour.");
            availableBehaviours.Add("Food Consumption");
        }

        if (availableBehaviours.Count == 0)
        {
            Debug.Log("[AddScripts] No behaviours available for this actor's tier.");
            // Optionally show a "No behaviours available" message in UI here
            return;
        }

        foreach (string behaviourName in availableBehaviours)
            SpawnBehaviourButton(behaviourName);
    }

    void SpawnBehaviourButton(string behaviourName)
    {
        GameObject buttonObj = Instantiate(scriptblockbuttonPrefab, scriptListContainer);

        Text label = buttonObj.GetComponentInChildren<Text>();
        if (label != null) label.text = behaviourName;

        // Legacy: Food Consumption still shows the target dropdown
        if (behaviourName == "Food Consumption")
        {
            Dropdown dropdown = buttonObj.GetComponentInChildren<Dropdown>(true);
            if (dropdown != null)
                SetupFoodConsumptionDropdown(dropdown);
        }
        // Tier-based behaviours need no dropdown — target is resolved at runtime
        // by FoodConsumer/FleeFromPredator via OverlapSphere

        Button btn = buttonObj.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString("PendingScriptToAdd", behaviourName);
                PlayerPrefs.Save();
                SceneManager.LoadScene("BehaviorScene");
            });
        }
        else
        {
            Debug.LogError("[AddScripts] No Button on scriptblockbuttonPrefab!");
        }
    }

 
    void SetupFoodConsumptionDropdown(Dropdown dropdown)
    {
        dropdown.gameObject.SetActive(true);
        dropdown.ClearOptions();

        List<string> actorDisplayNames = new List<string>();
        Dictionary<string, string> displayToIdMap = new Dictionary<string, string>();
        Dictionary<string, int> prefabCounts = new Dictionary<string, int>();

        foreach (var actor in allActors)
        {
            if (actor.uniqueID == currentActor.uniqueID) continue;

            if (!prefabCounts.ContainsKey(actor.prefabName))
                prefabCounts[actor.prefabName] = 1;
            else
                prefabCounts[actor.prefabName]++;

            string displayName =
                $"{actor.prefabName} ({prefabCounts[actor.prefabName]}) [L{actor.layerIndex}]";
            actorDisplayNames.Add(displayName);
            displayToIdMap[displayName] = actor.uniqueID;
        }

        dropdown.AddOptions(actorDisplayNames);

        if (actorDisplayNames.Count > 0)
        {
            string defaultID = displayToIdMap[actorDisplayNames[0]];
            PlayerPrefs.SetString("PendingFoodTargetID", defaultID);
            PlayerPrefs.Save();
        }

        dropdown.onValueChanged.AddListener((int index) =>
        {
            string selectedID = displayToIdMap[actorDisplayNames[index]];
            PlayerPrefs.SetString("PendingFoodTargetID", selectedID);
            PlayerPrefs.Save();
        });
    }
}