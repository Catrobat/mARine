using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MarineAR.AISpawner.EditorTools
{
    /// <summary>
    /// Editor-only helpers used by the scene builder: procedural UI sprites
    /// (rounded rects, outlines, gradients, glow, shadow, spinner ring) and factories
    /// for the common building blocks, so the generated UI reads as one modern,
    /// deep-ocean design system. Sprites are regenerated on every build so visual
    /// changes here always reach the scene.
    /// </summary>
    static class AISpawnerUiFactory
    {
        public const string SpriteFolder = "Assets/AISpawner/UI/Sprites";

        // ---------------- Palette: deep ocean glass + aqua accent ----------------
        public static readonly Color Sheet = new Color32(11, 25, 41, 246);
        public static readonly Color Card = new Color32(22, 40, 62, 255);
        public static readonly Color CardHover = new Color32(30, 52, 80, 255);
        public static readonly Color FieldBg = new Color32(14, 33, 54, 255);
        public static readonly Color Accent = new Color32(34, 211, 238, 255);        // aqua cyan
        public static readonly Color AccentDark = new Color32(14, 116, 144, 255);
        public static readonly Color AccentSoft = new Color32(34, 211, 238, 46);     // chip fill
        public static readonly Color AccentEdge = new Color32(34, 211, 238, 90);     // chip stroke
        public static readonly Color OnAccent = new Color32(4, 34, 42, 255);         // text on accent
        public static readonly Color TextPrimary = new Color32(241, 247, 251, 255);
        public static readonly Color TextSecondary = new Color32(147, 174, 196, 255);
        public static readonly Color TextTertiary = new Color32(92, 122, 147, 255);
        public static readonly Color Danger = new Color32(248, 113, 113, 255);
        public static readonly Color DangerBg = new Color32(58, 26, 34, 235);
        public static readonly Color DangerBgPressed = new Color32(78, 34, 44, 240);
        public static readonly Color DangerEdge = new Color32(248, 113, 113, 70);
        public static readonly Color Scrim = new Color32(4, 12, 20, 196);
        public static readonly Color Stroke = new Color32(255, 255, 255, 22);        // hairline on dark
        public static readonly Color StrokeSoft = new Color32(255, 255, 255, 12);
        public static readonly Color Monogram = new Color32(24, 104, 128, 255);      // tint over white gradient
        public static readonly Color ShadowColor = new Color32(0, 0, 0, 140);

        public static Sprite RoundedRect { get; private set; }        // r28 slice — cards, bars
        public static Sprite RoundedRectSmall { get; private set; }   // r14 slice — chips, fields, pills
        public static Sprite RoundedRectLarge { get; private set; }   // r44 slice — sheets
        public static Sprite Outline { get; private set; }            // r28 stroke slice
        public static Sprite OutlineSmall { get; private set; }       // r14 stroke slice
        public static Sprite OutlineLarge { get; private set; }       // r44 stroke slice
        public static Sprite Circle { get; private set; }
        public static Sprite CircleGradient { get; private set; }     // white vertical gradient — tint per use
        public static Sprite GradientBar { get; private set; }        // white horizontal gradient pill — tint per use
        public static Sprite GlowRing { get; private set; }           // soft halo, transparent center
        public static Sprite Shadow { get; private set; }             // blurred rounded rect
        public static Sprite Ring { get; private set; }               // spinner arc

        /// <summary>Regenerates every sprite asset used by the generated UI.</summary>
        public static void EnsureSprites()
        {
            // Clean slate: drop any sprites from earlier design iterations so the
            // folder only ever contains what the current build actually uses.
            AssetDatabase.DeleteAsset(SpriteFolder);
            Directory.CreateDirectory(SpriteFolder);
            AssetDatabase.Refresh();

            RoundedRect = MakeSprite("aispawner_rounded_28", 96, p => RoundedRectAlpha(p, 96, 28), null, new Vector4(34, 34, 34, 34));
            RoundedRectSmall = MakeSprite("aispawner_rounded_14", 48, p => RoundedRectAlpha(p, 48, 14), null, new Vector4(18, 18, 18, 18));
            RoundedRectLarge = MakeSprite("aispawner_rounded_44", 128, p => RoundedRectAlpha(p, 128, 44), null, new Vector4(52, 52, 52, 52));
            Outline = MakeSprite("aispawner_outline_28", 96, p => OutlineAlpha(p, 96, 28, 3f), null, new Vector4(34, 34, 34, 34));
            OutlineSmall = MakeSprite("aispawner_outline_14", 48, p => OutlineAlpha(p, 48, 14, 2.5f), null, new Vector4(18, 18, 18, 18));
            OutlineLarge = MakeSprite("aispawner_outline_44", 128, p => OutlineAlpha(p, 128, 44, 3f), null, new Vector4(52, 52, 52, 52));
            Circle = MakeSprite("aispawner_circle", 128, p => CircleAlpha(p, 128), null, Vector4.zero);
            CircleGradient = MakeSprite("aispawner_circle_gradient", 256, p => CircleAlpha(p, 256), p => VerticalShade(p, 256), Vector4.zero);
            GradientBar = MakeSprite("aispawner_gradient_bar", 0, null, null, Vector4.zero, custom: MakeGradientBar);
            GlowRing = MakeSprite("aispawner_glow_ring", 160, p => GlowRingAlpha(p, 160), null, Vector4.zero);
            Shadow = MakeSprite("aispawner_shadow", 160, p => ShadowAlpha(p, 160), null, new Vector4(64, 64, 64, 64));
            Ring = MakeSprite("aispawner_ring", 128, p => SpinnerRingAlpha(p, 128), null, Vector4.zero);
        }

        delegate float AlphaAt(Vector2 pixel);
        delegate byte ShadeAt(Vector2 pixel);
        delegate Texture2D CustomTexture();

        static Sprite MakeSprite(string name, int size, AlphaAt alphaAt, ShadeAt shadeAt, Vector4 border, CustomTexture custom = null)
        {
            string path = $"{SpriteFolder}/{name}.png";
            AssetDatabase.DeleteAsset(path);

            Texture2D texture;
            if (custom != null)
            {
                texture = custom();
            }
            else
            {
                texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                var pixels = new Color32[size * size];
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        var p = new Vector2(x + 0.5f, y + 0.5f);
                        byte shade = shadeAt != null ? shadeAt(p) : (byte)255;
                        float a = Mathf.Clamp01(alphaAt(p));
                        pixels[y * size + x] = new Color32(shade, shade, shade, (byte)(a * 255f));
                    }
                }
                texture.SetPixels32(pixels);
                texture.Apply();
            }

            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ---------------- SDF helpers ----------------

        static float RoundedRectDistance(Vector2 p, int size, float radius)
        {
            Vector2 half = Vector2.one * (size * 0.5f);
            Vector2 d = new Vector2(Mathf.Abs(p.x - half.x), Mathf.Abs(p.y - half.y)) - (half - Vector2.one * radius);
            float outside = Vector2.Max(d, Vector2.zero).magnitude;
            float inside = Mathf.Min(Mathf.Max(d.x, d.y), 0f);
            return outside + inside - radius;
        }

        static float RoundedRectAlpha(Vector2 p, int size, float radius)
        {
            // Inset by 1px so anti-aliased edges never clip at the sprite bounds.
            return 0.5f - (RoundedRectDistance(p, size, radius - 1f) + 1f);
        }

        static float OutlineAlpha(Vector2 p, int size, float radius, float stroke)
        {
            float dist = RoundedRectDistance(p, size, radius - 1f) + 1f;
            // Band centered on the edge: crisp hairline stroke.
            return 0.5f - (Mathf.Abs(dist + stroke * 0.5f) - stroke * 0.5f);
        }

        static float CircleAlpha(Vector2 p, int size)
        {
            return 0.5f - (Vector2.Distance(p, Vector2.one * (size * 0.5f)) - (size * 0.5f - 1.5f));
        }

        static byte VerticalShade(Vector2 p, int size)
        {
            // Light from the top: 100% → 58% brightness. Tinted by Image.color at use,
            // this turns any flat color into a soft vertical gradient.
            float t = Mathf.Clamp01(p.y / size);
            return (byte)Mathf.RoundToInt(Mathf.Lerp(148f, 255f, t));
        }

        static float GlowRingAlpha(Vector2 p, int size)
        {
            float R = size * 0.5f;
            float n = Vector2.Distance(p, Vector2.one * R) / R;
            if (n < 0.52f) return 0f;                              // transparent center
            if (n < 0.66f) return Smooth01((n - 0.52f) / 0.14f);   // rise to peak
            return Mathf.Clamp01(1f - (n - 0.66f) / 0.34f) * Mathf.Clamp01(1f - (n - 0.66f) / 0.34f);
        }

        static float ShadowAlpha(Vector2 p, int size)
        {
            float dist = RoundedRectDistance(p, size, 40f) + 14f;
            return Smooth01(Mathf.Clamp01(0.5f - dist / 26f));     // wide soft falloff
        }

        static float SpinnerRingAlpha(Vector2 p, int size)
        {
            Vector2 center = Vector2.one * (size * 0.5f);
            Vector2 offset = p - center;
            float band = Mathf.Abs(offset.magnitude - (size * 0.5f - 8f)) - 5f;

            float angle = Mathf.Atan2(offset.y, offset.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            if (angle > 300f)
                return 0f;

            return Mathf.Clamp01(0.5f - band) * Mathf.Lerp(0.12f, 1f, angle / 300f);
        }

        static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        static Texture2D MakeGradientBar()
        {
            // 256×64 white horizontal gradient pill; tinted per use (accent bars, pills).
            const int w = 256, h = 64;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    Vector2 half = new Vector2(w * 0.5f, h * 0.5f);
                    Vector2 d = new Vector2(Mathf.Abs(p.x - half.x), Mathf.Abs(p.y - half.y)) - (half - Vector2.one * 23f);
                    float dist = Vector2.Max(d, Vector2.zero).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - 22f;
                    float a = Mathf.Clamp01(0.5f - dist);
                    byte shade = (byte)Mathf.RoundToInt(Mathf.Lerp(168f, 255f, (float)x / w));
                    pixels[y * w + x] = new Color32(shade, shade, shade, (byte)(a * 255f));
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        // ------------------------------------------------------------------
        //  UI building blocks
        // ------------------------------------------------------------------

        public static GameObject CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = LayerMask.NameToLayer("UI");
            go.transform.SetParent(parent, false);
            return go;
        }

        public static RectTransform Stretch(GameObject go, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return rect;
        }

        public static RectTransform Place(GameObject go, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image AddImage(GameObject go, Sprite sprite, Color color, Image.Type type = Image.Type.Sliced, bool raycast = true)
        {
            var image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.type = sprite != null ? type : Image.Type.Simple;
            image.raycastTarget = raycast;
            return image;
        }

        /// <summary>Adds a full-stretch hairline outline overlay (decorative, no raycast).</summary>
        public static Image AddStroke(GameObject parent, Sprite outlineSprite, Color color)
        {
            GameObject go = CreateRect("Stroke", parent.transform);
            Stretch(go, Vector2.zero, Vector2.one);

            // Overlays must never participate in a parent LayoutGroup (e.g. FactsRow).
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.ignoreLayout = true;

            return AddImage(go, outlineSprite, color, Image.Type.Sliced, raycast: false);
        }

        /// <summary>Adds a soft drop shadow behind <paramref name="parent"/> (inserted as first sibling).</summary>
        public static Image AddShadow(GameObject parent, float spread = 30f, float yOffset = -12f)
        {
            GameObject go = CreateRect("Shadow", parent.transform);
            var rect = Stretch(go, Vector2.zero, Vector2.one, new Vector2(-spread, -spread + yOffset), new Vector2(spread, spread + yOffset));
            go.transform.SetAsFirstSibling();
            return AddImage(go, Shadow, ShadowColor, Image.Type.Sliced, raycast: false);
        }

        public static TextMeshProUGUI AddText(GameObject go, string text, float size, Color color,
            TextAlignmentOptions alignment = TextAlignmentOptions.Left, FontStyles style = FontStyles.Normal,
            float characterSpacing = 0f)
        {
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = alignment;
            tmp.fontStyle = style;
            tmp.characterSpacing = characterSpacing;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            return tmp;
        }

        public static Button AddButton(GameObject go, Graphic targetGraphic, Color normal, Color pressed)
        {
            var button = go.AddComponent<Button>();
            button.targetGraphic = targetGraphic;
            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = normal;
            colors.pressedColor = pressed;
            colors.selectedColor = normal;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            return button;
        }

        /// <summary>Primary pill: aqua gradient fill, dark bold label.</summary>
        public static Button CreatePrimaryPill(string name, Transform parent, string label, float fontSize = 34f)
        {
            GameObject go = CreateRect(name, parent);
            Image image = AddImage(go, GradientBar, Accent, Image.Type.Simple);
            Button button = AddButton(go, image, Accent, AccentDark);

            GameObject textGo = CreateRect("Label", go.transform);
            Stretch(textGo, Vector2.zero, Vector2.one, new Vector2(24f, 6f), new Vector2(-24f, -6f));
            AddText(textGo, label, fontSize, OnAccent, TextAlignmentOptions.Center, FontStyles.Bold, 1f);

            return button;
        }

        /// <summary>Secondary pill: dark card fill, hairline stroke, light label.</summary>
        public static Button CreateGhostPill(string name, Transform parent, string label, float fontSize = 34f)
        {
            GameObject go = CreateRect(name, parent);
            Image image = AddImage(go, RoundedRectSmall, Card);
            Button button = AddButton(go, image, Card, CardHover);
            AddStroke(go, OutlineSmall, Stroke);

            GameObject textGo = CreateRect("Label", go.transform);
            Stretch(textGo, Vector2.zero, Vector2.one, new Vector2(24f, 6f), new Vector2(-24f, -6f));
            AddText(textGo, label, fontSize, TextSecondary, TextAlignmentOptions.Center, FontStyles.Bold);

            return button;
        }

        /// <summary>Round translucent icon button (close/cancel “×”).</summary>
        public static Button CreateCircleIconButton(string name, Transform parent, string glyph, float glyphSize = 50f)
        {
            GameObject go = CreateRect(name, parent);
            Image bg = AddImage(go, Circle, new Color(1f, 1f, 1f, 0.07f), Image.Type.Simple);
            Button button = AddButton(go, bg, new Color(1f, 1f, 1f, 0.07f), new Color(1f, 1f, 1f, 0.18f));

            GameObject label = CreateRect("Label", go.transform);
            Stretch(label, Vector2.zero, Vector2.one);
            AddText(label, glyph, glyphSize, TextSecondary, TextAlignmentOptions.Center, FontStyles.Bold);

            return button;
        }

        /// <summary>Bottom-sheet grab handle.</summary>
        public static void AddSheetHandle(Transform sheet)
        {
            GameObject handle = CreateRect("Handle", sheet);
            Place(handle, new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(76f, 8f));
            AddImage(handle, RoundedRectSmall, new Color(1f, 1f, 1f, 0.18f), Image.Type.Sliced, raycast: false);
        }
    }
}
