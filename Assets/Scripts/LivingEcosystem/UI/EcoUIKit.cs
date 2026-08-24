using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // Small helpers for building the ecosystem interface entirely from code.
    //
    // Building it in code rather than in the scene is deliberate: FreeExploreEndless
    // is an Addressable scene, and every widget added here would otherwise be a
    // change to that scene's serialized data. This way the whole feature is additive
    // and the scene is untouched.
    public static class EcoUIKit
    {
        // Palette. Matches the green the environment builder already uses so the
        // panel does not look bolted on.
        public static readonly Color Accent      = new Color32(0x2B, 0xA8, 0x4A, 0xFF);
        public static readonly Color PanelBg     = new Color32(0x10, 0x1A, 0x24, 0xF2);
        public static readonly Color PanelBgSoft = new Color32(0x18, 0x26, 0x33, 0xFF);
        public static readonly Color TextMain    = new Color32(0xF2, 0xF6, 0xFA, 0xFF);
        public static readonly Color TextDim     = new Color32(0x9F, 0xB2, 0xC4, 0xFF);
        public static readonly Color Track       = new Color32(0x2C, 0x3C, 0x4C, 0xFF);

        public static Canvas CreateCanvas(string name, int sortOrder)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above the scene's own interface, so the panel is never hidden behind it.
            canvas.sortingOrder = sortOrder;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        public static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

        static Sprite _triangle;

        // An upward triangle, drawn in code. The obvious thing is to print "▲", but
        // the project's font is LiberationSans, which has no U+25B2 (nor the U+2713
        // tick) — those come out as missing-glyph boxes. A generated sprite always
        // draws, whatever font the interface is using.
        public static Sprite TriangleSprite
        {
            get
            {
                if (_triangle != null) return _triangle;

                const int size = 32;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp,
                    name = "LivingEcosystem/Triangle",
                };

                var clear = new Color(1f, 1f, 1f, 0f);
                for (int y = 0; y < size; y++)
                {
                    float ny = y / (float)(size - 1);          // 0 at the base
                    float halfWidth = (1f - ny) * 0.5f;        // widest at the base
                    for (int x = 0; x < size; x++)
                    {
                        float nx = x / (float)(size - 1) - 0.5f;
                        tex.SetPixel(x, y, Mathf.Abs(nx) <= halfWidth ? Color.white : clear);
                    }
                }
                tex.Apply();

                _triangle = Sprite.Create(tex, new UnityEngine.Rect(0f, 0f, size, size),
                                          new Vector2(0.5f, 0.5f));
                return _triangle;
            }
        }

        public static GameObject Panel(Transform parent, string name, Color colour)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = colour;
            return go;
        }

        // A section heading with room above it, so a long panel reads as a few
        // grouped things rather than one continuous list of rows.
        public static TMP_Text SectionHeader(Transform parent, string title)
        {
            var row = Empty(parent, "Section_" + title);
            var element = row.AddComponent<LayoutElement>();
            element.preferredHeight = 52f;
            element.minHeight = 52f;

            var label = Text(row.transform, title.ToUpperInvariant(), 20f, TextDim);
            label.characterSpacing = 8f;
            var labelRect = Rect(label.gameObject);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0f, 0f);
            labelRect.sizeDelta = new Vector2(0f, 26f);
            labelRect.anchoredPosition = new Vector2(2f, 2f);

            // A hairline under the heading, which is what actually separates the
            // groups; the words alone were getting lost among the rows.
            var rule = Panel(row.transform, "Rule", new Color(1f, 1f, 1f, 0.10f));
            var ruleRect = Rect(rule);
            ruleRect.anchorMin = new Vector2(0f, 0f);
            ruleRect.anchorMax = new Vector2(1f, 0f);
            ruleRect.pivot = new Vector2(0.5f, 0f);
            ruleRect.sizeDelta = new Vector2(0f, 1f);
            ruleRect.anchoredPosition = new Vector2(0f, 0f);

            return label;
        }

        public static GameObject Empty(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static TMP_Text Text(Transform parent, string content, float size,
                                    Color colour, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<TextMeshProUGUI>();
            t.text = content;
            t.fontSize = size;
            t.color = colour;
            t.alignment = align;
            t.textWrappingMode = TextWrappingModes.Normal;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, float fontSize,
                                    Color background, Color textColour, Action onClick)
        {
            var go = Panel(parent, "Button", background);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();

            var text = Text(go.transform, label, fontSize, textColour, TextAlignmentOptions.Center);
            Stretch(Rect(text.gameObject), 8f, 4f);

            if (onClick != null) button.onClick.AddListener(() => onClick());
            return button;
        }

        // A labelled 0..1 slider with a live value readout, matching the row shape the
        // environment builder already uses.
        public static Slider LabelledSlider(Transform parent, string label, string initialValue,
                                            float normalized, Action<float> onChange,
                                            out TMP_Text valueText)
        {
            var row = Empty(parent, "SliderRow");
            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 74f;
            layout.minHeight = 74f;

            var caption = Text(row.transform, label, 26f, TextDim);
            var capRect = Rect(caption.gameObject);
            capRect.anchorMin = new Vector2(0f, 1f);
            capRect.anchorMax = new Vector2(0.62f, 1f);
            capRect.pivot = new Vector2(0f, 1f);
            capRect.anchoredPosition = new Vector2(0f, 0f);
            capRect.sizeDelta = new Vector2(0f, 30f);

            valueText = Text(row.transform, initialValue, 26f, TextMain, TextAlignmentOptions.Right);
            var valRect = Rect(valueText.gameObject);
            valRect.anchorMin = new Vector2(0.62f, 1f);
            valRect.anchorMax = new Vector2(1f, 1f);
            valRect.pivot = new Vector2(1f, 1f);
            valRect.anchoredPosition = Vector2.zero;
            valRect.sizeDelta = new Vector2(0f, 30f);

            var sliderGo = new GameObject("Slider", typeof(RectTransform));
            sliderGo.transform.SetParent(row.transform, false);
            var sRect = Rect(sliderGo);
            sRect.anchorMin = new Vector2(0f, 0f);
            sRect.anchorMax = new Vector2(1f, 0f);
            sRect.pivot = new Vector2(0.5f, 0f);
            sRect.anchoredPosition = new Vector2(0f, 6f);
            sRect.sizeDelta = new Vector2(0f, 30f);

            var slider = sliderGo.AddComponent<Slider>();

            var background = Panel(sliderGo.transform, "Track", Track);
            var bgRect = Rect(background);
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.pivot = new Vector2(0.5f, 0.5f);
            bgRect.sizeDelta = new Vector2(0f, 8f);
            bgRect.anchoredPosition = Vector2.zero;

            var fillArea = Empty(sliderGo.transform, "Fill Area");
            var faRect = Rect(fillArea);
            faRect.anchorMin = new Vector2(0f, 0.5f);
            faRect.anchorMax = new Vector2(1f, 0.5f);
            faRect.pivot = new Vector2(0.5f, 0.5f);
            faRect.sizeDelta = new Vector2(0f, 8f);
            faRect.anchoredPosition = Vector2.zero;

            var fill = Panel(fillArea.transform, "Fill", Accent);
            var fillRect = Rect(fill);
            fillRect.sizeDelta = new Vector2(0f, 8f);

            var handleArea = Empty(sliderGo.transform, "Handle Slide Area");
            var haRect = Rect(handleArea);
            Stretch(haRect, 0f, 0f);

            var handle = Panel(handleArea.transform, "Handle", Color.white);
            var handleRect = Rect(handle);
            handleRect.sizeDelta = new Vector2(30f, 30f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(Mathf.Clamp01(normalized));

            if (onChange != null) slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }

        // A row of mutually exclusive options — used for Speed and Starting life.
        public static Button[] SegmentedControl(Transform parent, string label, string[] options,
                                                int selected, Action<int> onChange)
        {
            var row = Empty(parent, "Segmented");
            var layout = row.AddComponent<LayoutElement>();
            layout.preferredHeight = 78f;
            layout.minHeight = 78f;

            var caption = Text(row.transform, label, 26f, TextDim);
            var capRect = Rect(caption.gameObject);
            capRect.anchorMin = new Vector2(0f, 1f);
            capRect.anchorMax = new Vector2(1f, 1f);
            capRect.pivot = new Vector2(0f, 1f);
            capRect.sizeDelta = new Vector2(0f, 28f);
            capRect.anchoredPosition = Vector2.zero;

            var strip = Empty(row.transform, "Options");
            var stripRect = Rect(strip);
            stripRect.anchorMin = new Vector2(0f, 0f);
            stripRect.anchorMax = new Vector2(1f, 0f);
            stripRect.pivot = new Vector2(0.5f, 0f);
            stripRect.sizeDelta = new Vector2(0f, 44f);
            stripRect.anchoredPosition = Vector2.zero;

            var group = strip.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 6f;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            group.childControlWidth = true;
            group.childControlHeight = true;

            var buttons = new Button[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                int index = i;
                buttons[i] = Button(strip.transform, options[i], 24f,
                                    index == selected ? Accent : Track,
                                    index == selected ? Color.white : TextDim,
                                    () => onChange?.Invoke(index));
            }
            return buttons;
        }

        public static void PaintSegments(Button[] buttons, int selected)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) continue;
                var img = buttons[i].GetComponent<Image>();
                if (img != null) img.color = i == selected ? Accent : Track;
                var label = buttons[i].GetComponentInChildren<TMP_Text>();
                if (label != null) label.color = i == selected ? Color.white : TextDim;
            }
        }

        public static VerticalLayoutGroup VerticalList(GameObject host, float spacing, RectOffset padding)
        {
            var group = host.AddComponent<VerticalLayoutGroup>();
            group.spacing = spacing;
            group.padding = padding;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = false;
            group.childControlWidth = true;
            group.childControlHeight = true;
            return group;
        }

        public static void Stretch(RectTransform rect, float horizontalPadding, float verticalPadding)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(horizontalPadding, verticalPadding);
            rect.offsetMax = new Vector2(-horizontalPadding, -verticalPadding);
        }

        // A scrollable column, for content that can outgrow the screen.
        public static RectTransform ScrollColumn(Transform parent, out ScrollRect scrollRect)
        {
            var viewport = Panel(parent, "Viewport", new Color(0f, 0f, 0f, 0f));

            // RectMask2D, not Mask. A Mask builds its stencil from the graphic's
            // alpha, so a fully transparent viewport image clips every child away and
            // the panel opens empty. RectMask2D clips to the rectangle regardless of
            // what is drawn, needs no graphic at all, and is cheaper on mobile.
            viewport.AddComponent<RectMask2D>();

            var content = Empty(viewport.transform, "Content");
            var contentRect = Rect(content);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            // A fresh RectTransform defaults to 100x100. With the x anchors stretched,
            // that leaves the content 100 units WIDER than the viewport, overhanging
            // 50 units each side and getting clipped at both edges — which is why
            // "Ecosystem" read as "osystem". Width must come from the anchors alone.
            contentRect.sizeDelta = new Vector2(0f, 0f);

            // Padding, or the first row butts against the clipping edge and its top
            // line is shaved off. The bottom margin keeps the last row clear of the
            // panel edge when the list is scrolled to the end.
            VerticalList(content, 10f, new RectOffset(4, 4, 8, 20));
            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect = viewport.AddComponent<ScrollRect>();
            scrollRect.content = contentRect;
            scrollRect.viewport = Rect(viewport);
            scrollRect.horizontal = false;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 24f;

            return contentRect;
        }

        // Ensures there is an EventSystem, so the panel is tappable even if the host
        // scene somehow lacks one. Never replaces an existing one.
        public static void EnsureEventSystem()
        {
            if (UnityEngine.EventSystems.EventSystem.current != null) return;
            if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;

            var go = new GameObject("EventSystem (Living Ecosystem)",
                typeof(UnityEngine.EventSystems.EventSystem),
                typeof(UnityEngine.EventSystems.StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
    }
}
