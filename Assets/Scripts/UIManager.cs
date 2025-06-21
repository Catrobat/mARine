using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public void LoadARScene()
    {
        SceneManager.LoadScene("AR_Spawn");
    }

    public void LoadFreeExploreEcosystem()
    {
        SceneManager.LoadScene("FreeExplore");
    }

    public void LoadModuleWise()
    {
        SceneManager.LoadScene("ModuleWise");
    }

    public void LoadCustomCreateScene()
    {
        SceneManager.LoadScene("StartScreenScene");
    }
}
