using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // The in-scene ecosystem panel (Design Document 6.1).
    //
    // A tab on the edge of the screen slides out a compact panel. It does not pause
    // the simulation — the point is to watch the ecosystem respond while the panel is
    // still open. Cause and effect become directly observable rather than remembered.
    //
    // Built entirely from code onto its own canvas, so nothing in the host scene is
    // touched and nothing in it can be disturbed.
    public class EcosystemPanelUI : MonoBehaviour
    {
        LivingReefController _reef;

        RectTransform _panel;
        RectTransform _tab;
        bool _open;
        float _slide;

        TMP_Text _dayText, _healthText, _stageText;
        Image _healthDot;
        TMP_Text _temperatureValue, _acidityValue;
        Slider _temperatureSlider, _aciditySlider;
        Button[] _speedButtons;

        readonly TMP_Text[] _rowAmount = new TMP_Text[SpeciesLibrary.Count];
        readonly Image[] _rowTrend = new Image[SpeciesLibrary.Count];
        readonly TMP_Text[] _rowName = new TMP_Text[SpeciesLibrary.Count];
        readonly Image[] _rowDots = new Image[SpeciesLibrary.Count];
        readonly GameObject[] _rowRoot = new GameObject[SpeciesLibrary.Count];

        EnergyPyramidUI _pyramid;
        OctopusUIHub _octopusUI;
        TMP_Text _octopusSummary;
        GameObject _octopusSection;
        GameObject _octopusHeader;
        WhyPanelUI _whyPanel;
        OrganismPickerUI _picker;
        BarrenPromptUI _barrenPrompt;
        WelcomeBackUI _welcomeBack;
        bool _welcomeShown;

        Button _reportButton;
        EcoToast _toast;

        float _refresh;

        const float PanelWidth = 560f;

        public static EcosystemPanelUI Create(LivingReefController reef)
        {
            var canvas = EcoUIKit.CreateCanvas("Living Ecosystem UI", 5000);
            canvas.transform.SetParent(reef.transform, false);
            EcoUIKit.EnsureEventSystem();

            var ui = canvas.gameObject.AddComponent<EcosystemPanelUI>();
            ui._reef = reef;
            ui.Build(canvas.transform);
            return ui;
        }

        void Build(Transform root)
        {
            // ── The slide-out panel ──────────────────────────────────────────
            var panelGo = EcoUIKit.Panel(root, "Panel", EcoUIKit.PanelBg);
            _panel = EcoUIKit.Rect(panelGo);
            _panel.anchorMin = new Vector2(1f, 0f);
            _panel.anchorMax = new Vector2(1f, 1f);
            _panel.pivot = new Vector2(0f, 0.5f);
            _panel.sizeDelta = new Vector2(PanelWidth, -180f);
            _panel.anchoredPosition = new Vector2(0f, 0f);

            // ── The tab that opens it ────────────────────────────────────────
            var tabButton = EcoUIKit.Button(root, "Reef", 26f, EcoUIKit.Accent, Color.white, Toggle);
            _tab = EcoUIKit.Rect(tabButton.gameObject);
            _tab.anchorMin = new Vector2(1f, 0.5f);
            _tab.anchorMax = new Vector2(1f, 0.5f);
            _tab.pivot = new Vector2(1f, 0.5f);
            _tab.sizeDelta = new Vector2(120f, 132f);
            _tab.anchoredPosition = new Vector2(-8f, 0f);

            var column = EcoUIKit.ScrollColumn(panelGo.transform, out _);
            var viewport = column.parent as RectTransform;
            EcoUIKit.Stretch(viewport, 22f, 22f);

            // ── Header ───────────────────────────────────────────────────────
            var header = EcoUIKit.Empty(column, "Header");
            header.AddComponent<LayoutElement>().preferredHeight = 64f;
            var title = EcoUIKit.Text(header.transform, "Ecosystem", 32f, EcoUIKit.TextMain,
                                      TextAlignmentOptions.Left);
            EcoUIKit.Stretch(EcoUIKit.Rect(title.gameObject), 0f, 0f);

            var close = EcoUIKit.Button(header.transform, "×", 34f, EcoUIKit.Track, EcoUIKit.TextMain, Toggle);
            var closeRect = EcoUIKit.Rect(close.gameObject);
            closeRect.anchorMin = new Vector2(1f, 0.5f);
            closeRect.anchorMax = new Vector2(1f, 0.5f);
            closeRect.pivot = new Vector2(1f, 0.5f);
            closeRect.sizeDelta = new Vector2(52f, 52f);
            closeRect.anchoredPosition = Vector2.zero;

            // ── Water controls ───────────────────────────────────────────────
            EcoUIKit.SectionHeader(column, "The water");
            var s = _reef.Settings;
            _temperatureSlider = EcoUIKit.LabelledSlider(column, "Temperature",
                FormatTemperature(s.temperatureC),
                EcosystemBounds.Temperature.Normalize(s.temperatureC),
                v =>
                {
                    float c = EcosystemBounds.Temperature.Denormalize(v);
                    _reef.SetTemperature(c);
                    if (_temperatureValue != null) _temperatureValue.text = FormatTemperature(c);
                },
                out _temperatureValue);

            _aciditySlider = EcoUIKit.LabelledSlider(column, "Acidity",
                FormatAcidity(s.acidityPh),
                EcosystemBounds.Acidity.Normalize(s.acidityPh),
                v =>
                {
                    float ph = EcosystemBounds.Acidity.Denormalize(v);
                    _reef.SetAcidity(ph);
                    if (_acidityValue != null) _acidityValue.text = FormatAcidity(ph);
                },
                out _acidityValue);

            _speedButtons = EcoUIKit.SegmentedControl(column, "Speed",
                EcosystemSettings.SpeedOptions, s.speed, i => { _reef.SetSpeed(i); RefreshSpeed(); });

            // ── Organism readouts ────────────────────────────────────────────
            EcoUIKit.SectionHeader(column, "Who lives here");

            var organismsHeader = EcoUIKit.Empty(column, "OrganismsHeader");
            organismsHeader.AddComponent<LayoutElement>().preferredHeight = 46f;

            var addRemove = EcoUIKit.Text(organismsHeader.transform,
                "Add or remove a species", 21f, EcoUIKit.TextDim);
            var addRect = EcoUIKit.Rect(addRemove.gameObject);
            addRect.anchorMin = new Vector2(0f, 0.5f);
            addRect.anchorMax = new Vector2(0.62f, 0.5f);
            addRect.pivot = new Vector2(0f, 0.5f);
            addRect.sizeDelta = new Vector2(0f, 30f);
            addRect.anchoredPosition = new Vector2(2f, 0f);

            var edit = EcoUIKit.Button(organismsHeader.transform, "Change", 22f,
                EcoUIKit.Track, EcoUIKit.TextMain, () => _picker.Open());
            var editRect = EcoUIKit.Rect(edit.gameObject);
            editRect.anchorMin = new Vector2(1f, 0.5f);
            editRect.anchorMax = new Vector2(1f, 0.5f);
            editRect.pivot = new Vector2(1f, 0.5f);
            editRect.sizeDelta = new Vector2(130f, 44f);
            editRect.anchoredPosition = new Vector2(-2f, 0f);

            for (int i = 0; i < SpeciesLibrary.Count; i++)
                BuildOrganismRow(column, i);

            // ── Octopuses ────────────────────────────────────────────────────
            BuildOctopusSection(column);

            // ── Energy pyramid ───────────────────────────────────────────────
            EcoUIKit.SectionHeader(column, "Energy");
            _pyramid = EnergyPyramidUI.Create(column);

            // ── Health and day ───────────────────────────────────────────────
            EcoUIKit.SectionHeader(column, "How it is doing");
            var footer = EcoUIKit.Empty(column, "Footer");
            footer.AddComponent<LayoutElement>().preferredHeight = 118f;

            var dot = EcoUIKit.Panel(footer.transform, "HealthDot", Color.green);
            _healthDot = dot.GetComponent<Image>();
            var dotRect = EcoUIKit.Rect(dot);
            dotRect.anchorMin = new Vector2(0f, 1f);
            dotRect.anchorMax = new Vector2(0f, 1f);
            dotRect.pivot = new Vector2(0f, 1f);
            dotRect.sizeDelta = new Vector2(22f, 22f);
            dotRect.anchoredPosition = new Vector2(0f, -6f);

            _healthText = EcoUIKit.Text(footer.transform, "Health  balanced", 26f, EcoUIKit.TextMain);
            var healthRect = EcoUIKit.Rect(_healthText.gameObject);
            healthRect.anchorMin = new Vector2(0f, 1f);
            healthRect.anchorMax = new Vector2(1f, 1f);
            healthRect.pivot = new Vector2(0f, 1f);
            healthRect.sizeDelta = new Vector2(-34f, 32f);
            healthRect.anchoredPosition = new Vector2(34f, 0f);

            _stageText = EcoUIKit.Text(footer.transform, "", 22f, EcoUIKit.TextDim);
            var stageRect = EcoUIKit.Rect(_stageText.gameObject);
            stageRect.anchorMin = new Vector2(0f, 1f);
            stageRect.anchorMax = new Vector2(1f, 1f);
            stageRect.pivot = new Vector2(0f, 1f);
            stageRect.sizeDelta = new Vector2(-34f, 28f);
            stageRect.anchoredPosition = new Vector2(34f, -34f);

            _dayText = EcoUIKit.Text(footer.transform, "Day 0", 26f, EcoUIKit.TextMain);
            var dayRect = EcoUIKit.Rect(_dayText.gameObject);
            dayRect.anchorMin = new Vector2(0f, 0f);
            dayRect.anchorMax = new Vector2(0.5f, 0f);
            dayRect.pivot = new Vector2(0f, 0f);
            dayRect.sizeDelta = new Vector2(0f, 40f);
            dayRect.anchoredPosition = Vector2.zero;

            var why = EcoUIKit.Button(footer.transform, "Why?", 24f, EcoUIKit.Accent, Color.white,
                                      () => _whyPanel.Open());
            var whyRect = EcoUIKit.Rect(why.gameObject);
            whyRect.anchorMin = new Vector2(1f, 0f);
            whyRect.anchorMax = new Vector2(1f, 0f);
            whyRect.pivot = new Vector2(1f, 0f);
            whyRect.sizeDelta = new Vector2(150f, 48f);
            whyRect.anchoredPosition = Vector2.zero;

            BuildReportSection(column);

            // ── Overlays ─────────────────────────────────────────────────────
            _octopusUI = OctopusUIHub.Create(transform, _reef);
            _whyPanel = WhyPanelUI.Create(transform, _reef);
            _picker = OrganismPickerUI.Create(transform, _reef);
            _barrenPrompt = BarrenPromptUI.Create(transform, _reef);

            _toast = EcoToast.Create(transform);

            _welcomeBack = WelcomeBackUI.Create(transform, _reef);
            _welcomeBack.onWhyRequested += () => _whyPanel.Open();

            _slide = 1f;   // start closed
            ApplySlide();
            Refresh();
        }

        // Taking the reef away with you (Design Document 7.3).
        //
        // Just the button. A paragraph explaining what a "reef report" is tells the
        // learner something they will find out in one tap, and it sat there for the
        // whole session to explain a single moment. Where the file went is the one
        // thing they cannot work out for themselves, and that is what the toast says.
        void BuildReportSection(Transform column)
        {
            EcoUIKit.SectionHeader(column, "Take it with you");

            var host = EcoUIKit.Empty(column, "Report");
            host.AddComponent<LayoutElement>().preferredHeight = 62f;

            _reportButton = EcoUIKit.Button(host.transform, "Save my reef report", 24f,
                                            EcoUIKit.Accent, Color.white, SaveReport);
            var saveRect = EcoUIKit.Rect(_reportButton.gameObject);
            saveRect.anchorMin = new Vector2(0f, 1f);
            saveRect.anchorMax = new Vector2(1f, 1f);
            saveRect.pivot = new Vector2(0.5f, 1f);
            saveRect.sizeDelta = new Vector2(0f, 56f);
            saveRect.anchoredPosition = Vector2.zero;
        }

        void SaveReport()
        {
            var label = _reportButton != null
                ? _reportButton.GetComponentInChildren<TMP_Text>() : null;

            if (_reportButton != null) _reportButton.interactable = false;
            if (label != null) label.text = "Putting it together…";

            // Two pages of vector drawing: a handful of milliseconds, and doing it
            // inline means the answer is on screen before the learner's thumb has left
            // the button. Threading it would cost more in complexity than it saves.
            var result = Memory.ReportShare.ShareReport(_reef, EnvironmentName());

            if (label != null) label.text = "Save my reef report";
            if (_reportButton != null) _reportButton.interactable = true;

            _toast.Show(result.message, result.ok ? 5f : 7f);
        }

        static string EnvironmentName()
        {
            var profile = CreateEnv.EnvironmentSession.Selected;
            return profile != null && !string.IsNullOrEmpty(profile.displayName)
                ? profile.displayName : "My Reef";
        }

        // Generation, gene frequencies, and the way into the genetics screens.
        // The frequency line is the number that visibly moves when the learner warms
        // the water, so it earns its place on the main panel rather than being buried.
        void BuildOctopusSection(Transform column)
        {
            _octopusHeader = EcoUIKit.SectionHeader(column, "Octopuses").transform.parent.gameObject;

            _octopusSection = EcoUIKit.Empty(column, "Octopuses");
            _octopusSection.AddComponent<LayoutElement>().preferredHeight = 130f;

            // Three things, evenly spaced, matching the rhythm of the rows above:
            // an invitation, a way in, and one number worth watching.
            const float gap = 12f;
            const float hintHeight = 30f;
            const float buttonHeight = 46f;

            var hint = EcoUIKit.Text(_octopusSection.transform,
                                     "Tap an octopus in the water to meet it", 21f, EcoUIKit.TextDim);
            var hintRect = EcoUIKit.Rect(hint.gameObject);
            hintRect.anchorMin = new Vector2(0f, 1f);
            hintRect.anchorMax = new Vector2(1f, 1f);
            hintRect.pivot = new Vector2(0f, 1f);
            hintRect.sizeDelta = new Vector2(-4f, hintHeight);
            hintRect.anchoredPosition = new Vector2(2f, 0f);

            var tree = EcoUIKit.Button(_octopusSection.transform, "Family tree", 22f,
                                       EcoUIKit.Track, EcoUIKit.TextMain,
                                       () => _octopusUI.Tree.Open(-1));
            var treeRect = EcoUIKit.Rect(tree.gameObject);
            treeRect.anchorMin = new Vector2(0f, 1f);
            treeRect.anchorMax = new Vector2(1f, 1f);
            treeRect.pivot = new Vector2(0.5f, 1f);
            treeRect.sizeDelta = new Vector2(-4f, buttonHeight);
            treeRect.anchoredPosition = new Vector2(0f, -(hintHeight + gap));

            // The one figure that visibly moves when the learner warms the water, so
            // it sits on its own under the button rather than inside a block of stats.
            _octopusSummary = EcoUIKit.Text(_octopusSection.transform, "", 21f, EcoUIKit.TextDim);
            var sumRect = EcoUIKit.Rect(_octopusSummary.gameObject);
            sumRect.anchorMin = new Vector2(0f, 1f);
            sumRect.anchorMax = new Vector2(1f, 1f);
            sumRect.pivot = new Vector2(0f, 1f);
            sumRect.sizeDelta = new Vector2(-4f, hintHeight);
            sumRect.anchoredPosition = new Vector2(2f, -(hintHeight + gap + buttonHeight + gap));
        }

        void RefreshOctopusSection()
        {
            var pop = _reef.Octopuses;
            if (pop == null || _octopusSummary == null) return;

            bool present = _reef.Sim.IsPresent(SpeciesLibrary.Octopus);
            // Header and body travel together, or removing the octopus leaves a
            // heading with nothing underneath it.
            _octopusSection.SetActive(present);
            if (_octopusHeader != null) _octopusHeader.SetActive(present);
            if (!present) return;

            if (pop.AliveCount == 0)
            {
                _octopusSummary.text = "None here just now - young drift in from other reefs.";
                return;
            }

            int heat = Mathf.RoundToInt(pop.AlleleFrequency(Genetics.GeneId.HeatTolerance) * 100f);
            _octopusSummary.text = $"Heat-tolerant gene in the family: {heat}%";
        }

        void BuildOrganismRow(Transform column, int species)
        {
            var def = SpeciesLibrary.Get(species);
            if (def == null) return;

            var row = EcoUIKit.Empty(column, "Row_" + def.id);
            row.AddComponent<LayoutElement>().preferredHeight = 50f;
            _rowRoot[species] = row;

            var dot = EcoUIKit.Panel(row.transform, "Dot", def.tint);
            _rowDots[species] = dot.GetComponent<Image>();
            var dotRect = EcoUIKit.Rect(dot);
            dotRect.anchorMin = new Vector2(0f, 0.5f);
            dotRect.anchorMax = new Vector2(0f, 0.5f);
            dotRect.pivot = new Vector2(0f, 0.5f);
            dotRect.sizeDelta = new Vector2(16f, 16f);
            dotRect.anchoredPosition = new Vector2(2f, 0f);

            _rowName[species] = EcoUIKit.Text(row.transform, def.commonName, 23f, EcoUIKit.TextMain);
            var nameRect = EcoUIKit.Rect(_rowName[species].gameObject);
            nameRect.anchorMin = new Vector2(0f, 0f);
            nameRect.anchorMax = new Vector2(0.68f, 1f);
            nameRect.pivot = new Vector2(0f, 0.5f);
            nameRect.offsetMin = new Vector2(28f, 0f);
            nameRect.offsetMax = Vector2.zero;

            _rowAmount[species] = EcoUIKit.Text(row.transform, "0", 23f, EcoUIKit.TextMain,
                                                TextAlignmentOptions.Right);
            var amountRect = EcoUIKit.Rect(_rowAmount[species].gameObject);
            amountRect.anchorMin = new Vector2(0.68f, 0f);
            amountRect.anchorMax = new Vector2(0.90f, 1f);
            amountRect.offsetMin = Vector2.zero;
            amountRect.offsetMax = Vector2.zero;

            // A generated triangle, rotated for falling. See EcoUIKit.TriangleSprite
            // for why this is not just an arrow character.
            var trend = EcoUIKit.Panel(row.transform, "Trend", EcoUIKit.TextDim);
            var trendImage = trend.GetComponent<Image>();
            trendImage.sprite = EcoUIKit.TriangleSprite;
            trendImage.preserveAspect = true;
            trendImage.raycastTarget = false;
            _rowTrend[species] = trendImage;

            var trendRect = EcoUIKit.Rect(trend);
            trendRect.anchorMin = new Vector2(0.94f, 0.5f);
            trendRect.anchorMax = new Vector2(0.94f, 0.5f);
            trendRect.pivot = new Vector2(0.5f, 0.5f);
            trendRect.sizeDelta = new Vector2(18f, 18f);
            trendRect.anchoredPosition = Vector2.zero;

            // Tapping a row opens that species' card.
            var button = row.AddComponent<Button>();
            var hit = row.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            button.targetGraphic = hit;
            int index = species;
            button.onClick.AddListener(() => _whyPanel.OpenSpecies(index));
        }

        void Toggle()
        {
            _open = !_open;
        }

        void Update()
        {
            // Slide the panel in and out.
            float target = _open ? 0f : 1f;
            _slide = Mathf.MoveTowards(_slide, target, Time.unscaledDeltaTime * 4.5f);
            ApplySlide();

            // Only the tab is on screen when the panel is closed, so there is nothing
            // to keep up to date. Refreshing anyway meant rebuilding every readout,
            // the pyramid and the octopus block four times a second, all of it behind
            // a panel the learner could not see.
            if (_open || _slide < 0.999f)
            {
                _refresh -= Time.unscaledDeltaTime;
                if (_refresh <= 0f)
                {
                    _refresh = 0.25f;
                    Refresh();
                }
            }

            // Shown once, on the first frame the interface is live — which in AR is
            // after the environment has been placed, so the card is never waiting
            // behind a plane-scanning screen.
            if (!_welcomeShown)
            {
                _welcomeShown = true;
                _welcomeBack.ShowIfDue();
            }

            // The barren prompt appears on its own when the reef bottoms out.
            if (_reef.Health.stage == CollapseStage.Barren) _barrenPrompt.MaybeShow();
        }

        void ApplySlide()
        {
            float eased = Mathf.SmoothStep(0f, 1f, _slide);
            _panel.anchoredPosition = new Vector2(-PanelWidth + eased * PanelWidth, 0f);
            _tab.anchoredPosition = new Vector2(-8f - (1f - eased) * PanelWidth, 0f);
        }

        void RefreshSpeed()
        {
            EcoUIKit.PaintSegments(_speedButtons, _reef.Settings.speed);
        }

        // Assigning the same string still marks the canvas dirty and costs a full
        // rebuild, so every readout goes through here.
        static void Set(TMP_Text field, string value)
        {
            if (field != null && field.text != value) field.text = value;
        }

        void Refresh()
        {
            var sim = _reef.Sim;
            var health = _reef.Health;

            Set(_dayText, "Day " + sim.day);

            var healthColour = EcosystemHealth.Colour(health.level);
            _healthDot.color = healthColour;
            Set(_healthText, "Health  " + health.level.ToString().ToLowerInvariant());
            _healthText.color = healthColour;
            Set(_stageText, EcosystemHealth.Describe(health.stage));

            var all = SpeciesLibrary.All;
            for (int i = 0; i < all.Length; i++)
            {
                if (_rowRoot[i] == null) continue;

                bool present = sim.IsPresent(i);
                _rowRoot[i].SetActive(true);

                if (!present)
                {
                    _rowName[i].color = EcoUIKit.TextDim;
                    Set(_rowAmount[i], "removed");
                    _rowAmount[i].color = EcoUIKit.TextDim;
                    _rowTrend[i].enabled = false;
                    _rowDots[i].color = new Color(all[i].tint.r, all[i].tint.g, all[i].tint.b, 0.25f);
                    continue;
                }

                _rowName[i].color = EcoUIKit.TextMain;
                _rowDots[i].color = all[i].tint;

                float amount = sim.DisplayAmount(i);
                Set(_rowAmount[i], Mathf.RoundToInt(amount).ToString());
                _rowAmount[i].color = amount > 0f ? EcoUIKit.TextMain : EcoUIKit.TextDim;

                int dir = _reef.Reasons.TrendOf(i);
                _rowTrend[i].enabled = dir != 0;
                if (dir != 0)
                {
                    _rowTrend[i].color = dir > 0
                        ? new Color(0.42f, 0.82f, 0.48f)
                        : new Color(0.90f, 0.46f, 0.42f);
                    _rowTrend[i].rectTransform.localRotation =
                        Quaternion.Euler(0f, 0f, dir > 0 ? 0f : 180f);
                }
            }

            _pyramid.Refresh(sim);
            RefreshOctopusSection();
            RefreshSpeed();
        }

        public void RefreshOrganismRows() => Refresh();

        static string FormatTemperature(float c) => c.ToString("0.#") + " °C";
        static string FormatAcidity(float ph) => "pH " + ph.ToString("0.00");
    }
}
