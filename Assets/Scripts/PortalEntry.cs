using UnityEngine;

public class PortalEntry : MonoBehaviour
{
    public GameObject door; // Door to rotate

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            // Instantly rotate door by 90 degrees on Y
            door.transform.Rotate(0f, 90f, 0f);
            Debug.Log("Trigger Entered by: " + other.name);
        }
    }
}
