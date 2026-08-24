using System;
using UnityEngine;
using UnityEngine.UI;

namespace MarineAR.AISpawner.UI
{
    /// <summary>
    /// The floating AI entry-point button. Idles with a soft pulse and raises
    /// <see cref="Clicked"/> to open the organism browser panel.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class AIButton : MonoBehaviour
    {
        [SerializeField]
        CanvasGroup m_CanvasGroup;

        Button m_Button;
        Coroutine m_PulseRoutine;

        public event Action Clicked;

        void Awake()
        {
            m_Button = GetComponent<Button>();
            if (m_CanvasGroup == null)
                m_CanvasGroup = GetComponent<CanvasGroup>();
        }

        void OnEnable()
        {
            m_Button.onClick.AddListener(HandleClick);
            m_PulseRoutine = StartCoroutine(UITween.Pulse(transform));
        }

        void OnDisable()
        {
            m_Button.onClick.RemoveListener(HandleClick);
            if (m_PulseRoutine != null)
            {
                StopCoroutine(m_PulseRoutine);
                m_PulseRoutine = null;
            }
            transform.localScale = Vector3.one;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            if (m_CanvasGroup != null)
                StartCoroutine(UITween.Fade(m_CanvasGroup, m_CanvasGroup.alpha, 1f, 0.25f));
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        void HandleClick()
        {
            Clicked?.Invoke();
        }
    }
}
