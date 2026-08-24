using UnityEngine;


public class HungerSystem : MonoBehaviour
{
    // Public state readable by FoodConsumer, UI, etc.
    public float CurrentHunger { get; private set; }
    public float MaxHunger { get; private set; }
    public float HungerPercent => MaxHunger > 0 ? CurrentHunger / MaxHunger : 1f;

  
    public bool CanHunt => CurrentHunger <= huntHungerThreshold;

    public bool IsStarving => CurrentHunger <= slowHungerThreshold;

    // Config
    private float hungerDepletionRate = 2f;
    private float huntHungerThreshold = 70f;  // below this → starts hunting
    private float slowHungerThreshold = 30f;  // below this → slowed
    private float starvingSpeedMultiplier = 0.4f;
    private bool diesWhenStarving = true;

    private AutonomousMovement autoMove;
    private HungerPhase currentPhase = HungerPhase.Full;
    private bool isInitialized = false;

    private enum HungerPhase { Full, Hungry, Starving }


    public void Initialize(float max, float depletionRate, float huntThreshold,
                           float slowThreshold, float slowMultiplier, bool dies)
    {
        MaxHunger = max;
        CurrentHunger = max;   // start full — not hunting
        hungerDepletionRate = depletionRate;
        huntHungerThreshold = huntThreshold;
        slowHungerThreshold = Mathf.Min(slowThreshold, huntThreshold * 0.6f);
        starvingSpeedMultiplier = slowMultiplier;
        diesWhenStarving = dies;
        autoMove = GetComponent<AutonomousMovement>();
        currentPhase = HungerPhase.Full;
        isInitialized = true;

        Debug.Log($"[Hunger] {gameObject.name} initialized. " +
                  $"Max={max}, Rate={depletionRate}/s, " +
                  $"HuntsBelow={huntThreshold}, SlowsBelow={slowHungerThreshold}");
    }


    void Update()
    {
        if (!isInitialized) return;

        CurrentHunger -= hungerDepletionRate * Time.deltaTime;
        CurrentHunger = Mathf.Max(0f, CurrentHunger);

        UpdatePhase();

        if (CurrentHunger <= 0f && diesWhenStarving)
            Starve();
    }

    void UpdatePhase()
    {
        HungerPhase newPhase;

        if (CurrentHunger > huntHungerThreshold)
            newPhase = HungerPhase.Full;
        else if (CurrentHunger > slowHungerThreshold)
            newPhase = HungerPhase.Hungry;
        else
            newPhase = HungerPhase.Starving;

        if (newPhase == currentPhase) return;

        currentPhase = newPhase;

        switch (newPhase)
        {
            case HungerPhase.Full:
                autoMove?.SetSpeedMultiplier(1f);
                autoMove?.StopHungerLayerSearch();
                Debug.Log($"[Hunger] {gameObject.name} → Full " +
                          $"(hunger={CurrentHunger:F0}/{MaxHunger}) — ignoring prey");
                break;

            case HungerPhase.Hungry:
                autoMove?.SetSpeedMultiplier(1f);
                autoMove?.StartHungerLayerSearch();
                Debug.Log($"[Hunger] {gameObject.name} → Hungry " +
                          $"(hunger={CurrentHunger:F0}/{MaxHunger}) — will hunt prey");
                break;

            case HungerPhase.Starving:
                autoMove?.SetSpeedMultiplier(starvingSpeedMultiplier);
                Debug.Log($"[Hunger] {gameObject.name} → Starving " +
                          $"(hunger={CurrentHunger:F0}/{MaxHunger}) — heavily slowed");
                break;
        }
    }


    public void Feed()
    {
        float prevHunger = CurrentHunger;
        CurrentHunger = MaxHunger;
        currentPhase = HungerPhase.Full;

        autoMove?.SetSpeedMultiplier(1f);
        autoMove?.StopHungerLayerSearch();

        Debug.Log($"[Hunger] {gameObject.name} fed " +
                  $"({prevHunger:F0} → {MaxHunger}). Now full — stopping hunt.");
    }

    void Starve()
    {
        Debug.Log($"[Hunger] {gameObject.name} starved to death.");
        ActorIdentity id = GetComponent<ActorIdentity>();
        if (id != null) EnvironmentDataCache.RemoveActorById(id.uniqueId);
        Destroy(gameObject);
    }
}