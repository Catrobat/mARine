using UnityEngine;

public class Billboard : MonoBehaviour
{
    public Camera cam;

    void LateUpdate()
    {
        if (cam != null)
            transform.LookAt(transform.position + cam.transform.rotation * Vector3.forward, cam.transform.rotation * Vector3.up);
    }
}
