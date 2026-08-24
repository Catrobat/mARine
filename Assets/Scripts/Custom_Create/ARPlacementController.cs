using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementController : MonoBehaviour
{
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public ARPlaneManager planeManager;

    [Header("Environment Prefabs")]
    public ActorDatabase actorDatabase;

    [Header("UI")]
    public FloatingJoystick joystick;
    public GameObject joystickUIRoot;

    [Header("Layer Settings")]
    public SandboxSettings settings;

    [Header("Ability System")]
    [Tooltip("ScriptableObject mapping prefab names to their abilities")]
    public ActorAbilityConfig abilityConfig;

    [Tooltip("The panel that shows ability buttons in the AR scene")]
    public AbilityUIPanel abilityUIPanel;

    [Header("Food Chain")]
    [Tooltip("ScriptableObject defining species tiers and behaviour rules")]
    public FoodChainConfig foodChainConfig;

    [Tooltip("Move speed for hunting actors")]
    public float huntMoveSpeed = 1.5f;

    [Tooltip("Move speed for fleeing actors")]
    public float fleeMoveSpeed = 2.5f;

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool placed = false;

    void Start()
    {
        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(false);

        // Hide ability panel until a main player with abilities is spawned
        if (abilityUIPanel != null)
            abilityUIPanel.gameObject.SetActive(false);
    }

    void Update()
    {
        if (placed) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
            TryPlaceAt(Input.mousePosition);
#else
        if (Input.touchCount > 0 &&
            Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
            TryPlaceAt(Input.GetTouch(0).position);
#endif
    }

    private void TryPlaceAt(Vector2 screenPosition)
    {
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
        {
            Pose pose = hits[0].pose;
            PlaceSavedEnvironment(pose.position);
            placed = true;
        }
    }

    private void PlaceSavedEnvironment(Vector3 position)
    {
        string envKey = PlayerPrefs.GetString("SelectedEnvironmentKey");
        string json = PlayerPrefs.GetString(envKey);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("No environment data found for key: " + envKey);
            return;
        }

        EnvironmentData data = JsonUtility.FromJson<EnvironmentData>(json);
        if (data == null)
        {
            Debug.LogError("Failed to parse environment data.");
            return;
        }

        data.MigrateFromLegacy();

        int layerTotal = data.layerCount > 0 ? data.layerCount : 3;

        GameObject root = new GameObject(data.environmentName);
        root.transform.position = position;
        root.transform.rotation = Quaternion.identity;

        // Initialize sandbox movement bounds so hunting/fleeing actors
        // and the main player stay within the configured sandbox area
        // (SandboxSettings.sandboxWidth/Depth for X/Z, layer heights for Y)
        SandboxBounds.Initialize(root.transform.position, settings, layerTotal);

        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }

        // Spawn environment plane
        if (!string.IsNullOrEmpty(data.environmentPlanePrefabName))
        {
            GameObject planePrefab =
                actorDatabase.GetActorByName(data.environmentPlanePrefabName);
            if (planePrefab != null)
            {
                GameObject plane = Instantiate(planePrefab, root.transform);
                plane.transform.localPosition = Vector3.zero;
                plane.transform.localRotation = Quaternion.identity;
            }
        }

        bool mainPlayerFound = false;
        ActorAbilityManager mainPlayerAbilityManager = null;
        Vector3 rootWorldPos = root.transform.position;

        // Collect all actors across all layers
        List<PlacedActorData> allActors = new List<PlacedActorData>();
        for (int i = 0; i < layerTotal; i++)
            allActors.AddRange(data.GetLayerActors(i));

        Debug.Log($"Spawning {allActors.Count} actors across {layerTotal} layers.");

        foreach (var actor in allActors)
        {
            GameObject prefab = actorDatabase.GetActorByName(actor.prefabName);
            if (prefab == null)
            {
                Debug.LogWarning($"Prefab not found: {actor.prefabName}");
                continue;
            }

            GameObject go = Instantiate(prefab, root.transform);

            float localY = GetLocalLayerHeight(actor.layerIndex, layerTotal);
            go.transform.position = new Vector3(
                root.transform.position.x + actor.localPosition.x,
                root.transform.position.y + localY,
                root.transform.position.z + actor.localPosition.z
            );
            go.transform.localRotation = actor.localRotation;

            if (string.IsNullOrEmpty(actor.uniqueID))
                actor.uniqueID = System.Guid.NewGuid().ToString();

            go.name = actor.uniqueID;
            go.tag = "Actor";

            ActorIdentity identity = go.AddComponent<ActorIdentity>();
            identity.uniqueId = actor.uniqueID;

            // Main player setup
            if (actor.isMainPlayer)
            {
                var controller = go.AddComponent<MovementController>();
                controller.joystick = joystick;
                mainPlayerFound = true;

                // --- Ability System ---
                // Attach abilities defined in ActorAbilityConfig for this prefab
                mainPlayerAbilityManager = AttachAbilities(go, actor.prefabName);

                Debug.Log($"Main player spawned: {actor.prefabName} Layer {actor.layerIndex}");
            }

            // Food consumption behaviour
            // Attach tier identity so other actors can read this actor's tier
            // without needing access to the config asset
            ActorTierIdentity tierID = go.AddComponent<ActorTierIdentity>();
            tierID.Initialize(actor.foodChainTier, actor.prefabName);

            // Attach abilities to ALL actors (not just main player)
            AttachAbilities(go, actor.prefabName, actor.isMainPlayer);

            // Attach behaviours from addedScripts
            if (actor.addedScripts != null)
                AttachBehaviours(go, actor);

            // Attach autonomous movement + hunger to non-player actors
            if (!actor.isMainPlayer)
                AttachAutonomousSystems(go, actor.prefabName, rootWorldPos, layerTotal);

            Debug.Log($"Spawned: {actor.prefabName} Layer {actor.layerIndex} localY={localY}");
        }

        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(mainPlayerFound);

        // Setup ability UI panel for the main player
        if (abilityUIPanel != null)
            abilityUIPanel.SetupForActor(mainPlayerAbilityManager);

        Debug.Log("Environment placed successfully.");
    }

    
    private ActorAbilityManager AttachAbilities(GameObject actorGO, string prefabName,
                                                bool isMainPlayer = false)
    {
        if (abilityConfig == null) return null;

        List<string> abilityTypeNames = abilityConfig.GetAbilityNamesForPrefab(prefabName);
        if (abilityTypeNames == null || abilityTypeNames.Count == 0) return null;

        bool anyAttached = false;

        foreach (string typeName in abilityTypeNames)
        {
            System.Type abilityType = System.Type.GetType(typeName);
            if (abilityType == null)
            {
                Debug.LogError($"[AR] Ability type '{typeName}' not found.");
                continue;
            }
            if (!typeof(ActorAbility).IsAssignableFrom(abilityType))
            {
                Debug.LogError($"[AR] '{typeName}' does not extend ActorAbility.");
                continue;
            }

            Component comp = actorGO.AddComponent(abilityType);

            // Initialize defense values on CamouflageAbility (and any future defense ability)
            if (comp is CamouflageAbility cam && foodChainConfig != null)
            {
                cam.InitializeDefense(
                    foodChainConfig.defenseTriggerChance,
                    foodChainConfig.predatorStunDuration,
                    foodChainConfig.predatorSlowDuration,
                    foodChainConfig.predatorSlowMultiplier
                );
                Debug.Log($"[AR] CamouflageAbility on {prefabName} — " +
                          $"defense chance={foodChainConfig.defenseTriggerChance:P0}, " +
                          $"mode={(isMainPlayer ? "manual+auto" : "auto-defense only")}");
            }
            else
            {
                Debug.Log($"[AR] Attached ability '{typeName}' to {prefabName}");
            }

            anyAttached = true;
        }

        if (!anyAttached) return null;

        ActorAbilityManager manager = actorGO.AddComponent<ActorAbilityManager>();

        // Only show the ability UI panel for the main player
        // Non-main-player actors have abilities but no button
        if (isMainPlayer && abilityUIPanel != null)
            abilityUIPanel.SetupForActor(manager);

        return manager;
    }

    float GetLocalLayerHeight(int layerIndex, int totalLayers)
    {
        float spacing = settings != null ? settings.layerSpacing : 1.5f;
        float bottomY = 0.1f;
        float topY = bottomY + (totalLayers - 1) * spacing;
        return topY - (layerIndex * spacing);
    }

    float GetLayerHeight(int layerIndex, int totalLayers)
    {
        if (settings != null)
            return settings.GetLayerHeight(layerIndex, totalLayers);

        float bottomY = 0f;
        float spacing = 1.5f;
        float topY = bottomY + (totalLayers - 1) * spacing;
        return topY - (layerIndex * spacing);
    }

   
    private void AttachBehaviours(GameObject actorGO, PlacedActorData actor)
    {
        foreach (string scriptName in actor.addedScripts)
        {
            switch (scriptName)
            {
                case "Hunt Prey":
                    {
                        FoodConsumer fc = actorGO.AddComponent<FoodConsumer>();
                        if (foodChainConfig != null)
                        {
                            fc.Initialize(
                                actor.foodChainTier,
                                foodChainConfig.huntTierDifference,
                                foodChainConfig.huntDetectionRadius,
                                foodChainConfig.consumeDistance,
                                huntMoveSpeed,
                                foodChainConfig.huntSuccessChance
                            );
                        }
                        Debug.Log($"[AR] Hunt Prey attached to {actor.prefabName} (tier {actor.foodChainTier})");
                        break;
                    }

                case "Flee Predators":
                    {
                        FleeFromPredator flee = actorGO.AddComponent<FleeFromPredator>();
                        if (foodChainConfig != null)
                        {
                            flee.Initialize(
                                actor.foodChainTier,
                                foodChainConfig.fleeTierDifference,
                                foodChainConfig.fleeDetectionRadius,
                                fleeMoveSpeed,
                                foodChainConfig.escapeChance
                            );
                        }
                        Debug.Log($"[AR] Flee Predators attached to {actor.prefabName} (tier {actor.foodChainTier})");
                        break;
                    }

                case "Food Consumption":
                    {
                        // Legacy: manual target via uniqueID
                        FoodConsumer fc = actorGO.AddComponent<FoodConsumer>();
                        fc.foodTargetUniqueID = actor.foodTargetUniqueID;
                        if (foodChainConfig != null)
                        {
                            fc.Initialize(
                                actor.foodChainTier,
                                foodChainConfig.huntTierDifference,
                                foodChainConfig.huntDetectionRadius,
                                foodChainConfig.consumeDistance,
                                huntMoveSpeed
                            );
                        }
                        Debug.Log($"[AR] Food Consumption (legacy) attached to {actor.prefabName}");
                        break;
                    }

                default:
                    {
                        // Dynamic resolution for custom behaviours (e.g. "PatrolBehaviour")
                        System.Type t = System.Type.GetType(scriptName);
                        if (t != null && typeof(MonoBehaviour).IsAssignableFrom(t))
                        {
                            actorGO.AddComponent(t);
                            Debug.Log($"[AR] Dynamic behaviour '{scriptName}' attached to {actor.prefabName}");
                        }
                        else
                        {
                            Debug.LogWarning($"[AR] Unknown behaviour '{scriptName}' — " +
                                             $"no matching class found. Skipping.");
                        }
                        break;
                    }
            }
        }
    }


    private void AttachAutonomousSystems(GameObject actorGO, string prefabName,
                                         Vector3 rootWorldPos, int totalLayers)
    {
        if (foodChainConfig == null)
        {
            Debug.LogWarning("[AR] No FoodChainConfig — skipping autonomous systems.");
            return;
        }

        FoodChainConfig.SpeciesEntry entry = foodChainConfig.GetEntry(prefabName);

        float layerSpacing = settings != null ? settings.layerSpacing : 1.5f;
        float bottomY = settings != null ? settings.bottomLayerY : 0f;

        float prefWorldY = foodChainConfig.GetPreferredWorldY(
            prefabName, rootWorldPos.y, totalLayers, layerSpacing, bottomY);

        float huntLayerY = prefWorldY;
        if (entry != null)
        {
            int midLayer = totalLayers / 2;
            int huntLayerIndex;

            if (entry.preferredLayerIndex <= midLayer)
            {
                huntLayerIndex = Mathf.Min(entry.preferredLayerIndex + 1, totalLayers - 1);
            }
            else
            {
                huntLayerIndex = Mathf.Max(entry.preferredLayerIndex - 1, 0);
            }

            float topY = bottomY + (totalLayers - 1) * layerSpacing;
            huntLayerY = rootWorldPos.y + topY - (huntLayerIndex * layerSpacing);
        }

        // --- AutonomousMovement 
        AutonomousMovement autoMove = actorGO.AddComponent<AutonomousMovement>();
        autoMove.Initialize(
            prefWorldY,
            entry != null ? entry.wanderYRange : 0.5f,
            entry != null ? entry.wanderSpeed : 0.4f,
            entry != null ? entry.settleSpeed : 0.3f,
            entry != null ? entry.settleTime : 45f,
            entry != null ? entry.wanderDirectionChangeInterval : 3f
        );
        autoMove.SetHuntingLayerY(huntLayerY);

        // --- HungerSystem 
        // huntHungerThreshold: actor only hunts when hunger drops below this value.
        // Above this = full, ignores prey, wanders peacefully.
        // Below this = hungry, actively hunts across layers.
        HungerSystem hunger = actorGO.AddComponent<HungerSystem>();
        hunger.Initialize(
            entry != null ? entry.maxHunger : 100f,
            entry != null ? entry.hungerDepletionRate : 2f,
            entry != null ? entry.huntHungerThreshold : 70f,
            entry != null ? entry.slowHungerThreshold : 30f,
            entry != null ? entry.starvingSpeedMultiplier : 0.4f,
            entry != null ? entry.diesWhenStarving : true
        );

        Debug.Log($"[AR] AutoMove + Hunger attached to {prefabName} " +
                  $"preferredY={prefWorldY:F1}, huntLayerY={huntLayerY:F1}");
    }

}