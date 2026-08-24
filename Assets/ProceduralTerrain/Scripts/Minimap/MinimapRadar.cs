using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// GTA-style radar. Scans for things near the camera and draws them as blips:
//   the level glyph at your depth, an up glyph above you, a down glyph below you.
public class MinimapRadar : MonoBehaviour
{
    [Header("Tracking")]
    [Tooltip("Leave empty to use Camera.main (the AR camera).")]
    public Camera trackedCamera;
    [Tooltip("World-space scan radius in metres (about your terrain placementDistance).")]
    public float scanRadius = 18f;
    [Range(1f, 30f)] public float refreshHz = 8f;
    [Tooltip("Radar rotates with the camera's facing (GTA-style).")]
    public bool headingUp = true;
    [Tooltip("Vertical size of one layer, in metres. Same layer = dot, one up = up glyph, one down = down glyph.")]
    public float layerHeight = 2f;
    [Tooltip("Only show things within this many layers above/below you (1 = just the next layer each way).")]
    public int visibleLayerRange = 1;
    [Tooltip("Up/down glyphs for other layers. Off: you swim above the seabed, so nearly " +
          "everything is one layer down and the radar becomes a field of down-triangles.")]
    public bool showDepthBands = false;
    [Tooltip("Clamp out-of-range things to the rim. Off: it packs the edge with blips " +
          "for things that are not actually near you.")]
    public bool clampOutOfRange = false;
    [Tooltip("Hard cap on blips drawn. Past this the radar is noise, not information.")]
    public int maxBlips = 60;

    [Header("Sources")]
    [Tooltip("Show our own registered props (corals/seaweed).")]
    public bool useRegistry = true;
    [Tooltip("Also scan physics colliders and show those whose tag is listed below.")]
    public bool scanPhysicsByTag = false;
    public LayerMask physicsMask = ~0;
    public List<TagStyle> tagStyles = new List<TagStyle>();

    [Header("Layout")]
    [Tooltip("Leave empty to auto-build a corner radar panel.")]
    public RectTransform radarArea;
    public float radarPixelRadius = 95f;
    public Vector2 cornerOffset = new Vector2(-130f, -130f);

    [Header("Blips")]
    public float clusterCellPixels = 22f;
    public float baseBlipSize = 10f;
    [Tooltip("Extra pixels at full density. Small on purpose: a blip that triples reads " +
          "as a different kind of thing, not as more of the same.")]
    public float sizePerExtra = 3f;
    public int densityCap = 8;
    public string glyphLevel = "●"; // filled circle
    public string glyphAbove = "▲"; // up triangle
    public string glyphBelow = "▼"; // down triangle
    public Color fallbackColor = new Color(0.4f, 0.9f, 1f, 1f);

    [Header("Orientation")]
    [Tooltip("Ring at half the scan radius, so distances are readable at a glance.")]
    public bool showRangeRing = true;
    [Tooltip("Rotating N marker. With headingUp on it is the only cue for which way you face.")]
    public bool showNorthMarker = true;
    public Color ringColor = new Color(1f, 1f, 1f, 0.16f);
    public Color northColor = new Color(1f, 1f, 1f, 0.55f);

    [Header("Legend")]
    [Tooltip("Tap the radar to show what the blip colours mean.")]
    public bool legendOnTap = true;
    public Color legendPanelColor = new Color(0f, 0.06f, 0.12f, 0.88f);
    public int legendFontSize = 13;

    [Header("Player marker (always drawn on top)")]
    public Color playerColor = new Color(1f, 0.92f, 0.2f, 1f);
    public int playerSize = 16;
    public string playerGlyph = "▲";

    [System.Serializable]
    public class TagStyle
    {
        public string tag = "Actor";
        public Color color = new Color(0.4f, 0.9f, 1f, 1f);
        public bool show = true;
    }

    enum Band { Level, Above, Below }
    struct Blip { public Vector2 pos; public Band band; public Color color; public int count; }

    Font _font;
    RectTransform _container;
    Text _you;
    Text _north;
    RectTransform _legendPanel;
    bool _legendOpen;
    int _legendBuiltVersion = -1;
    readonly List<Text> _pool = new List<Text>();
    readonly List<Blip> _blips = new List<Blip>();
    readonly Dictionary<long, int> _cells = new Dictionary<long, int>();
    Collider[] _hits = new Collider[256];
    float _timer;
    float _yaw;

