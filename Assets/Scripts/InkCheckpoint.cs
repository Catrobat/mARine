using UnityEngine;

public class InkCheckpoint : MonoBehaviour
{
    public Transform octopusTransform;
    public GameObject spewInkPromptUI;
    public float activationRadius = 2.5f;
    public bool isPromptActivated = false;

    private void Update()
    {
        float distance = Vector3.Distance(octopusTransform.position, transform.position);

        if (distance <= activationRadius && !isPromptActivated)
        {
            spewInkPromptUI.SetActive(true);
            isPromptActivated = true;
        }
        else if (distance > activationRadius && isPromptActivated)
        {
            spewInkPromptUI.SetActive(false);
            isPromptActivated = false;
        }
    }
}
