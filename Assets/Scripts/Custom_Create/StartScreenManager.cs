using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

public class StartScreenManager : MonoBehaviour
{
    public Transform moduleGridContainer;       // Assign in Inspector (Scroll View Content)
    public GameObject moduleButtonPrefab;       // Assign prefab with icon + name + button
    public Button createModuleButton;           // Assign "+" button in Inspector

    void Start()
    {
        Debug.Log("Saved Modules Folder: " + Application.persistentDataPath);
        // Set up the Create (+) button
        createModuleButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene("CreateProjectScene");
        });

        // Load existing modules from persistent data path
        string[] modulePaths = ModuleSaveManager.GetAllModulePaths();

        foreach (string path in modulePaths)
        {
            string json = ModuleSaveManager.LoadModule(path);
            EnvironmentData data = JsonUtility.FromJson<EnvironmentData>(json);
            string moduleName = data.environmentName;

            GameObject btn = Instantiate(moduleButtonPrefab, moduleGridContainer);
            Text nameText = btn.transform.Find("ModuleNameText").GetComponent<Text>();
            Button loadButton = btn.GetComponent<Button>();

            nameText.text = moduleName;

            loadButton.onClick.AddListener(() =>
            {
                PlayerPrefs.SetString("LastSavedEnvironment", moduleName);
                PlayerPrefs.SetString("SelectedModulePath", path);
                PlayerPrefs.Save();
                SceneManager.LoadScene("PlacedActorListScene");
            });
        }

    }
}
