using UnityEngine;
using System.Collections;

public class WaterPlaneMovement : MonoBehaviour
{
    private Transform highPolyPlane; // The HighPolyPlane Transform
    private Vector3 initialPosition; // Initial position of the HighPolyPlane
    private bool isMoving = false;
    private const float moveDistance = 0.5f; // Distance to move per button press
    private const float moveDuration = 1f; // Duration of the movement

    public void Initialize()
    {
        // Find the HighPolyPlane child tagged as "Underwater"
        highPolyPlane = transform.Find("HighPolyPlane");
        if (highPolyPlane != null && highPolyPlane.CompareTag("Underwater"))
        {
            initialPosition = highPolyPlane.localPosition;
        }
        else
        {
            Debug.LogError("HighPolyPlane with tag 'Underwater' not found in the hierarchy!");
            highPolyPlane = null;
        }
    }

    public void GoDeeper()
    {
        if (highPolyPlane == null || isMoving) return;
        StartCoroutine(MovePlane(Vector3.up * moveDistance)); // Move up on Y-axis
    }

    public void GoUpper()
    {
        if (highPolyPlane == null || isMoving) return;
        StartCoroutine(MovePlane(Vector3.down * moveDistance)); // Move down on Y-axis
    }

    private IEnumerator MovePlane(Vector3 direction)
    {
        isMoving = true;

        Vector3 startPos = highPolyPlane.localPosition;
        Vector3 targetPos = startPos + direction;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            float t = elapsed / moveDuration;
            float smoothStep = Mathf.SmoothStep(0f, 1f, t);
            highPolyPlane.localPosition = Vector3.Lerp(startPos, targetPos, smoothStep);
            elapsed += Time.deltaTime;
            yield return null;
        }

        highPolyPlane.localPosition = targetPos;
        isMoving = false;
    }
}