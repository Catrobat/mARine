using System.Collections;
using TMPro;
using UnityEngine;

namespace MarineAR.AISpawner.UI
{
    /// <summary>
    /// Top-of-screen pill banner for guidance ("Scan the environment…") and
    /// transient notices. Sticky messages stay until replaced or hidden;
    /// timed messages fade out on their own.
    /// </summary>
    public sealed class PromptBanner : MonoBehaviour
    {
        [SerializeField]
        GameObject m_Root;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        TMP_Text m_Text;

        Coroutine m_ActiveRoutine;

        void Awake()
        {
            if (m_Root != null)
                m_Root.SetActive(false);
        }

        /// <summary>
        /// Shows a message. <paramref name="duration"/> ≤ 0 keeps it until
        /// <see cref="Hide"/> or the next <see cref="Show"/> call.
        /// </summary>
        public void Show(string message, float duration = 0f)
        {
            if (m_Root == null || m_Text == null)
                return;

            if (m_ActiveRoutine != null)
                StopCoroutine(m_ActiveRoutine);

            m_Text.text = message;
            m_Root.SetActive(true);
            m_ActiveRoutine = StartCoroutine(ShowRoutine(duration));
        }

        public void Hide()
        {
            if (m_ActiveRoutine != null)
            {
                StopCoroutine(m_ActiveRoutine);
                m_ActiveRoutine = null;
            }

            if (m_Root != null && m_Root.activeSelf && m_CanvasGroup != null && isActiveAndEnabled)
                StartCoroutine(UITween.Fade(m_CanvasGroup, m_CanvasGroup.alpha, 0f, 0.18f, () => m_Root.SetActive(false)));
            else if (m_Root != null)
                m_Root.SetActive(false);
        }

        IEnumerator ShowRoutine(float duration)
        {
            if (m_CanvasGroup != null)
                yield return UITween.Fade(m_CanvasGroup, 0f, 1f, 0.22f);

            if (duration > 0f)
            {
                yield return new WaitForSecondsRealtime(duration);
                if (m_CanvasGroup != null)
                    yield return UITween.Fade(m_CanvasGroup, 1f, 0f, 0.3f);
                m_Root.SetActive(false);
            }

            m_ActiveRoutine = null;
        }
    }
}
