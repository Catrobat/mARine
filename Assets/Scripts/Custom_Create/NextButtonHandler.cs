using UnityEngine;
using UnityEngine.SceneManagement;

public class NextButtonHandler : MonoBehaviour
{
    public void OnNextClicked()
    {
        EnvironmentData data = EnvironmentDataCache.GetData();
        if (data == null)
        {
            Debug.LogError("Environment data is null in cache!");
            return;
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(data.environmentName, json);
        PlayerPrefs.SetString("LastSavedEnvironment", data.environmentName);
        PlayerPrefs.SetString("SelectedModulePath", ""); // clear selected module path
        PlayerPrefs.Save();

        Debug.Log("Saved from cache on Next:\n" + json);

        SceneManager.LoadScene("PlacedActorListScene");
    }
}
