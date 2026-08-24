using System;
using MarineAR.AISpawner.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarineAR.AISpawner.UI
{
    /// <summary>
    /// One row in the organism browser: common name, scientific name, download size,
    /// a "cached" chip, and a thumbnail slot reserved for future artwork.
    /// </summary>
    public sealed class OrganismListItemView : MonoBehaviour
    {
        [SerializeField]
        Button m_Button;

        [SerializeField]
        TMP_Text m_NameText;

        [SerializeField]
        TMP_Text m_ScientificText;

        [SerializeField]
        TMP_Text m_SizeText;

        [SerializeField]
        GameObject m_CachedChip;

        [SerializeField]
        [Tooltip("Reserved for future thumbnail support; shows an initial letter for now.")]
        Image m_ThumbnailImage;

        [SerializeField]
        TMP_Text m_ThumbnailLetter;

        MarineOrganism m_Organism;
        Action<MarineOrganism> m_OnSelected;

        public MarineOrganism Organism => m_Organism;

        void OnEnable()
        {
            if (m_Button != null)
                m_Button.onClick.AddListener(HandleClick);
        }

        void OnDisable()
        {
            if (m_Button != null)
                m_Button.onClick.RemoveListener(HandleClick);
        }

        public void Bind(MarineOrganism organism, bool isCached, Action<MarineOrganism> onSelected)
        {
            m_Organism = organism;
            m_OnSelected = onSelected;

            if (m_NameText != null)
                m_NameText.text = organism.DisplayName;

            if (m_ScientificText != null)
            {
                bool hasScientific = !string.IsNullOrEmpty(organism.scientific_name);
                m_ScientificText.gameObject.SetActive(hasScientific);
                if (hasScientific)
                    m_ScientificText.text = organism.scientific_name;
            }

            if (m_SizeText != null)
                m_SizeText.text = organism.ModelSizeLabel;

            SetCached(isCached);

            if (m_ThumbnailLetter != null)
            {
                string display = organism.DisplayName;
                m_ThumbnailLetter.text = string.IsNullOrEmpty(display)
                    ? "?"
                    : char.ToUpperInvariant(display[0]).ToString();
            }
        }

        public void SetCached(bool isCached)
        {
            if (m_CachedChip != null)
                m_CachedChip.SetActive(isCached);
        }

        void HandleClick()
        {
            if (m_Organism != null)
                m_OnSelected?.Invoke(m_Organism);
        }
    }
}
