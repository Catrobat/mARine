using UnityEngine;

public class CreateModeManager : MonoBehaviour
{
    public EnvironmentSpawner spawner;
    public GridManager gridManager;

    void Start()
    {
        CreateEnvironmentAndGrid(); // Auto-run at scene start
    }

    public void CreateEnvironmentAndGrid()
    {
        spawner.SpawnEnvironmentPlane();

        GameObject plane = spawner.GetSpawnedPlane();
        if (plane != null)
        {
            gridManager.GenerateGrid(plane);
        }
        else
        {
            Debug.LogWarning("No plane found to generate grid.");
        }
    }
}
