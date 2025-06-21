using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using TMPro;
using UnityEngine.Rendering;

public class ARPlaneSpawner : MonoBehaviour
{
    [SerializeField] private GameObject waterPlanePrefab; // Potal_With_water prefab
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Volume postProcessVolume; // Optional: Assign in Inspector if in scene
    [SerializeField] private TextMeshProUGUI layerText; // Optional: Assign in Inspector if in scene

    private ARRaycastManager _arRaycastManager;
    private ARPlaneManager _arPlaneManager;
    private ARAnchorManager _arAnchorManager;
    private OceanInPortalDepthController _oceanDepthController;

    private InputAction _touchAction;
    private bool _planePlaced;

    private void Awake()
    {
        _arRaycastManager = GetComponent<ARRaycastManager>();
        _arPlaneManager = GetComponent<ARPlaneManager>();
        _arAnchorManager = GetComponent<ARAnchorManager>();

        if (_arAnchorManager == null)
        {
            Debug.LogError("ARAnchorManager component missing from the GameObject.");
        }

        _touchAction = inputActions.FindAction("Touch");
        if (_touchAction == null)
        {
            Debug.LogWarning("Touch action not found");
        }
    }

    private void OnEnable()
    {
        _touchAction?.Enable();
    }

    private void OnDisable()
    {
        _touchAction?.Disable();
    }

    void Update()
    {
        if (_planePlaced || _arAnchorManager == null) return;

        if (_touchAction.WasPerformedThisFrame())
        {
            Vector2 touchPosition = _touchAction.ReadValue<Vector2>();
            var hits = new List<ARRaycastHit>();
            if (_arRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                var hit = hits[0];
                Pose hitPose = hit.pose;
                TrackableId planeId = hit.trackableId;
                ARPlane hitPlane = _arPlaneManager.GetPlane(planeId);

                if (hitPlane != null)
                {
                    // Attach anchor to the detected plane
                    ARAnchor anchor = _arAnchorManager.AttachAnchor(hitPlane, hitPose);
                    if (anchor == null)
                    {
                        Debug.LogError("Failed to create anchor.");
                        return;
                    }

                    // Instantiate water plane as child of anchor
                    GameObject spawnedPlane = Instantiate(waterPlanePrefab, Vector3.zero, Quaternion.identity, anchor.transform);

                    // Get the OceanDepthController from the spawned prefab
                    _oceanDepthController = spawnedPlane.GetComponent<OceanInPortalDepthController>();
                    if (_oceanDepthController != null)
                    {
                        // Initialize the OceanDepthController
                        _oceanDepthController.Initialize();

                        // Optionally assign post-processing volume and UI text if they exist in the scene
                        if (postProcessVolume != null)
                            _oceanDepthController.SetPostProcessVolume(postProcessVolume);

                        if (layerText != null)
                            _oceanDepthController.SetTextMeshProUGUI(layerText);
                    }
                    else
                    {
                        Debug.LogWarning("OceanDepthController not found on the spawned prefab.");
                    }

                    _planePlaced = true;

                    // Disable plane detection and hide existing planes
                    _arPlaneManager.enabled = false;
                    foreach (var plane in _arPlaneManager.trackables)
                    {
                        plane.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    // Public methods to trigger layer changes (e.g., from UI buttons)
    public void GoDeeper()
    {
        if (_oceanDepthController != null)
            _oceanDepthController.GoDeeper();
        else
            Debug.LogWarning("OceanDepthController not initialized yet!");
    }

    public void GoUpper()
    {
        if (_oceanDepthController != null)
            _oceanDepthController.GoUpper();
        else
            Debug.LogWarning("OceanDepthController not initialized yet!");
    }
}