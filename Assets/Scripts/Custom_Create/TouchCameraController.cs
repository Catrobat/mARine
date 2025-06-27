using UnityEngine;
using UnityEngine.EventSystems;

public class TouchCameraController : MonoBehaviour
{
    public float panSpeed = 0.5f;
    public float zoomSpeed = 0.5f;
    public float minZoom = 5f;
    public float maxZoom = 25f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandlePCControls();
#else
        HandleTouchControls();
#endif
    }

    void HandlePCControls()
    {
        if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl)) return;

        // Pan with Ctrl + LMB
        if (Input.GetMouseButton(0))
        {
            float moveX = -Input.GetAxis("Mouse X") * panSpeed;
            float moveZ = -Input.GetAxis("Mouse Y") * panSpeed;

            transform.Translate(new Vector3(moveX, 0, moveZ), Space.World);
        }

        // Zoom with Ctrl + Scroll
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.01f)
        {
            Vector3 camPos = transform.position;
            camPos.y -= scroll * zoomSpeed * 100f * Time.deltaTime;
            camPos.y = Mathf.Clamp(camPos.y, minZoom, maxZoom);
            transform.position = camPos;
        }
    }

    void HandleTouchControls()
    {
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 delta = touch.deltaPosition;
                Vector3 move = new Vector3(-delta.x * panSpeed * Time.deltaTime, 0f, -delta.y * panSpeed * Time.deltaTime);
                transform.Translate(move, Space.World);
            }
        }
        else if (Input.touchCount == 2)
        {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);

            if (EventSystem.current.IsPointerOverGameObject(t0.fingerId) ||
                EventSystem.current.IsPointerOverGameObject(t1.fingerId))
                return;

            Vector2 prevPos0 = t0.position - t0.deltaPosition;
            Vector2 prevPos1 = t1.position - t1.deltaPosition;

            float prevDistance = Vector2.Distance(prevPos0, prevPos1);
            float currDistance = Vector2.Distance(t0.position, t1.position);

            float delta = prevDistance - currDistance;

            Vector3 camPos = transform.position;
            camPos.y += delta * zoomSpeed * Time.deltaTime;
            camPos.y = Mathf.Clamp(camPos.y, minZoom, maxZoom);
            transform.position = camPos;
        }
    }
}
