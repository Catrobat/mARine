using UnityEngine;


public abstract class ActorAbility : MonoBehaviour
{
    // Display name shown on the UI button
    public abstract string AbilityName { get; }

    // Optional icon name for future UI icon support
    public virtual string IconName => "";

    // Whether this ability is currently active/toggled on
    public bool IsActive { get; protected set; } = false;


    public abstract void Activate();

    public virtual void Deactivate()
    {
        IsActive = false;
    }

    protected virtual void OnDestroy()
    {
        Deactivate();
    }
}
