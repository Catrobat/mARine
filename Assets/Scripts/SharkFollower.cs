using System.Collections;
using UnityEngine;

public class SharkFollower : MonoBehaviour
{
    public Transform octopusTransform;
    public float followSpeed = 2f;
    public float followDistance = 3f;
    private bool _avoidInk = false;

    private void Update()
    {
        if (_avoidInk) return;

        Vector3 direction = (octopusTransform.position - transform.position).normalized;
        float distance = Vector3.Distance(octopusTransform.position, transform.position);

        if (distance > followDistance)
        {
            transform.position += direction * (followSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(direction),
                Time.deltaTime * 3f);
        }
    }

    public void OnInkHit()
    {
        _avoidInk = true;
        StartCoroutine(BackOff());
    }

    private IEnumerator BackOff()
    {
        Vector3 retreatDir = -transform.forward;
        float retreatTime = 2f;
        float t = 0;

        while (t < retreatTime)
        {
            transform.position += retreatDir * (followSpeed * Time.deltaTime);
            t += Time.deltaTime;
            yield return null;
        }

        _avoidInk = false;
    }

    public void ExitScene()
    {
        // Shark swims upward or off-screen
        StartCoroutine(SwimAway());
    }

    private IEnumerator SwimAway()
    {
        Vector3 exitDir = Vector3.up + transform.forward;
        while (true)
        {
            transform.position += exitDir.normalized * (followSpeed * 1.5f * Time.deltaTime);
            yield return null;
        }
    }
}