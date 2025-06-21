using UnityEngine;
using UnityEngine.SceneManagement;

public class ARSceneNavigator : MonoBehaviour
{
    public void GoToARPlacementScene()
    {
        SceneManager.LoadScene("ARPlacementScene");
    }
}
