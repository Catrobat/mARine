using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class MarineDemoSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/MarineDemo.unity";
    private static readonly Color BackgroundColor = new Color(0.93f, 0.94f, 0.96f);
    private static readonly Color HeaderColor = new Color(0.12f, 0.16f, 0.22f);
    private static readonly Color SurfaceColor = Color.white;
    private static readonly Color SurfaceOutline = new Color(0.82f, 0.84f, 0.88f);
    private static readonly Color BodyTextColor = new Color(0.11f, 0.14f, 0.18f);
    private static readonly Color MutedTextColor = new Color(0.35f, 0.38f, 0.43f);
    private static readonly Color AccentBlue = new Color(0.17f, 0.34f, 0.67f);
    private static readonly Color AccentGreen = new Color(0.18f, 0.48f, 0.30f);

    [MenuItem("Tools/Marine Demo/Create Demo Scene")]
    [MenuItem("Assets/Create/Marine Demo Scene")]
    [MenuItem("GameObject/Marine Demo/Create Demo Scene", false, 10)]
    [MenuItem("Window/Marine Demo/Build Demo Scene")]
    public static void CreateDemoScene()
    {
        Directory.CreateDirectory("Assets/Scenes");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "MarineDemo";

        var camera = new GameObject("Main Camera");
        camera.AddComponent<Camera>();

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        var canvas = CreateCanvas();
        CreateBackground(canvas.transform);
        var content = CreateContentRoot(canvas.transform);
        var header = CreateHeaderBar(content.transform);
        var headerTitle = CreateText(header.transform, "titleText", "Marine Demo", 34, TextAlignmentOptions.Left, Color.white);
        SetRect(headerTitle.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(36f, 0f), new Vector2(-300f, 0f));

        var languageDropdown = CreateDropdown(header.transform);
        SetRect(languageDropdown.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-34f, 0f), new Vector2(220f, 44f));

        var statusText = CreateText(content.transform, "statusText", "Ready", 20, TextAlignmentOptions.Center, MutedTextColor);
        SetRect(statusText.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -126f), new Vector2(0f, 30f));

        var managerObject = new GameObject("MarineManager");
        var manager = managerObject.AddComponent<MarineDemoManager>();

        var recordButton = CreateButton(content.transform, "recordButton", "Hold to Speak", AccentBlue);
        SetRect(recordButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 78f), new Vector2(320f, 88f));

        var transcriptionLabel = CreateText(content.transform, "transcriptionLabel", "Transcription", 18, TextAlignmentOptions.Left, MutedTextColor);
        SetRect(transcriptionLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 6f), new Vector2(0f, 24f));

        var transcriptionBox = CreateSurface(content.transform, "transcriptionSurface");
        SetRect(transcriptionBox.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -58f), new Vector2(0f, 96f));

        var transcriptionText = CreateText(transcriptionBox.transform, "transcriptionText", "", 22, TextAlignmentOptions.Left, BodyTextColor);
        SetRect(transcriptionText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 0f), new Vector2(-44f, -20f));

        var responseLabel = CreateText(content.transform, "responseLabel", "Response", 18, TextAlignmentOptions.Left, MutedTextColor);
        SetRect(responseLabel.rectTransform, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -124f), new Vector2(0f, 24f));

        var responseBox = CreateSurface(content.transform, "responseSurface");
        SetRect(responseBox.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -188f), new Vector2(0f, 96f));

        var responseText = CreateText(responseBox.transform, "responseText", "I heard you clearly.", 22, TextAlignmentOptions.Left, BodyTextColor);
        SetRect(responseText.rectTransform, Vector2.zero, Vector2.one, new Vector2(22f, 0f), new Vector2(-44f, -20f));

        var bottomRow = new GameObject("BottomRow");
        bottomRow.transform.SetParent(content.transform, false);
        var bottomRowRect = bottomRow.AddComponent<RectTransform>();
        SetRect(bottomRowRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 48f), new Vector2(0f, 54f));

        var ttsButton = CreateButton(bottomRow.transform, "ttsButton", "Speak Response", AccentGreen);
        SetRect(ttsButton.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(132f, 0f), new Vector2(240f, 48f));

        var echoModeToggle = CreateToggle(bottomRow.transform);
        SetRect(echoModeToggle.GetComponent<RectTransform>(), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-164f, 0f), new Vector2(184f, 42f));

        Assign(manager, "languageDropdown", languageDropdown);
        Assign(manager, "statusText", statusText);
        Assign(manager, "recordButton", recordButton);
        Assign(manager, "transcriptionText", transcriptionText);
        Assign(manager, "responseText", responseText);
        Assign(manager, "ttsButton", ttsButton);
        Assign(manager, "echoModeToggle", echoModeToggle);

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Marine Demo", "Created Assets/Scenes/MarineDemo.unity", "OK");
    }

    [MenuItem("Window/Marine Demo/Open Builder")]
    public static void OpenBuilder()
    {
        MarineDemoBuilderWindow.ShowWindow();
    }

    private static Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Canvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateBackground(Transform parent)
    {
        var go = new GameObject("Background");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = BackgroundColor;
        var rect = go.GetComponent<RectTransform>();
        SetRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
    }

    private static GameObject CreateContentRoot(Transform parent)
    {
        var go = new GameObject("Content");
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.offsetMin = new Vector2(48f, 40f);
        rect.offsetMax = new Vector2(-48f, -40f);
        return go;
    }

    private static GameObject CreateHeaderBar(Transform parent)
    {
        var go = new GameObject("HeaderBar");
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = HeaderColor;
        var rect = go.GetComponent<RectTransform>();
        SetRect(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -44f), new Vector2(0f, 84f));
        return go;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
    {
        return CreateText(parent, name, value, size, alignment, BodyTextColor);
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        string value,
        float size,
        TextAlignmentOptions alignment,
        Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.enableWordWrapping = true;
        text.enableAutoSizing = false;
        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = color;
        var button = go.AddComponent<Button>();
        var text = CreateText(go.transform, "Text", label, 22, TextAlignmentOptions.Center, Color.white);
        text.color = Color.white;
        SetRect(text.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        return button;
    }

    private static GameObject CreateSurface(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.color = SurfaceColor;
        go.AddComponent<Outline>().effectColor = SurfaceOutline;
        return go;
    }

    private static TMP_Dropdown CreateDropdown(Transform parent)
    {
        var go = new GameObject("languageDropdown");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.96f, 0.97f, 0.99f);
        var dropdown = go.AddComponent<TMP_Dropdown>();
        dropdown.options.Add(new TMP_Dropdown.OptionData("English"));
        dropdown.options.Add(new TMP_Dropdown.OptionData("German"));

        var label = CreateText(go.transform, "Label", "English", 18, TextAlignmentOptions.Left, BodyTextColor);
        SetRect(label.rectTransform, Vector2.zero, Vector2.one, new Vector2(10f, 0f), new Vector2(-28f, 0f));
        dropdown.captionText = label;

        var arrow = CreateText(go.transform, "Arrow", "v", 18, TextAlignmentOptions.Center, MutedTextColor);
        SetRect(arrow.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-14f, 0f), new Vector2(24f, 0f));

        var template = new GameObject("Template");
        template.transform.SetParent(go.transform, false);
        template.SetActive(false);
        var templateRect = template.AddComponent<RectTransform>();
        SetRect(templateRect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -64f), new Vector2(0f, 120f));
        template.AddComponent<Image>().color = new Color(0.98f, 0.98f, 0.99f);
        var scrollRect = template.AddComponent<ScrollRect>();

        var viewport = new GameObject("Viewport");
        viewport.transform.SetParent(template.transform, false);
        viewport.AddComponent<Image>().color = Color.white;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        SetRect(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 80f);

        var item = new GameObject("Item");
        item.transform.SetParent(content.transform, false);
        var itemToggle = item.AddComponent<Toggle>();
        itemToggle.targetGraphic = item.AddComponent<Image>();
        itemToggle.targetGraphic.color = new Color(0.9f, 0.93f, 0.97f);
        SetRect(item.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -20f), new Vector2(0f, 40f));

        var itemLabel = CreateText(item.transform, "Item Label", "Option", 18, TextAlignmentOptions.Left, BodyTextColor);
        SetRect(itemLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(12f, 0f), new Vector2(-24f, 0f));

        scrollRect.content = contentRect;
        scrollRect.viewport = viewport.GetComponent<RectTransform>();
        scrollRect.horizontal = false;

        dropdown.template = templateRect;
        dropdown.itemText = itemLabel;
        return dropdown;
    }

    private static Toggle CreateToggle(Transform parent)
    {
        var root = new GameObject("echoModeToggle");
        root.transform.SetParent(parent, false);
        var toggle = root.AddComponent<Toggle>();

        var background = new GameObject("Background");
        background.transform.SetParent(root.transform, false);
        background.AddComponent<Image>().color = Color.white;
        SetRect(background.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(26f, 26f));

        var checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(background.transform, false);
        checkmark.AddComponent<Image>().color = new Color(0.18f, 0.45f, 0.28f);
        SetRect(checkmark.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var label = CreateText(root.transform, "Label", "Echo Mode", 18, TextAlignmentOptions.Left, BodyTextColor);
        SetRect(label.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(56f, 0f), new Vector2(-56f, 0f));

        toggle.targetGraphic = background.GetComponent<Image>();
        toggle.graphic = checkmark.GetComponent<Image>();
        return toggle;
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
    }

    private static void Assign(Object target, string propertyName, Object value)
    {
        var serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedProperties();
    }
}

public sealed class MarineDemoBuilderWindow : EditorWindow
{
    public static void ShowWindow()
    {
        var window = GetWindow<MarineDemoBuilderWindow>("Marine Demo");
        window.minSize = new Vector2(320f, 160f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Marine Demo", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Builds the demo scene and places it at Assets/Scenes/MarineDemo.unity.",
            MessageType.Info);

        if (GUILayout.Button("Build Demo Scene", GUILayout.Height(34f)))
            MarineDemoSceneBuilder.CreateDemoScene();

        if (GUILayout.Button("Open Created Scene", GUILayout.Height(28f)))
            EditorSceneManager.OpenScene("Assets/Scenes/MarineDemo.unity");
    }
}
