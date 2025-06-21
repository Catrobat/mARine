using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.UI;

public class ARDepthController : MonoBehaviour
{
    public Slider depthSlider;
    public XROrigin arSessionOrigin;
    public float minDepth = 0f;
    public float maxDepth = 1f;

    private Vector3 _originalPosition;

    private void Start()
    {
        _originalPosition = arSessionOrigin.transform.position;
        depthSlider.minValue = minDepth;
        depthSlider.maxValue = maxDepth;
        depthSlider.onValueChanged.AddListener(OnDepthChanged);
    }

    private void OnDepthChanged(float depth)
    {
        Vector3 newPos = _originalPosition;
        newPos.y += depth;
        arSessionOrigin.transform.position = newPos;
    }
}