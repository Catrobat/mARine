using UnityEngine;
using System.Collections.Generic;


public class ActorAbilityManager : MonoBehaviour
{
    // Auto-populated from components on this GameObject and children
    private List<ActorAbility> abilities = new List<ActorAbility>();

    void Awake()
    {
        RefreshAbilities();
    }

  
    public void RefreshAbilities()
    {
        abilities.Clear();
        abilities.AddRange(GetComponentsInChildren<ActorAbility>(includeInactive: true));
        Debug.Log($"[ActorAbilityManager] Found {abilities.Count} abilities on {gameObject.name}");
    }

    public List<ActorAbility> GetAbilities() => abilities;

    public void ActivateAbility(string abilityName)
    {
        ActorAbility ability = abilities.Find(a => a.AbilityName == abilityName);
        if (ability != null)
            ability.Activate();
        else
            Debug.LogWarning($"[ActorAbilityManager] Ability '{abilityName}' not found on {gameObject.name}");
    }


    public void DeactivateAll()
    {
        foreach (var ability in abilities)
            ability.Deactivate();
    }
}
