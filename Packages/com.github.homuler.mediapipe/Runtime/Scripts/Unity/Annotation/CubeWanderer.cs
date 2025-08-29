using UnityEngine;

namespace Mediapipe.Unity
{
    public class CubeWanderer : MonoBehaviour
    {
        private float baseSpeed = 1f;
        private float panicMultiplier = 2f;
        private bool panic = false;

        private float noiseOffsetX;
        private float noiseOffsetY;

        private Camera mainCam;
        private float depth;

        // 🔹 Repulsion
        [Header("Repulsion Settings")]
        [SerializeField] private float repulsionRadius = 2f;
        [SerializeField] private float repulsionStrength = 3f;
        private Transform humanTarget;

        // 🔹 Last movement direction (for rotation)
        private Vector3 lastMoveDir = Vector3.right;

        public void Init(float baseSpeed, float panicMultiplier)
        {
            this.baseSpeed = baseSpeed;
            this.panicMultiplier = panicMultiplier;
        }

        public void SetHumanTarget(Transform target)
        {
            humanTarget = target;
        }

        private void Start()
        {
            mainCam = Camera.main;
            depth = Mathf.Abs(mainCam.transform.position.z - transform.position.z);

            noiseOffsetX = Random.value * 100f;
            noiseOffsetY = Random.value * 100f;
        }

        private void Update()
        {
            float speed = baseSpeed * (panic ? panicMultiplier : 1f);

            // Wander movement
            noiseOffsetX += Time.deltaTime * 0.2f;
            noiseOffsetY += Time.deltaTime * 0.2f;
            float moveX = Mathf.PerlinNoise(noiseOffsetX, 0f) * 2f - 1f;
            float moveY = Mathf.PerlinNoise(0f, noiseOffsetY) * 2f - 1f;
            Vector3 movement = new Vector3(moveX, moveY, 0f);

            // Repel from human cube if nearby
            if (humanTarget != null)
            {
                Vector3 toCube = transform.position - humanTarget.position;
                float dist = toCube.magnitude;
                if (dist < repulsionRadius && dist > 0.001f)
                {
                    float force = (1f - dist / repulsionRadius) * repulsionStrength;
                    movement += toCube.normalized * force;
                }
            }

            // Apply movement
            Vector3 delta = movement * speed * Time.deltaTime;
            transform.position += delta;

            // 🔹 Update facing direction if moving
            if (delta.sqrMagnitude > 0.0001f)
            {
                lastMoveDir = delta.normalized;

                // Point the cube's local X-axis in movement direction
                transform.right = lastMoveDir;
            }

            // Keep inside camera FOV
            ClampToCameraView();
        }

        private void ClampToCameraView()
        {
            Vector3 screenPos = mainCam.WorldToScreenPoint(transform.position);

            screenPos.x = Mathf.Clamp(screenPos.x, 0, Screen.width);
            screenPos.y = Mathf.Clamp(screenPos.y, 0, Screen.height);

            transform.position = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, depth));
        }

        public void SetPanic(bool panicState)
        {
            panic = panicState;
        }
    }
}
