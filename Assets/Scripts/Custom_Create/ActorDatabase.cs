using UnityEngine;
using System.Linq;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Actor Database")]
public class ActorDatabase : ScriptableObject
{
    public List<GameObject> actorPrefabs;

    public GameObject GetActorByName(string name)
    {
        return actorPrefabs.FirstOrDefault(p => p.name == name);
    }
}
