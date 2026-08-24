using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class EnvironmentPlacementManager : MonoBehaviour
{
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARPlaneManager planeManager;

    private static readonly List<ARRaycastHit> hits = new();

    private bool hasPlaced = false;

    private void Awake()
    {
        if (raycastManager == null)
            raycastManager = FindFirstObjectByType<ARRaycastManager>();
        if (planeManager == null)
            planeManager = FindFirstObjectByType<ARPlaneManager>();

        if (raycastManager == null)
        {
            Debug.LogError("[Placement] ARRaycastManager not found.");
            enabled = false;
        }
    }

    private void Update()
    {
        if (hasPlaced)
            return;

#if UNITY_EDITOR

        if (Input.GetMouseButtonDown(0))
        {
            TryPlace(Input.mousePosition);
        }

#else

        if (Input.touchCount == 0)
            return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase != TouchPhase.Began)
            return;

        TryPlace(touch.position);

#endif
    }

    private void TryPlace(Vector2 screenPosition)
    {
        if (!raycastManager.Raycast(screenPosition, hits, TrackableType.PlaneWithinPolygon))
            return;

        if (ExploreSession.SelectedBoundlessEnvironment == null)
        {
            Debug.LogError("[Placement] No environment selected.");
            return;
        }

        GameObject prefab = ExploreSession.SelectedBoundlessEnvironment.EnvironmentPrefab;

        if (prefab == null)
        {
            Debug.LogError("[Placement] Environment prefab is null.");
            return;
        }

        Transform cameraAnchor = prefab.transform.Find("CameraAnchor");

        if (cameraAnchor == null)
        {
            Debug.LogError("[Placement] 'CameraAnchor' not found in prefab.");
            return;
        }

        Pose hitPose = hits[0].pose;

        Quaternion rotation = Quaternion.Euler(
            0f,
            Camera.main.transform.eulerAngles.y,
            0f);

        // CameraAnchor position relative to the prefab root after rotation.
        Vector3 rotatedAnchorOffset = rotation * cameraAnchor.localPosition;

        // Spawn the environment so CameraAnchor ends up at the camera position.
        Vector3 spawnPosition =
            Camera.main.transform.position - rotatedAnchorOffset;

        GameObject environmentInstance = Instantiate(
            prefab,
            spawnPosition,
            rotation);

        hasPlaced = true;

        // Environment is down — stop scanning: turn off plane detection and hide the
        // detected planes so the camera no longer highlights surfaces or offers to
        // re-spawn. (Same pattern as ARPlacementController in custom-create.)
        StopPlaneDetection();

        Debug.Log("[Placement] Environment placed successfully.");
    }

    private void StopPlaneDetection()
    {
        if (planeManager != null)
        {
            planeManager.enabled = false;
            foreach (var plane in planeManager.trackables)
                plane.gameObject.SetActive(false);
        }
    }
}