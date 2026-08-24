
using UnityEngine;
using System.Collections.Generic;

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

        int total = currentData.layerCount > 0 ? currentData.layerCount : 5;
        for (int i = 0; i < total; i++)
        {
            var list = currentData.GetLayerActors(i);
            var actor = list.Find(a => a.uniqueID == id);
            if (actor != null)
            {
                list.Remove(actor);
                Debug.Log($"Removed actor {id} from Layer {i}.");
                return;
            }
        }

        Debug.LogWarning($"Actor {id} not found in any layer.");
    }

    public static List<PlacedActorData> GetAllActors()
    {
        var all = new List<PlacedActorData>();
        if (currentData == null) return all;

        int total = currentData.layerCount > 0 ? currentData.layerCount : 5;
        for (int i = 0; i < total; i++)
            all.AddRange(currentData.GetLayerActors(i));

        return all;
    }
}