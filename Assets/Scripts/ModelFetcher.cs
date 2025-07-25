using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Threading.Tasks;
using GLTFast;

[System.Serializable]
public class ModelInfo
{
    public string name;
    public string model_url;
}

public class ModelFetcher : MonoBehaviour
{
    [SerializeField] private int modelId = 2;
    [SerializeField] private string backendUrl = "http://localhost:8080/api/model";

    private async void Start()
    {
        await FetchModelByIdAsync(); // Call async directly (no coroutine needed)
    }

    private async Task FetchModelByIdAsync()
    {
        string url = $"{backendUrl}?id={modelId}";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            var operation = www.SendWebRequest();
            while (!operation.isDone)
                await Task.Yield(); // don't block main thread

            if (www.result == UnityWebRequest.Result.Success)
            {
                string rawJson = www.downloadHandler.text;
                ModelInfo model = JsonUtility.FromJson<ModelInfo>(rawJson);

                Debug.Log($"Fetched model: {model.name}, URL: {model.model_url}");

                await LoadModelAsync(model.model_url);
            }
            else
            {
                Debug.LogError("Error fetching model: " + www.error);
            }
        }
    }

    private async Task LoadModelAsync(string url)
    {
        Debug.Log("Trying to load model from: " + url);

        var gltf = new GltfImport();
        bool success = await gltf.Load(new System.Uri(url));

        if (success)
        {
            GameObject modelGO = new GameObject("LoadedModel");
            gltf.InstantiateMainScene(modelGO.transform);

            Debug.Log("Model loaded and instantiated.");
        }
        else
        {
            Debug.LogError("Failed to load model at: " + url);
        }
    }
}

