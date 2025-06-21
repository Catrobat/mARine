using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARRaycastDebug : MonoBehaviour
{
    [SerializeField] private Text debugText;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private ARPlaneManager arPlaneManager;
    [SerializeField] private ARAnchorManager arAnchorManager;
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private GameObject portalPrefab;
    
    
    private InputAction _touchAction;
    private bool _portalSpawned;

    private void Awake()
    {
        _touchAction = inputActions.FindAction("Touch");
        if (_touchAction == null)
        {
            Debug.LogWarning("Touch action not found");
        }
    }

    private void OnEnable()
    {
        _touchAction.Enable();
    }

    private void OnDisable()
    {
        _touchAction.Disable();
    }

    private void Start()
    {
        debugText.text = "application started";
    }

    private void Update()
    {
        if (_touchAction.WasPerformedThisFrame() && !_portalSpawned)
        {
            Vector2 touchPosition = _touchAction.ReadValue<Vector2>();
            debugText.text = "touch position: " + touchPosition;

            var hits = new System.Collections.Generic.List<ARRaycastHit>();
            if (arRaycastManager.Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                TrackableId planeId = hits[0].trackableId;
                ARPlane hitPlane = arPlaneManager.GetPlane(planeId);
                
                PlacePortal(hitPose, hitPlane);
            }
            else
            {
                debugText.text = "plane detected: " + false;
            }
        }
    }
    
    private void PlacePortal(Pose pose, ARPlane arPlane)
    {
        if (!arPlane) return;
        
        // Attach an anchor to the detected plane at the pose
        ARAnchor anchor = arAnchorManager.AttachAnchor(arPlane, pose);
        
        if (anchor == null) return;

        Instantiate(portalPrefab, pose.position, pose.rotation, anchor.transform);
        _portalSpawned = true;
        
        arPlaneManager.SetTrackablesActive(false);
        arPlaneManager.enabled = false;

        debugText.text = "Placed a portal";
    }
}