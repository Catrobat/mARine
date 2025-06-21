using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class NautilusInteraction : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    private Camera arCamera;
    private ARRaycastManager raycastManager;
    [SerializeField] private GameObject uiCanvasPrefab;

    private InputAction _touchAction;
    private GameObject spawnedCanvas;
    private bool isSelected = false;

    private void Awake()
    {
        _touchAction = inputActions.FindActionMap("Touch").FindAction("Press");
        if (_touchAction == null)
        {
            Debug.LogWarning("Touch action not found in InputActionAsset!");
        }

        if (arCamera == null)
            arCamera = Camera.main;
    }

    private void OnEnable()
    {
        _touchAction?.Enable();
    }

    private void OnDisable()
    {
        _touchAction?.Disable();
    }

    private void Update()
    {
        if (_touchAction != null && _touchAction.WasPerformedThisFrame())
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();

            if (IsPointerOverUI(touchPosition)) return;

            Ray ray = arCamera.ScreenPointToRay(touchPosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform == transform)
                {
                    isSelected = true;
                    ShowCanvasNearObject();
                    Debug.Log("Nautilus selected!");
                }
                else
                {
                    isSelected = false;
                    if (spawnedCanvas != null)
                        Destroy(spawnedCanvas);
                }
            }
        }

        // Optional: Move object if selected and dragging
        if (isSelected && Touchscreen.current.primaryTouch.press.isPressed && Touchscreen.current.primaryTouch.delta.ReadValue().magnitude > 0.1f)
        {
            Vector2 touchPosition = Touchscreen.current.primaryTouch.position.ReadValue();
            List<ARRaycastHit> hits = new List<ARRaycastHit>();
            if (raycastManager.Raycast(touchPosition, hits, UnityEngine.XR.ARSubsystems.TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;
                transform.position = hitPose.position;
                if (spawnedCanvas)
                    spawnedCanvas.transform.position = transform.position + Vector3.right * 0.1f;
            }
        }
    }

    private void ShowCanvasNearObject()
    {
        if (spawnedCanvas != null) return;

        spawnedCanvas = Instantiate(uiCanvasPrefab, transform.position + Vector3.right * 0.1f, Quaternion.identity);
        spawnedCanvas.transform.LookAt(arCamera.transform);
    }

    private bool IsPointerOverUI(Vector2 touchPos)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = touchPos
        };
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        return results.Count > 0;
    }
}
