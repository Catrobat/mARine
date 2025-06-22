using UnityEngine;
using UnityEngine.Splines;

public class SharkSplineFollower : MonoBehaviour
{
    public SplineContainer spline;
    public float speed = 2f;
    public float t = 0f;

    private void Update()
    {
        // Update t value
        t += speed * Time.deltaTime / spline.CalculateLength();
        t %= 1f; // loop

        // Evaluate new position and tangent
        Vector3 position = spline.EvaluatePosition(t);
        Vector3 tangent = spline.EvaluateTangent(t);

        // Move shark
        transform.position = position;

        // Face the direction of movement
        if (tangent != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(tangent);
        }
    }
}