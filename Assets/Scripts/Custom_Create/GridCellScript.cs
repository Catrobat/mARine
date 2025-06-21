using UnityEngine;

public class GridCellScript : MonoBehaviour
{
    public bool isOccupied = false;
    private GameObject placedActor;

    // Reference to the root GameObject where all placed actors will be grouped
    public Transform environmentRoot;

    public void PlaceActor(GameObject actorPrefab)
    {
        if (isOccupied || actorPrefab == null) return;

        // Instantiate actor
        placedActor = Instantiate(actorPrefab, transform.position, Quaternion.identity);

        // Assign it under environment root if available
        if (environmentRoot == null)
        {
            environmentRoot = GameObject.Find("EnvironmentRoot")?.transform;
        }

        if (environmentRoot != null)
        {
            placedActor.transform.SetParent(environmentRoot);
        }

        isOccupied = true;
    }

}
