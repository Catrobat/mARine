using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

public class WaterPlaneSpawner : MonoBehaviour
{
    [SerializeField] private GameObject waterPlanePrefab;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform fixedWorldContainer;
    [SerializeField] private FreeExpGoalManager goalManager;    
    [SerializeField] private CrossPlatformTTS globalTTS;

    private ARRaycastManager _arRaycastManager;
    private ARPlaneManager _arPlaneManager;
    private ARAnchorManager _arAnchorManager;

    private WaterPlaneMover _moverInstance;

    private InputAction _touchAction;
    private bool _planePlaced;
    private bool _scanGoalTriggered = false;

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
        if (_planePlaced || goalManager == null || goalManager.GetCurrentGoalType() == FreeExpGoalManager.FreeExplorerGoals.TutorialComplete)
            return;

        // Handle Scan and auto-completion (2s over, plane visible now)
        if (!_scanGoalTriggered && goalManager.GetCurrentGoalType() == FreeExpGoalManager.FreeExplorerGoals.Scan)
        {
            if (_arPlaneManager.trackables.count > 0)
            {
                _scanGoalTriggered = true;
                goalManager.CompleteCurrentGoal();
            }
        }

        // Tap-to-place
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

                    // Dynamically find the MarineBuddy in the instantiated prefab
                    MarineBuddy buddy = spawnedPlane.GetComponentInChildren<MarineBuddy>(true);
                    if (buddy != null)
                    {
                        goalManager.SetMarineBuddy(buddy);
                        buddy.SetTTSManager(globalTTS);
                    }
                    
                    _planePlaced = true;

                    // Disable plane detection and hide existing planes
                    _arPlaneManager.enabled = false;
                    foreach (var plane in _arPlaneManager.trackables)
                        plane.gameObject.SetActive(false);
                }
            }

            // Trigger Spawn step to complete
            if (goalManager.GetCurrentGoalType() == FreeExpGoalManager.FreeExplorerGoals.Spawn)
            {
                goalManager.CompleteCurrentGoal();
            }
        }
    }
}
