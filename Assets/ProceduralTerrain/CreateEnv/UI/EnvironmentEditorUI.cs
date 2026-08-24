using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.UI
{
    // Create/Edit form. Thin:
    public class EnvironmentEditorUI : MonoBehaviour
    {
        [Header("Wiring (set by the Editor generator)")]
        public RectTransform formContainer;    // ScrollView content
        public GameObject sliderRowPrefab;     // children: Label(TMP), Slider, Value(TMP)
        public GameObject dropdownRowPrefab;   // children: Label(TMP), Dropdown(TMP_Dropdown)
        public GameObject headerRowPrefab;     // child:   Label(TMP)
        public TMP_InputField nameInput;
        public TMP_Text titleText;
        public Button saveButton, cancelButton;

        [Header("Dialogs (set by the Editor generator)")]
        public GameObject savedDialog;         // "Environment Saved!" confirmation
        public TMP_Text savedMessage;
        public Button savedOkButton;
        public GameObject unsavedDialog;       // Save / Discard / Cancel on dirty close
        public Button unsavedSaveButton, unsavedDiscardButton, unsavedCancelButton;

        EnvironmentProfile _work;
        Action _onDone;
        string _editingId;
        bool _dirty;

        void Awake()
        {
            // The builder only creates/saves environments — it never plays them.
            // Playing happens in FreeExploreConfig, which loads the saved profile.
            if (saveButton != null)   saveButton.onClick.AddListener(OnSavePressed);
            // Cancel acts as "back": it returns to the host scene, guarding unsaved
            // edits with the Unsaved Changes dialog.
            if (cancelButton != null) cancelButton.onClick.AddListener(OnCancelPressed);

            if (savedOkButton != null)        savedOkButton.onClick.AddListener(Close);
            if (unsavedSaveButton != null)    unsavedSaveButton.onClick.AddListener(() => { unsavedDialog.SetActive(false); OnSavePressed(); });
            if (unsavedDiscardButton != null) unsavedDiscardButton.onClick.AddListener(Close);
            if (unsavedCancelButton != null)  unsavedCancelButton.onClick.AddListener(() => unsavedDialog.SetActive(false));
        }

        public void Open(EnvironmentProfile existing, Action onDone)
        {
            _onDone   = onDone;
            _editingId = (existing != null && !existing.isBuiltIn) ? existing.id : null;
            _work     = existing != null ? existing.Clone() : EnvironmentBounds.Default();
            if (existing == null)
            {
                _work.displayName = "My Environment";
                // Each new environment gets its own terrain layout; the seed is not a
                // user-facing choice, it just keeps environments from looking identical.
                _work.seed = UnityEngine.Random.Range(0, 10000);
                _work.scatterSeed = _work.seed;
            }
            Remap();
            _dirty = false;

            gameObject.SetActive(true);
            if (savedDialog != null)   savedDialog.SetActive(false);
            if (unsavedDialog != null) unsavedDialog.SetActive(false);
            if (titleText != null) titleText.text = _editingId != null ? "Edit Environment" : "Create Environment";
            if (nameInput != null)
            {
                nameInput.SetTextWithoutNotify(_work.displayName);
                nameInput.onValueChanged.RemoveAllListeners();
                nameInput.onValueChanged.AddListener(s => { _work.displayName = s; _dirty = true; });
            }
            BuildForm();
        }

        void Hide() => gameObject.SetActive(false);

        void Close()
        {
            Hide();
            _onDone?.Invoke();
        }

        void OnSavePressed()
        {
            var saved = Save();
            _dirty = false;
            if (savedDialog == null) { Close(); return; }
            if (savedMessage != null)
                savedMessage.text = $"“{saved.displayName}” has been saved successfully.";
            savedDialog.SetActive(true);
        }

        void OnCancelPressed()
        {
            if (_dirty && unsavedDialog != null) unsavedDialog.SetActive(true);
            else Close();
        }

        // Marks the form dirty, then re-derives the technical profile from the
        // friendly choices (and clamps it).
        void Edited()
        {
            _dirty = true;
            SimpleEnvironmentMapper.Apply(_work);
        }

        // Initial derivation on Open — same mapping, but not a user edit.
        void Remap() => SimpleEnvironmentMapper.Apply(_work);

        void BuildForm()
        {
            for (int i = formContainer.childCount - 1; i >= 0; i--)
                Destroy(formContainer.GetChild(i).gameObject);

            var s = _work.simple;

            Header("Seafloor");
            Dropdown("Seafloor profile", SimpleEnvironmentSettings.SeafloorProfiles,
                     s.seafloorProfile, v => { s.seafloorProfile = v; Edited(); });
            Slider("Terrain complexity", s.terrainComplexity, v => { s.terrainComplexity = v; Edited(); });
            Dropdown("Seafloor surface", SimpleEnvironmentSettings.SeafloorSurfaces,
                     s.seafloorSurface, v => { s.seafloorSurface = v; Edited(); });

            Header("Habitat");
            Dropdown("Marine habitat", SimpleEnvironmentSettings.MarineHabitats,
                     s.marineHabitat, v => { s.marineHabitat = v; Edited(); });
            Slider("Habitat density", s.habitatDensity, v => { s.habitatDensity = v; Edited(); });

            Header("Water");
            Dropdown("Water model", SimpleEnvironmentSettings.WaterModels,
                     s.waterModel, v => { s.waterModel = v; Edited(); });

            if (s.waterModel == 1)
            {
                Dropdown("Water colour", SimpleEnvironmentSettings.WaterColours,
                         s.waterColour, v => { s.waterColour = v; Edited(); });
            }
            else
            {
                int wt = s.ResolvedWaterType();
                Dropdown("Water type", OceanModel.WaterTypeNames, wt,
                         v => { s.waterType = v; Edited(); });
                Note($"visibility ≈ {OceanModel.VisibilityMetres(wt):0.#} m");
            }

            Slider("Water clarity", s.waterClarity, v => { s.waterClarity = v; Edited(); });

            Header("Time of day");
            Dropdown("Time of day", SimpleEnvironmentSettings.TimesOfDay,
                     s.timeOfDay, v => { s.timeOfDay = v; Edited(); });
            if (s.timeOfDay == 3)
                Note("Moonlight only — the reef goes dark beyond a few metres.");

            Header("Exploration");
            Dropdown("Exploration area", SimpleEnvironmentSettings.ExplorationAreas,
                     s.explorationArea, v => { s.explorationArea = v; Edited(); });

            // Advanced: real controls, but ones with a sensible default that most environments
            // never need to touch.
            Header("Advanced settings");
            RangedSlider("Site depth", s.siteDepthMeters, 1f, 45f,
                         v => $"{v:0} m", v => { s.siteDepthMeters = v; Edited(); });
            if (s.waterModel != 1)
                Note($"daylight at depth ≈ {OceanModel.LightAtDepth(s.ResolvedWaterType(), s.siteDepthMeters) * 100f:0}%");
            Dropdown("Water movement", SimpleEnvironmentSettings.WaterMovements,
                     s.waterMovement, v => { s.waterMovement = v; Edited(); });

            // Living Ecosystem, appended below the existing sections (Design Doc §5).
            // Built from these same row prefabs, so it inherits this form's styling.
            Ecosystem.EcosystemFormSection.Build(
                formContainer, headerRowPrefab, dropdownRowPrefab, sliderRowPrefab,
                _work, Edited);
        }

        EnvironmentProfile Save()
        {
            if (_editingId != null) _work.id = _editingId;
            Remap();
            return EnvironmentRepository.Save(_work);
        }

        // ── row builders (instantiate the styled prefabs, then bind) ─────────
        // Read-only derived value, shown under the control that produces it, so the builder
        // can see what the physics made of their choice instead of having to guess what
        void Note(string text)
        {
            if (headerRowPrefab == null) return;
            var row = Instantiate(headerRowPrefab, formContainer);
            Set(row, "Label", "    " + text);
        }

        void Header(string text)
        {
            var row = Instantiate(headerRowPrefab, formContainer);
            Set(row, "Label", text);
        }

        void Slider(string cap, float value, Action<float> set)
        {
            var row = Instantiate(sliderRowPrefab, formContainer);
            Set(row, "Label", cap);
            var valueText = Find<TMP_Text>(row, "Value");
            var slider = Find<UnityEngine.UI.Slider>(row, "Slider");
            if (slider == null) return;
            slider.minValue = 0f; slider.maxValue = 1f; slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(Mathf.Clamp01(value));
            if (valueText != null) valueText.text = Percent(value);
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v => { if (valueText != null) valueText.text = Percent(v); set(v); });
        }

        // Like Slider, but in real units.
        void RangedSlider(string cap, float value, float min, float max,
                          Func<float, string> format, Action<float> set)
        {
            var row = Instantiate(sliderRowPrefab, formContainer);
            Set(row, "Label", cap);
            var valueText = Find<TMP_Text>(row, "Value");
            var slider = Find<UnityEngine.UI.Slider>(row, "Slider");
            if (slider == null) return;
            slider.minValue = min; slider.maxValue = max; slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(Mathf.Clamp(value, min, max));
            if (valueText != null) valueText.text = format(value);
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v =>
            {
                if (valueText != null) valueText.text = format(v);
                set(v);
            });
        }

        void Dropdown(string cap, string[] options, int value, Action<int> onChange)
        {
            var row = Instantiate(dropdownRowPrefab, formContainer);
            Set(row, "Label", cap);
            var dd = Find<TMP_Dropdown>(row, "Dropdown");
            if (dd == null) return;
            dd.ClearOptions();
            dd.AddOptions(new List<string>(options));
            dd.SetValueWithoutNotify(Mathf.Clamp(value, 0, options.Length - 1));
            dd.onValueChanged.RemoveAllListeners();
            dd.onValueChanged.AddListener(v => onChange(v));
        }

        static string Percent(float v) => Mathf.RoundToInt(Mathf.Clamp01(v) * 100f) + "%";

        static void Set(GameObject root, string child, string value)
        {
            var t = Find<TMP_Text>(root, child);
            if (t != null) t.text = value;
        }

        static T Find<T>(GameObject root, string child) where T : Component
        {
            var t = root.transform.Find(child);
            return t != null ? t.GetComponent<T>() : null;
        }
    }
}
