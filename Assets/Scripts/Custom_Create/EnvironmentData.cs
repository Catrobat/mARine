/*using System.Collections.Generic;

[System.Serializable]
public class EnvironmentData
{
    public string environmentName;
    public string environmentPlanePrefabName;

    // Keep this for backward compatibility when loading old saves
    public List<PlacedActorData> placedActors = new List<PlacedActorData>();

    // Three layer lists
    public List<PlacedActorData> layer0Actors = new List<PlacedActorData>();
    public List<PlacedActorData> layer1Actors = new List<PlacedActorData>();
    public List<PlacedActorData> layer2Actors = new List<PlacedActorData>();

    public List<PlacedActorData> GetLayerActors(int index)
    {
        if (index == 0) return layer0Actors;
        if (index == 1) return layer1Actors;
        return layer2Actors;
    }

    // Call this when loading old saves to migrate data into layer0
    public void MigrateFromLegacy()
    {
        if (placedActors == null || placedActors.Count == 0) return;
        foreach (var actor in placedActors)
        {
            actor.layerIndex = 0;
            layer0Actors.Add(actor);
        }
        placedActors.Clear();
    }
}*/

using System.Collections.Generic;

[System.Serializable]
public class EnvironmentData
{
    public string environmentName;
    public string environmentPlanePrefabName;
    public int layerCount = 3;

    // Legacy — kept for migration
    public List<PlacedActorData> placedActors = new List<PlacedActorData>();

    // Five layer lists
    public List<PlacedActorData> layer0Actors = new List<PlacedActorData>();
    public List<PlacedActorData> layer1Actors = new List<PlacedActorData>();
    public List<PlacedActorData> layer2Actors = new List<PlacedActorData>();
    public List<PlacedActorData> layer3Actors = new List<PlacedActorData>();
    public List<PlacedActorData> layer4Actors = new List<PlacedActorData>();

    public List<PlacedActorData> GetLayerActors(int index)
    {
        switch (index)
        {
            case 0: return layer0Actors;
            case 1: return layer1Actors;
            case 2: return layer2Actors;
            case 3: return layer3Actors;
            case 4: return layer4Actors;
            default: return layer0Actors;
        }
    }

    public void MigrateFromLegacy()
    {
        if (placedActors == null || placedActors.Count == 0) return;
        foreach (var actor in placedActors)
        {
            actor.layerIndex = 0;
            layer0Actors.Add(actor);
        }
        placedActors.Clear();
    }
}