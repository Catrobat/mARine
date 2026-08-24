using System.Linq;
using MarineAR.AISpawner.Runtime;
using MarineAR.AISpawner.UI;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Transformers;

namespace MarineAR.AISpawner.EditorTools
{
    /// <summary>
    /// One-shot generator for the AISpawner feature scene.
    ///
    /// The scene is created as a copy of the production AR_Spawn scene — AR_Spawn itself
    /// is never modified — so the entire proven AR stack (XR Origin rig, AR Session,
    /// screen-space interactor, ObjectSpawner + ARInteractorSpawnTrigger, EventSystem,
    /// BackNavigator) is reused verbatim. The template's object menu UI is stripped and
    /// replaced with the AISpawner browser UI, and the scene is registered in the
    /// "Essential Package" Addressables group so it ships with the essential download.
    ///
    /// Menu: Marine AR → AI Spawner → Build AISpawner Scene.
    /// Safe to re-run; it rebuilds scene, prefabs and sprites from scratch.
    /// </summary>
    public static class AISpawnerSceneBuilder
    {
        const string k_SourceScenePath = "Assets/Scenes/AR_Spawn.unity";
        const string k_ScenePath = "Assets/AISpawner/Scenes/AISpawnerScene.unity";
        const string k_SceneAddress = "AISpawnerScene";
        const string k_EssentialGroupName = "Essential Package";

        const string k_OrganismRigPath = "Assets/AISpawner/Prefabs/OrganismRig.prefab";
        const string k_ListItemPath = "Assets/AISpawner/Prefabs/OrganismListItem.prefab";
        const string k_FactsRowPath = "Assets/AISpawner/Prefabs/FactsRow.prefab";

        const string k_OctoPrefabPath = "Assets/Prefabs/OctoPrefab.prefab";
        const string k_FeatheredPlanePath = "Assets/MobileARTemplateAssets/Prefabs/ARFeatheredPlane.prefab";

