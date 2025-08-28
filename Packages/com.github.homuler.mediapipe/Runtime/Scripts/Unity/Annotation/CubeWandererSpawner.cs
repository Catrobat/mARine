using UnityEngine;

namespace Mediapipe.Unity
{
    public class CubeWandererSpawner : MonoBehaviour
    {
        [Header("Spawner Settings")]
        [SerializeField] private GameObject wandererPrefab;
        [SerializeField] private int count = 10;
        [SerializeField] private float spawnDepth = 10f;
        [SerializeField] private float screenPadding = 0.1f; // % padding from edges

        [Header("Wanderer Movement Settings")]
        [SerializeField] private float baseSpeed = 1f;
        [SerializeField] private float panicMultiplier = 2f;

        private Camera mainCam;

        private void Start()
        {
            mainCam = Camera.main;

            if (wandererPrefab == null)
            {
                Debug.LogError("[CubeWandererSpawner] No prefab assigned!");
                return;
            }

            SpawnWanderers();
        }

        private void SpawnWanderers()
        {
            PoseCubeSpawner poseSpawner = FindObjectOfType<PoseCubeSpawner>();

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = GetRandomPositionInView();

                GameObject wanderer = Instantiate(wandererPrefab, pos, Quaternion.identity);

                CubeWanderer cw = wanderer.GetComponent<CubeWanderer>();
                if (cw == null)
                    cw = wanderer.AddComponent<CubeWanderer>();

                cw.Init(baseSpeed, panicMultiplier);

                // 🔹 Assign human cube as repulsion target
                if (poseSpawner != null && poseSpawner.HumanCube != null)
                {
                    cw.SetHumanTarget(poseSpawner.HumanCube.transform);
                }
            }
        }

        private Vector3 GetRandomPositionInView()
        {
            float x = Random.Range(screenPadding, 1f - screenPadding) * Screen.width;
            float y = Random.Range(screenPadding, 1f - screenPadding) * Screen.height;

            Vector3 screenPos = new Vector3(x, y, spawnDepth);
            return mainCam.ScreenToWorldPoint(screenPos);
        }
    }
}
