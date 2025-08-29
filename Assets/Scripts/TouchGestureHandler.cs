using UnityEngine;

public class TouchGestureHandler : MonoBehaviour
{
    private float initialDistance;
    private Vector3 initialScale;

    void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (t1.phase == TouchPhase.Began)
            {
                initialDistance = Vector2.Distance(t0.position, t1.position);
                initialScale = transform.localScale;
            }
            else if (t0.phase == TouchPhase.Moved || t1.phase == TouchPhase.Moved)
            {
                float currentDistance = Vector2.Distance(t0.position, t1.position);
                if (Mathf.Approximately(initialDistance, 0)) return;

                float factor = currentDistance / initialDistance;
                transform.localScale = initialScale * factor;

                // Optional: Rotation with angle between fingers
                Vector2 prevDir = t1.position - t1.deltaPosition - (t0.position - t0.deltaPosition);
                Vector2 curDir = t1.position - t0.position;
                float angle = Vector2.SignedAngle(prevDir, curDir);
                transform.Rotate(Vector3.up, -angle);
            }
        }
    }
}
