using System;
using System.Threading.Tasks;
using MarineAR.AISpawner.Models;
using MarineAR.AISpawner.Repository;
using MarineAR.AISpawner.Services;
using MarineAR.AISpawner.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace MarineAR.AISpawner.Runtime
{
    /// <summary>
    /// Composition root and flow orchestrator for the AISpawner scene.
    ///
    /// Initialization (every scene entry): Firestore <c>aiContent/marine</c> →
    /// enabled gate → manifest download (never cached) → in-memory repository →
    /// browser ready. No model or facts bytes are fetched during initialization.
    ///
    /// Selection: download via LRU cache → glTFast import → placement armed on the
    /// scene's existing ObjectSpawner/ARInteractorSpawnTrigger pipeline.
    ///
    /// Exit: every cached file is deleted; the cache lives only for this session.
    /// </summary>
    public sealed class AISpawnManager : MonoBehaviour
    {
        const string k_PlacementPrompt =
            "Scan the environment and tap on a detected plane to place the organism.";

        [Header("Scene Integration (existing AR pipeline)")]
        [SerializeField]
        ObjectSpawner m_ObjectSpawner;

        [SerializeField]
        ARInteractorSpawnTrigger m_SpawnTrigger;

        [SerializeField]
        XRInteractionGroup m_InteractionGroup;

        [Header("Organism Rig")]
        [SerializeField]
        [Tooltip("Prefab carrying the XRGrabInteractable + ARTransformer setup copied from the AR_Spawn spawnables.")]
        GameObject m_OrganismRigPrefab;

        [Header("UI")]
        [SerializeField]
        AIButton m_AIButton;

        [SerializeField]
        AIListController m_ListController;

        [SerializeField]
        DownloadProgressUI m_DownloadProgressUI;

        [SerializeField]
        PromptBanner m_PromptBanner;

        [SerializeField]
        FactsSheetController m_FactsSheet;

        [SerializeField]
        Button m_DeleteButton;

        [SerializeField]
        Button m_FactsButton;

        [SerializeField]
        TMP_Text m_FactsButtonLabel;

        // Services — plain C# objects owned by this composition root.
        FirestoreService m_FirestoreService;
        ManifestService m_ManifestService;
        DownloadService m_DownloadService;
        CacheService m_CacheService;
        MarineRepository m_Repository;
        DownloadManager m_DownloadManager;
        OrganismModelBuilder m_ModelBuilder;
        PlacementManager m_PlacementManager;

        AiContentConfig m_Config;
        MarineOrganism m_PendingOrganism;
        string m_LastPlacedOrganismId;
        bool m_Initialized;
        bool m_Destroyed;

        void Awake()
        {
            m_FirestoreService = new FirestoreService();
            m_ManifestService = new ManifestService();
            m_DownloadService = new DownloadService();
            m_CacheService = new CacheService();
            m_Repository = new MarineRepository();
            m_DownloadManager = new DownloadManager(m_DownloadService, m_CacheService, m_Repository);

            var templateParent = new GameObject("Organism Templates").transform;
            templateParent.SetParent(transform, false);
            m_ModelBuilder = new OrganismModelBuilder(m_OrganismRigPrefab, templateParent);

            if (m_InteractionGroup == null)
                m_InteractionGroup = FindAnyObjectByType<XRInteractionGroup>();

            m_PlacementManager = new PlacementManager(m_ObjectSpawner, m_SpawnTrigger, m_InteractionGroup);
            m_PlacementManager.OrganismPlaced += OnOrganismPlaced;

            m_ListController.Initialize(m_Repository, id => m_CacheService.Contains(id));

            // UI wiring.
            m_AIButton.Clicked += OnAIButtonClicked;
            m_ListController.OrganismSelected += OnOrganismSelected;
            m_ListController.RetryRequested += OnInitRetryRequested;
            m_DownloadProgressUI.CancelRequested += OnDownloadCancelRequested;
            m_DownloadProgressUI.RetryRequested += OnDownloadRetryRequested;

            if (m_DeleteButton != null)
            {
                m_DeleteButton.onClick.AddListener(() => m_PlacementManager.DeleteFocusedOrganism());
                m_DeleteButton.gameObject.SetActive(false);
            }

            if (m_FactsButton != null)
            {
                m_FactsButton.onClick.AddListener(OnFactsButtonClicked);
                m_FactsButton.gameObject.SetActive(false);
            }
        }

        async void Start()
        {
            try
            {
                await InitializeAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AISpawner] Unhandled initialization failure: {e}");
            }
        }

        void Update()
        {
            // Mirror the AR_Spawn delete UX: the button appears while a placed
            // organism holds manipulation focus.
            if (m_DeleteButton != null)
            {
                bool showDelete = m_PlacementManager != null && m_PlacementManager.HasFocusedOrganism;
                if (m_DeleteButton.gameObject.activeSelf != showDelete)
                    m_DeleteButton.gameObject.SetActive(showDelete);
            }
        }

        void OnDestroy()
        {
            m_Destroyed = true;

            m_PlacementManager?.Dispose();
            m_DownloadManager?.CancelActive();
            m_ModelBuilder?.DisposeAll();

            // Scene-exit contract: delete every cached GLB and facts file.
            // Nothing else in temporaryCachePath is touched.
            m_CacheService?.ClearAll();
        }

        async Task InitializeAsync()
        {
            m_Initialized = false;
            m_ListController.ShowLoading("Connecting to mission control…");

            // 1) Firestore: aiContent/marine — the only source of the manifest URL.
            try
            {
                m_Config = await m_FirestoreService.FetchAiContentConfigAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AISpawner] Firestore unavailable: {e.Message}");
                if (m_Destroyed) return;
                m_ListController.ShowError("Could not reach the content service.\nCheck your connection and try again.");
                return;
            }

            if (m_Destroyed)
                return;

            // 2) Enabled gate — disable the AI feature gracefully.
            if (m_Config == null || !m_Config.Enabled)
            {
                m_AIButton.Hide();
                m_ListController.Close();
                m_PromptBanner.Show("AI organisms are currently unavailable.", 4f);
                Debug.Log("[AISpawner] AI content disabled via Firestore flag.");
                return;
            }

            // 3) Manifest: fetched fresh on every scene entry, metadata only.
            m_ListController.ShowLoading("Fetching live organism catalog…");
            Manifest manifest;
            try
            {
                manifest = await m_ManifestService.FetchManifestAsync(m_Config.ManifestUrl);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AISpawner] Manifest fetch failed: {e.Message}");
                if (m_Destroyed) return;
                m_ListController.ShowError("Could not load the organism catalog.\nCheck your connection and try again.");
                return;
            }

            if (m_Destroyed)
                return;

            // 4) Populate the in-memory repository — no GLBs, no facts.
            m_Repository.Populate(manifest);
            m_Initialized = true;

            string dataset = !string.IsNullOrEmpty(m_Config.Dataset) ? m_Config.Dataset : manifest.dataset;
            m_ListController.SetHeader("Marine AI Organisms", $"{m_Repository.Count} species · {dataset}");
            m_ListController.ShowList();
            m_AIButton.Show();
        }

        void OnAIButtonClicked()
        {
            m_ListController.Open();
            if (!m_Initialized)
                m_ListController.ShowLoading("Fetching live organism catalog…");
        }

        void OnInitRetryRequested()
        {
            _ = RetryInitializationAsync();
        }

        async Task RetryInitializationAsync()
        {
            try
            {
                await InitializeAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[AISpawner] Retry failed: {e}");
            }
        }

        async void OnOrganismSelected(MarineOrganism organism)
        {
            if (organism == null)
                return;

            m_PendingOrganism = organism;
            m_ListController.Close();
            m_PlacementManager.CancelPlacement();
            m_PromptBanner.Hide();

            m_DownloadProgressUI.ShowDownloading(organism.DisplayName, organism.model_bytes);

            CacheService.CachedOrganism payload;
            try
            {
                payload = await m_DownloadManager.FetchOrganismAsync(
                    organism, p => m_DownloadProgressUI.SetProgress(p));
            }
            catch (OrganismDownloadException e)
            {
                if (m_Destroyed) return;
                m_DownloadProgressUI.ShowError(e.Message);
                m_ListController.RefreshCachedChips();
                return;
            }

            if (m_Destroyed || payload == null)
                return; // cancelled or superseded — a newer selection owns the UI now

            if (m_PendingOrganism != organism)
                return;

            // Import through the existing glTFast pipeline.
            try
            {
                await m_ModelBuilder.BuildTemplateAsync(organism, payload.ModelPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[AISpawner] GLB import failed for '{organism.id}': {e.Message}");
                if (m_Destroyed) return;

                // A corrupt/unreadable asset is a content failure — same recovery path
                // as a failed transfer. Drop it from cache so retry re-downloads.
                m_CacheService.Remove(organism.id);
                m_DownloadProgressUI.ShowError(DownloadManager.FailureMessage);
                m_ListController.RefreshCachedChips();
                return;
            }

            if (m_Destroyed || m_PendingOrganism != organism)
                return;

            m_DownloadProgressUI.Hide();
            m_ListController.RefreshCachedChips();

            m_PlacementManager.BeginPlacement(m_ModelBuilder.CurrentTemplate);
            m_PromptBanner.Show(k_PlacementPrompt);
        }

        void OnDownloadCancelRequested()
        {
            m_DownloadManager.CancelActive();
            m_PendingOrganism = null;
            m_DownloadProgressUI.Hide();
        }

        void OnDownloadRetryRequested()
        {
            MarineOrganism organism = m_PendingOrganism;
            m_DownloadProgressUI.Hide();
            if (organism != null)
                OnOrganismSelected(organism);
        }

        void OnOrganismPlaced(GameObject instance)
        {
            m_LastPlacedOrganismId = m_ModelBuilder.CurrentTemplateId;
            m_PendingOrganism = null;

            m_PromptBanner.Show("Organism placed — drag to move, twist to rotate, pinch to scale.", 4.5f);

            if (m_FactsButton != null && !string.IsNullOrEmpty(m_LastPlacedOrganismId))
            {
                MarineOrganism organism = m_Repository.GetById(m_LastPlacedOrganismId);
                if (m_FactsButtonLabel != null && organism != null)
                    m_FactsButtonLabel.text = $"Facts · {organism.DisplayName}";
                m_FactsButton.gameObject.SetActive(true);
            }
        }

        void OnFactsButtonClicked()
        {
            if (string.IsNullOrEmpty(m_LastPlacedOrganismId))
                return;

            MarineOrganism organism = m_Repository.GetById(m_LastPlacedOrganismId);
            FactsDocument facts = m_Repository.GetFacts(m_LastPlacedOrganismId);
            m_FactsSheet.Show(organism != null ? organism.DisplayName : "Organism", facts);
        }
    }
}
