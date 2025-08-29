using UnityEngine;

public class GridCellScript : MonoBehaviour
{
    public bool isOccupied = false;
    private GameObject placedActor;
    public Transform environmentRoot;
    public Renderer cellRenderer;

    private void Awake()
    {
        if (!cellRenderer)
            cellRenderer = GetComponent<Renderer>();
    }

    public void PlaceActor(GameObject actorPrefab)
    {
        if (isOccupied || actorPrefab == null) return;

        // Generate unique ID for this actor
        string uniqueID = System.Guid.NewGuid().ToString();

        placedActor = Instantiate(actorPrefab, transform.position, Quaternion.identity);
        placedActor.name = uniqueID;
        placedActor.tag = "Actor";

        // Attach ActorIdentity
        var identity = placedActor.AddComponent<ActorIdentity>();
        identity.uniqueId = uniqueID;

        if (environmentRoot == null)
            environmentRoot = GameObject.Find("EnvironmentRoot")?.transform;

        if (environmentRoot != null)
            placedActor.transform.SetParent(environmentRoot);

        // Add to live EnvironmentData cache
        PlacedActorData actorData = new PlacedActorData();
        actorData.prefabName = actorPrefab.name.Replace("(Clone)", "").Trim();
        actorData.localPosition = transform.localPosition; // local to environment root
        actorData.localRotation = Quaternion.identity; // assume upright
        actorData.uniqueID = uniqueID;

        if (EnvironmentDataCache.currentData != null)
        {
            EnvironmentDataCache.currentData.placedActors.Add(actorData);
            Debug.Log($"Added actor to cache: {actorData.prefabName}, ID: {uniqueID}");
        }
        else
        {
            Debug.LogWarning("EnvironmentDataCache is null!");
        }

        isOccupied = true;
    }

    public void MarkAsUnusable()
    {
        isOccupied = true;
        if (cellRenderer)
            cellRenderer.material.color = Color.red;
    }
}
