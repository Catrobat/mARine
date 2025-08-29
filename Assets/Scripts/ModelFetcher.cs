using GLTFast;
using GLTFast.Logging;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[System.Serializable]
public class ModelInfo
{
    public int id;
    public string name;
    public string description;
    public string category;
    public string model_url;
    public string thumbnail_url;
    public string created_at;
}

public class ModelFetcher : MonoBehaviour
{
    [SerializeField] private int modelId = 2;
    [SerializeField] private string backendUrl = "http://localhost:8080/api/model";

    private async void Start()
    {
        try
        {
            await FetchModelByIdAsync();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ModelFetcher] Error in Start: {e}");
        }
    }

    private async Task FetchModelByIdAsync()
    {
        string url = $"{backendUrl}?id={modelId}";

        using UnityWebRequest www = UnityWebRequest.Get(url);
        var operation = www.SendWebRequest();

        while (!operation.isDone)
            await Task.Yield();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ModelFetcher] API request failed: {www.error}\nResponse: {www.downloadHandler?.text}");
            return;
        }

        string rawJson = www.downloadHandler.text;

        if (string.IsNullOrEmpty(rawJson))
        {
            Debug.LogError("[ModelFetcher] Empty JSON response from backend.");
            return;
        }

        ModelInfo model = JsonUtility.FromJson<ModelInfo>(rawJson);

        if (model == null || string.IsNullOrEmpty(model.model_url))
        {
            Debug.LogError("[ModelFetcher] Invalid model data or missing model_url.");
            return;
        }

        Debug.Log($"[ModelFetcher] Loaded metadata: {model.name} - {model.model_url}");

        await LoadModelAsync(model.model_url);
    }

    private async Task LoadModelAsync(string url)
    {
        if (!UriIsValid(url))
        {
            Debug.LogError($"[ModelFetcher] Invalid model URL: {url}");
            return;
        }

        Debug.Log($"[ModelFetcher] Loading GLB from: {url}");

            var importSettings = new ImportSettings
        {
            // Removed generateMipMaps as it's not a valid property
        };

        var gltf = new GltfImport(logger: new ConsoleLogger());

        try
        {
            bool loaded = await gltf.Load(new System.Uri(url), importSettings);

            if (!loaded)
            {
                Debug.LogError($"[ModelFetcher] GLB Load() returned false.");
                return;
            }

            GameObject instantiatedGO = new GameObject("LoadedModel");
            bool instantiated = await gltf.InstantiateMainSceneAsync(instantiatedGO.transform);

            if (!instantiated)
            {
                Debug.LogError("[ModelFetcher] InstantiateMainSceneAsync returned false.");
                Destroy(instantiatedGO);
                return;
            }

            instantiatedGO.transform.position = Vector3.zero;
            instantiatedGO.transform.localScale = Vector3.one;
            instantiatedGO.layer = LayerMask.NameToLayer("Default");

            DebugLogModelDetails(instantiatedGO);

            // Optional: Apply URP shader override
            Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpShader != null)
            {
                var renderers = instantiatedGO.GetComponentsInChildren<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    foreach (var mat in renderer.materials)
                    {
                        if (mat != null)
                            mat.shader = urpShader;
                    }
                }
            }

            // Focus camera on the loaded model
            FocusCameraOn(instantiatedGO.transform);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[ModelFetcher] Exception during model load: {ex.Message}");
        }
        finally
        {
            gltf?.Dispose();
        }
    }

    private void DebugLogModelDetails(GameObject go)
    {
        Debug.Log($"[ModelFetcher] Model Hierarchy:");
        PrintHierarchy(go.transform);

        var meshFilters = go.GetComponentsInChildren<MeshFilter>();
        foreach (var mf in meshFilters)
        {
            Debug.Log($"MeshFilter: {mf.name}, Mesh: {(mf.sharedMesh != null ? mf.sharedMesh.name : "NULL")}");
        }

        var renderers = go.GetComponentsInChildren<MeshRenderer>();
        foreach (var mr in renderers)
        {
            Debug.Log($"Renderer: {mr.name}, Materials: {mr.materials.Length}");
            foreach (var mat in mr.materials)
            {
                Debug.Log(mat != null ? $"Material: {mat.name}, Shader: {mat.shader?.name}" : "Material is NULL");
            }
        }
    }

    private void PrintHierarchy(Transform parent, string indent = "")
    {
        Debug.Log(indent + parent.name);
        foreach (Transform child in parent)
        {
            PrintHierarchy(child, indent + "  ");
        }
    }

    private void FocusCameraOn(Transform target)
    {
        if (Camera.main == null)
            return;

        Vector3 center = target.position + Vector3.up * 0.5f;
        Camera.main.transform.position = center + new Vector3(0, 0.5f, -2f);
        Camera.main.transform.LookAt(center);
    }

    private bool UriIsValid(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out _);
    }
}