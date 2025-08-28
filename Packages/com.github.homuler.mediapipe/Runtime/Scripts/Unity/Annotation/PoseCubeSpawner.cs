using System.Collections.Generic;
using UnityEngine;
using Mediapipe;  // This is where Landmark is defined

namespace Mediapipe.Unity
{
    public class PoseCubeSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject cubePrefab;
        private GameObject spawnedCube;

        public void UpdatePose(IList<Landmark> landmarks)
        {
            if (landmarks == null || landmarks.Count == 0)
            {
                DestroyCube();
                return;
            }

            Landmark nose = landmarks[0];

            Vector3 screenPos = new Vector3(
                nose.X * UnityEngine.Screen.width,
                (1 - nose.Y) * UnityEngine.Screen.height,
                10.0f
            );

            Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);

            if (spawnedCube == null && cubePrefab != null)
            {
                SpawnCube(worldPos);
            }
            else if (spawnedCube != null)
            {
                spawnedCube.transform.position = worldPos;
                Debug.Log($"[PoseCubeSpawner] Updated cube position to {worldPos}");
            }
            else
            {
                Debug.LogWarning("[PoseCubeSpawner] No prefab assigned → cannot spawn cube.");
            }
        }

        public void SpawnCube(Vector3? position = null)
        {
            if (spawnedCube == null && cubePrefab != null)
            {
                Vector3 spawnPos = position ?? Vector3.zero;
                spawnedCube = Instantiate(cubePrefab, spawnPos, Quaternion.identity);
                Debug.Log($"[PoseCubeSpawner] Spawned cube at {spawnPos}");
            }
        }

        public void DestroyCube()
        {
            if (spawnedCube != null)
            {
                Destroy(spawnedCube);
                spawnedCube = null;
                Debug.Log("[PoseCubeSpawner] Cube destroyed.");
            }
        }

        public bool HasCube => spawnedCube != null;

        // 🔹 Expose the cube for others
        public GameObject HumanCube => spawnedCube;
    }
}
