using UnityEngine;

public class UIWobble : MonoBehaviour
{
    [Header("Wobble Settings")]
    [SerializeField] private float amplitude = 10f;   // how far it moves (in pixels)
    [SerializeField] private float frequency = 2f;    // how fast it wobbles
    [SerializeField] private bool rotate = true;      // also add rotation?

    private RectTransform rectTransform;
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        initialPosition = rectTransform.anchoredPosition;
        initialRotation = rectTransform.localRotation;
    }

    void Update()
    {
        float wobbleX = Mathf.Sin(Time.time * frequency) * amplitude;
        float wobbleY = Mathf.Cos(Time.time * frequency * 0.8f) * amplitude;

        rectTransform.anchoredPosition = initialPosition + new Vector3(wobbleX, wobbleY, 0);

        if (rotate)
        {
            float wobbleRot = Mathf.Sin(Time.time * frequency * 1.2f) * 5f; // 5° tilt
            rectTransform.localRotation = initialRotation * Quaternion.Euler(0, 0, wobbleRot);
        }
    }
}
