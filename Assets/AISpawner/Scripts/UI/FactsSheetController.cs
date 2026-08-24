using System.Collections.Generic;
using MarineAR.AISpawner.Models;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarineAR.AISpawner.UI
{
    /// <summary>
    /// Bottom sheet showing the downloaded facts of the most recently placed organism.
    /// Rows are generated from whatever keys the facts JSON carries, so new fields in
    /// the evolving dataset appear automatically.
    /// </summary>
    public sealed class FactsSheetController : MonoBehaviour
    {
        [SerializeField]
        GameObject m_Root;

        [SerializeField]
        CanvasGroup m_CanvasGroup;

        [SerializeField]
        TMP_Text m_TitleText;

        [SerializeField]
        RectTransform m_Content;

        [SerializeField]
        GameObject m_RowPrefab;

        [SerializeField]
        Button m_CloseButton;

        [SerializeField]
        Button m_ScrimButton;

        readonly List<GameObject> m_Rows = new List<GameObject>();

        void Awake()
        {
            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(Hide);
            if (m_ScrimButton != null)
                m_ScrimButton.onClick.AddListener(Hide);
            if (m_Root != null)
                m_Root.SetActive(false);
        }

        public void Show(string organismName, FactsDocument facts)
        {
            if (m_Root == null)
                return;

            if (m_TitleText != null)
                m_TitleText.text = organismName;

            Rebuild(facts);

            m_Root.SetActive(true);
            if (m_CanvasGroup != null)
                StartCoroutine(UITween.Fade(m_CanvasGroup, 0f, 1f, 0.22f));
        }

        public void Hide()
        {
            if (m_Root != null)
                m_Root.SetActive(false);
        }

        void Rebuild(FactsDocument facts)
        {
            foreach (GameObject row in m_Rows)
                Destroy(row);
            m_Rows.Clear();

            if (facts == null || facts.Count == 0)
            {
                AddRow("Facts", "No additional information is available for this organism.");
                return;
            }

            foreach (KeyValuePair<string, string> entry in facts.Entries)
                AddRow(FactsDocument.PrettifyKey(entry.Key), entry.Value);
        }

        void AddRow(string label, string value)
        {
            if (m_RowPrefab == null || m_Content == null)
                return;

            GameObject row = Instantiate(m_RowPrefab, m_Content);
            row.SetActive(true);

            TMP_Text[] texts = row.GetComponentsInChildren<TMP_Text>(true);
            if (texts.Length > 0)
                texts[0].text = label;
            if (texts.Length > 1)
                texts[1].text = value;

            m_Rows.Add(row);
        }
    }
}
