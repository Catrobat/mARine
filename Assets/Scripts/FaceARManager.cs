using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Core Face AR controller. Manages AR face tracking and mask instantiation.
/// Listens for ARFaceManager trackable events, spawns the selected mask prefab
/// as a child of each detected ARFace transform.
/// </summary>
public class FaceARManager : MonoBehaviour
{
    [Header("AR Components")]
    [SerializeField] private ARFaceManager arFaceManager;
    [SerializeField] private ARSession arSession;

    [Header("Mask Prefabs")]
    [Tooltip("Array of mask prefabs. Each should have a ScubaMaskController component on the root.")]
    [SerializeField] private GameObject[] maskPrefabs;

    [Header("Settings")]
    [SerializeField] private int activeMaskIndex = 0;
    [SerializeField] private bool useScreenSpaceMasks = true;

    // Runtime state
    private readonly Dictionary<TrackableId, GameObject> _spawnedMasks = new();
    private bool _isARActive = true;
    private Canvas _maskCanvas;
    private Camera _arCamera;
    private bool _loggedFirstFace;

    /// <summary>True if at least one face is currently tracked.</summary>
    public bool IsTracking => _spawnedMasks.Count > 0;

    /// <summary>Index of the currently active mask prefab.</summary>
    public int ActiveMaskIndex => activeMaskIndex;

    /// <summary>Total number of available mask prefabs.</summary>
    public int MaskCount => maskPrefabs != null ? maskPrefabs.Length : 0;

    /// <summary>Whether the AR face tracking is currently active.</summary>
    public bool IsARActive => _isARActive;

    void OnEnable()
    {
        if (arFaceManager != null)
            arFaceManager.trackablesChanged.AddListener(OnFacesChanged);
    }

    void OnDisable()
    {
        if (arFaceManager != null)
            arFaceManager.trackablesChanged.RemoveListener(OnFacesChanged);
    }

    /// <summary>
    /// Switch to a different mask type by index. Re-spawns masks on all tracked faces.
    /// </summary>
    public void SetActiveMask(int index)
    {
        if (maskPrefabs == null || index < 0 || index >= maskPrefabs.Length)
            return;

        activeMaskIndex = index;
        ClearAllMasks();

        // Re-spawn for all currently tracked faces
        if (arFaceManager != null && _isARActive)
        {
            foreach (var face in arFaceManager.trackables)
            {
                SpawnMask(face);
            }
        }

        Debug.Log($"[FaceAR] Switched to mask index {index}");
    }

    /// <summary>Stop AR face tracking and remove all masks.</summary>
    public void StopAR()
    {
        _isARActive = false;
        if (arFaceManager != null)
            arFaceManager.enabled = false;
        ClearAllMasks();
        Debug.Log("[FaceAR] AR stopped");
    }

    /// <summary>Resume AR face tracking.</summary>
    public void StartAR()
    {
        _isARActive = true;
        if (arFaceManager != null)
            arFaceManager.enabled = true;
        Debug.Log("[FaceAR] AR started");
    }

    /// <summary>Toggle AR on/off.</summary>
    public void ToggleAR()
    {
        if (_isARActive) StopAR();
        else StartAR();
    }

    /// <summary>Reset AR session and clear masks.</summary>
    public void ResetAR()
    {
        ClearAllMasks();
        if (arSession != null)
            arSession.Reset();
        Debug.Log("[FaceAR] AR session reset");
    }

    private void OnFacesChanged(ARTrackablesChangedEventArgs<ARFace> args)
    {
        if (!_isARActive) return;

        foreach (var face in args.added)
        {
            if (!_loggedFirstFace)
            {
                Debug.Log("[FaceAR] First AR face detected.");
                _loggedFirstFace = true;
            }

            SpawnMask(face);
        }

        // Updated faces: mask follows automatically (parented to face transform)

        foreach (var kvp in args.removed)
            RemoveMask(kvp.Key);
    }

    private void SpawnMask(ARFace face)
    {
        if (maskPrefabs == null || maskPrefabs.Length == 0) return;

        var prefab = maskPrefabs[Mathf.Clamp(activeMaskIndex, 0, maskPrefabs.Length - 1)];
        if (prefab == null) return;

        if (_spawnedMasks.ContainsKey(face.trackableId)) return;

        GameObject maskInstance;
        if (useScreenSpaceMasks)
        {
            maskInstance = CreateScreenSpaceMask(face, prefab);
        }
        else
        {
            maskInstance = Instantiate(prefab, face.transform);
            var controller = maskInstance.GetComponent<ScubaMaskController>();
            if (controller != null)
                controller.Initialize(face);
        }

        _spawnedMasks[face.trackableId] = maskInstance;
    }

    private GameObject CreateScreenSpaceMask(ARFace face, GameObject prefab)
    {
        EnsureMaskCanvas();

        var go = new GameObject($"{prefab.name}_Overlay");
        go.transform.SetParent(_maskCanvas.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(240f, 120f);

        var image = go.AddComponent<Image>();
        image.sprite = ExtractMaskSprite(prefab);
        image.color = Color.white;
        image.raycastTarget = false;
        image.preserveAspect = true;

        var controller = go.AddComponent<FaceScreenSpaceMaskController>();
        controller.Initialize(face, _arCamera, _maskCanvas, image.sprite);

        return go;
    }

    private void EnsureMaskCanvas()
    {
        if (_maskCanvas != null) return;

        _arCamera = Camera.main != null
            ? Camera.main
            : FindFirstObjectByType<ARCameraManager>()?.GetComponent<Camera>();

        var canvasGO = new GameObject("FaceMaskCanvas");
        _maskCanvas = canvasGO.AddComponent<Canvas>();
        _maskCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _maskCanvas.sortingOrder = 0;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 2340f);
        scaler.matchWidthOrHeight = 0.5f;
    }

    private static Sprite ExtractMaskSprite(GameObject prefab)
    {
        var renderer = prefab != null ? prefab.GetComponentInChildren<SpriteRenderer>(true) : null;
        return renderer != null ? renderer.sprite : null;
    }

    private void RemoveMask(TrackableId trackableId)
    {
        if (_spawnedMasks.TryGetValue(trackableId, out var mask))
        {
            if (mask != null) Destroy(mask);
            _spawnedMasks.Remove(trackableId);
        }
    }

    private void ClearAllMasks()
    {
        foreach (var kvp in _spawnedMasks)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        _spawnedMasks.Clear();
    }

    void OnDestroy()
    {
        ClearAllMasks();
    }
}
