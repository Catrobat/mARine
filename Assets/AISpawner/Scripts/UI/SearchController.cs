using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarineAR.AISpawner.UI
{
    /// <summary>
    /// Wraps the panel's search input field: raises <see cref="QueryChanged"/> as the
    /// user types and manages the clear ("×") button. Pure view logic — filtering
    /// itself happens in <see cref="AIListController"/> against the repository.
    /// </summary>
    public sealed class SearchController : MonoBehaviour
    {
        [SerializeField]
        TMP_InputField m_InputField;

        [SerializeField]
        Button m_ClearButton;

        public event Action<string> QueryChanged;

        public string CurrentQuery => m_InputField != null ? m_InputField.text : string.Empty;

        void OnEnable()
        {
            if (m_InputField != null)
                m_InputField.onValueChanged.AddListener(HandleValueChanged);
            if (m_ClearButton != null)
            {
                m_ClearButton.onClick.AddListener(Clear);
                m_ClearButton.gameObject.SetActive(false);
            }
        }

        void OnDisable()
        {
            if (m_InputField != null)
                m_InputField.onValueChanged.RemoveListener(HandleValueChanged);
            if (m_ClearButton != null)
                m_ClearButton.onClick.RemoveListener(Clear);
        }

        public void Clear()
        {
            if (m_InputField != null)
                m_InputField.text = string.Empty;
        }

        void HandleValueChanged(string value)
        {
            if (m_ClearButton != null)
                m_ClearButton.gameObject.SetActive(!string.IsNullOrEmpty(value));
            QueryChanged?.Invoke(value);
        }
    }
}
