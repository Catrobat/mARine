using UnityEngine;

public class GridManager : MonoBehaviour
{
    public GameObject cellPrefab;
    public int gridSizeX = 5;
    public int gridSizeY = 5;
    public float cellHeightOffset = 0.01f;
    public float spacing = 0.02f;
    private GameObject[,] gridCells;

    public void GenerateGrid(GameObject plane)
    {
        if (!plane) return;

        Vector3 planeSize = plane.transform.localScale * 10f;
        Vector3 planeOrigin = plane.transform.position;

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
                cell.transform.localScale = new Vector3(cellWidth, 0.01f, cellHeight);
                cell.name = $"GridCell_{x}_{y}";
                cell.transform.SetParent(this.transform);
                gridCells[x, y] = cell;

                // Collision check
                Collider[] hits = Physics.OverlapBox(cellCenter, new Vector3(cellWidth / 2, 0.05f, cellHeight / 2));
                bool hasObstacle = false;

                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Obstacle"))
                    {
                        hasObstacle = true;
                        break;
                    }
                }

                if (hasObstacle)
                {
                    var script = cell.GetComponent<GridCellScript>();
                    script?.MarkAsUnusable();
                    Debug.DrawLine(cellCenter + Vector3.up * 0.1f, cellCenter + Vector3.up * 0.5f, Color.red, 10f);
                }

            }
        }

        Debug.Log("Grid generated with spacing: " + spacing);
    }

    public GameObject[,] GetGrid() => gridCells;
}
