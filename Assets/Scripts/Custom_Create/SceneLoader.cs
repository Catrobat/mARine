using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadInteractionScene()
    {
        SceneManager.LoadScene("InteractionScene");
    }

    public void LoadPortalScene()
    {
        SceneManager.LoadScene("PortalScene");
    }
}
