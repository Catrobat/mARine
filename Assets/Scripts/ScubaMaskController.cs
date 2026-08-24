using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Controls positioning and scaling of a scuba mask relative to the tracked ARFace.
/// Attach this to the root of each mask prefab.
/// The mask is parented to the ARFace transform, so it tracks automatically.
/// Optional vertex-based auto-scaling adjusts mask size to fit the detected face.
/// </summary>
public class ScubaMaskController : MonoBehaviour
{
    [Header("Position Offset (Local to Face)")]
    [Tooltip("Offset from face center. Y+ is up (toward forehead), Z+ is forward (toward camera).")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0.005f, 0.035f);

    [Tooltip("Rotation offset in Euler angles")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;

    [Header("Scale")]
    [SerializeField] private float maskScale = 1f;

    [Tooltip("Automatically scale mask based on detected eye distance")]
    [SerializeField] private bool autoScaleToFace = true;

    [Tooltip("Use face mesh eye landmarks for mask placement when available.")]
    [SerializeField] private bool anchorToEyeLandmarks = true;

    [Tooltip("Reference interpupillary distance (meters) for scaling")]
    [SerializeField] private float referenceEyeDistance = 0.063f;

    [Header("Smoothing")]
    [Tooltip("How quickly the scale adjusts (higher = faster)")]
    [SerializeField] private float scaleSmoothing = 8f;

    // ARCore canonical face mesh landmark vertex indices
    private const int LEFT_EYE_OUTER = 33;
    private const int RIGHT_EYE_OUTER = 263;

    private ARFace _trackedFace;
    private float _currentScale;
    private float _targetScale;
    private Vector3 _targetLocalPosition;

    /// <summary>
    /// Called by FaceARManager after instantiation.
    /// </summary>
    public void Initialize(ARFace face)
    {
        _trackedFace = face;
        transform.localPosition = positionOffset;
        transform.localRotation = Quaternion.Euler(rotationOffset);
        _targetLocalPosition = positionOffset;

        _currentScale = maskScale;
        _targetScale = maskScale;
        transform.localScale = Vector3.one * _currentScale;
        ConfigureRenderers();
    }

    void LateUpdate()
    {
        if (_trackedFace == null) return;

        _targetLocalPosition = positionOffset;

        if (anchorToEyeLandmarks
            && _trackedFace.vertices.IsCreated
            && _trackedFace.vertices.Length > RIGHT_EYE_OUTER)
        {
            Vector3 leftEye = _trackedFace.vertices[LEFT_EYE_OUTER];
            Vector3 rightEye = _trackedFace.vertices[RIGHT_EYE_OUTER];
            Vector3 eyeCenter = (leftEye + rightEye) * 0.5f;

            _targetLocalPosition = new Vector3(
                eyeCenter.x + positionOffset.x,
                eyeCenter.y + positionOffset.y,
                eyeCenter.z + positionOffset.z);
        }

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            _targetLocalPosition,
            Time.deltaTime * scaleSmoothing);
        transform.localRotation = Quaternion.Euler(rotationOffset);

        // Auto-scale based on face vertex eye distance
        if (autoScaleToFace
            && _trackedFace.vertices.IsCreated
            && _trackedFace.vertices.Length > RIGHT_EYE_OUTER)
        {
            Vector3 leftEye = _trackedFace.vertices[LEFT_EYE_OUTER];
            Vector3 rightEye = _trackedFace.vertices[RIGHT_EYE_OUTER];
            float eyeDistance = Vector3.Distance(leftEye, rightEye);

            if (eyeDistance > 0.01f)
            {
                _targetScale = maskScale * (eyeDistance / referenceEyeDistance);
            }
        }

        // Smooth scale transition
        _currentScale = Mathf.Lerp(_currentScale, _targetScale, Time.deltaTime * scaleSmoothing);
        transform.localScale = Vector3.one * _currentScale;
    }

    private void ConfigureRenderers()
    {
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in renderers)
        {
            sr.sortingOrder = 50;
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sr.receiveShadows = false;
        }
    }

    /// <summary>Update the mask offset at runtime.</summary>
    public void SetOffset(Vector3 position, Vector3 rotation)
    {
        positionOffset = position;
        rotationOffset = rotation;
    }

    /// <summary>Update the base mask scale.</summary>
    public void SetScale(float scale)
    {
        maskScale = scale;
        _targetScale = scale;
    }
}
