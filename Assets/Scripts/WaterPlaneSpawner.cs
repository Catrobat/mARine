using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;
using UnityEngine.UI;

public class WaterPlaneSpawner : MonoBehaviour
{
    [SerializeField] private GameObject waterPlanePrefab;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform fixedWorldContainer;

    private ARRaycastManager _arRaycastManager;
    private ARPlaneManager _arPlaneManager;
    private ARAnchorManager _arAnchorManager;

    private WaterPlaneMover _moverInstance;
    public Button upButton;
    public Button downButton;
    // public GameObject waterCamPlane;

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
                    GameObject spawnedPlane = Instantiate(waterPlanePrefab, hitPose.position, hitPose.rotation, fixedWorldContainer);
                    _moverInstance = spawnedPlane.GetComponent<WaterPlaneMover>();
                    
                    // Assign button listeners
                    upButton.onClick.AddListener(_moverInstance.MoveWaterDown);
                    downButton.onClick.AddListener(_moverInstance.MoveWaterUp);

                    // No need to set water surface on OceanDepthController anymore
                    /*
                    OceanDepthController depthController = FindAnyObjectByType<OceanDepthController>();
                    if (depthController != null)
                    {
                        depthController.SetWaterSurface(spawnedPlane.transform);
                    }
                    else
                    {
                        Debug.LogWarning("OceanDepthAndWaterFXController not found.");
                    }
                    */

                    // waterCamPlane.SetActive(true);
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
}