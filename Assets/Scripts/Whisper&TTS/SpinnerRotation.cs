using UnityEngine;

public class SpinnerRotation : MonoBehaviour
{
    public float speed = 180f;
    
    void Update() => transform.Rotate(0, 0, speed * Time.deltaTime);
}
