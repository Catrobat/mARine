#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Editor utility that builds the complete FaceAR scene with a single menu click.
/// Creates: URP pipeline asset, materials, mask prefabs (from primitives),
/// the AR scene hierarchy, water effects, UI canvas, and wires all references.
///
/// Usage: Menu → FaceAR → Setup Complete Scene
/// </summary>
public static class FaceARSceneSetup
{
    // ─── Colors ────────────────────────────────────────────────
    private static readonly Color TurquoiseColor = new(0.25f, 0.88f, 0.82f, 1f);
    private static readonly Color DarkVisorColor = new(0.05f, 0.05f, 0.08f, 0.85f);
    private static readonly Color StrapColor = new(0.15f, 0.15f, 0.15f, 1f);

    // ─── Menu Entry ────────────────────────────────────────────
    [MenuItem("FaceAR/Setup Complete Scene", false, 1)]
    public static void SetupScene()
    {
        if (!EditorUtility.DisplayDialog("FaceAR Scene Setup",
            "This will create a new FaceAR scene with all objects,\n" +
            "materials, prefabs, and UI.\n\n" +
            "Continue?", "Yes, Build It", "Cancel"))
            return;

        EnsureFolders();
        var urpAsset = SetupURP();
        FixAndroidPlayerSettings();    // Force OpenGLES3 — Vulkan causes AR background black-screen on Unity 6
        EnsureXRStartupEnabled();      // Guard: verify XR AutomaticLoading/Running/InitOnStart are ON
        AddARBackgroundRendererFeatureToAllRenderers();
        var materials = CreateMaterials();
        var maskPrefabs = CreateMaskPrefabs(materials);
        var scene = CreateScene(maskPrefabs, materials);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/FaceARScene.unity");
        Debug.Log("[FaceAR] ✅ Scene setup complete! Open Assets/Scenes/FaceARScene.unity");
    }

    // ─── Folder Structure ──────────────────────────────────────
    private static void EnsureFolders()
    {
        string[] folders = {
            "Assets/Scenes",
            "Assets/Materials",
            "Assets/Prefabs",
            "Assets/Settings",
            "Assets/Textures"
        };
        foreach (var f in folders)
        {
            if (!AssetDatabase.IsValidFolder(f))
            {
                var parts = f.Split('/');
                string parent = parts[0];
                for (int i = 1; i < parts.Length; i++)
                {
                    string full = parent + "/" + parts[i];
                    if (!AssetDatabase.IsValidFolder(full))
                        AssetDatabase.CreateFolder(parent, parts[i]);
                    parent = full;
                }
            }
        }
    }

    // ─── URP Setup ─────────────────────────────────────────────
    private static UniversalRenderPipelineAsset SetupURP()
    {
        const string rendererPath = "Assets/Settings/FaceAR_ForwardRenderer.asset";
        const string pipelinePath = "Assets/Settings/FaceAR_URPAsset.asset";

        // Rebuild from scratch on every run so all fields are guaranteed correct
        if (AssetDatabase.LoadAssetAtPath<UniversalRendererData>(rendererPath) != null)
            AssetDatabase.DeleteAsset(rendererPath);
        if (AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath) != null)
            AssetDatabase.DeleteAsset(pipelinePath);
        AssetDatabase.Refresh();

        // --- Renderer Data + AR Background Feature ---
        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        rendererData.name = "FaceAR_ForwardRenderer";

        var arFeature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
        arFeature.name = "ARBackgroundRendererFeature";
        rendererData.rendererFeatures.Add(arFeature);

        AssetDatabase.CreateAsset(rendererData, rendererPath);
        AssetDatabase.AddObjectToAsset(arFeature, rendererData);
        EditorUtility.SetDirty(rendererData);

