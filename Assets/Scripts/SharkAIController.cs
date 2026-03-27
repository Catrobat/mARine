using UnityEngine;

public class SharkAIController : MonoBehaviour
{
    public Transform octopusTransform;
    public float detectionRange = 10f;

    private SharkFollower _follower;
    private SharkSplineFollower _splineFollower;
    private float _sqrDetectionRange; // Optimization: Store squared value to skip Sqrt()

    void Awake()
    {
        _follower = GetComponent<SharkFollower>();
        _splineFollower = GetComponent<SharkSplineFollower>();

        // Pre-calculating this once saves us a Mathf.Sqrt call 60 times a second.
        _sqrDetectionRange = detectionRange * detectionRange;
    }

    void Update()
    {
        // Safety check: if the target is destroyed or missing, we stay in idle/spline mode.
        if (octopusTransform == null) return;

        // Using sqrMagnitude is significantly faster than Vector3.Distance for mobile AR.
        float sqrDistance = (octopusTransform.position - transform.position).sqrMagnitude;

        if (sqrDistance < _sqrDetectionRange)
        {
            if (!_follower.enabled)
            {
                Debug.Log("Shark: Octopus in range. Initiating Pursuit.");
                StartChase();
            }
        }
        else
        {
            if (_follower.enabled)
            {
                Debug.Log("Shark: Target lost. Returning to Spline Path.");
                StopChase();
            }
        }
    }

    private void StartChase()
    {
        // Disable pathfinding so it doesn't fight against the pursuit logic. 
        if (_splineFollower != null) _splineFollower.enabled = false;
        if (_follower != null) _follower.enabled = true;
    }

    private void StopChase()
    {
        if (_follower != null) _follower.enabled = false;
        if (_splineFollower != null) _splineFollower.enabled = true;
    }
}