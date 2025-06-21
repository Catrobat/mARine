using UnityEngine;
using UnityEngine.SceneManagement;

public class NextButtonHandler : MonoBehaviour
{
    public Transform environmentRoot; // Parent of actors + plane

    public void OnNextClicked()
    {
        string environmentName = PlayerPrefs.GetString("NewModuleName", "UnnamedModule");
        string selectedPlaneName = PlayerPrefs.GetString("SelectedEnvironmentPrefabName", "UnknownPrefab");

        EnvironmentData data = new EnvironmentData();
        data.environmentName = environmentName;
        data.environmentPlanePrefabName = selectedPlaneName;

        foreach (Transform child in environmentRoot)
        {
            if (child.CompareTag("Actor"))
            {
                PlacedActorData actor = new PlacedActorData();
                actor.prefabName = child.name.Replace("(Clone)", "").Trim();
                actor.localPosition = child.localPosition;
                actor.localRotation = child.localRotation;
                data.placedActors.Add(actor);
            }
        }

        // Save the environment data to PlayerPrefs
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(environmentName, json);
        PlayerPrefs.SetString("LastSavedEnvironment", environmentName);
        PlayerPrefs.SetString("SelectedModulePath", ""); // Clear previously selected module path
        PlayerPrefs.Save();

        Debug.Log("Saved on Next:\n" + json);

        SceneManager.LoadScene("PlacedActorListScene");
    }
}
