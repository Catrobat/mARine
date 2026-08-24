using System;
using System.Collections;
using System.Collections.Generic;
using MarineAR.AISpawner.Models;
using MarineAR.AISpawner.Repository;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MarineAR.AISpawner.UI
{
    /// <summary>
    /// The modern organism browser panel: searchable list generated dynamically from
    /// the manifest repository, with loading / empty / error(+retry) states and smooth
    /// open/close animations. Instantiates rows incrementally to keep the first open
    /// hitch-free even with hundreds of organisms.
    /// </summary>
    public sealed class AIListController : MonoBehaviour
    {
        const int k_ItemsPerFrame = 16;
        const float k_OpenDuration = 0.28f;

        [Header("Panel")]
        [SerializeField]
        GameObject m_PanelRoot;

        [SerializeField]
        CanvasGroup m_PanelCanvasGroup;

        [SerializeField]
        RectTransform m_Sheet;

        [SerializeField]
        Button m_CloseButton;

        [SerializeField]
        Button m_ScrimButton;

        [SerializeField]
        TMP_Text m_TitleText;

        [SerializeField]
        TMP_Text m_SubtitleText;

        [Header("Search")]
        [SerializeField]
        SearchController m_SearchController;

        [Header("List")]
        [SerializeField]
        RectTransform m_ListContent;

        [SerializeField]
        OrganismListItemView m_ItemPrefab;

        [Header("States")]
        [SerializeField]
        GameObject m_LoadingState;

        [SerializeField]
        TMP_Text m_LoadingText;

        [SerializeField]
        GameObject m_ErrorState;

        [SerializeField]
        TMP_Text m_ErrorText;

        [SerializeField]
        Button m_RetryButton;

        [SerializeField]
        GameObject m_EmptyState;

        [SerializeField]
        GameObject m_ScrollView;

        readonly List<OrganismListItemView> m_Items = new List<OrganismListItemView>();

        MarineRepository m_Repository;
        Func<string, bool> m_IsCached;
        Coroutine m_BuildRoutine;
        Vector2 m_SheetShownPosition;
        bool m_IsOpen;

        public event Action<MarineOrganism> OrganismSelected;
        public event Action RetryRequested;

        public bool IsOpen => m_IsOpen;

        void Awake()
        {
            m_SheetShownPosition = m_Sheet != null ? m_Sheet.anchoredPosition : Vector2.zero;

            if (m_CloseButton != null)
                m_CloseButton.onClick.AddListener(Close);
            if (m_ScrimButton != null)
                m_ScrimButton.onClick.AddListener(Close);
            if (m_RetryButton != null)
                m_RetryButton.onClick.AddListener(() => RetryRequested?.Invoke());
            if (m_SearchController != null)
                m_SearchController.QueryChanged += ApplyFilter;

            if (m_PanelRoot != null)
                m_PanelRoot.SetActive(false);
        }

        void OnDisable()
        {
            // The scene's BackNavigation can deactivate this panel directly through
            // its UICanvasTag (Android back button) — keep the open flag in sync.
            m_IsOpen = false;
        }

        void OnDestroy()
        {
            if (m_SearchController != null)
                m_SearchController.QueryChanged -= ApplyFilter;
        }

        /// <summary>Injects data dependencies. Call before first open.</summary>
        public void Initialize(MarineRepository repository, Func<string, bool> isCachedLookup)
        {
            m_Repository = repository;
            m_IsCached = isCachedLookup;
        }

        public void Open()
        {
            if (m_PanelRoot == null || m_IsOpen)
                return;

            m_IsOpen = true;
            m_PanelRoot.SetActive(true);

            if (m_PanelCanvasGroup != null)
                StartCoroutine(UITween.Fade(m_PanelCanvasGroup, 0f, 1f, k_OpenDuration));

            if (m_Sheet != null)
            {
                Vector2 hidden = m_SheetShownPosition + new Vector2(0f, -m_Sheet.rect.height * 0.35f);
                StartCoroutine(UITween.SlideAnchored(m_Sheet, hidden, m_SheetShownPosition, k_OpenDuration));
            }

            RefreshList();
        }

        public void Close()
        {
            if (m_PanelRoot == null || !m_IsOpen)
                return;

            m_IsOpen = false;

            if (m_PanelCanvasGroup != null)
            {
                StartCoroutine(UITween.Fade(m_PanelCanvasGroup, m_PanelCanvasGroup.alpha, 0f, 0.18f,
                    () => m_PanelRoot.SetActive(false)));
            }
            else
            {
                m_PanelRoot.SetActive(false);
            }
        }

        /// <summary>Shows the loading state (used during Firestore + manifest fetch).</summary>
        public void ShowLoading(string message)
        {
            SetState(loading: true, error: false, empty: false, list: false);
            if (m_LoadingText != null)
                m_LoadingText.text = message;
        }

        /// <summary>Shows the error state with a retry affordance.</summary>
        public void ShowError(string message)
        {
            SetState(loading: false, error: true, empty: false, list: false);
            if (m_ErrorText != null)
                m_ErrorText.text = message;
        }

        /// <summary>Renders the repository contents (respecting the active search query).</summary>
        public void ShowList()
        {
            RefreshList();
        }

        /// <summary>Updates header texts from live config/manifest values.</summary>
        public void SetHeader(string title, string subtitle)
        {
            if (m_TitleText != null && !string.IsNullOrEmpty(title))
                m_TitleText.text = title;
            if (m_SubtitleText != null)
                m_SubtitleText.text = subtitle ?? string.Empty;
        }

        /// <summary>Re-evaluates every row's "cached" chip (e.g. after LRU eviction).</summary>
        public void RefreshCachedChips()
        {
            foreach (OrganismListItemView item in m_Items)
            {
                if (item.Organism != null)
                    item.SetCached(m_IsCached != null && m_IsCached(item.Organism.id));
            }
        }

        void RefreshList()
        {
            if (m_Repository == null || !m_Repository.IsPopulated)
                return;

            ApplyFilter(m_SearchController != null ? m_SearchController.CurrentQuery : string.Empty);
        }

        void ApplyFilter(string query)
        {
            if (m_Repository == null || !m_Repository.IsPopulated)
                return;

            // While the panel is closed we cannot run the incremental build coroutine;
            // Open() calls RefreshList again once the panel is active.
            if (!isActiveAndEnabled)
                return;

            List<MarineOrganism> results = m_Repository.Search(query);

            if (results.Count == 0)
            {
                SetState(loading: false, error: false, empty: true, list: false);
                return;
            }

            SetState(loading: false, error: false, empty: false, list: true);

            if (m_BuildRoutine != null)
                StopCoroutine(m_BuildRoutine);
            m_BuildRoutine = StartCoroutine(BuildItems(results));
        }

        IEnumerator BuildItems(List<MarineOrganism> organisms)
        {
            // Reuse existing rows, growing the pool incrementally to avoid a spike.
            int index = 0;
            for (; index < organisms.Count; index++)
            {
                OrganismListItemView item;
                if (index < m_Items.Count)
                {
                    item = m_Items[index];
                }
                else
                {
                    item = Instantiate(m_ItemPrefab, m_ListContent);
                    m_Items.Add(item);

                    if ((index + 1) % k_ItemsPerFrame == 0)
                        yield return null;
                }

                MarineOrganism organism = organisms[index];
                item.gameObject.SetActive(true);
                item.Bind(organism, m_IsCached != null && m_IsCached(organism.id), HandleItemSelected);
            }

            for (; index < m_Items.Count; index++)
                m_Items[index].gameObject.SetActive(false);

            m_BuildRoutine = null;
        }

        void HandleItemSelected(MarineOrganism organism)
        {
            OrganismSelected?.Invoke(organism);
        }

        void SetState(bool loading, bool error, bool empty, bool list)
        {
            if (m_LoadingState != null)
                m_LoadingState.SetActive(loading);
            if (m_ErrorState != null)
                m_ErrorState.SetActive(error);
            if (m_EmptyState != null)
                m_EmptyState.SetActive(empty);
            if (m_ScrollView != null)
                m_ScrollView.SetActive(list);
        }
    }
}
