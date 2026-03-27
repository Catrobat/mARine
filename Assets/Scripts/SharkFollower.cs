using UnityEngine;
using System.Collections;

public class SharkFollower : MonoBehaviour
{
    [Header("Movement Specs")]
    public Transform octopusTransform;
    public float followSpeed = 5f;
    public float turnSpeed = 2f;
    public float stoppingDistance = 1.5f;

    private float _sqrStoppingDistance;
    private bool _isBlinded = false; // Tracks if we are currently hit by octopus ink

    void Start()
    {
        _sqrStoppingDistance = stoppingDistance * stoppingDistance;
    }

    // OctopusInk.cs calls this. Default 3s duration ensures it works even if 
    // the calling script doesn't provide a specific time.
    public void OnInkHit(float duration = 3f)
    {
        if (!_isBlinded)
        {
            StartCoroutine(ApplyInkEffect(duration));
        }
    }

    private IEnumerator ApplyInkEffect(float duration)
    {
        Debug.Log("Shark: Blinded by ink. Pausing pursuit.");
        _isBlinded = true;
        yield return new WaitForSeconds(duration);
        _isBlinded = false;
        Debug.Log("Shark: Vision restored.");
    }

    void Update()
    {
        // If the shark is blinded or has no target, We freeze movement logic here.
        if (_isBlinded || octopusTransform == null) return;

        Vector3 direction = octopusTransform.position - transform.position;

        // Performance check: Only move if we aren't already 'touching' the target.
        if (direction.sqrMagnitude < _sqrStoppingDistance) return;

        // Smoothly rotate the Shark towards the octopus.
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // Add a procedural 'wiggle' to the speed to make it look like its swimming. 
        float swimEffect = Mathf.Sin(Time.time * 5f) * 0.2f;
        transform.position += transform.forward * (followSpeed + swimEffect) * Time.deltaTime;
    }
}