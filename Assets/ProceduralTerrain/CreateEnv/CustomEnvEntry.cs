using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv
{
    [RequireComponent(typeof(Button))]
    public class CustomEnvEntry : MonoBehaviour
    {
        [SerializeField] private string sceneName = "CustomEnvBuilder";

        private void Start()
        {
            GetComponent<Button>().onClick.AddListener(LoadCustomEnvironment);
        }

        private async void LoadCustomEnvironment()
        {
            await SceneLoaderBackend.LoadAddressableSceneAsync(sceneName);
        }
    }
}