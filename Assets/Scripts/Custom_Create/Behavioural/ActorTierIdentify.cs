using UnityEngine;




public class ActorTierIdentity : MonoBehaviour
{
  
    public int Tier { get; private set; } = 1;

 
    public string PrefabName { get; private set; } = "";

    public void Initialize(int tier, string prefabName)
    {
        Tier = tier;
        PrefabName = prefabName;
    }
}