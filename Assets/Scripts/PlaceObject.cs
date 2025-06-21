using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class PlaceObject : MonoBehaviour
{
    [SerializeField] private GameObject portalWithWaterPrefab; // Potal_with_water prefab
    [SerializeField] private InputActionAsset inputActions;

    private ARRaycastManager _arRaycastManager;
    private ARPlaneManager _arPlaneManager;
    private ARAnchorManager _arAnchorManager;
    private InputAction _touchAction;
    private bool _planePlaced;
    private WaterPlaneMovement _waterPlaneMovement; // Reference to the movement script

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

                    // Instantiate Potal_with_water as child of anchor
                    GameObject spawnedPortal = Instantiate(portalWithWaterPrefab, Vector3.zero, Quaternion.identity, anchor.transform);

                    // Find the HighPolyPlane child and tag it as "Underwater"
                    Transform highPolyPlane = spawnedPortal.transform.Find("HighPolyPlane");
                    if (highPolyPlane != null)
                    {
                        highPolyPlane.gameObject.tag = "Underwater";
                    }
                    else
                    {
                        Debug.LogError("HighPolyPlane not found as a child of Potal_with_water!");
                    }

                    // Get or add the WaterPlaneMovement script to control movement
                    _waterPlaneMovement = spawnedPortal.GetComponent<WaterPlaneMovement>();
                    if (_waterPlaneMovement == null)
                    {
                        _waterPlaneMovement = spawnedPortal.AddComponent<WaterPlaneMovement>();
                    }

                    // Initialize the WaterPlaneMovement script
                    _waterPlaneMovement.Initialize();

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

    // Public methods for UI buttons to call
    public void GoDeeper()
    {
        if (_waterPlaneMovement != null)
            _waterPlaneMovement.GoDeeper();
        else
            Debug.LogWarning("WaterPlaneMovement not initialized yet!");
    }

    public void GoUpper()
    {
        if (_waterPlaneMovement != null)
            _waterPlaneMovement.GoUpper();
        else
            Debug.LogWarning("WaterPlaneMovement not initialized yet!");
    }
}
