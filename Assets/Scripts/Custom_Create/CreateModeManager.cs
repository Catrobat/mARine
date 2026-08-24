
using UnityEngine;
using System.Collections.Generic;

public class CreateModeManager : MonoBehaviour
{
    [Header("Spawner")]
    public EnvironmentSpawner spawner;

    [Header("All Five Grid Managers")]
    public GridManager gridManager0;
    public GridManager gridManager1;
    public GridManager gridManager2;
    public GridManager gridManager3;
    public GridManager gridManager4;

    [Header("References")]
    public LayerManager layerManager;
    public SandboxSettings settings;

    public void CreateEnvironmentAndGrids()
    {
        if (spawner == null)
        {
            Debug.LogError("Spawner not assigned!");
            return;
        }

        if (layerManager == null)
        {
            Debug.LogError("LayerManager not assigned!");
            return;
        }

        if (settings == null)
        {
            Debug.LogError("SandboxSettings not assigned!");
            return;
        }

        spawner.SpawnEnvironmentPlane();
        GameObject plane = spawner.GetSpawnedPlane();

        if (plane == null)
        {
            Debug.LogWarning("No plane found.");
            return;
        }

        // Adapt plane height to fit all layers
        AdaptEnvironmentToLayerCount(plane, settings.layerCount);

        layerManager.SetSandboxCenter(plane.transform.position);

        int count = settings.layerCount;

        // Pass total layer count to each GridManager before generating
        GridManager[] allManagers = { gridManager0, gridManager1,
                                      gridManager2, gridManager3, gridManager4 };

        for (int i = 0; i < count; i++)
        {
            if (allManagers[i] != null)
            {
                allManagers[i].SetTotalLayerCount(count);
                allManagers[i].GenerateGrid(plane);
            }
        }

        EnvironmentData data = new EnvironmentData
        {
            environmentName = PlayerPrefs.GetString("NewModuleName", "UnnamedModule"),
            environmentPlanePrefabName = PlayerPrefs.GetString("SelectedEnvironmentPrefabName", "UnknownPrefab"),
            layerCount = count,
            layer0Actors = new List<PlacedActorData>(),
            layer1Actors = new List<PlacedActorData>(),
            layer2Actors = new List<PlacedActorData>(),
            layer3Actors = new List<PlacedActorData>(),
            layer4Actors = new List<PlacedActorData>()
        };

        EnvironmentDataCache.SetData(data);
        layerManager.SetActiveLayer(0);

        Debug.Log($"Environment created with {count} layers.");
    }

    void AdaptEnvironmentToLayerCount(GameObject plane, int layerCount)
    {
        if (plane == null || settings == null) return;

        // Calculate total vertical height needed
        float totalHeight = settings.GetTotalHeight(layerCount);

        // Scale the plane vertically to accommodate all layers
        // We don't change X and Z — only inform the system of bounds
        Debug.Log($"Sandbox total vertical span: {totalHeight} " +
                  $"for {layerCount} layers with spacing {settings.layerSpacing}");

        // Move plane Y to bottom layer position so grid generates correctly
        Vector3 pos = plane.transform.position;
        pos.y = settings.bottomLayerY;
        plane.transform.position = pos;

        Debug.Log($"Plane base Y set to: {pos.y}");
    }
}