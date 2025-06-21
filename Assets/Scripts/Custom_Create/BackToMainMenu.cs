using UnityEngine;
using UnityEngine.SceneManagement;

public class BackToMainMenu : MonoBehaviour
{
    public void GoBackToMain()
    {
        SceneManager.LoadScene("MainScreenScene");
    }
}
