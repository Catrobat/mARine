
using UnityEngine;
using UnityEngine.EventSystems;

public class GridTapPlacer : MonoBehaviour
{
    [Header("References")]
    public Camera arCamera;
    public ActorSelector actorSelector;
    public LayerManager layerManager;

    [Header("Food Chain")]
    [Tooltip("Assign the same FoodChainConfig ScriptableObject used by ARPlacementController")]
    public FoodChainConfig foodChainConfig;

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            TryPlaceActor(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
            TryPlaceActor(Input.GetTouch(0).position);
        }
#endif
    }

    void TryPlaceActor(Vector2 screenPos)
    {
        if (layerManager == null)
        {
            Debug.LogWarning("[GridTapPlacer] LayerManager not assigned!");
            return;
        }

        GridManager activeGridManager = layerManager.GetActiveGridManager();
        if (activeGridManager == null)
        {
            Debug.LogWarning("[GridTapPlacer] No active GridManager found!");
            return;
        }

        GameObject[,] activeGrid = activeGridManager.GetGrid();
        if (activeGrid == null)
        {
            Debug.LogWarning("[GridTapPlacer] Active grid is null!");
            return;
        }

        Ray ray = arCamera.ScreenPointToRay(screenPos);

        GridCellScript hitCell = null;
        float closestDistance = Mathf.Infinity;

        for (int x = 0; x < activeGrid.GetLength(0); x++)
        {
            for (int y = 0; y < activeGrid.GetLength(1); y++)
            {
                GameObject cellObject = activeGrid[x, y];
                if (cellObject == null) continue;

                Collider cellCollider = cellObject.GetComponent<Collider>();
                if (cellCollider == null) continue;

                if (cellCollider.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        hitCell = cellObject.GetComponent<GridCellScript>();
                    }
                }
            }
        }

        if (hitCell != null && !hitCell.isOccupied)
        {
            GameObject selectedActor = actorSelector.GetSelectedActor();
            if (selectedActor == null)
            {
                Debug.Log("[GridTapPlacer] No actor selected!");
                return;
            }

            // Pass foodChainConfig so tier is stamped at placement time
            hitCell.PlaceActor(selectedActor, foodChainConfig);

            Debug.Log($"[GridTapPlacer] Placed actor on Layer {layerManager.GetActiveLayer()}");
            actorSelector.ClearSelection();
        }
    }
}