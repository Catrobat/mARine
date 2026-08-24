

using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class PlacedActorData
{
    public string prefabName;
    public Vector3 localPosition;
    public Quaternion localRotation;
    public bool isMainPlayer = false;
    public string uniqueID;
    public List<string> addedScripts = new();
    public string foodTargetUniqueID;       // legacy: manual food target (still used as fallback)
    public int layerIndex = 0;              // 0 = surface, N-1 = sea floor

   
    // Food Chain
  
    public int foodChainTier = 1;
}