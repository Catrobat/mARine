using UnityEngine;

public class ColorChange : MonoBehaviour
{
    public Material newMaterial;
    public GameObject targetModel;

    public void ChangeColor()
    {
        Renderer[] renderers = targetModel.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in renderers)
        {
            rend.material = newMaterial;
        }
    }
}
