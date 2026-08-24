using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public async void LoadARScene()
    {
        await SceneLoaderBackend.LoadAddressableSceneAsync("AISpawnerScene");
    }

    public async void LoadFreeExploreEcosystem()
    {
        await SceneLoaderBackend.LoadAddressableSceneAsync("FreeExploreConfig");
    }

    public void LoadCustomCreateScene()
    {
        SceneManager.LoadScene("StartScreenScene");
    }

    public void LoadHumanInteractionScene()
    {
        SceneManager.LoadScene("FaceARScene");
    }
}
