using UnityEngine;

namespace CreateEnv.UI
{
    // CustomEnvBuilder is just the settings editor; there is no environment list here.
    public class StartScreenUI : MonoBehaviour
    {
        [Header("Wiring")]
        [Tooltip("Panel carrying the EnvironmentEditorUI (the configurable settings form).")]
        public GameObject editorPanel;

        [Header("Navigation")]
        [Tooltip("Scene returned to after Save/Cancel — where saved environments are played.")]
        public string configSceneName = "FreeExploreConfig";

        private async void Start()
        {
            var editor = editorPanel != null ? editorPanel.GetComponent<EnvironmentEditorUI>() : null;
            if (editor == null)
            {
                Debug.LogWarning("[StartScreenUI] No EnvironmentEditorUI on editorPanel; returning to config.");
                await SceneLoaderBackend.LoadAddressableSceneAsync(configSceneName);
                return;
            }

            // Consume a pending edit request (one-shot handoff from FreeExploreConfig).
            string editId = EnvironmentSession.EditRequestId;
            EnvironmentSession.EditRequestId = null;
            var existing = string.IsNullOrEmpty(editId) ? null : EnvironmentRepository.Load(editId);

            // Open the settings form straight away; both Save and Cancel come back here.
            editor.Open(existing, ReturnToConfig);
        }

        private async void ReturnToConfig()
        {
            await SceneLoaderBackend.LoadAddressableSceneAsync(configSceneName);
        }
    }
}