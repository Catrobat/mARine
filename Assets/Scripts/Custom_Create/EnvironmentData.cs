using System.Collections.Generic;

[System.Serializable]
public class EnvironmentData
{
    public string environmentName;

    public string environmentPlanePrefabName; // ADD THIS

    public List<PlacedActorData> placedActors = new List<PlacedActorData>();
}
