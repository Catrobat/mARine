using UnityEngine;
using UnityEngine.SceneManagement;

public class SwipeBackHandler : MonoBehaviour
{
    private Vector2 startTouchPosition;
    private bool swipeStarted = false;
    public float minSwipeDistance = 100f; // minimum swipe distance in pixels to trigger back

    void Update()
    {
        // Touch input (mobile)
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began && touch.position.x < Screen.width * 0.2f)
            {
                swipeStarted = true;
                startTouchPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved && swipeStarted)
            {
                float swipeDistance = touch.position.x - startTouchPosition.x;
                if (swipeDistance > minSwipeDistance)
                {
                    Debug.Log("Swipe back detected! Going to PlacedActorListScene");
                    SceneManager.LoadScene("PlacedActorListScene");
                    swipeStarted = false;
                }
            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                swipeStarted = false;
            }
        }
        else
        {
            // Mouse input (PC)
            if (Input.GetMouseButtonDown(0) && Input.mousePosition.x < Screen.width * 0.2f)
            {
                swipeStarted = true;
                startTouchPosition = Input.mousePosition;
            }
            else if (Input.GetMouseButton(0) && swipeStarted)
            {
                float swipeDistance = Input.mousePosition.x - startTouchPosition.x;
                if (swipeDistance > minSwipeDistance)
                {
                    Debug.Log("Mouse swipe back detected! Going to PlacedActorListScene");
                    SceneManager.LoadScene("PlacedActorListScene");
                    swipeStarted = false;
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                swipeStarted = false;
            }
        }
    }
}
