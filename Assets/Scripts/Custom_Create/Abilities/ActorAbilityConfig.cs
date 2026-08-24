using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(menuName = "MarineAR/Actor Ability Config")]
public class ActorAbilityConfig : ScriptableObject
{
    [System.Serializable]
    public class ActorAbilityEntry
    {
        [Tooltip("Must exactly match the prefab name in ActorDatabase (e.g. 'Octo_Yellow')")]
        public string prefabName;

        [Tooltip("Class names of ActorAbility subclasses to attach at runtime " +
                 "(e.g. 'CamouflageAbility'). Must be exact C# class names.")]
        public List<string> abilityTypeNames = new List<string>();
    }

    public List<ActorAbilityEntry> entries = new List<ActorAbilityEntry>();

    
    public List<string> GetAbilityNamesForPrefab(string prefabName)
    {
        ActorAbilityEntry entry = entries.Find(e => e.prefabName == prefabName);
        return entry != null ? entry.abilityTypeNames : new List<string>();
    }


    public bool HasAbilities(string prefabName)
    {
        return entries.Exists(e => e.prefabName == prefabName && e.abilityTypeNames.Count > 0);
    }
}
