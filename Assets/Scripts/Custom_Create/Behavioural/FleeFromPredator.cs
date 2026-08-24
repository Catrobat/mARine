using UnityEngine;


public class FleeFromPredator : MonoBehaviour
{
    private int myTier = 1;
    private int fleeTierDifference = 1;
    private float fleeDetectionRadius = 4f;
    private float fleeSpeed = 2.5f;
    private float escapeChance = 0.7f;

    private float checkTimer = 0f;
    private const float fleeCheckInterval = 0.25f;
    private Vector3 fleeDirection = Vector3.zero;
    private bool isFleeing = false;
    private float fleeDuration = 1.5f;
    private float fleeTimer = 0f;

    // Cooldown so defense ability isn't spammed every check
    private float defenseCooldown = 0f;
    private const float defenseCooldownDuration = 6f;
    private AutonomousMovement autoMove;

    public void Initialize(int tier, int tierDifference, float detectionRadius, float speed,
                           float escape = 0.7f)
    {
        myTier = tier;
        fleeTierDifference = tierDifference;
        fleeDetectionRadius = detectionRadius;
        fleeSpeed = speed;
        escapeChance = escape;
    }

    void Update()
    {
        if (defenseCooldown > 0f)
            defenseCooldown -= Time.deltaTime;

        checkTimer += Time.deltaTime;
        if (checkTimer >= fleeCheckInterval)
        {
            checkTimer = 0f;
            CheckForPredators();
        }

        if (isFleeing)
            PerformFlee();
    }

    void CheckForPredators()
    {
        Vector3 threatCenter = Vector3.zero;
        int threatCount = 0;
        GameObject nearestPredator = null;
        float nearestDist = float.MaxValue;

        Collider[] nearby = Physics.OverlapSphere(transform.position, fleeDetectionRadius);
        foreach (Collider col in nearby)
        {
            if (!col.CompareTag("Actor")) continue;
            if (col.gameObject == gameObject) continue;

            ActorTierIdentity tierID = col.GetComponent<ActorTierIdentity>();
            if (tierID == null) continue;
            if (tierID.Tier - myTier < fleeTierDifference) continue;

            // Skip stunned predators — they can't hunt
            FoodConsumer fc = col.GetComponent<FoodConsumer>();
            if (fc != null && fc.IsStunned) continue;

            threatCenter += col.transform.position;
            threatCount++;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearestPredator = col.gameObject;
            }
        }

        if (threatCount == 0) return;

        // Chance-based escape roll
        if (Random.value > escapeChance)
        {
            Debug.Log($"[Flee] {gameObject.name} escape roll FAILED — predator gains ground");
            isFleeing = false;
            return;
        }

        // Try to trigger defense ability (e.g. camouflage) — has its own chance roll
        if (nearestPredator != null && defenseCooldown <= 0f)
        {
            TryTriggerDefense(nearestPredator);
        }

        // Flee away from average threat position
        threatCenter /= threatCount;
        fleeDirection = (transform.position - threatCenter).normalized;

        // Redirect if heading out of bounds
        fleeDirection = SandboxBounds.GetRedirectedDirection(transform.position, fleeDirection);

        isFleeing = true;
        fleeTimer = fleeDuration;

        // Suspend autonomous wandering while fleeing
        if (autoMove == null) autoMove = GetComponent<AutonomousMovement>();
        autoMove?.Suspend();
    }

   
    void TryTriggerDefense(GameObject predator)
    {
        ActorAbility[] abilities = GetComponents<ActorAbility>();
        foreach (var ability in abilities)
        {
            // Only abilities that support auto-defense trigger it
            // CamouflageAbility implements TriggerDefense via the interface
            if (ability is CamouflageAbility cam)
            {
                bool triggered = cam.TriggerDefense(predator);
                if (triggered)
                {
                    defenseCooldown = defenseCooldownDuration;
                    Debug.Log($"[Flee] {gameObject.name} defense triggered via {cam.AbilityName}");
                }
                break;
            }
        }
    }

    void PerformFlee()
    {
        fleeTimer -= Time.deltaTime;
        if (fleeTimer <= 0f)
        {
            isFleeing = false;
            fleeDirection = Vector3.zero;
            // Resume autonomous wandering after flee ends
            if (autoMove == null) autoMove = GetComponent<AutonomousMovement>();
            autoMove?.Resume();
            return;
        }

        Vector3 nextPosition = transform.position + fleeDirection * fleeSpeed * Time.deltaTime;
        transform.position = SandboxBounds.Clamp(nextPosition);

        if (fleeDirection != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(fleeDirection), 10f * Time.deltaTime);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, fleeDetectionRadius);
    }
}