    void Awake()
    {
        ResolveCamera();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        BuildUI();
    }

    void BuildUI()
    {
        if (radarArea == null)
        {
            var canvasGO = new GameObject("MinimapCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);

            var root = new GameObject("Radar", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvasGO.transform, false);
            root.anchorMin = root.anchorMax = root.pivot = new Vector2(1f, 1f);
            root.anchoredPosition = cornerOffset;
            root.sizeDelta = Vector2.one * (radarPixelRadius * 2f);
            radarArea = root;

            MakeCircle(root, radarPixelRadius + 3f, new Color(0.45f, 0.85f, 1f, 0.35f)); // rim ring
            MakeCircle(root, radarPixelRadius, new Color(0f, 0.06f, 0.12f, 0.55f));       // dark fill

            // Half-radius ring: without it every blip is just "somewhere in the circle".
            if (showRangeRing)
            {
                MakeCircle(root, radarPixelRadius * 0.5f + 1f, ringColor);
                MakeCircle(root, radarPixelRadius * 0.5f, new Color(0f, 0.06f, 0.12f, 0.55f));
            }
        }

        _container = new GameObject("Blips", typeof(RectTransform)).GetComponent<RectTransform>();
        _container.SetParent(radarArea, false);
        _container.anchorMin = _container.anchorMax = _container.pivot = new Vector2(0.5f, 0.5f);
        _container.anchoredPosition = Vector2.zero;
        _container.sizeDelta = Vector2.zero;

        // On its own transform so Render() can spin it: with headingUp the radar turns
        // under you and nothing else says which way you point.
        if (showNorthMarker)
        {
            _north = MakeText("N");
            _north.color = northColor;
            _north.fontSize = Mathf.Max(10, playerSize - 4);
            _north.fontStyle = FontStyle.Bold;
        }

        // A transparent, raycastable disc over the radar. The rim and fill images are
        // all raycastTarget=false, so without this there is nothing to tap.
        if (legendOnTap)
        {
            var hit = new GameObject("TapTarget", typeof(Image)).GetComponent<Image>();
            hit.transform.SetParent(radarArea, false);
            hit.sprite = CircleSprite();
            hit.color = new Color(0f, 0f, 0f, 0f);
            hit.raycastTarget = true;
            var hrt = hit.rectTransform;
            hrt.anchorMin = hrt.anchorMax = hrt.pivot = new Vector2(0.5f, 0.5f);
            hrt.sizeDelta = Vector2.one * (radarPixelRadius * 2f);

            var btn = hit.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(ToggleLegend);
        }

        // centre "you" marker — re-asserted on top every frame in Render()
        _you = MakeText(playerGlyph);
        _you.color = playerColor;
        _you.fontSize = playerSize;
        _you.rectTransform.anchoredPosition = Vector2.zero;
    }

    void MakeCircle(RectTransform parent, float radius, Color color)
    {
        var img = new GameObject("Circle", typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(parent, false);
        img.sprite = CircleSprite();
        img.color = color;
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = Vector2.one * (radius * 2f);
    }

    void Update()
    {
        if (trackedCamera == null) { ResolveCamera(); if (trackedCamera == null) return; }
        _timer += Time.unscaledDeltaTime;
        if (_timer < 1f / Mathf.Max(1f, refreshHz)) return;
        _timer = 0f;
        Rebuild();
    }

    void Rebuild()
    {
        _blips.Clear();
        _cells.Clear();

        Vector3 cam = trackedCamera.transform.position;
        float yaw = trackedCamera.transform.eulerAngles.y * Mathf.Deg2Rad;
        _yaw = yaw;
        float cs = Mathf.Cos(yaw), sn = Mathf.Sin(yaw);
        float sqr = scanRadius * scanRadius;
        float pxPerM = radarPixelRadius / Mathf.Max(0.01f, scanRadius);

        if (useRegistry)
        {
            var all = MinimapRegistry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var m = all[i];
                if (m != null) Add(m.transform.position, m.color, cam, cs, sn, sqr, pxPerM);
            }
        }

        if (scanPhysicsByTag)
        {
            int n = Physics.OverlapSphereNonAlloc(cam, scanRadius, _hits, physicsMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < n; i++)
            {
                var col = _hits[i];
                if (col == null) continue;
                GameObject go = col.attachedRigidbody ? col.attachedRigidbody.gameObject : col.transform.root.gameObject;
                if (go.GetComponentInParent<MinimapMarker>() != null) continue; // ours, already via registry
                if (!TryTag(go.tag, out Color c)) continue;
                Add(go.transform.position, c, cam, cs, sn, sqr, pxPerM);
            }
        }

        Render();
    }

    void Add(Vector3 worldPos, Color color, Vector3 cam, float cs, float sn, float sqr, float pxPerM)
    {
        Vector3 d = worldPos - cam;

        // Discrete layers (like the sandbox): quantise height relative to you, and
        // only keep things within +/- visibleLayerRange so the radar never gets crowded vertically.
        int layerDiff = Mathf.RoundToInt(d.y / Mathf.Max(0.01f, layerHeight));
        if (Mathf.Abs(layerDiff) > Mathf.Max(0, visibleLayerRange)) return;

        Vector2 flat = new Vector2(d.x, d.z);
        if (flat.sqrMagnitude > sqr)
        {
            if (!clampOutOfRange) return;                 // out of range: simply not shown
            flat = flat.normalized * scanRadius;
        }

        Vector2 px = headingUp
            ? new Vector2(flat.x * cs - flat.y * sn, flat.x * sn + flat.y * cs) * pxPerM
            : flat * pxPerM;
        if (px.magnitude > radarPixelRadius) px = px.normalized * radarPixelRadius;

        Band band = !showDepthBands || layerDiff == 0
                  ? Band.Level
                  : (layerDiff > 0 ? Band.Above : Band.Below);

        int gx = Mathf.RoundToInt(px.x / Mathf.Max(1f, clusterCellPixels));
        int gy = Mathf.RoundToInt(px.y / Mathf.Max(1f, clusterCellPixels));
        long key = (((long)(gx + 2048)) << 20) ^ (((long)(gy + 2048)) << 8) ^ (long)(layerDiff + 64);

        if (_cells.TryGetValue(key, out int idx))
        {
            var b = _blips[idx];
            b.count++;
            b.pos = Vector2.Lerp(b.pos, px, 1f / b.count);
            _blips[idx] = b;
        }
        else
        {
            _cells[key] = _blips.Count;
            _blips.Add(new Blip { pos = px, band = band, color = color, count = 1 });
        }
    }

    void Render()
    {
        // Nearest first, then cut: a radar showing everything shows nothing.
        if (_blips.Count > maxBlips && maxBlips > 0)
        {
            _blips.Sort((x, y) => x.pos.sqrMagnitude.CompareTo(y.pos.sqrMagnitude));
            _blips.RemoveRange(maxBlips, _blips.Count - maxBlips);
        }

        while (_pool.Count < _blips.Count) _pool.Add(MakeText(glyphLevel));

        for (int i = 0; i < _pool.Count; i++)
        {
            Text t = _pool[i];
            if (i >= _blips.Count)
            {
                if (t.gameObject.activeSelf) t.gameObject.SetActive(false);
                continue;
            }
            Blip b = _blips[i];
            if (!t.gameObject.activeSelf) t.gameObject.SetActive(true);

            t.text = b.band == Band.Level ? glyphLevel : (b.band == Band.Above ? glyphAbove : glyphBelow);
            float dens = densityCap > 1 ? Mathf.Clamp01((b.count - 1f) / (densityCap - 1f)) : 0f;

            // Density lifts the blip; it does not repaint it. Washing to white threw
            // away the species colour exactly where there was most of it.
            t.color = Color.Lerp(b.color, Color.white, dens * 0.25f);
            t.fontSize = Mathf.RoundToInt(baseBlipSize + sizePerExtra * dens);
            t.rectTransform.anchoredPosition = b.pos;
        }

        // Heading-up rotates the radar by -yaw, so north lands at that angle from up.
        if (_north != null)
        {
            float a = headingUp ? _yaw : 0f;
            float r = radarPixelRadius - 12f;
            _north.rectTransform.anchoredPosition = new Vector2(-Mathf.Sin(a) * r, Mathf.Cos(a) * r);
            _north.transform.SetAsLastSibling();
        }

        if (_you != null) _you.transform.SetAsLastSibling(); // player marker always highest z
    }

    public void ToggleLegend()
    {
        _legendOpen = !_legendOpen;
        if (_legendOpen) BuildLegend();
        if (_legendPanel != null) _legendPanel.gameObject.SetActive(_legendOpen);
    }

    // Rebuilt from the live registry, so it lists what is actually around you. Only
    // redone when the set of kinds has changed, not every time the panel opens.
    void BuildLegend()
    {
        var entries = MinimapRegistry.Legend;
        if (_legendPanel != null && _legendBuiltVersion == MinimapRegistry.LegendVersion) return;
        _legendBuiltVersion = MinimapRegistry.LegendVersion;

        if (_legendPanel != null) Destroy(_legendPanel.gameObject);

        float row = legendFontSize + 8f;
        float width = 168f;
        float height = Mathf.Max(row, entries.Count * row) + 16f;

        var panel = new GameObject("Legend", typeof(Image)).GetComponent<Image>();
        panel.transform.SetParent(radarArea, false);
        panel.sprite = null;
        panel.color = legendPanelColor;
        panel.raycastTarget = false;
        _legendPanel = panel.rectTransform;
        // Below the radar, right-aligned with it, so it never covers the blips.
        _legendPanel.anchorMin = _legendPanel.anchorMax = new Vector2(0.5f, 0.5f);
        _legendPanel.pivot = new Vector2(0.5f, 1f);
        _legendPanel.anchoredPosition = new Vector2(0f, -(radarPixelRadius + 8f));
        _legendPanel.sizeDelta = new Vector2(width, height);

        if (entries.Count == 0)
        {
            var empty = MakeLegendText("Nothing nearby yet", Color.white, _legendPanel);
            empty.rectTransform.anchoredPosition = new Vector2(10f, -8f);
            return;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            float y = -8f - i * row;

            var dot = MakeLegendText(glyphLevel, e.color, _legendPanel);
            dot.rectTransform.anchoredPosition = new Vector2(12f, y);

            var lbl = MakeLegendText(e.label, Color.white, _legendPanel);
            lbl.rectTransform.anchoredPosition = new Vector2(30f, y);
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.rectTransform.sizeDelta = new Vector2(width - 36f, row);
        }
    }

    Text MakeLegendText(string label, Color color, RectTransform parent)
    {
        var t = new GameObject("LegendRow", typeof(Text)).GetComponent<Text>();
        t.font = _font;
        t.text = label;
        t.color = color;
        t.fontSize = legendFontSize;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        var rt = t.rectTransform;
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(20f, legendFontSize + 4f);
        return t;
    }

    Text MakeText(string s)
    {
        var go = new GameObject("Blip", typeof(Text));
        var t = go.GetComponent<Text>();
        t.font = _font;
        t.text = s;
        t.alignment = TextAnchor.MiddleCenter;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
        t.fontSize = Mathf.RoundToInt(baseBlipSize);
        var rt = t.rectTransform;
        rt.SetParent(_container, false);
        rt.sizeDelta = new Vector2(48f, 48f);
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        return t;
    }

    void ResolveCamera()
    {
        if (trackedCamera != null) return;
        trackedCamera = Camera.main;
        if (trackedCamera == null) trackedCamera = FindFirstObjectByType<Camera>();
    }

    static Sprite _circleSprite;
    static Sprite CircleSprite()
    {
        if (_circleSprite != null) return _circleSprite;
        const int size = 128;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float r = size * 0.5f;
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r;
                float a = Mathf.Clamp01(r - Mathf.Sqrt(dx * dx + dy * dy)); // ~1px anti-aliased edge
                px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        return _circleSprite;
    }

    bool TryTag(string tag, out Color color)
    {
        for (int i = 0; i < tagStyles.Count; i++)
            if (tagStyles[i].show && tagStyles[i].tag == tag) { color = tagStyles[i].color; return true; }
        color = fallbackColor;
        return false;
    }
}
