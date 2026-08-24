using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem
{
    // The Living Ecosystem section of Create Environment (Design Document 5),
    // appended below the current sections. No new screens, and no new art: it is
    // built from the same three row prefabs the environment editor already uses, so
    // it inherits that form's styling exactly.
    //
    // Every row is built once. Switching the feature off hides the rest of the
    // section rather than rebuilding the form, and the warning lines are rewritten in
    // place as choices are made. Nothing here destroys a row after the form is built,
    // which keeps it clear of Unity's end-of-frame Destroy and its double-row hazard.
    public static class EcosystemFormSection
    {
        // Called by EnvironmentEditorUI at the end of BuildForm.
        public static void Build(RectTransform container,
                                 GameObject headerRowPrefab,
                                 GameObject dropdownRowPrefab,
                                 GameObject sliderRowPrefab,
                                 EnvironmentProfile profile,
                                 Action edited)
        {
            if (container == null || profile == null) return;
            if (headerRowPrefab == null || dropdownRowPrefab == null || sliderRowPrefab == null) return;

            if (profile.ecosystem == null) profile.ecosystem = new EcosystemSettings();
            var eco = profile.ecosystem;
            eco.Clamp();

            // Everything below the on/off switch, hidden as one group when off.
            var body = new List<GameObject>(32);
            TMP_Text warningsLabel = null;

            void RefreshWarnings()
            {
                if (warningsLabel == null) return;
                var warnings = EcosystemWarnings.For(eco.present);
                if (warnings.Count == 0)
                {
                    warningsLabel.text = "This roster is balanced. Nothing obvious is missing.";
                    warningsLabel.color = new Color32(0x66, 0x78, 0x8C, 0xFF);
                    return;
                }
                var sb = new StringBuilder();
                for (int i = 0; i < warnings.Count && i < 3; i++)
                    sb.Append(i > 0 ? "\n" : "").Append("•  ").Append(warnings[i]);
                warningsLabel.text = sb.ToString();
                warningsLabel.color = new Color32(0xC2, 0x7A, 0x14, 0xFF);
            }

            void SetBodyVisible(bool visible)
            {
                for (int i = 0; i < body.Count; i++)
                    if (body[i] != null) body[i].SetActive(visible);
            }

            Header(container, headerRowPrefab, "Living Ecosystem");

            Dropdown(container, dropdownRowPrefab, "Enable living ecosystem",
                     new[] { "Off", "On" }, eco.enabled ? 1 : 0,
                     v =>
                     {
                         eco.enabled = v == 1;
                         SetBodyVisible(eco.enabled);
                         edited?.Invoke();
                     });

            body.Add(Note(container, headerRowPrefab,
                          "Algae grow, animals graze and hunt, and populations rise and crash. " +
                          "Turn this off and the environment behaves exactly as it does today."));

            // ── Which organisms are present? ─────────────────────────────────
            body.Add(Header(container, headerRowPrefab, "Which organisms are present?"));

            BuildGroup(container, headerRowPrefab, dropdownRowPrefab, eco, edited, RefreshWarnings,
                       body, "Producers", TrophicLevel.Producer);
            BuildGroup(container, headerRowPrefab, dropdownRowPrefab, eco, edited, RefreshWarnings,
                       body, "Plant eaters", TrophicLevel.PlantEater);
            BuildGroup(container, headerRowPrefab, dropdownRowPrefab, eco, edited, RefreshWarnings,
                       body, "Hunters", TrophicLevel.Hunter, TrophicLevel.TopPredator);

            // Guard rails, not blocks: the learner may always build an unbalanced
            // reef, but never without being told what it will do.
            var warningRow = Note(container, headerRowPrefab, "");
            warningsLabel = Find<TMP_Text>(warningRow, "Label");
            body.Add(warningRow);
            RefreshWarnings();

            // ── Conditions ───────────────────────────────────────────────────
            body.Add(Header(container, headerRowPrefab, "Conditions"));

            body.Add(Dropdown(container, dropdownRowPrefab, "Starting life",
                     EcosystemSettings.StartingLifeOptions, eco.startingLife,
                     v => { eco.startingLife = v; edited?.Invoke(); }));

            body.Add(Slider(container, sliderRowPrefab, "Water temperature",
                     EcosystemBounds.Temperature.Normalize(eco.temperatureC),
                     t =>
                     {
                         eco.temperatureC = EcosystemBounds.Temperature.Denormalize(t);
                         edited?.Invoke();
                         return eco.temperatureC.ToString("0.#") + " °C";
                     },
                     eco.temperatureC.ToString("0.#") + " °C"));

            body.Add(Slider(container, sliderRowPrefab, "Water acidity",
                     EcosystemBounds.Acidity.Normalize(eco.acidityPh),
                     t =>
                     {
                         eco.acidityPh = EcosystemBounds.Acidity.Denormalize(t);
                         edited?.Invoke();
                         return "pH " + eco.acidityPh.ToString("0.00");
                     },
                     "pH " + eco.acidityPh.ToString("0.00")));

            body.Add(Dropdown(container, dropdownRowPrefab, "Time speed",
                     EcosystemSettings.SpeedOptions, eco.speed,
                     v => { eco.speed = v; edited?.Invoke(); }));

            // ── Predict, observe, compare ────────────────────────────────────
            body.Add(Header(container, headerRowPrefab, EcosystemWarnings.PredictionQuestion));

            var options = new List<string> { "I would rather not say" };
            options.AddRange(EcosystemWarnings.PredictionOptions);
            body.Add(Dropdown(container, dropdownRowPrefab, "Your prediction",
                     options.ToArray(), Mathf.Clamp(eco.predictionIndex + 1, 0, options.Count - 1),
                     v => { eco.predictionIndex = v - 1; edited?.Invoke(); }));

            body.Add(Note(container, headerRowPrefab,
                     "One tick is one day. At Normal that is 2 seconds; at Fast, a quarter of a second. " +
                     "You can change any of this from inside the scene and watch it respond."));

            SetBodyVisible(eco.enabled);
        }

        static void BuildGroup(RectTransform container, GameObject headerPrefab, GameObject dropdownPrefab,
                               EcosystemSettings eco, Action edited, Action refreshWarnings,
                               List<GameObject> body, string heading, params TrophicLevel[] levels)
        {
            body.Add(Note(container, headerPrefab, heading));

            var all = SpeciesLibrary.All;
            for (int i = 0; i < all.Length; i++)
            {
                bool inGroup = false;
                foreach (var level in levels) if (all[i].level == level) inGroup = true;
                if (!inGroup) continue;

                int index = i;
                body.Add(Dropdown(container, dropdownPrefab, all[i].commonName,
                         new[] { "Removed", "Present" },
                         eco.IsPresent(index) ? 1 : 0,
                         v =>
                         {
                             if (eco.present != null && index < eco.present.Length)
                                 eco.present[index] = v == 1;
                             edited?.Invoke();
                             // The warning lines follow the choice as it is made.
                             refreshWarnings?.Invoke();
                         }));
            }
        }

        // ── Row builders (the editor's own prefabs, then bound) ──────────────
        static GameObject Header(RectTransform container, GameObject prefab, string text)
        {
            var row = UnityEngine.Object.Instantiate(prefab, container);
            SetText(row, "Label", text);
            return row;
        }

        // A wrapping note. Built from scratch rather than from the header prefab:
        // that prefab's label sits in a fixed-height row, so a note long enough to
        // wrap overflows and prints on top of whatever comes next. Here the text
        // component IS the row, so TextMeshPro reports its own preferred height to
        // the surrounding vertical layout and the row grows to fit.
        //
        // The font is borrowed from the prefab so it still matches the form.
        static GameObject Note(RectTransform container, GameObject prefab, string text, Color? colour = null)
        {
            var template = Find<TMP_Text>(prefab, "Label");

            var row = new GameObject("Note", typeof(RectTransform));
            row.transform.SetParent(container, false);

            var label = row.AddComponent<TextMeshProUGUI>();
            if (template != null)
            {
                label.font = template.font;
                label.fontSize = Mathf.Max(12f, template.fontSize * 0.78f);
            }
            else
            {
                label.fontSize = 14f;
            }

            label.text = text;
            label.color = colour ?? new Color32(0x66, 0x78, 0x8C, 0xFF);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.alignment = TextAlignmentOptions.TopLeft;
            label.margin = new Vector4(2f, 4f, 2f, 4f);
            label.raycastTarget = false;

            // The form sizes its rows from LayoutElement, so the row has to measure
            // its own wrapped text and report a height back.
            row.AddComponent<LayoutElement>();
            row.AddComponent<NoteAutoHeight>();

            return row;
        }

        static GameObject Dropdown(RectTransform container, GameObject prefab, string caption,
                                   string[] options, int value, Action<int> onChange)
        {
            var row = UnityEngine.Object.Instantiate(prefab, container);
            SetText(row, "Label", caption);

            var dd = Find<TMP_Dropdown>(row, "Dropdown");
            if (dd == null) return row;

            dd.ClearOptions();
            dd.AddOptions(new List<string>(options));
            dd.SetValueWithoutNotify(Mathf.Clamp(value, 0, options.Length - 1));
            dd.onValueChanged.RemoveAllListeners();
            dd.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return row;
        }

        // Like the editor's own slider row, but the value text is formatted by the
        // caller so it can read "24 °C" or "pH 8.10" instead of a percentage.
        static GameObject Slider(RectTransform container, GameObject prefab, string caption,
                                 float normalized, Func<float, string> onChange, string initialText)
        {
            var row = UnityEngine.Object.Instantiate(prefab, container);
            SetText(row, "Label", caption);

            var valueText = Find<TMP_Text>(row, "Value");
            var slider = Find<Slider>(row, "Slider");
            if (slider == null) return row;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(Mathf.Clamp01(normalized));
            if (valueText != null) valueText.text = initialText;

            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v =>
            {
                string text = onChange != null ? onChange(v) : null;
                if (valueText != null && text != null) valueText.text = text;
            });
            return row;
        }

        static void SetText(GameObject root, string child, string value)
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