        [MenuItem("Marine AR/AI Spawner/Build AISpawner Scene")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            AISpawnerUiFactory.EnsureSprites();

            GameObject organismRig = BuildOrganismRigPrefab();
            GameObject listItem = BuildListItemPrefab();
            GameObject factsRow = BuildFactsRowPrefab();

            // --- 1) Copy AR_Spawn → AISpawnerScene (source stays untouched). ---
            System.IO.Directory.CreateDirectory("Assets/AISpawner/Scenes");
            AssetDatabase.DeleteAsset(k_ScenePath);
            if (!AssetDatabase.CopyAsset(k_SourceScenePath, k_ScenePath))
            {
                Debug.LogError($"[AISpawner] Could not copy {k_SourceScenePath}. Aborting.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(k_ScenePath, OpenSceneMode.Single);

            // --- 2) Strip the AR_Spawn template UI, keep the AR/interaction stack. ---
            StripTemplateObjects();

            // --- 3) Configure the retained spawner pipeline for runtime injection. ---
            var spawner = Object.FindAnyObjectByType<ObjectSpawner>(FindObjectsInactive.Include);
            var spawnTrigger = Object.FindAnyObjectByType<ARInteractorSpawnTrigger>(FindObjectsInactive.Include);
            var interactionGroup = Object.FindAnyObjectByType<XRInteractionGroup>(FindObjectsInactive.Include);

            if (spawner == null || spawnTrigger == null)
            {
                Debug.LogError("[AISpawner] Copied scene is missing ObjectSpawner/ARInteractorSpawnTrigger. Aborting.");
                return;
            }

            spawner.objectPrefabs.Clear();
            spawner.spawnOptionIndex = 0;
            spawner.applyRandomAngleAtSpawn = false;
            spawnTrigger.enabled = false;
            EditorUtility.SetDirty(spawner);
            EditorUtility.SetDirty(spawnTrigger);

            // Keep plane visuals: the template menu manager used to assign this at runtime.
            var planeManager = Object.FindAnyObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
            var featheredPlane = AssetDatabase.LoadAssetAtPath<GameObject>(k_FeatheredPlanePath);
            if (planeManager != null && featheredPlane != null)
            {
                planeManager.planePrefab = featheredPlane;
                EditorUtility.SetDirty(planeManager);
            }

            // --- 4) Build the AISpawner UI + manager. ---
            BuildUiAndManager(spawner, spawnTrigger, interactionGroup, organismRig, listItem, factsRow);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            // --- 5) Register with the Essential Addressables package. ---
            RegisterAddressable();

            Debug.Log($"[AISpawner] Scene built at {k_ScenePath} and registered as '{k_SceneAddress}' in '{k_EssentialGroupName}'.");
        }

        // ------------------------------------------------------------------
        //  Prefabs
        // ------------------------------------------------------------------

        /// <summary>
        /// Builds the OrganismRig prefab. The manipulation components are copied from
        /// the existing OctoPrefab spawnable so runtime-loaded organisms behave exactly
        /// like the production AR_Spawn objects (same grab/transform tuning).
        /// </summary>
        static GameObject BuildOrganismRigPrefab()
        {
            System.IO.Directory.CreateDirectory("Assets/AISpawner/Prefabs");

            var rig = new GameObject("OrganismRig");
            try
            {
                var octo = AssetDatabase.LoadAssetAtPath<GameObject>(k_OctoPrefabPath);
                bool copied = false;
                if (octo != null)
                {
                    var sourceGrab = octo.GetComponent<XRGrabInteractable>();
                    var sourceTransformer = octo.GetComponent<ARTransformer>();
                    if (sourceGrab != null && sourceTransformer != null)
                    {
                        // Copy the exact serialized configuration used in production.
                        ComponentUtility.CopyComponent(sourceGrab);
                        ComponentUtility.PasteComponentAsNew(rig);
                        ComponentUtility.CopyComponent(sourceTransformer);
                        ComponentUtility.PasteComponentAsNew(rig);
                        copied = true;
                    }
                }

                if (!copied)
                {
                    Debug.LogWarning("[AISpawner] OctoPrefab not found — using default manipulation settings.");
                    rig.AddComponent<XRGrabInteractable>();
                    var transformer = rig.AddComponent<ARTransformer>();
                    transformer.minScale = 0.5f;
                    transformer.maxScale = 2f;
                }

                var grab = rig.GetComponent<XRGrabInteractable>();
                grab.colliders.Clear(); // auto-collected from the fitted BoxCollider on activation

                var rb = rig.GetComponent<Rigidbody>();
                if (rb == null)
                    rb = rig.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;

                var collider = rig.GetComponent<BoxCollider>();
                if (collider == null)
                    collider = rig.AddComponent<BoxCollider>();
                collider.size = Vector3.one * 0.2f; // placeholder; refitted per model at runtime

                var anchor = new GameObject("ModelAnchor");
                anchor.transform.SetParent(rig.transform, false);

                AssetDatabase.DeleteAsset(k_OrganismRigPath);
                return PrefabUtility.SaveAsPrefabAsset(rig, k_OrganismRigPath);
            }
            finally
            {
                Object.DestroyImmediate(rig);
            }
        }

        static GameObject BuildListItemPrefab()
        {
            GameObject root = AISpawnerUiFactory.CreateRect("OrganismListItem", null);
            try
            {
                var rect = (RectTransform)root.transform;
                rect.sizeDelta = new Vector2(0f, 150f);

                var layoutElement = root.AddComponent<LayoutElement>();
                layoutElement.preferredHeight = 150f;

                Image bg = AISpawnerUiFactory.AddImage(root, AISpawnerUiFactory.RoundedRect, AISpawnerUiFactory.Card);
                Button button = AISpawnerUiFactory.AddButton(root, bg, AISpawnerUiFactory.Card, AISpawnerUiFactory.CardHover);
                AISpawnerUiFactory.AddStroke(root, AISpawnerUiFactory.Outline, AISpawnerUiFactory.StrokeSoft);

                // Thumbnail (future artwork slot; gradient monogram for now).
                GameObject thumb = AISpawnerUiFactory.CreateRect("Thumbnail", root.transform);
                AISpawnerUiFactory.Place(thumb, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(106f, 106f));
                Image thumbImage = AISpawnerUiFactory.AddImage(thumb, AISpawnerUiFactory.CircleGradient, AISpawnerUiFactory.Monogram, Image.Type.Simple, raycast: false);

                GameObject letter = AISpawnerUiFactory.CreateRect("Letter", thumb.transform);
                AISpawnerUiFactory.Stretch(letter, Vector2.zero, Vector2.one);
                TextMeshProUGUI letterText = AISpawnerUiFactory.AddText(letter, "A", 46f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);

                // Name + scientific name.
                GameObject name = AISpawnerUiFactory.CreateRect("Name", root.transform);
                AISpawnerUiFactory.Stretch(name, new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(154f, -68f), new Vector2(-190f, -16f));
                TextMeshProUGUI nameText = AISpawnerUiFactory.AddText(name, "Common Name", 40f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.BottomLeft, FontStyles.Bold);

                GameObject sci = AISpawnerUiFactory.CreateRect("Scientific", root.transform);
                AISpawnerUiFactory.Stretch(sci, new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(154f, 18f), new Vector2(-190f, 66f));
                TextMeshProUGUI sciText = AISpawnerUiFactory.AddText(sci, "Scientific name", 29f, AISpawnerUiFactory.TextSecondary, TextAlignmentOptions.TopLeft, FontStyles.Italic);

                // Right column: size + cached chip.
                GameObject size = AISpawnerUiFactory.CreateRect("Size", root.transform);
                AISpawnerUiFactory.Place(size, new Vector2(1f, 0.5f), new Vector2(-30f, 28f), new Vector2(150f, 40f));
                TextMeshProUGUI sizeText = AISpawnerUiFactory.AddText(size, "0.0 MB", 27f, AISpawnerUiFactory.TextTertiary, TextAlignmentOptions.Right);

                GameObject chip = AISpawnerUiFactory.CreateRect("CachedChip", root.transform);
                AISpawnerUiFactory.Place(chip, new Vector2(1f, 0.5f), new Vector2(-30f, -30f), new Vector2(150f, 50f));
                AISpawnerUiFactory.AddImage(chip, AISpawnerUiFactory.RoundedRectSmall, AISpawnerUiFactory.AccentSoft, Image.Type.Sliced, raycast: false);
                AISpawnerUiFactory.AddStroke(chip, AISpawnerUiFactory.OutlineSmall, AISpawnerUiFactory.AccentEdge);
                GameObject chipLabel = AISpawnerUiFactory.CreateRect("Label", chip.transform);
                AISpawnerUiFactory.Stretch(chipLabel, Vector2.zero, Vector2.one);
                AISpawnerUiFactory.AddText(chipLabel, "Cached", 25f, AISpawnerUiFactory.Accent, TextAlignmentOptions.Center, FontStyles.Bold);
                chip.SetActive(false);

                var view = root.AddComponent<OrganismListItemView>();
                var so = new SerializedObject(view);
                so.FindProperty("m_Button").objectReferenceValue = button;
                so.FindProperty("m_NameText").objectReferenceValue = nameText;
                so.FindProperty("m_ScientificText").objectReferenceValue = sciText;
                so.FindProperty("m_SizeText").objectReferenceValue = sizeText;
                so.FindProperty("m_CachedChip").objectReferenceValue = chip;
                so.FindProperty("m_ThumbnailImage").objectReferenceValue = thumbImage;
                so.FindProperty("m_ThumbnailLetter").objectReferenceValue = letterText;
                so.ApplyModifiedPropertiesWithoutUndo();

                AssetDatabase.DeleteAsset(k_ListItemPath);
                return PrefabUtility.SaveAsPrefabAsset(root, k_ListItemPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        static GameObject BuildFactsRowPrefab()
        {
            GameObject root = AISpawnerUiFactory.CreateRect("FactsRow", null);
            try
            {
                AISpawnerUiFactory.AddImage(root, AISpawnerUiFactory.RoundedRectSmall, new Color(1f, 1f, 1f, 0.045f), Image.Type.Sliced, raycast: false);
                AISpawnerUiFactory.AddStroke(root, AISpawnerUiFactory.OutlineSmall, AISpawnerUiFactory.StrokeSoft);

                var layout = root.AddComponent<VerticalLayoutGroup>();
                layout.padding = new RectOffset(32, 32, 20, 20);
                layout.spacing = 8f;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;

                GameObject label = AISpawnerUiFactory.CreateRect("Label", root.transform);
                AISpawnerUiFactory.AddText(label, "Label", 25f, AISpawnerUiFactory.Accent, TextAlignmentOptions.Left, FontStyles.Bold | FontStyles.UpperCase, 6f);

                GameObject value = AISpawnerUiFactory.CreateRect("Value", root.transform);
                var valueText = AISpawnerUiFactory.AddText(value, "Value", 33f, AISpawnerUiFactory.TextPrimary);
                valueText.overflowMode = TextOverflowModes.Overflow;

                AssetDatabase.DeleteAsset(k_FactsRowPath);
                return PrefabUtility.SaveAsPrefabAsset(root, k_FactsRowPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        // ------------------------------------------------------------------
        //  Scene surgery
        // ------------------------------------------------------------------

        /// <summary>Removes AR_Spawn's template menu UI from the copied scene.</summary>
        static void StripTemplateObjects()
        {
            // Whole hierarchies replaced by the AISpawner UI.
            string[] rootNamesToRemove = { "UI", "Coaching UI", "Greeting Prompt", "GreetingCTA" };
            foreach (string name in rootNamesToRemove)
            {
                foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects().ToArray())
                {
                    if (root != null && root.name == name)
                        Object.DestroyImmediate(root);
                }
            }

            // Template managers that may live anywhere (their UI is gone).
            DestroyAll<ARTemplateMenuManager>();
            DestroyAll<GoalManager>();
            foreach (var debugMenu in Object.FindObjectsByType<ARDebugMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(debugMenu.gameObject);
        }

        static void DestroyAll<T>() where T : Component
        {
            foreach (T component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(component);
        }

        // ------------------------------------------------------------------
        //  UI + manager
        // ------------------------------------------------------------------

        static void BuildUiAndManager(
            ObjectSpawner spawner,
            ARInteractorSpawnTrigger spawnTrigger,
            XRInteractionGroup interactionGroup,
            GameObject organismRig,
            GameObject listItemPrefab,
            GameObject factsRowPrefab)
        {
            // Root canvas.
            GameObject canvasGo = AISpawnerUiFactory.CreateRect("AISpawner UI", null);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 2340f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // ---- AI entry button: aqua gradient orb with halo glow. ----
            GameObject aiButtonGo = AISpawnerUiFactory.CreateRect("AI Button", canvasGo.transform);
            AISpawnerUiFactory.Place(aiButtonGo, new Vector2(1f, 0f), new Vector2(-56f, 84f), new Vector2(160f, 160f));

            GameObject glow = AISpawnerUiFactory.CreateRect("Glow", aiButtonGo.transform);
            AISpawnerUiFactory.Place(glow, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(252f, 252f));
            Color glowColor = AISpawnerUiFactory.Accent;
            glowColor.a = 0.5f;
            AISpawnerUiFactory.AddImage(glow, AISpawnerUiFactory.GlowRing, glowColor, Image.Type.Simple, raycast: false);

            Image aiBg = AISpawnerUiFactory.AddImage(aiButtonGo, AISpawnerUiFactory.CircleGradient, AISpawnerUiFactory.Accent, Image.Type.Simple);
            AISpawnerUiFactory.AddButton(aiButtonGo, aiBg, AISpawnerUiFactory.Accent, AISpawnerUiFactory.AccentDark);
            aiButtonGo.AddComponent<CanvasGroup>();

            GameObject aiLabel = AISpawnerUiFactory.CreateRect("Label", aiButtonGo.transform);
            AISpawnerUiFactory.Stretch(aiLabel, Vector2.zero, Vector2.one);
            AISpawnerUiFactory.AddText(aiLabel, "AI", 56f, AISpawnerUiFactory.OnAccent, TextAlignmentOptions.Center, FontStyles.Bold, 2f);
            var aiButton = aiButtonGo.AddComponent<AIButton>();

            // ---- Browser panel. ----
            var panelParts = BuildBrowserPanel(canvasGo.transform, listItemPrefab);

            // ---- Download progress card. ----
            var progressParts = BuildProgressCard(canvasGo.transform);

            // ---- Prompt banner. ----
            var bannerParts = BuildPromptBanner(canvasGo.transform);

            // ---- Facts sheet + facts/delete buttons. ----
            var factsParts = BuildFactsSheet(canvasGo.transform, factsRowPrefab);

            // Delete: destructive ghost pill.
            GameObject deleteGo = AISpawnerUiFactory.CreateRect("Delete Button", canvasGo.transform);
            AISpawnerUiFactory.Place(deleteGo, new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(320f, 112f));
            Image deleteBg = AISpawnerUiFactory.AddImage(deleteGo, AISpawnerUiFactory.RoundedRectSmall, AISpawnerUiFactory.DangerBg);
            Button deleteButton = AISpawnerUiFactory.AddButton(deleteGo, deleteBg, AISpawnerUiFactory.DangerBg, AISpawnerUiFactory.DangerBgPressed);
            AISpawnerUiFactory.AddStroke(deleteGo, AISpawnerUiFactory.OutlineSmall, AISpawnerUiFactory.DangerEdge);
            GameObject deleteLabel = AISpawnerUiFactory.CreateRect("Label", deleteGo.transform);
            AISpawnerUiFactory.Stretch(deleteLabel, Vector2.zero, Vector2.one);
            AISpawnerUiFactory.AddText(deleteLabel, "Delete", 36f, AISpawnerUiFactory.Danger, TextAlignmentOptions.Center, FontStyles.Bold);

            // Facts: glass pill, bottom-left.
            GameObject factsButtonGo = AISpawnerUiFactory.CreateRect("Facts Button", canvasGo.transform);
            AISpawnerUiFactory.Place(factsButtonGo, new Vector2(0f, 0f), new Vector2(56f, 104f), new Vector2(430f, 112f));
            Image factsBg = AISpawnerUiFactory.AddImage(factsButtonGo, AISpawnerUiFactory.RoundedRectSmall, AISpawnerUiFactory.Card);
            Button factsButton = AISpawnerUiFactory.AddButton(factsButtonGo, factsBg, AISpawnerUiFactory.Card, AISpawnerUiFactory.CardHover);
            AISpawnerUiFactory.AddStroke(factsButtonGo, AISpawnerUiFactory.OutlineSmall, AISpawnerUiFactory.Stroke);
            GameObject factsButtonLabel = AISpawnerUiFactory.CreateRect("Label", factsButtonGo.transform);
            AISpawnerUiFactory.Stretch(factsButtonLabel, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-24f, 0f));
            TextMeshProUGUI factsLabelText = AISpawnerUiFactory.AddText(factsButtonLabel, "Facts", 34f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);

            // ---- Manager (composition root). ----
            GameObject managerGo = new GameObject("AISpawner System");
            var manager = managerGo.AddComponent<AISpawnManager>();

            var so = new SerializedObject(manager);
            so.FindProperty("m_ObjectSpawner").objectReferenceValue = spawner;
            so.FindProperty("m_SpawnTrigger").objectReferenceValue = spawnTrigger;
            so.FindProperty("m_InteractionGroup").objectReferenceValue = interactionGroup;
            so.FindProperty("m_OrganismRigPrefab").objectReferenceValue = organismRig;
            so.FindProperty("m_AIButton").objectReferenceValue = aiButton;
            so.FindProperty("m_ListController").objectReferenceValue = panelParts;
            so.FindProperty("m_DownloadProgressUI").objectReferenceValue = progressParts;
            so.FindProperty("m_PromptBanner").objectReferenceValue = bannerParts;
            so.FindProperty("m_FactsSheet").objectReferenceValue = factsParts;
            so.FindProperty("m_DeleteButton").objectReferenceValue = deleteButton;
            so.FindProperty("m_FactsButton").objectReferenceValue = factsButton;
            so.FindProperty("m_FactsButtonLabel").objectReferenceValue = factsLabelText;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static AIListController BuildBrowserPanel(Transform canvas, GameObject listItemPrefab)
        {
            GameObject panelRoot = AISpawnerUiFactory.CreateRect("AI Panel", canvas);
            AISpawnerUiFactory.Stretch(panelRoot, Vector2.zero, Vector2.one);
            var panelGroup = panelRoot.AddComponent<CanvasGroup>();
            panelRoot.AddComponent<UICanvasTag>(); // Android back button closes the panel first

            // Scrim.
            GameObject scrim = AISpawnerUiFactory.CreateRect("Scrim", panelRoot.transform);
            AISpawnerUiFactory.Stretch(scrim, Vector2.zero, Vector2.one);
            Image scrimImage = AISpawnerUiFactory.AddImage(scrim, null, AISpawnerUiFactory.Scrim);
            Button scrimButton = AISpawnerUiFactory.AddButton(scrim, scrimImage, AISpawnerUiFactory.Scrim, AISpawnerUiFactory.Scrim);

            // Sheet.
            GameObject sheet = AISpawnerUiFactory.CreateRect("Sheet", panelRoot.transform);
            AISpawnerUiFactory.Stretch(sheet, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.82f));
            AISpawnerUiFactory.AddImage(sheet, AISpawnerUiFactory.RoundedRectLarge, AISpawnerUiFactory.Sheet);
            AISpawnerUiFactory.AddStroke(sheet, AISpawnerUiFactory.OutlineLarge, AISpawnerUiFactory.Stroke);
            AISpawnerUiFactory.AddSheetHandle(sheet.transform);

            // Header.
            GameObject title = AISpawnerUiFactory.CreateRect("Title", sheet.transform);
            AISpawnerUiFactory.Stretch(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(48f, -142f), new Vector2(-150f, -52f));
            TextMeshProUGUI titleText = AISpawnerUiFactory.AddText(title, "Marine AI Organisms", 50f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.BottomLeft, FontStyles.Bold);

            GameObject subtitle = AISpawnerUiFactory.CreateRect("Subtitle", sheet.transform);
            AISpawnerUiFactory.Stretch(subtitle, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(48f, -196f), new Vector2(-150f, -148f));
            TextMeshProUGUI subtitleText = AISpawnerUiFactory.AddText(subtitle, "", 28f, AISpawnerUiFactory.TextSecondary, TextAlignmentOptions.TopLeft);

            Button closeButton = AISpawnerUiFactory.CreateCircleIconButton("Close Button", sheet.transform, "×");
            AISpawnerUiFactory.Place(closeButton.gameObject, new Vector2(1f, 1f), new Vector2(-44f, -48f), new Vector2(84f, 84f));

            // Search bar.
            GameObject searchBar = AISpawnerUiFactory.CreateRect("Search Bar", sheet.transform);
            AISpawnerUiFactory.Stretch(searchBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -322f), new Vector2(-44f, -216f));
            AISpawnerUiFactory.AddImage(searchBar, AISpawnerUiFactory.RoundedRectSmall, AISpawnerUiFactory.FieldBg);
            AISpawnerUiFactory.AddStroke(searchBar, AISpawnerUiFactory.OutlineSmall, AISpawnerUiFactory.Stroke);

            GameObject textArea = AISpawnerUiFactory.CreateRect("Text Area", searchBar.transform);
            RectTransform textAreaRect = AISpawnerUiFactory.Stretch(textArea, Vector2.zero, Vector2.one, new Vector2(36f, 8f), new Vector2(-104f, -8f));
            textArea.AddComponent<RectMask2D>();

            GameObject placeholder = AISpawnerUiFactory.CreateRect("Placeholder", textArea.transform);
            AISpawnerUiFactory.Stretch(placeholder, Vector2.zero, Vector2.one);
            TextMeshProUGUI placeholderText = AISpawnerUiFactory.AddText(placeholder, "Search by name or species…", 34f, new Color(1f, 1f, 1f, 0.32f), TextAlignmentOptions.Left);
            placeholderText.fontStyle = FontStyles.Italic;

            GameObject inputText = AISpawnerUiFactory.CreateRect("Text", textArea.transform);
            AISpawnerUiFactory.Stretch(inputText, Vector2.zero, Vector2.one);
            TextMeshProUGUI inputTmp = AISpawnerUiFactory.AddText(inputText, "", 34f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.Left);

            var inputField = searchBar.AddComponent<TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = inputTmp;
            inputField.placeholder = placeholderText;
            inputField.lineType = TMP_InputField.LineType.SingleLine;

            Button clearButton = AISpawnerUiFactory.CreateCircleIconButton("Clear Button", searchBar.transform, "×", 38f);
            AISpawnerUiFactory.Place(clearButton.gameObject, new Vector2(1f, 0.5f), new Vector2(-18f, 0f), new Vector2(68f, 68f));

            var search = searchBar.AddComponent<SearchController>();
            var searchSo = new SerializedObject(search);
            searchSo.FindProperty("m_InputField").objectReferenceValue = inputField;
            searchSo.FindProperty("m_ClearButton").objectReferenceValue = clearButton;
            searchSo.ApplyModifiedPropertiesWithoutUndo();

            // List area.
            GameObject scrollView = AISpawnerUiFactory.CreateRect("Scroll View", sheet.transform);
            AISpawnerUiFactory.Stretch(scrollView, Vector2.zero, Vector2.one, new Vector2(36f, 36f), new Vector2(-36f, -346f));
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 40f;

            GameObject viewport = AISpawnerUiFactory.CreateRect("Viewport", scrollView.transform);
            AISpawnerUiFactory.Stretch(viewport, Vector2.zero, Vector2.one);
            viewport.AddComponent<RectMask2D>();
            AISpawnerUiFactory.AddImage(viewport, null, Color.clear);

            GameObject content = AISpawnerUiFactory.CreateRect("Content", viewport.transform);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 18f;
            contentLayout.padding = new RectOffset(0, 0, 6, 28);
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRect;

            // States.
            GameObject loading = BuildCenteredState(sheet.transform, "Loading State", out TextMeshProUGUI loadingText, "Fetching live catalog…");
            loadingText.color = AISpawnerUiFactory.TextSecondary;
            GameObject spinnerGo = AISpawnerUiFactory.CreateRect("Spinner", loading.transform);
            AISpawnerUiFactory.Place(spinnerGo, new Vector2(0.5f, 0.5f), new Vector2(0f, 100f), new Vector2(110f, 110f));
            AISpawnerUiFactory.AddImage(spinnerGo, AISpawnerUiFactory.Ring, AISpawnerUiFactory.Accent, Image.Type.Simple, raycast: false);
            spinnerGo.AddComponent<UISpinner>();

            GameObject error = BuildCenteredState(sheet.transform, "Error State", out TextMeshProUGUI errorText, "Something went wrong.");
            Button retryButton = AISpawnerUiFactory.CreatePrimaryPill("Retry Button", error.transform, "Try Again");
            AISpawnerUiFactory.Place(retryButton.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(330f, 104f));

            GameObject empty = BuildCenteredState(sheet.transform, "Empty State", out TextMeshProUGUI emptyText, "No organisms match your search.");
            emptyText.color = AISpawnerUiFactory.TextSecondary;
            GameObject emptyTitle = AISpawnerUiFactory.CreateRect("Empty Title", empty.transform);
            AISpawnerUiFactory.Place(emptyTitle, new Vector2(0.5f, 0.5f), new Vector2(0f, 70f), new Vector2(700f, 60f));
            AISpawnerUiFactory.AddText(emptyTitle, "No matches", 40f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.Center, FontStyles.Bold);

            loading.SetActive(false);
            error.SetActive(false);
            empty.SetActive(false);

            // Controller.
            var controller = panelRoot.AddComponent<AIListController>();
            var so = new SerializedObject(controller);
            so.FindProperty("m_PanelRoot").objectReferenceValue = panelRoot;
            so.FindProperty("m_PanelCanvasGroup").objectReferenceValue = panelGroup;
            so.FindProperty("m_Sheet").objectReferenceValue = sheet.transform;
            so.FindProperty("m_CloseButton").objectReferenceValue = closeButton;
            so.FindProperty("m_ScrimButton").objectReferenceValue = scrimButton;
            so.FindProperty("m_TitleText").objectReferenceValue = titleText;
            so.FindProperty("m_SubtitleText").objectReferenceValue = subtitleText;
            so.FindProperty("m_SearchController").objectReferenceValue = search;
            so.FindProperty("m_ListContent").objectReferenceValue = contentRect;
            so.FindProperty("m_ItemPrefab").objectReferenceValue = listItemPrefab.GetComponent<OrganismListItemView>();
            so.FindProperty("m_LoadingState").objectReferenceValue = loading;
            so.FindProperty("m_LoadingText").objectReferenceValue = loadingText;
            so.FindProperty("m_ErrorState").objectReferenceValue = error;
            so.FindProperty("m_ErrorText").objectReferenceValue = errorText;
            so.FindProperty("m_RetryButton").objectReferenceValue = retryButton;
            so.FindProperty("m_EmptyState").objectReferenceValue = empty;
            so.FindProperty("m_ScrollView").objectReferenceValue = scrollView;
            so.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        static GameObject BuildCenteredState(Transform sheet, string name, out TextMeshProUGUI text, string initial)
        {
            GameObject state = AISpawnerUiFactory.CreateRect(name, sheet);
            AISpawnerUiFactory.Stretch(state, Vector2.zero, Vector2.one, new Vector2(60f, 40f), new Vector2(-60f, -346f));

            GameObject label = AISpawnerUiFactory.CreateRect("Message", state.transform);
            AISpawnerUiFactory.Place(label, new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(780f, 180f));
            text = AISpawnerUiFactory.AddText(label, initial, 32f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.Center);
            return state;
        }

        static DownloadProgressUI BuildProgressCard(Transform canvas)
        {
            // Component lives on an always-active host; the visual card child toggles.
            GameObject host = AISpawnerUiFactory.CreateRect("Download Progress Host", canvas);
            AISpawnerUiFactory.Stretch(host, Vector2.zero, Vector2.one);

            GameObject root = AISpawnerUiFactory.CreateRect("Card", host.transform);
            AISpawnerUiFactory.Place(root, new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(980f, 252f));
            var group = root.AddComponent<CanvasGroup>();

            // Shadow first (renders lowest), then the background on its own layer —
            // uGUI children draw after parents, so the root itself carries no graphic.
            AISpawnerUiFactory.AddShadow(root);
            GameObject cardBg = AISpawnerUiFactory.CreateRect("Bg", root.transform);
            AISpawnerUiFactory.Stretch(cardBg, Vector2.zero, Vector2.one);
            AISpawnerUiFactory.AddImage(cardBg, AISpawnerUiFactory.RoundedRect, AISpawnerUiFactory.Sheet);
            AISpawnerUiFactory.AddStroke(cardBg, AISpawnerUiFactory.Outline, AISpawnerUiFactory.Stroke);

            GameObject title = AISpawnerUiFactory.CreateRect("Title", root.transform);
            AISpawnerUiFactory.Stretch(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(44f, -86f), new Vector2(-116f, -26f));
            TextMeshProUGUI titleText = AISpawnerUiFactory.AddText(title, "Downloading…", 36f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.BottomLeft, FontStyles.Bold);

            Button cancelButton = AISpawnerUiFactory.CreateCircleIconButton("Cancel Button", root.transform, "×", 40f);
            AISpawnerUiFactory.Place(cancelButton.gameObject, new Vector2(1f, 1f), new Vector2(-30f, -30f), new Vector2(68f, 68f));

            GameObject barBg = AISpawnerUiFactory.CreateRect("Bar Background", root.transform);
            AISpawnerUiFactory.Stretch(barBg, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(44f, 96f), new Vector2(-44f, 128f));
            AISpawnerUiFactory.AddImage(barBg, AISpawnerUiFactory.RoundedRectSmall, new Color(1f, 1f, 1f, 0.1f), Image.Type.Sliced, raycast: false);

            GameObject barFill = AISpawnerUiFactory.CreateRect("Bar Fill", barBg.transform);
            AISpawnerUiFactory.Stretch(barFill, Vector2.zero, Vector2.one, new Vector2(3f, 3f), new Vector2(-3f, -3f));
            Image fillImage = AISpawnerUiFactory.AddImage(barFill, AISpawnerUiFactory.GradientBar, AISpawnerUiFactory.Accent, Image.Type.Simple, raycast: false);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillAmount = 0f;

            GameObject detail = AISpawnerUiFactory.CreateRect("Detail", root.transform);
            AISpawnerUiFactory.Stretch(detail, new Vector2(0f, 0f), new Vector2(0.62f, 0f), new Vector2(44f, 26f), new Vector2(0f, 82f));
            TextMeshProUGUI detailText = AISpawnerUiFactory.AddText(detail, "", 27f, AISpawnerUiFactory.TextSecondary);

            GameObject percent = AISpawnerUiFactory.CreateRect("Percent", root.transform);
            AISpawnerUiFactory.Stretch(percent, new Vector2(0.62f, 0f), new Vector2(1f, 0f), new Vector2(0f, 26f), new Vector2(-44f, 82f));
            TextMeshProUGUI percentText = AISpawnerUiFactory.AddText(percent, "0%", 30f, AISpawnerUiFactory.Accent, TextAlignmentOptions.Right, FontStyles.Bold);

            // Error overlay.
            GameObject errorGroup = AISpawnerUiFactory.CreateRect("Error Group", root.transform);
            AISpawnerUiFactory.Stretch(errorGroup, Vector2.zero, Vector2.one);
            AISpawnerUiFactory.AddImage(errorGroup, AISpawnerUiFactory.RoundedRect, AISpawnerUiFactory.Sheet);
            AISpawnerUiFactory.AddStroke(errorGroup, AISpawnerUiFactory.Outline, AISpawnerUiFactory.Stroke);

            GameObject errorText = AISpawnerUiFactory.CreateRect("Error Text", errorGroup.transform);
            AISpawnerUiFactory.Stretch(errorText, new Vector2(0f, 0.36f), new Vector2(1f, 1f), new Vector2(44f, 0f), new Vector2(-44f, -18f));
            TextMeshProUGUI errorTmp = AISpawnerUiFactory.AddText(errorText, "", 28f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.Center);

            Button retryButton = AISpawnerUiFactory.CreatePrimaryPill("Retry Button", errorGroup.transform, "Try Again", 30f);
            AISpawnerUiFactory.Place(retryButton.gameObject, new Vector2(0.3f, 0f), new Vector2(0f, 26f), new Vector2(300f, 92f));

            Button dismissButton = AISpawnerUiFactory.CreateGhostPill("Dismiss Button", errorGroup.transform, "Dismiss", 30f);
            AISpawnerUiFactory.Place(dismissButton.gameObject, new Vector2(0.72f, 0f), new Vector2(0f, 26f), new Vector2(280f, 92f));

            errorGroup.SetActive(false);
            root.SetActive(false);

            var ui = host.AddComponent<DownloadProgressUI>();
            var so = new SerializedObject(ui);
            so.FindProperty("m_Root").objectReferenceValue = root;
            so.FindProperty("m_CanvasGroup").objectReferenceValue = group;
            so.FindProperty("m_TitleText").objectReferenceValue = titleText;
            so.FindProperty("m_DetailText").objectReferenceValue = detailText;
            so.FindProperty("m_ProgressFill").objectReferenceValue = fillImage;
            so.FindProperty("m_PercentText").objectReferenceValue = percentText;
            so.FindProperty("m_CancelButton").objectReferenceValue = cancelButton;
            so.FindProperty("m_ErrorGroup").objectReferenceValue = errorGroup;
            so.FindProperty("m_ErrorText").objectReferenceValue = errorTmp;
            so.FindProperty("m_RetryButton").objectReferenceValue = retryButton;
            so.FindProperty("m_DismissButton").objectReferenceValue = dismissButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            return ui;
        }

        static PromptBanner BuildPromptBanner(Transform canvas)
        {
            GameObject host = AISpawnerUiFactory.CreateRect("Prompt Banner Host", canvas);
            AISpawnerUiFactory.Stretch(host, Vector2.zero, Vector2.one);

            GameObject root = AISpawnerUiFactory.CreateRect("Banner", host.transform);
            AISpawnerUiFactory.Place(root, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(1000f, 144f));
            AISpawnerUiFactory.AddImage(root, AISpawnerUiFactory.RoundedRect, AISpawnerUiFactory.Sheet, Image.Type.Sliced, raycast: false);
            AISpawnerUiFactory.AddStroke(root, AISpawnerUiFactory.Outline, AISpawnerUiFactory.Stroke);
            var group = root.AddComponent<CanvasGroup>();

            // Accent edge: signals "guidance" at a glance.
            GameObject edge = AISpawnerUiFactory.CreateRect("Accent Edge", root.transform);
            AISpawnerUiFactory.Place(edge, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(8f, 92f));
            AISpawnerUiFactory.AddImage(edge, AISpawnerUiFactory.RoundedRectSmall, AISpawnerUiFactory.Accent, Image.Type.Sliced, raycast: false);

            GameObject label = AISpawnerUiFactory.CreateRect("Message", root.transform);
            AISpawnerUiFactory.Stretch(label, Vector2.zero, Vector2.one, new Vector2(60f, 14f), new Vector2(-40f, -14f));
            TextMeshProUGUI text = AISpawnerUiFactory.AddText(label, "", 30f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.Left);

            root.SetActive(false);

            var banner = host.AddComponent<PromptBanner>();
            var so = new SerializedObject(banner);
            so.FindProperty("m_Root").objectReferenceValue = root;
            so.FindProperty("m_CanvasGroup").objectReferenceValue = group;
            so.FindProperty("m_Text").objectReferenceValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            return banner;
        }

        static FactsSheetController BuildFactsSheet(Transform canvas, GameObject rowPrefab)
        {
            GameObject host = AISpawnerUiFactory.CreateRect("Facts Sheet Host", canvas);
            AISpawnerUiFactory.Stretch(host, Vector2.zero, Vector2.one);

            GameObject root = AISpawnerUiFactory.CreateRect("Facts Sheet", host.transform);
            AISpawnerUiFactory.Stretch(root, Vector2.zero, Vector2.one);
            var group = root.AddComponent<CanvasGroup>();
            root.AddComponent<UICanvasTag>();

            GameObject scrim = AISpawnerUiFactory.CreateRect("Scrim", root.transform);
            AISpawnerUiFactory.Stretch(scrim, Vector2.zero, Vector2.one);
            Image scrimImage = AISpawnerUiFactory.AddImage(scrim, null, AISpawnerUiFactory.Scrim);
            Button scrimButton = AISpawnerUiFactory.AddButton(scrim, scrimImage, AISpawnerUiFactory.Scrim, AISpawnerUiFactory.Scrim);

            GameObject sheet = AISpawnerUiFactory.CreateRect("Sheet", root.transform);
            AISpawnerUiFactory.Stretch(sheet, new Vector2(0.02f, 0f), new Vector2(0.98f, 0.68f));
            AISpawnerUiFactory.AddImage(sheet, AISpawnerUiFactory.RoundedRectLarge, AISpawnerUiFactory.Sheet);
            AISpawnerUiFactory.AddStroke(sheet, AISpawnerUiFactory.OutlineLarge, AISpawnerUiFactory.Stroke);
            AISpawnerUiFactory.AddSheetHandle(sheet.transform);

            GameObject title = AISpawnerUiFactory.CreateRect("Title", sheet.transform);
            AISpawnerUiFactory.Stretch(title, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(48f, -124f), new Vector2(-150f, -44f));
            TextMeshProUGUI titleText = AISpawnerUiFactory.AddText(title, "Organism", 46f, AISpawnerUiFactory.TextPrimary, TextAlignmentOptions.BottomLeft, FontStyles.Bold);

            // Accent underline anchors the header like a section marker.
            GameObject underline = AISpawnerUiFactory.CreateRect("Title Accent", sheet.transform);
            AISpawnerUiFactory.Place(underline, new Vector2(0f, 1f), new Vector2(48f, -142f), new Vector2(132f, 6f));
            AISpawnerUiFactory.AddImage(underline, AISpawnerUiFactory.GradientBar, AISpawnerUiFactory.Accent, Image.Type.Simple, raycast: false);

            Button closeButton = AISpawnerUiFactory.CreateCircleIconButton("Close Button", sheet.transform, "×");
            AISpawnerUiFactory.Place(closeButton.gameObject, new Vector2(1f, 1f), new Vector2(-44f, -48f), new Vector2(84f, 84f));

            GameObject scrollView = AISpawnerUiFactory.CreateRect("Scroll View", sheet.transform);
            AISpawnerUiFactory.Stretch(scrollView, Vector2.zero, Vector2.one, new Vector2(36f, 36f), new Vector2(-36f, -172f));
            var scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.scrollSensitivity = 40f;

            GameObject viewport = AISpawnerUiFactory.CreateRect("Viewport", scrollView.transform);
            AISpawnerUiFactory.Stretch(viewport, Vector2.zero, Vector2.one);
            viewport.AddComponent<RectMask2D>();
            AISpawnerUiFactory.AddImage(viewport, null, Color.clear);

            GameObject content = AISpawnerUiFactory.CreateRect("Content", viewport.transform);
            var contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            // Zeroed offsets: a fresh RectTransform defaults to a 100px sizeDelta, which
            // previously left the content 50px wider than the viewport on each side and
            // cropped the first characters of every facts row.
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var layout = content.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16f;
            layout.padding = new RectOffset(0, 0, 6, 28);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = (RectTransform)viewport.transform;
            scrollRect.content = contentRect;

            root.SetActive(false);

            var controller = host.AddComponent<FactsSheetController>();
            var so = new SerializedObject(controller);
            so.FindProperty("m_Root").objectReferenceValue = root;
            so.FindProperty("m_CanvasGroup").objectReferenceValue = group;
            so.FindProperty("m_TitleText").objectReferenceValue = titleText;
            so.FindProperty("m_Content").objectReferenceValue = contentRect;
            so.FindProperty("m_RowPrefab").objectReferenceValue = rowPrefab;
            so.FindProperty("m_CloseButton").objectReferenceValue = closeButton;
            so.FindProperty("m_ScrimButton").objectReferenceValue = scrimButton;
            so.ApplyModifiedPropertiesWithoutUndo();

            return controller;
        }

        // ------------------------------------------------------------------
        //  Addressables
        // ------------------------------------------------------------------

        static void RegisterAddressable()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[AISpawner] Addressables settings not found — register the scene manually.");
                return;
            }

            AddressableAssetGroup group = settings.groups.FirstOrDefault(g => g != null && g.Name == k_EssentialGroupName);
            if (group == null)
            {
                Debug.LogError($"[AISpawner] Addressables group '{k_EssentialGroupName}' not found — register the scene manually.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(k_ScenePath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = k_SceneAddress;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entry, true);
            AssetDatabase.SaveAssets();
        }
    }
}
