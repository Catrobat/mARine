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

            // Setup live environment cache
            EnvironmentData data = new EnvironmentData
            {
                environmentName = PlayerPrefs.GetString("NewModuleName", "UnnamedModule"),
                environmentPlanePrefabName = PlayerPrefs.GetString("SelectedEnvironmentPrefabName", "UnknownPrefab"),
                placedActors = new System.Collections.Generic.List<PlacedActorData>()
            };

            EnvironmentDataCache.SetData(data);
        }
        else
        {
            Debug.LogWarning("No plane found to generate grid.");
        }
    }
}