        // Save now so TryGetGUIDAndLocalFileIdentifier returns a valid file ID
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Populate m_RendererFeatureMap — URP 17's Render Graph uses this list to locate
        // renderer features at runtime. When features are added programmatically the map
        // is left empty and the feature is silently skipped, causing a black camera feed.
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(arFeature, out string _, out long featureLocalId);
        if (featureLocalId != 0)
        {
            var rdSO = new SerializedObject(rendererData);
            var featureMapProp = rdSO.FindProperty("m_RendererFeatureMap");
            if (featureMapProp != null)
            {
                featureMapProp.arraySize = 1;
                featureMapProp.GetArrayElementAtIndex(0).longValue = featureLocalId;
                rdSO.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rendererData);
                AssetDatabase.SaveAssets();
            }
        }

        // --- Pipeline Asset ---
        var pipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
        pipelineAsset.name = "FaceAR_URPAsset";
        AssetDatabase.CreateAsset(pipelineAsset, pipelinePath);

        // Wire renderer to pipeline
        var so = new SerializedObject(pipelineAsset);
        var rendererList = so.FindProperty("m_RendererDataList");
        if (rendererList != null)
        {
            rendererList.arraySize = 1;
            rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Assign to all quality levels
        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = pipelineAsset;
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[FaceAR] ✅ URP pipeline + ARBackgroundRendererFeature configured (m_RendererFeatureMap populated)");
        return pipelineAsset;
    }

    // ─── Android Player Settings ────────────────────────────────

    /// <summary>
    /// Forces OpenGLES3 on Android and disables Auto Graphics API.
    /// Vulkan has a known incompatibility with URP 17's ARBackgroundRendererFeature on some
    /// devices — the camera feed renders black even when the renderer feature is present.
    /// </summary>
    private static void FixAndroidPlayerSettings()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(
            BuildTarget.Android,
            new[] { GraphicsDeviceType.OpenGLES3 });
        Debug.Log("[FaceAR] ✅ Android graphics API set to OpenGLES3 only (Vulkan disabled).");
    }

    // ─── XR Startup Enforcement ─────────────────────────────────

    /// <summary>
    /// Reads every sub-asset inside XRGeneralSettingsPerBuildTarget.asset and enforces
    /// that AutomaticLoading, AutomaticRunning, and InitManagerOnStart are all true.
    /// If any of these is 0 the build looks completely dead on device — no AR starts.
    /// </summary>
    private static void EnsureXRStartupEnabled()
    {
        const string xrSettingsPath = "Assets/XR/XRGeneralSettingsPerBuildTarget.asset";
        var allSubAssets = AssetDatabase.LoadAllAssetsAtPath(xrSettingsPath);

        if (allSubAssets == null || allSubAssets.Length == 0)
        {
            Debug.LogWarning("[FaceAR] ⚠️  XRGeneralSettingsPerBuildTarget.asset not found. " +
                             "Open Project Settings → XR Plug-in Management and enable ARCore.");
            return;
        }

        bool anyFixed = false;
        string[] boolFields = { "m_AutomaticLoading", "m_AutomaticRunning", "m_InitManagerOnStart" };

        foreach (var asset in allSubAssets)
        {
            if (asset == null) continue;
            var so = new SerializedObject(asset);

            foreach (var fieldName in boolFields)
            {
                var prop = so.FindProperty(fieldName);
                if (prop != null && !prop.boolValue)
                {
                    Debug.LogWarning($"[FaceAR] ⚠️  XR field '{fieldName}' was OFF on '{asset.name}' — forcing ON.");
                    prop.boolValue = true;
                    anyFixed = true;
                }
            }

            if (so.hasModifiedProperties)
            {
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
        }

        if (anyFixed)
            AssetDatabase.SaveAssets();

        Debug.Log("[FaceAR] ✅ XR startup verified — AutomaticLoading, AutomaticRunning, InitManagerOnStart all ON.");
    }

    private static void AddARBackgroundRendererFeatureToAllRenderers()
    {
        // 1. Check all UniversalRendererData assets in the project
        string[] guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            if (rendererData != null)
            {
                EnsureARFeatureInRenderer(rendererData, path);
            }
        }

        // 2. Double check the active render pipeline's renderer data specifically
        var urpAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        if (urpAsset == null)
        {
            // Try to load URP asset from QualitySettings
            urpAsset = QualitySettings.renderPipeline as UniversalRenderPipelineAsset;
        }

        if (urpAsset != null)
        {
            var so = new SerializedObject(urpAsset);
            var rendererListProp = so.FindProperty("m_RendererDataList");
            if (rendererListProp != null && rendererListProp.arraySize > 0)
            {
                for (int i = 0; i < rendererListProp.arraySize; i++)
                {
                    var rendererDataProp = rendererListProp.GetArrayElementAtIndex(i);
                    var rendererData = rendererDataProp.objectReferenceValue as UniversalRendererData;
                    if (rendererData != null)
                    {
                        EnsureARFeatureInRenderer(rendererData, AssetDatabase.GetAssetPath(rendererData));
                    }
                }
            }
        }

        AssetDatabase.SaveAssets();
    }

    private static void EnsureARFeatureInRenderer(UniversalRendererData rendererData, string path)
    {
        bool hasFeature = false;
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature is ARBackgroundRendererFeature)
            {
                hasFeature = true;
                break;
            }
        }

        if (!hasFeature)
        {
            var arFeature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
            arFeature.name = "ARBackgroundRendererFeature";
            AssetDatabase.AddObjectToAsset(arFeature, rendererData);
            rendererData.rendererFeatures.Add(arFeature);
            EditorUtility.SetDirty(rendererData);
            Debug.Log($"[FaceAR] Added ARBackgroundRendererFeature to renderer at {path}");
        }
    }

    // ─── Materials ─────────────────────────────────────────────
    private struct MaterialSet
    {
        public Material turquoise;
        public Material darkVisor;
        public Material strap;
        public Material waterSurface;
        public Material underwaterTint;
        public Material bubble;
    }

    private static MaterialSet CreateMaterials()
    {
        var set = new MaterialSet();

        // Turquoise mask frame material
        set.turquoise = CreateURPMaterial("FaceAR_Turquoise", TurquoiseColor, false);

        // Dark transparent visor
        set.darkVisor = CreateURPMaterial("FaceAR_DarkVisor", DarkVisorColor, true);

        // Dark strap
        set.strap = CreateURPMaterial("FaceAR_Strap", StrapColor, false);

        // Water surface shader material
        var waterShader = Shader.Find("FaceAR/WaterSurface");
        if (waterShader != null)
        {
            set.waterSurface = new Material(waterShader) { name = "FaceAR_WaterSurface" };
            SaveMaterial(set.waterSurface, "Assets/Materials/FaceAR_WaterSurface.mat");
        }

        // Underwater tint shader material
        var underwaterShader = Shader.Find("FaceAR/UnderwaterTint");
        if (underwaterShader != null)
        {
            set.underwaterTint = new Material(underwaterShader) { name = "FaceAR_UnderwaterTint" };
            SaveMaterial(set.underwaterTint, "Assets/Materials/FaceAR_UnderwaterTint.mat");
        }

        // Procedural Bubble Material
        set.bubble = CreateBubbleMaterialAsset();

        return set;
    }

    private static Material CreateBubbleMaterialAsset()
    {
        string texPath = "Assets/Textures/FaceAR_BubbleTexture.png";
        string matPath = "Assets/Materials/FaceAR_Bubble.mat";

        // 1. Generate PNG texture if it doesn't exist
        int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        float radius = size / 2f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - radius + 0.5f) / radius;
                float dy = (y - radius + 0.5f) / radius;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                if (d > 1f)
                {
                    tex.SetPixel(x, y, Color.clear);
                }
                else
                {
                    float edge = Mathf.Exp(-Mathf.Pow((d - 0.9f) / 0.08f, 2f)) * 0.75f;
                    float hdx = dx + 0.35f;
                    float hdy = dy + 0.35f;
                    float hd = Mathf.Sqrt(hdx * hdx + hdy * hdy);
                    float spec = Mathf.Exp(-Mathf.Pow(hd / 0.22f, 2f)) * 0.8f;
                    float inner = (1f - d) * 0.15f;
                    float alpha = Mathf.Clamp01(edge + spec + inner);
                    tex.SetPixel(x, y, new Color(0.82f, 0.93f, 1f, alpha));
                }
            }
        }
        tex.Apply();
        var bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(texPath, bytes);
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(texPath);
        var importedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);

        // Configure texture import settings
        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        // 2. Create or load material
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                  ?? Shader.Find("Particles/Standard Unlit")
                  ?? Shader.Find("Unlit/Transparent");

        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(shader) { name = "FaceAR_Bubble" };
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else
        {
            mat.shader = shader;
        }

        if (mat.HasProperty("_MainTex"))
            mat.SetTexture("_MainTex", importedTex);
        else if (mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", importedTex);

        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f); // Transparent mode

        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static Material CreateURPMaterial(string name, Color color, bool transparent)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit")
                  ?? Shader.Find("Standard");

        var mat = new Material(shader) { name = name };

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

        if (transparent)
        {
            if (mat.HasProperty("_Surface"))
                mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)RenderQueue.Transparent;
        }

        SaveMaterial(mat, $"Assets/Materials/{name}.mat");
        return mat;
    }

    private static void SaveMaterial(Material mat, string path)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            EditorUtility.CopySerialized(mat, existing);
        }
        else
        {
            AssetDatabase.CreateAsset(mat, path);
        }
    }

    // ─── Mask Prefabs ──────────────────────────────────────────

    private static GameObject[] CreateMaskPrefabs(MaterialSet mats)
    {
        var prefabs = new GameObject[3];
        prefabs[0] = CreateScubaMaskPrefab("scubamask1", "ScubaMask");
        prefabs[1] = CreateScubaMaskPrefab("scubamask2", "ScubaGoggles");
        prefabs[2] = CreateScubaMaskPrefab("scubamask3", "ScubaMaskAlt");
        return prefabs;
    }

    private static GameObject CreateScubaMaskPrefab(string textureName, string prefabName)
    {
        string texPath = $"Assets/Textures/{textureName}.png";
        ConfigureAsSprite(texPath);

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(texPath);
        if (sprite == null)
        {
            Debug.LogError($"[FaceAR] Sprite not found at {texPath}. Please ensure the PNG is in Assets/Textures/ folder.");
        }

        var root = new GameObject(prefabName);
        var ctrl = root.AddComponent<ScubaMaskController>();

        // Create a child visual object for the SpriteRenderer
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        
        // Slightly move forward (Z+) so it sits in front of the face mesh nicely
        visual.transform.localPosition = new Vector3(0f, 0.01f, 0.015f);
        // Scale = 0.12 means the mask is 12cm wide in world space (ARFace is ~15-18cm wide).
        // ConfigureAsSprite sets PPU = textureWidth so 1 Unity unit = exactly 1 image width.
        // Therefore this scale directly maps to 12cm regardless of image resolution.
        visual.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);

        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 50;
        if (sprite != null)
        {
            sr.sprite = sprite;
        }

        string path = $"Assets/Prefabs/{prefabName}.prefab";
        var prefab = SavePrefab(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void ConfigureAsSprite(string texPath)
    {
        var importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (importer == null) return;

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(texPath);
        }

        // Set PPU = texture pixel width so that 1 Unity unit = exactly 1 image width.
        // This decouples the mask's visual size from the image resolution:
        // setting scale=0.18 on the visual will always produce an 18cm mask in world space.
        importer.GetSourceTextureWidthAndHeight(out int texWidth, out int texHeight);
        if (texWidth > 0 && (int)importer.spritePixelsPerUnit != texWidth)
        {
            importer.spritePixelsPerUnit = texWidth;
            importer.SaveAndReimport();
            AssetDatabase.WriteImportSettingsIfDirty(texPath);
            AssetDatabase.ImportAsset(texPath);
            Debug.Log($"[FaceAR] Set {texPath} PPU={texWidth} (1 unit = 1 image width)");
        }
    }

    private static GameObject SavePrefab(GameObject obj, string path)
    {
        var prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
        Debug.Log($"[FaceAR] Prefab saved: {path}");
        return prefab;
    }

    // ─── Scene Creation ────────────────────────────────────────

    private static Scene CreateScene(GameObject[] maskPrefabs, MaterialSet mats)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- AR Session ---
        var arSessionGO = new GameObject("AR Session");
        arSessionGO.AddComponent<ARSession>();
        var inputProvider = arSessionGO.AddComponent<ARInputManager>();

        // --- Event System (Required for UI clicks to register) ---
        var eventSystemGO = new GameObject("EventSystem");
        eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystemGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        // --- XR Origin ---
        var xrOriginGO = new GameObject("XR Origin");
        var xrOrigin = xrOriginGO.AddComponent<Unity.XR.CoreUtils.XROrigin>();
        var arFaceManager = xrOriginGO.AddComponent<ARFaceManager>();
        arFaceManager.requestedMaximumFaceCount = 1;

        // Camera Offset
        var cameraOffsetGO = new GameObject("Camera Offset");
        cameraOffsetGO.transform.SetParent(xrOriginGO.transform, false);
        xrOrigin.CameraFloorOffsetObject = cameraOffsetGO;

        // AR Camera
        var cameraGO = new GameObject("Main Camera");
        cameraGO.tag = "MainCamera";
        cameraGO.transform.SetParent(cameraOffsetGO.transform, false);

        var cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 20f;

        var cameraManager = cameraGO.AddComponent<ARCameraManager>();
        cameraManager.requestedFacingDirection = CameraFacingDirection.User;
        cameraManager.requestedBackgroundRenderingMode = CameraBackgroundRenderingMode.BeforeOpaques;
        cameraGO.AddComponent<ARCameraBackground>();

        // TrackedPoseDriver - use new Input System version
        var tpd = cameraGO.AddComponent<UnityEngine.InputSystem.XR.TrackedPoseDriver>();

        xrOrigin.Camera = cam;

        // --- Managers ---
        var managersGO = new GameObject("Managers");

        // FaceARManager
        var faceManagerGO = new GameObject("FaceARManager");
        faceManagerGO.transform.SetParent(managersGO.transform, false);
        var faceManager = faceManagerGO.AddComponent<FaceARManager>();

        // Wire AR references via SerializedObject
        var fmSO = new SerializedObject(faceManager);
        fmSO.FindProperty("arFaceManager").objectReferenceValue = arFaceManager;
        fmSO.FindProperty("arSession").objectReferenceValue = arSessionGO.GetComponent<ARSession>();

        // Set mask prefabs
        var maskPrefabsProp = fmSO.FindProperty("maskPrefabs");
        maskPrefabsProp.arraySize = maskPrefabs.Length;
        for (int i = 0; i < maskPrefabs.Length; i++)
            maskPrefabsProp.GetArrayElementAtIndex(i).objectReferenceValue = maskPrefabs[i];
        fmSO.ApplyModifiedPropertiesWithoutUndo();

        // CameraPermissionHandler
        var permGO = new GameObject("PermissionHandler");
        permGO.transform.SetParent(managersGO.transform, false);
        permGO.AddComponent<CameraPermissionHandler>();

        // ScreenshotCapture
        var snapGO = new GameObject("ScreenshotCapture");
        snapGO.transform.SetParent(managersGO.transform, false);
        var screenshotCapture = snapGO.AddComponent<ScreenshotCapture>();

        // --- Bubble Particles ---
        var bubblesGO = new GameObject("BubbleSpawner");
        bubblesGO.transform.SetParent(cameraGO.transform, false);
        bubblesGO.transform.localPosition = new Vector3(0f, -0.15f, 0.5f);
        bubblesGO.AddComponent<ParticleSystem>();
        var bubbleSpawner = bubblesGO.AddComponent<BubbleSpawner>();
        var bsSO = new SerializedObject(bubbleSpawner);
        bsSO.FindProperty("bubbleMaterial").objectReferenceValue = mats.bubble;
        bsSO.ApplyModifiedPropertiesWithoutUndo();

        // --- Scene Light ---
        var lightGO = new GameObject("Scene Light");
        lightGO.transform.SetParent(managersGO.transform, false);
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var sceneLight = lightGO.AddComponent<Light>();
        sceneLight.type = LightType.Directional;
        sceneLight.intensity = 1f;
        sceneLight.color = new Color(1f, 0.97f, 0.92f);

        // --- Water Effects Canvas ---
        // Enabled by default for the underwater sea environment feel.
        // RawImages use custom shaders; if shader is missing, color is forced transparent.
        var waterCanvas = CreateCanvas("WaterCanvas", 5);

        // Water Surface (fullscreen RawImage)
        // Only add material if the shader compiled successfully; otherwise the RawImage
        // defaults to rendering a solid white rectangle which blocks the camera feed.
        var waterSurfaceGO = CreateFullscreenRawImage(waterCanvas, "WaterSurface", mats.waterSurface);
        if (mats.waterSurface == null)
        {
            // Fallback: Semi-transparent sea-blue color so it still feels like water
            var ri = waterSurfaceGO.GetComponent<RawImage>();
            if (ri != null) ri.color = new Color(0f, 0.45f, 0.75f, 0.2f);
        }
        var waterController = waterSurfaceGO.AddComponent<WaterSurfaceController>();
        var wcSO = new SerializedObject(waterController);
        wcSO.FindProperty("waterImage").objectReferenceValue = waterSurfaceGO.GetComponent<RawImage>();
        wcSO.ApplyModifiedPropertiesWithoutUndo();

        // Underwater Overlay (lower half of screen)
        var underwaterGO = CreateFullscreenRawImage(waterCanvas, "UnderwaterOverlay", mats.underwaterTint);
        if (mats.underwaterTint == null)
        {
            // Fallback: Deeper blue tint for the underwater overlay
            var ri = underwaterGO.GetComponent<RawImage>();
            if (ri != null) ri.color = new Color(0f, 0.22f, 0.5f, 0.15f);
        }
        var underwaterOverlay = underwaterGO.AddComponent<UnderwaterOverlay>();
        var uoSO = new SerializedObject(underwaterOverlay);
        uoSO.FindProperty("overlayImage").objectReferenceValue = underwaterGO.GetComponent<RawImage>();
        uoSO.ApplyModifiedPropertiesWithoutUndo();

        // Underwater overlay covers the full screen for immersive undersea look.
        // The shader is already semi-transparent (alpha ~0.3) so the camera feed shows through.
        var uwRect = underwaterGO.GetComponent<RectTransform>();
        uwRect.anchorMin = new Vector2(0f, 0f);
        uwRect.anchorMax = new Vector2(1f, 1f);
        uwRect.offsetMin = Vector2.zero;
        uwRect.offsetMax = Vector2.zero;

        // --- UI Canvas ---
        var uiCanvas = CreateCanvas("UICanvas", 10);
        BuildUI(uiCanvas, faceManager, waterController, underwaterOverlay,
                bubbleSpawner, screenshotCapture, cam, sceneLight, maskPrefabs);

        return scene;
    }

    // ─── UI Construction ───────────────────────────────────────

    private static void BuildUI(GameObject canvas, FaceARManager faceManager,
        WaterSurfaceController waterCtrl, UnderwaterOverlay underwaterOverlay,
        BubbleSpawner bubbleSpawner, ScreenshotCapture screenshotCapture,
        Camera arCamera, Light sceneLight, GameObject[] maskPrefabs)
    {
        // --- Bottom Panel: Mask Selector ---
        // Holds the 3 mask thumbnails horizontally at the bottom.
        var bottomPanel = CreateUIPanel(canvas, "BottomControls",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 40f), new Vector2(440f, 140f), new Color(0.12f, 0.12f, 0.14f, 0.75f));

        string[] maskNames = { "Scuba\nMask", "Scuba\nGoggles", "Scuba\nMask Alt" };
        string[] textureNames = { "scubamask1", "scubamask2", "scubamask3" };
        var maskButtons = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            float xPos = -140f + i * 140f;
            string maskTexPath = $"Assets/Textures/{textureNames[i]}.png";
            ConfigureAsSprite(maskTexPath); // ensure it's imported as sprite
            var maskSprite = AssetDatabase.LoadAssetAtPath<Sprite>(maskTexPath);

            var maskBtnGO = CreateMaskSelectorButton(bottomPanel, $"MaskBtn_{i}",
                maskNames[i], new Vector2(xPos, 0f), maskSprite);
            maskButtons[i] = maskBtnGO.GetComponent<Button>();
        }

        // MaskSwitcher
        var maskSwitcherGO = new GameObject("MaskSwitcher");
        maskSwitcherGO.transform.SetParent(canvas.transform, false);
        var maskSwitcher = maskSwitcherGO.AddComponent<MaskSwitcher>();
        var msSO = new SerializedObject(maskSwitcher);
        msSO.FindProperty("faceManager").objectReferenceValue = faceManager;
        var mbProp = msSO.FindProperty("maskButtons");
        mbProp.arraySize = maskButtons.Length;
        for (int i = 0; i < maskButtons.Length; i++)
            mbProp.GetArrayElementAtIndex(i).objectReferenceValue = maskButtons[i];
        msSO.ApplyModifiedPropertiesWithoutUndo();

        // --- FaceARUIController ---
        var uiCtrlGO = new GameObject("UIController");
        uiCtrlGO.transform.SetParent(canvas.transform, false);
        var uiCtrl = uiCtrlGO.AddComponent<FaceARUIController>();

        var uiSO = new SerializedObject(uiCtrl);
        uiSO.FindProperty("faceManager").objectReferenceValue = faceManager;
        uiSO.FindProperty("waterController").objectReferenceValue = waterCtrl;
        uiSO.FindProperty("underwaterOverlay").objectReferenceValue = underwaterOverlay;
        uiSO.FindProperty("bubbleSpawner").objectReferenceValue = bubbleSpawner;
        uiSO.FindProperty("screenshotCapture").objectReferenceValue = screenshotCapture;
        uiSO.FindProperty("maskSwitcher").objectReferenceValue = maskSwitcher;

        uiSO.FindProperty("backButton").objectReferenceValue = null;
        uiSO.FindProperty("stopARButton").objectReferenceValue = null;
        uiSO.FindProperty("quizButton").objectReferenceValue = null;
        uiSO.FindProperty("snapButton").objectReferenceValue = null;
        uiSO.FindProperty("audioButton").objectReferenceValue = null;
        uiSO.FindProperty("lightButton").objectReferenceValue = null;
        uiSO.FindProperty("resetButton").objectReferenceValue = null;
        uiSO.FindProperty("zoomOutButton").objectReferenceValue = null;
        uiSO.FindProperty("zoomInButton").objectReferenceValue = null;
        uiSO.FindProperty("waterButton").objectReferenceValue = null;
        uiSO.FindProperty("stopARLabel").objectReferenceValue = null;

        uiSO.FindProperty("arCamera").objectReferenceValue = arCamera;
        uiSO.FindProperty("sceneLight").objectReferenceValue = sceneLight;
        uiSO.ApplyModifiedPropertiesWithoutUndo();

        // --- Permission Denied Panel ---
        var permDeniedPanel = CreateUIPanel(canvas, "PermissionDeniedPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(350f, 200f), new Color(0.1f, 0.1f, 0.12f, 0.9f));

        var deniedText = CreateText(permDeniedPanel, "DeniedText",
            "Camera permission is required.\nPlease grant camera access in Settings.",
            new Vector2(0.5f, 0.6f), new Vector2(0.5f, 0.6f),
            Vector2.zero, new Vector2(320f, 80f), 16, TextAnchor.MiddleCenter);

        var retryBtn = CreateButton(permDeniedPanel, "RetryButton", "Retry Permission",
            new Vector2(0.5f, 0.25f), new Vector2(0.5f, 0.25f),
            Vector2.zero, new Vector2(200f, 40f), TurquoiseColor, 14);

        permDeniedPanel.SetActive(false);

        // Wire the permission handler
        var permHandler = Object.FindFirstObjectByType<CameraPermissionHandler>();
        if (permHandler != null)
        {
            var phSO = new SerializedObject(permHandler);
            phSO.FindProperty("permissionDeniedPanel").objectReferenceValue = permDeniedPanel;
            phSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire retry button
            var retryButton = retryBtn.GetComponent<Button>();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(
                retryButton.onClick,
                new UnityEngine.Events.UnityAction(permHandler.RequestCameraPermission));
        }
    }

    // ─── UI Helper Methods ─────────────────────────────────────

    private static GameObject CreateCanvas(string name, int sortOrder)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 2340);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static GameObject CreateUIPanel(GameObject parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.pivot = anchorMin;

        if (color.a > 0)
        {
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        return go;
    }

    private static GameObject CreateFullscreenRawImage(GameObject canvas, string name, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(canvas.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var rawImg = go.AddComponent<RawImage>();
        rawImg.raycastTarget = false;
        if (mat != null) rawImg.material = mat;

        return go;
    }

    private static GameObject CreateButton(GameObject parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
        Color bgColor, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.pivot = new Vector2(0.5f, 0.5f);

        var img = go.AddComponent<Image>();
        img.color = bgColor;

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;

        // Label
        CreateText(go, "Label", label,
            new Vector2(0f, 0f), new Vector2(1f, 1f),
            Vector2.zero, Vector2.zero, fontSize, TextAnchor.MiddleCenter);

        return go;
    }

    private static GameObject CreateMaskSelectorButton(GameObject parent, string name,
        string label, Vector2 position, Sprite maskSprite)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(120f, 120f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.22f, 0.85f);

        var btn = go.AddComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.22f, 0.85f);
        colors.highlightedColor = new Color(0.3f, 0.9f, 0.8f, 0.9f);
        colors.selectedColor = new Color(0.25f, 0.88f, 0.82f, 1f);
        colors.pressedColor = new Color(0.15f, 0.7f, 0.65f, 1f);
        btn.colors = colors;

        // Mask icon
        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(go.transform, false);
        var iconRect = iconGO.AddComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.15f, 0.35f);
        iconRect.anchorMax = new Vector2(0.85f, 0.95f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        var iconImg = iconGO.AddComponent<Image>();
        if (maskSprite != null)
        {
            iconImg.sprite = maskSprite;
            iconImg.color = Color.white;
        }
        else
        {
            iconImg.color = TurquoiseColor;
        }
        iconImg.raycastTarget = false;

        // Label text
        CreateText(go, "Label", label,
            new Vector2(0f, 0f), new Vector2(1f, 0.35f),
            Vector2.zero, Vector2.zero, 14, TextAnchor.MiddleCenter);

        return go;
    }

    private static GameObject CreateText(GameObject parent, string name, string content,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size,
        int fontSize, TextAnchor alignment)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);

        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.pivot = new Vector2(0.5f, 0.5f);

        // Use zero offsets if size is zero (stretch mode)
        if (size == Vector2.zero)
        {
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        var text = go.AddComponent<Text>();
        text.text = content;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        // Add shadow for readability
        var shadow = go.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.5f);
        shadow.effectDistance = new Vector2(1, -1);

        return go;
    }

    // CreateCircleSprite removed — circle buttons are no longer used in the UI
}
#endif
