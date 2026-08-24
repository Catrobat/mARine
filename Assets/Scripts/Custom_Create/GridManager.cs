using UnityEngine;
using System.Collections;

public class GridManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public int gridSizeX = 5;
    public int gridSizeY = 5;
    public float cellHeightOffset = 0.01f;
    public float spacing = 0.02f;

    [Header("Layer Settings")]
    public int layerIndex = 0;
    public SandboxSettings settings;

    [Header("Obstacle Detection")]
    [Tooltip("How far above and below the cell center to scan for obstacles. " +
             "Increase this if stones are not being detected.")]
    public float obstacleCheckHalfHeight = 1.5f;

    [Tooltip("Color to show on cells that are blocked by an obstacle")]
    public Color unusableColor = Color.red;

    private int totalLayerCount = 3;
    private GameObject[,] gridCells;

    // Called from CreateModeManager before GenerateGrid
    public void SetTotalLayerCount(int count)
    {
        totalLayerCount = count;
    }

    public void GenerateGrid(GameObject plane)
    {
        if (!plane) return;

        float layerY = settings != null
            ? settings.GetLayerHeight(layerIndex, totalLayerCount)
            : layerIndex * 1.5f;

        Vector3 planeSize = plane.transform.localScale * 10f;
        Vector3 planeOrigin = plane.transform.position;
        planeOrigin.y = layerY;

        float cellWidth = (planeSize.x / gridSizeX) - spacing;
        float cellHeight = (planeSize.z / gridSizeY) - spacing;

        Vector3 bottomLeft = planeOrigin - new Vector3(planeSize.x / 2, 0, planeSize.z / 2);

        gridCells = new GameObject[gridSizeX, gridSizeY];

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                float posX = x * (cellWidth + spacing) + cellWidth / 2;
                float posZ = y * (cellHeight + spacing) + cellHeight / 2;
                Vector3 cellCenter = bottomLeft + new Vector3(posX, cellHeightOffset, posZ);

                GameObject cell = Instantiate(cellPrefab, cellCenter, Quaternion.identity);
                cell.transform.localScale = new Vector3(cellWidth, cellWidth, cellHeight);
                cell.name = $"GridCell_L{layerIndex}_{x}_{y}";
                cell.transform.SetParent(this.transform);

                GridCellScript script = cell.GetComponent<GridCellScript>();
                if (script != null)
                {
                    script.layerIndex = layerIndex;
                    script.settings = settings;
                    script.totalLayerCount = totalLayerCount;
                }

                gridCells[x, y] = cell;

                // Set initial layer color (normal state)
                // MarkAsUnusable() will override this to red for blocked cells
                SetNormalColor(cell);
            }
        }

        StartCoroutine(CheckObstaclesNextFrame(cellWidth, cellHeight));

        Debug.Log($"Grid generated for Layer {layerIndex} at Y={layerY} " +
                  $"(total layers: {totalLayerCount})");
    }


    IEnumerator CheckObstaclesNextFrame(float cellWidth, float cellHeight)
    {
        // Wait for end of frame — physics sync happens here
        yield return new WaitForEndOfFrame();

        // Force sync in case physics hasn't ticked yet
        Physics.SyncTransforms();

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                GameObject cell = gridCells[x, y];
                if (cell == null) continue;

                GridCellScript script = cell.GetComponent<GridCellScript>();
                if (script == null) continue;

               
                if (script.isOccupied) continue;

                Vector3 cellCenter = cell.transform.position;

           
                Collider[] hits = Physics.OverlapBox(
                    cellCenter,
                    new Vector3(cellWidth * 0.45f, obstacleCheckHalfHeight, cellHeight * 0.45f),
                    Quaternion.identity,
                    Physics.AllLayers,
                    QueryTriggerInteraction.Ignore
                );

                bool hasObstacle = false;
                foreach (var hit in hits)
                {
                    // Skip the cell's own collider
                    if (hit.gameObject == cell) continue;
                    // Skip other grid cells
                    if (hit.GetComponent<GridCellScript>() != null) continue;

                    if (hit.CompareTag("Obstacle"))
                    {
                        hasObstacle = true;
                        break;
                    }
                }

                if (hasObstacle)
                {
                    script.MarkAsUnusable();
                    // FIX: Apply red color here directly as well,
                    // so it's guaranteed even if MarkAsUnusable only
                    // sets a flag without touching the renderer.
                    ApplyUnusableColor(cell);
                    Debug.Log($"[GridManager] Cell [{x},{y}] L{layerIndex} blocked by obstacle.");
                }
            }
        }
    }

    public void RestoreUnusableCellColors()
    {
        if (gridCells == null) return;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                GameObject cell = gridCells[x, y];
                if (cell == null) continue;

                GridCellScript script = cell.GetComponent<GridCellScript>();
                if (script == null || !script.isOccupied) continue;

                // Distinguish obstacle-blocked cells from actor-occupied cells:
                // Obstacle cells are flagged isOccupied but have NO actor child.
                // Actor-occupied cells have a child GameObject (the placed actor).
                bool hasActorChild = cell.transform.childCount > 0;
                if (!hasActorChild && script.cellRenderer != null)
                    script.cellRenderer.material.color = unusableColor;
            }
        }
    }

    void SetNormalColor(GameObject cell)
    {
        if (LayerManager.Instance == null) return;
        Renderer rend = cell.GetComponent<Renderer>();
        if (rend == null) return;

        Color color = (layerIndex == LayerManager.Instance.GetActiveLayer())
            ? LayerManager.Instance.topLayerColor
            : LayerManager.Instance.otherLayerColor;

        rend.material.color = color;
    }

    void ApplyUnusableColor(GameObject cell)
    {
        Renderer rend = cell.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = unusableColor;
    }

    public GameObject[,] GetGrid() => gridCells;
}