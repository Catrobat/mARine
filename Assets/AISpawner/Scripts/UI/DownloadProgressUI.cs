using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarineAR.AISpawner.UI
{
    /// <summary>
    /// Floating download card: organism name, smoothed progress bar, percentage and
    /// transfer size, with cancel while downloading and retry after failure.
    /// </summary>
    public sealed class DownloadProgressUI : MonoBehaviour
    {
        [SerializeField]
        GameObject m_Root;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        TMP_Text m_TitleText;

        [SerializeField]
        TMP_Text m_DetailText;

        [SerializeField]
        Image m_ProgressFill;

        [SerializeField]
        TMP_Text m_PercentText;

        [SerializeField]
        Button m_CancelButton;

        [SerializeField]
        GameObject m_ErrorGroup;

        [SerializeField]
        TMP_Text m_ErrorText;

        [SerializeField]
        Button m_RetryButton;

        [SerializeField]
        Button m_DismissButton;

        float m_TargetProgress;
        float m_DisplayedProgress;
        long m_TotalBytes;
        bool m_Downloading;

        public event Action CancelRequested;
        public event Action RetryRequested;

        void Awake()
        {
            if (m_CancelButton != null)
                m_CancelButton.onClick.AddListener(() => CancelRequested?.Invoke());
            if (m_RetryButton != null)
                m_RetryButton.onClick.AddListener(() => RetryRequested?.Invoke());
            if (m_DismissButton != null)
                m_DismissButton.onClick.AddListener(Hide);

            if (m_Root != null)
                m_Root.SetActive(false);
        }

        void Update()
        {
            if (!m_Downloading || m_ProgressFill == null)
                return;

            m_DisplayedProgress = UITween.SmoothTowards(m_DisplayedProgress, m_TargetProgress);
            m_ProgressFill.fillAmount = m_DisplayedProgress;

            if (m_PercentText != null)
                m_PercentText.text = $"{m_DisplayedProgress * 100f:F0}%";

            if (m_DetailText != null && m_TotalBytes > 0)
            {
                float totalMb = m_TotalBytes / 1024f / 1024f;
                m_DetailText.text = $"{m_DisplayedProgress * totalMb:F1} MB / {totalMb:F1} MB";
            }
        }

        /// <summary>Shows the card in download mode for the given organism.</summary>
        public void ShowDownloading(string organismName, long totalBytes)
        {
            m_Downloading = true;
            m_TargetProgress = 0f;
            m_DisplayedProgress = 0f;
            m_TotalBytes = totalBytes;

            if (m_Root != null)
                m_Root.SetActive(true);
            if (m_TitleText != null)
                m_TitleText.text = $"Downloading {organismName}";
            if (m_ErrorGroup != null)
                m_ErrorGroup.SetActive(false);
            if (m_CancelButton != null)
                m_CancelButton.gameObject.SetActive(true);
            if (m_ProgressFill != null)
                m_ProgressFill.fillAmount = 0f;
            if (m_PercentText != null)
                m_PercentText.text = "0%";
            if (m_DetailText != null)
                m_DetailText.text = "Preparing download…";

            if (m_CanvasGroup != null)
                StartCoroutine(UITween.Fade(m_CanvasGroup, 0f, 1f, 0.2f));
        }

        /// <summary>Feeds raw progress in [0, 1]; the bar catches up smoothly.</summary>
        public void SetProgress(float progress)
        {
            m_TargetProgress = Mathf.Clamp01(progress);
        }

        /// <summary>Switches the card into its failure state with a retry affordance.</summary>
        public void ShowError(string message)
        {
            m_Downloading = false;

            if (m_Root != null)
                m_Root.SetActive(true);
            if (m_ErrorGroup != null)
                m_ErrorGroup.SetActive(true);
            if (m_ErrorText != null)
                m_ErrorText.text = message;
            if (m_CancelButton != null)
                m_CancelButton.gameObject.SetActive(false);
        }

        public void Hide()
        {
            m_Downloading = false;
            if (m_Root != null)
                m_Root.SetActive(false);
        }
    }
}
