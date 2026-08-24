using UnityEngine;

public class AutonomousMovement : MonoBehaviour
{
    public enum State { Settling, Wandering, HuntingLayer, Suspended }
    public State CurrentState { get; private set; } = State.Settling;

    // Config  set by Initialize()
    private float preferredWorldY = 0f;
    private float wanderYRange = 0.5f;
    private float wanderSpeed = 0.4f;
    private float settleSpeed = 0.3f;
    private float settleTime = 45f;
    private float dirChangeInterval = 3f;

    // Hunger-driven layer search config
    private float huntingLayerY = 0f;   // Y of the layer to search when hungry
    private float huntLayerTimeout = 15f;  // how long to search before giving up
    private bool hasHuntingLayer = false;

    // Internal
    private float settleTimer = 0f;
    private float dirTimer = 0f;
    private float huntLayerTimer = 0f;
    private Vector3 currentTarget = Vector3.zero;
    private float currentSpeed = 0f;
    private bool isInitialized = false;

 
    public void Initialize(float prefWorldY, float yRange, float wSpeed,
                           float sSpeed, float sTime, float dirInterval)
    {
        preferredWorldY = prefWorldY;
        wanderYRange = yRange;
        wanderSpeed = wSpeed;
        settleSpeed = sSpeed;
        currentSpeed = wSpeed;
        dirChangeInterval = dirInterval;
        settleTime = sTime + Random.Range(-15f, 15f);
        settleTime = Mathf.Max(10f, settleTime);
        CurrentState = State.Settling;
        settleTimer = 0f;
        isInitialized = true;

        PickNewWanderTarget();
        Debug.Log($"[AutoMove] {gameObject.name} SETTLING → Y={preferredWorldY:F1} " +
                  $"over {settleTime:F0}s");
    }

  
    public void SetHuntingLayerY(float y)
    {
        huntingLayerY = y;
        hasHuntingLayer = true;
    }

    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeed = wanderSpeed * Mathf.Max(0.1f, multiplier);
    }

    public void Suspend()
    {
        if (CurrentState != State.Suspended)
        {
            CurrentState = State.Suspended;
        }
    }

    public void Resume()
    {
        if (CurrentState == State.Suspended)
        {
            CurrentState = State.Wandering;
            PickNewWanderTarget();
        }
    }


    public void StartHungerLayerSearch()
    {
        if (!hasHuntingLayer) return;
        if (CurrentState == State.Suspended) return;
        if (CurrentState == State.HuntingLayer) return;

        CurrentState = State.HuntingLayer;
        huntLayerTimer = 0f;
        PickNewWanderTarget(huntingLayerY);
        Debug.Log($"[AutoMove] {gameObject.name} HUNGRY — searching layer Y={huntingLayerY:F1}");
    }

    public void StopHungerLayerSearch()
    {
        if (CurrentState == State.HuntingLayer)
        {
            CurrentState = State.Wandering;
            PickNewWanderTarget();
        }
    }


    void Update()
    {
        if (!isInitialized) return;

        switch (CurrentState)
        {
            case State.Settling: UpdateSettling(); break;
            case State.Wandering: UpdateWandering(); break;
            case State.HuntingLayer: UpdateHuntingLayer(); break;
            case State.Suspended: break;
        }
    }

    void UpdateSettling()
    {
        settleTimer += Time.deltaTime;
        MoveTowardTarget(settleSpeed);

        dirTimer += Time.deltaTime;
        if (dirTimer >= dirChangeInterval || ReachedTarget())
        {
            dirTimer = 0f;
            // During settling, drift toward preferred Y but roam X/Z freely
            float targetY = Mathf.Lerp(transform.position.y, preferredWorldY, 0.3f);
            PickNewWanderTarget(targetY);
        }

        if (settleTimer >= settleTime)
        {
            CurrentState = State.Wandering;
            PickNewWanderTarget();
            Debug.Log($"[AutoMove] {gameObject.name} settled → WANDERING");
        }
    }

    void UpdateWandering()
    {
        dirTimer += Time.deltaTime;
        if (dirTimer >= dirChangeInterval || ReachedTarget())
        {
            dirTimer = 0f;
            PickNewWanderTarget();
        }
        MoveTowardTarget(currentSpeed);
    }

    void UpdateHuntingLayer()
    {
        huntLayerTimer += Time.deltaTime;

        // Move toward the hunting layer target
        dirTimer += Time.deltaTime;
        if (dirTimer >= dirChangeInterval || ReachedTarget())
        {
            dirTimer = 0f;
            PickNewWanderTarget(huntingLayerY);
        }
        MoveTowardTarget(currentSpeed * 1.3f); // slightly faster when hungry

        // Give up after timeout and return to preferred layer
        if (huntLayerTimer >= huntLayerTimeout)
        {
            Debug.Log($"[AutoMove] {gameObject.name} gave up hunger search, returning home");
            CurrentState = State.Wandering;
            PickNewWanderTarget();
        }
    }

    void PickNewWanderTarget(float? targetY = null)
    {
        if (!SandboxBounds.IsInitialized)
        {
            currentTarget = transform.position + Random.insideUnitSphere * 0.5f;
            return;
        }

        float minY = SandboxBounds.MinY;
        float maxY = SandboxBounds.MaxY;

        float y;
        if (targetY.HasValue)
        {
            // Aim for a specific layer Y with some natural variance
            y = targetY.Value + Random.Range(-wanderYRange * 0.5f, wanderYRange * 0.5f);
        }
        else
        {
            // Wander within preferred Y band
            float bandMin = Mathf.Max(minY, preferredWorldY - wanderYRange);
            float bandMax = Mathf.Min(maxY, preferredWorldY + wanderYRange);
            y = Random.Range(bandMin, bandMax);
        }

        // Pick a random XZ point anywhere in the full sandbox
        // Actors should roam the entire area, not just drift near their spawn
        Vector3 c = SandboxBounds.IsInitialized
            ? SandboxBounds.Center
            : transform.position;
        float hw = SandboxBounds.IsInitialized ? SandboxBounds.HalfWidth : 2.5f;
        float hd = SandboxBounds.IsInitialized ? SandboxBounds.HalfDepth : 2.5f;

        float margin = 0.3f;
        Vector3 randomPoint = new Vector3(
            c.x + Random.Range(-(hw - margin), hw - margin),
            y,
            c.z + Random.Range(-(hd - margin), hd - margin)
        );

        currentTarget = SandboxBounds.Clamp(randomPoint);
        dirTimer = 0f;
    }

    void MoveTowardTarget(float speed)
    {
        if (currentTarget == Vector3.zero) return;

        Vector3 direction = (currentTarget - transform.position);
        if (direction.magnitude < 0.05f) return;

        Vector3 nextPos = transform.position +
                          direction.normalized * speed * Time.deltaTime;

        nextPos = SandboxBounds.Clamp(nextPos);
        transform.position = nextPos;

        // Smooth rotation to face swim direction  fish like feel
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRot, 4f * Time.deltaTime);
        }
    }

    bool ReachedTarget()
    {
        return Vector3.Distance(transform.position, currentTarget) < 0.15f;
    }

    void OnDrawGizmosSelected()
    {
        // Show preferred layer band
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.15f);
        Gizmos.DrawLine(
            transform.position + Vector3.up * (preferredWorldY - wanderYRange - transform.position.y),
            transform.position + Vector3.up * (preferredWorldY + wanderYRange - transform.position.y)
        );
        // Show current target
        if (currentTarget != Vector3.zero)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(currentTarget, 0.1f);
            Gizmos.DrawLine(transform.position, currentTarget);
        }
    }
}