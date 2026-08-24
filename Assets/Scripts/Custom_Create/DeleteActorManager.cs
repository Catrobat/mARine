using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeleteActorManager : MonoBehaviour
{
    public static DeleteActorManager Instance;
    public bool isDeleteModeActive = false;
    public Button deleteButton;
    public Camera sceneCamera;

    private Image buttonImage;
    private Color defaultColor;
    private Color targetColor;
    private Color deleteColor = Color.yellow;
    private float colorLerpSpeed = 5f;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!deleteButton)
        {
            Debug.LogError("Delete button is not assigned!");
            return;
        }

        buttonImage = deleteButton.GetComponent<Image>();
        if (!buttonImage)
        {
            Debug.LogError("Delete button has no Image component!");
            return;
        }

        defaultColor = buttonImage.color;
        targetColor = defaultColor;

        deleteButton.onClick.AddListener(() =>
        {
            isDeleteModeActive = !isDeleteModeActive;
            targetColor = isDeleteModeActive ? deleteColor : defaultColor;
            Debug.Log(isDeleteModeActive ? "Delete mode on. Tap actors to delete." : "Delete mode off.");
        });
    }

    void Update()
    {
        if (buttonImage != null)
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * colorLerpSpeed);

        if (!isDeleteModeActive) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
            TryDeleteActor(Input.mousePosition);
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began &&
            !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
            TryDeleteActor(Input.GetTouch(0).position);
#endif
    }

    void TryDeleteActor(Vector2 screenPos)
    {
        Ray ray = sceneCamera.ScreenPointToRay(screenPos);

        // RaycastAll so actors on any layer can be hit
        RaycastHit[] hits = Physics.RaycastAll(ray);

        foreach (RaycastHit hit in hits)
        {
            GameObject obj = hit.collider.gameObject;

            if (obj.CompareTag("Actor"))
            {
                string id = obj.name;

                // Get which layer this actor is on before destroying
                int actorLayerIndex = GetActorLayerFromCache(id);

                // Destroy the actor
                Destroy(obj);

                // Remove from cache
                EnvironmentDataCache.RemoveActorById(id);

                // Free the grid cell so it can be used again
                FreeGridCell(actorLayerIndex, id);

                Debug.Log($"Deleted actor ID: {id} from Layer {actorLayerIndex}");
                return;
            }
        }
    }

    int GetActorLayerFromCache(string uniqueID)
    {
        if (EnvironmentDataCache.currentData == null) return 0;

        for (int i = 0; i < 3; i++)
        {
            var list = EnvironmentDataCache.currentData.GetLayerActors(i);
            if (list.Find(a => a.uniqueID == uniqueID) != null)
                return i;
        }

        Debug.LogWarning($"Actor {uniqueID} not found in any layer cache.");
        return 0;
    }

    void FreeGridCell(int layerIndex, string uniqueID)
    {
        if (LayerManager.Instance == null)
        {
            Debug.LogWarning("LayerManager instance not found!");
            return;
        }

        GridManager gm = null;
        if (layerIndex == 0) gm = LayerManager.Instance.gridManager0;
        else if (layerIndex == 1) gm = LayerManager.Instance.gridManager1;
        else gm = LayerManager.Instance.gridManager2;

        if (gm == null)
        {
            Debug.LogWarning($"GridManager for layer {layerIndex} is null!");
            return;
        }

        GameObject[,] grid = gm.GetGrid();
        if (grid == null) return;

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                GameObject cellObj = grid[x, y];
                if (cellObj == null) continue;

                GridCellScript cell = cellObj.GetComponent<GridCellScript>();
                if (cell != null && cell.isOccupied)
                {
                    // Check children of layer root for matching uniqueID
                    string rootName = $"Layer{layerIndex}_Root";
                    GameObject layerRoot = GameObject.Find(rootName);
                    if (layerRoot == null) continue;

                    Transform actor = layerRoot.transform.Find(uniqueID);
                    if (actor != null)
                    {
                        cell.isOccupied = false;
                        Debug.Log($"Freed cell [{x},{y}] on Layer {layerIndex}");
                        return;
                    }
                }
            }
        }
    }
}