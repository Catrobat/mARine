using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Minimal AR "scan the floor → tap a plane → spawn the procedural terrain" placer.
[RequireComponent(typeof(ARRaycastManager))]
public class EndlessTerrainPlacer : MonoBehaviour
{
    [Tooltip("The ProceduralTerrainEnvironment prefab to spawn on the first tap.")]
    [SerializeField] private GameObject terrainPrefab;

    [Tooltip("Hide planes and stop scanning once the terrain has been placed.")]
    [SerializeField] private bool stopScanningAfterPlace = true;

    ARRaycastManager _raycasts;
    ARPlaneManager   _planes;
    ARAnchorManager  _anchors;
    GameObject       _placed;
    readonly List<ARRaycastHit> _hits = new List<ARRaycastHit>();

    void Awake()
    {
        _raycasts = GetComponent<ARRaycastManager>();
        _planes   = GetComponent<ARPlaneManager>();
        _anchors  = GetComponent<ARAnchorManager>();
    }

    void Update()
    {
        if (_placed != null) return; // already placed — one-shot

        var touch = Touchscreen.current?.primaryTouch;
        if (touch == null || !touch.press.wasPressedThisFrame) return;

        Vector2 screenPos = touch.position.ReadValue();
        if (!_raycasts.Raycast(screenPos, _hits, TrackableType.PlaneWithinPolygon)) return;

        Pose pose = _hits[0].pose;

        // Anchor to the detected plane so the terrain stays fixed in the real world.
        Transform parent = transform;
        if (_anchors != null && _planes != null)
        {
            ARPlane plane = _planes.GetPlane(_hits[0].trackableId);
            if (plane != null)
            {
                ARAnchor anchor = _anchors.AttachAnchor(plane, pose);
                if (anchor != null) parent = anchor.transform;
            }
        }

        _placed = Instantiate(terrainPrefab, pose.position, pose.rotation, parent);

        if (stopScanningAfterPlace && _planes != null)
        {
            _planes.enabled = false;
            foreach (var p in _planes.trackables) p.gameObject.SetActive(false);
        }
    }
}
