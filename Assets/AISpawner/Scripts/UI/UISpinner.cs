using UnityEngine;

namespace MarineAR.AISpawner.UI
{
    /// <summary>Rotates its RectTransform for loading indicators.</summary>
    public sealed class UISpinner : MonoBehaviour
    {
        [SerializeField]
        float m_DegreesPerSecond = 220f;

        RectTransform m_Rect;

        void Awake()
        {
            m_Rect = (RectTransform)transform;
        }

        void Update()
        {
            m_Rect.Rotate(0f, 0f, -m_DegreesPerSecond * Time.unscaledDeltaTime);
        }
    }
}
