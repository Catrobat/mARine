using UnityEngine;

public static class EnvironmentDataCache
{
    public static EnvironmentData currentData;

    public static void SetData(EnvironmentData data)
    {
        currentData = data;
    }

    public static EnvironmentData GetData()
    {
        return currentData;
    }

    public static void RemoveActorById(string id)
    {
        if (currentData == null) return;

        var actorToRemove = currentData.placedActors.Find(a => a.uniqueID == id);
        if (actorToRemove != null)
        {
            currentData.placedActors.Remove(actorToRemove);
            Debug.Log("Removed actor from EnvironmentData.");
        }
    }
}
