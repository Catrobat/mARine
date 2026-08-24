using UnityEngine;
using UnityEngine.EventSystems;

public class MovementController : MonoBehaviour
{
    [Header("Horizontal Movement")]
    public Joystick joystick;
    public float speed = 2f;
    public float rotationSpeed = 10f;

    [Header("Vertical Movement")]
    [Tooltip("Speed of swimming up/down when buttons are held")]
    public float verticalSpeed = 1.5f;

    // Set true while the Up or Down button is held
    private bool isMovingUp = false;
    private bool isMovingDown = false;

    void Start()
    {
        if (joystick == null)
        {
            GameObject joystickObj = GameObject.FindGameObjectWithTag("Joystick");
            if (joystickObj != null)
                joystick = joystickObj.GetComponent<Joystick>();
            else
                Debug.LogError("[MovementController] Joystick GameObject with tag 'Joystick' not found!");
        }
    }

    void Update()
    {
        HandleHorizontalMovement();
        HandleVerticalMovement();
    }

    void HandleHorizontalMovement()
    {
        if (joystick == null)
        {
            Debug.LogWarning("[MovementController] Joystick reference not assigned!");
            return;
        }

        Vector3 direction = new Vector3(joystick.Horizontal, 0, joystick.Vertical);

        if (direction.magnitude > 0.1f)
        {
            Vector3 nextPosition = transform.position + direction.normalized * speed * Time.deltaTime;

            // Keep horizontal movement within sandbox X/Z bounds
            nextPosition = SandboxBounds.ClampHorizontal(nextPosition);
            transform.position = nextPosition;

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }

    void HandleVerticalMovement()
    {
        if (!isMovingUp && !isMovingDown) return;

        float verticalDelta = 0f;
        if (isMovingUp) verticalDelta += verticalSpeed * Time.deltaTime;
        if (isMovingDown) verticalDelta -= verticalSpeed * Time.deltaTime;

        Vector3 pos = transform.position;
        pos.y += verticalDelta;

        // Clamp to surface-to-seafloor range so player can't swim
        // above the top layer or below the bottom layer
        pos.y = SandboxBounds.ClampY(pos.y);

        transform.position = pos;
    }

    
    public void StartMovingUp()
    {
        isMovingUp = true;
    }

    
    public void StopMovingUp()
    {
        isMovingUp = false;
    }

    
    public void StartMovingDown()
    {
        isMovingDown = true;
    }

    public void StopMovingDown()
    {
        isMovingDown = false;
    }
}