using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Splines;

public class MarineBuddyMovementController: MonoBehaviour
{
    [SerializeField] private float moveSpeed = 1.5f; 
    private bool isMoving = false;

    [SerializeField] public GameObject canvas;  // Canvas object attached to seahorse
    [SerializeField] public SplineContainer splineContainer;

    public IEnumerator FollowSpline(SplineContainer spline, UnityAction onReached = null, float duration = 5f)
    {
        if (isMoving || spline == null)
            yield break;


        canvas.SetActive(false);
        isMoving = true;

        float time = 0f;
        while (time < duration)
        {
            float t = time/duration;
            Vector3 currentPos = spline.EvaluatePosition(t);
            transform.position = currentPos;

            // Smooth LookAt
            float nextT = Mathf.Min(t + 0.01f, 1f);  // Clamp nextT to 1.0
            Vector3 nextPos = spline.EvaluatePosition(nextT);
            Vector3 direction = (nextPos - currentPos).normalized;

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }

            time += Time.deltaTime;
            UpdateCanvasOrientation();
            yield return null;
        }

        transform.position = spline.EvaluatePosition(1f);  // snap to end
        isMoving = false;
        canvas.SetActive(true);
        onReached?.Invoke();
    }

    private void UpdateCanvasOrientation()  
    {
        if (canvas != null)
        {
            canvas.transform.LookAt(Camera.main.transform.position, Vector3.up);
            // Optional: Lock the canvas to always be upright by setting local rotation
           // canvas.transform.localEulerAngles = new Vector3(0, canvas.transform.localEulerAngles.y, 0);
        }
    }

    private void Update()
    {
        if (!isMoving && canvas != null)
        {
            UpdateCanvasOrientation();
        }
    }
}
