using UnityEngine;

public class SharkAIController : MonoBehaviour
{
    public SharkSplineFollower splineFollower;
    public SharkFollower follower;
    public float patrolDuration = 5f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        splineFollower.enabled = true;
        follower.enabled = false;

        Invoke(nameof(StartChase), patrolDuration);     
    }

    private void StartChase()
    {
        splineFollower.enabled = false;
        follower.enabled = true;

        Debug.Log("Shark is now chasing the octopus!");
    }
}