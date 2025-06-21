using UnityEngine;

public class MovementController : MonoBehaviour
{
    public Joystick joystick;
    public float speed = 2f;
    public float rotationSpeed = 10f; // How fast the actor rotates towards movement direction

    void Start()
    {
        if (joystick == null)
        {
            GameObject joystickObj = GameObject.FindGameObjectWithTag("Joystick");
            if (joystickObj != null)
                joystick = joystickObj.GetComponent<Joystick>();
            else
                Debug.LogError("Joystick GameObject with tag 'Joystick' not found!");
        }
    }

    void Update()
    {
        if (joystick == null)
        {
            Debug.LogWarning("Joystick reference not assigned!");
            return;
        }

        Vector3 direction = new Vector3(joystick.Horizontal, 0, joystick.Vertical);

        if (direction.magnitude > 0.1f) // Add a small deadzone to avoid jitter
        {
            // Move the actor
            transform.Translate(direction.normalized * speed * Time.deltaTime, Space.World);

            // Rotate actor smoothly toward movement direction
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}
