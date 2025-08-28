using UnityEngine;
using UnityEngine.UI;

namespace Mediapipe.Unity
{
    public class UICubeToggle : MonoBehaviour
    {
        [SerializeField] private PoseCubeSpawner cubeSpawner;
        [SerializeField] private Toggle cubeToggle;

        private const string PrefKey = "CubeToggleState";
        private bool lastCubeState = false;

        private void Start()
        {
            if (cubeSpawner == null)
            {
                Debug.LogWarning("[UICubeToggle] CubeSpawner not assigned in Inspector.");
                return;
            }

            // Load last saved choice (default OFF)
            lastCubeState = PlayerPrefs.GetInt(PrefKey, 0) == 1;

            if (cubeToggle != null)
            {
                cubeToggle.isOn = false; // Always start OFF until person detected
                cubeToggle.interactable = false;

                cubeToggle.onValueChanged.AddListener(OnToggleChanged);
            }
            else
            {
                Debug.LogWarning("[UICubeToggle] Toggle not assigned in Inspector.");
            }
        }

        private void OnToggleChanged(bool isOn)
        {
            if (cubeSpawner != null)
            {
                if (isOn)
                    cubeSpawner.SpawnCube();
                else
                    cubeSpawner.DestroyCube();

                lastCubeState = isOn;

                // Save choice
                PlayerPrefs.SetInt(PrefKey, isOn ? 1 : 0);
                PlayerPrefs.Save();

                Debug.Log($"[UICubeToggle] Cube toggled {(isOn ? "ON" : "OFF")} (saved)");
            }

            // --- Panic mode for wanderers ---
            CubeWanderer[] wanderers = FindObjectsByType<CubeWanderer>(FindObjectsSortMode.None);
            foreach (var w in wanderers)
                w.SetPanic(isOn);
        }

        public void SetPersonDetected(bool detected)
        {
            if (cubeToggle == null || cubeSpawner == null)
                return;

            cubeToggle.interactable = detected;

            if (detected)
            {
                // Restore saved state but without triggering OnToggleChanged again
                lastCubeState = PlayerPrefs.GetInt(PrefKey, 0) == 1;

                cubeToggle.onValueChanged.RemoveListener(OnToggleChanged); // prevent double-call
                cubeToggle.isOn = lastCubeState;
                cubeToggle.onValueChanged.AddListener(OnToggleChanged);

                if (lastCubeState)
                    cubeSpawner.SpawnCube();
                else
                    cubeSpawner.DestroyCube();

                Debug.Log($"[UICubeToggle] Person detected → restoring Cube {(lastCubeState ? "ON" : "OFF")}");

                // Sync panic state for wanderers
                CubeWanderer[] wanderers = FindObjectsByType<CubeWanderer>(FindObjectsSortMode.None);
                foreach (var w in wanderers)
                    w.SetPanic(lastCubeState);
            }
            else
            {
                // Person lost → force OFF
                cubeSpawner.DestroyCube();

                cubeToggle.onValueChanged.RemoveListener(OnToggleChanged);
                cubeToggle.isOn = false;
                cubeToggle.onValueChanged.AddListener(OnToggleChanged);

                // Reset wanderers to normal
                CubeWanderer[] wanderers = FindObjectsByType<CubeWanderer>(FindObjectsSortMode.None);
                foreach (var w in wanderers)
                    w.SetPanic(false);

                Debug.Log("[UICubeToggle] Person lost → Cube OFF, toggle disabled.");
            }
        }

        private void OnDestroy()
        {
            if (cubeToggle != null)
                cubeToggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }
}
