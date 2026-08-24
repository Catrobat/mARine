using UnityEngine;

public class GridCellScript : MonoBehaviour
{
    public bool isOccupied = false;

    [Header("Layer")]
    public int layerIndex = 0;
    public int totalLayerCount = 3;
    public SandboxSettings settings;

    private GameObject placedActor;
    public Renderer cellRenderer;

    private void Awake()
    {
        if (!cellRenderer)
            cellRenderer = GetComponent<Renderer>();
    }

  
    public void PlaceActor(GameObject actorPrefab, FoodChainConfig foodChainConfig = null)
    {
        if (isOccupied || actorPrefab == null) return;

        string uniqueID = System.Guid.NewGuid().ToString();

        // Place actor on TOP of cube surface
        Vector3 cellSize = transform.localScale;
        float cellHeight = cellSize.y;
        Vector3 spawnPosition = transform.position;
        spawnPosition.y = transform.position.y + (cellHeight / 2f);

        placedActor = Instantiate(actorPrefab, spawnPosition, Quaternion.identity);
        placedActor.name = uniqueID;
        placedActor.tag = "Actor";

        ActorIdentity identity = placedActor.AddComponent<ActorIdentity>();
        identity.uniqueId = uniqueID;

        // Parent to layer root so visibility toggling works
        if (LayerManager.Instance != null)
        {
            GameObject layerRoot = LayerManager.Instance.GetRootForLayer(layerIndex);
            if (layerRoot != null)
            {
                placedActor.transform.SetParent(layerRoot.transform);
                Debug.Log($"Actor parented to '{layerRoot.name}'");
            }
            LayerManager.Instance.RegisterActor(placedActor, layerIndex);
        }
        else
        {
            Debug.LogWarning("LayerManager.Instance is null — actor not parented or registered!");
        }

        // Save position relative to sandbox center (Y = 0, recalculated at AR spawn)
        Vector3 sandboxCenter = LayerManager.Instance != null
            ? LayerManager.Instance.sandboxCenter
            : Vector3.zero;

        string prefabName = actorPrefab.name.Replace("(Clone)", "").Trim();

        // Stamp tier from config — configurable per species, no hardcoding
        int tier = foodChainConfig != null ? foodChainConfig.GetTier(prefabName) : 1;

        // Auto-assign Hunt Prey / Flee Predators based on tier vs existing actors
        // User can remove these in BehaviorScene if needed
        System.Collections.Generic.List<string> autoScripts =
            new System.Collections.Generic.List<string>();

        if (foodChainConfig != null && EnvironmentDataCache.currentData != null)
        {
            int total = EnvironmentDataCache.currentData.layerCount > 0
                ? EnvironmentDataCache.currentData.layerCount : 5;

            bool hasPrey = false;
            bool hasPredator = false;

            for (int i = 0; i < total; i++)
            {
                foreach (var a in EnvironmentDataCache.currentData.GetLayerActors(i))
                {
                    if (!hasPrey && foodChainConfig.autoAssignHuntBehaviour &&
                        foodChainConfig.ShouldHunt(tier, a.foodChainTier))
                        hasPrey = true;

                    if (!hasPredator && foodChainConfig.autoAssignFleeBehaviour &&
                        foodChainConfig.ShouldFlee(tier, a.foodChainTier))
                        hasPredator = true;

                    if (hasPrey && hasPredator) break;
                }
                if (hasPrey && hasPredator) break;
            }

            if (hasPrey) autoScripts.Add("Hunt Prey");
            if (hasPredator) autoScripts.Add("Flee Predators");

            if (autoScripts.Count > 0)
                Debug.Log($"[GridCell] Auto-assigned to '{prefabName}': " +
                          string.Join(", ", autoScripts));
        }

        PlacedActorData actorData = new PlacedActorData
        {
            prefabName = prefabName,
            localPosition = new Vector3(
                                transform.position.x - sandboxCenter.x,
                                0f,
                                transform.position.z - sandboxCenter.z),
            localRotation = Quaternion.identity,
            uniqueID = uniqueID,
            layerIndex = this.layerIndex,
            foodChainTier = tier,
            addedScripts = autoScripts   // pre-populated, user can edit in BehaviorScene
        };

        if (EnvironmentDataCache.currentData != null)
        {
            EnvironmentDataCache.currentData
                .GetLayerActors(layerIndex)
                .Add(actorData);
            Debug.Log($"Placed '{prefabName}' (tier {tier}) on Layer {layerIndex}");
        }
        else
        {
            Debug.LogWarning("EnvironmentDataCache is null — actor data not saved!");
        }

        isOccupied = true;
    }

    public void MarkAsUnusable()
    {
        isOccupied = true;
        if (cellRenderer)
            cellRenderer.material.color = Color.red;
    }

    float GetLayerHeight()
    {
        if (settings == null)
            return layerIndex * 1.5f;
        return settings.GetLayerHeight(layerIndex, totalLayerCount);
    }
}