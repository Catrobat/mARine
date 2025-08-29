using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlacedActorData
{
    public string prefabName;                           // Name of the actor prefab
    public Vector3 localPosition;
    public Quaternion localRotation;
    public bool isMainPlayer = false;

    public string uniqueID;                             // Assigned at runtime
    public List<string> addedScripts = new();           // Attached behaviors (e.g., Food Consumption)
    public string foodTargetUniqueID;                   // For food behavior
}
