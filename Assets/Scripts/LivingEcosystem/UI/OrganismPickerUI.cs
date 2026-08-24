using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // The organism picker (Design Document 5.1). Nine species as checkboxes, all on
    // by default, grouped by their level in the pyramid.
    //
    // Unchecking a species removes it entirely and the web reorganises around the
    // gap. The learner is always allowed to build an unbalanced ecosystem — breaking
    // it is the lesson — so the warnings under the list guide without blocking.
    public class OrganismPickerUI : MonoBehaviour
    {
        LivingReefController _reef;
        GameObject _root;
        TMP_Text _warnings;

        readonly Image[] _checkBoxes = new Image[SpeciesLibrary.Count];
        readonly GameObject[] _checkMarks = new GameObject[SpeciesLibrary.Count];
        readonly bool[] _draft = new bool[SpeciesLibrary.Count];

        public static OrganismPickerUI Create(Transform parent, LivingReefController reef)
        {
            var host = EcoUIKit.Empty(parent, "OrganismPicker");
            EcoUIKit.Stretch(EcoUIKit.Rect(host), 0f, 0f);

            var ui = host.AddComponent<OrganismPickerUI>();
            ui._reef = reef;
            ui.Build(host.transform);
            return ui;
        }

        void Build(Transform parent)
        {
            var scrim = EcoUIKit.Panel(parent, "Scrim", new Color(0f, 0f, 0f, 0.62f));
            EcoUIKit.Stretch(EcoUIKit.Rect(scrim), 0f, 0f);

            var card = EcoUIKit.Panel(scrim.transform, "Card", EcoUIKit.PanelBgSoft);
            var cardRect = EcoUIKit.Rect(card);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(900f, 1240f);

            var title = EcoUIKit.Text(card.transform, "Which organisms are present?", 32f, EcoUIKit.TextMain);
            var titleRect = EcoUIKit.Rect(title.gameObject);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-64f, 52f);
            titleRect.anchoredPosition = new Vector2(0f, -26f);

            var scrollHost = EcoUIKit.Empty(card.transform, "Scroll");
            var scrollRect = EcoUIKit.Rect(scrollHost);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(40f, 262f);
            scrollRect.offsetMax = new Vector2(-40f, -100f);

            var column = EcoUIKit.ScrollColumn(scrollHost.transform, out _);
            EcoUIKit.Stretch(EcoUIKit.Rect(column.parent.gameObject), 0f, 0f);

            BuildGroup(column, "Producers", TrophicLevel.Producer);
            BuildGroup(column, "Plant eaters", TrophicLevel.PlantEater);
            BuildGroup(column, "Hunters", TrophicLevel.Hunter, TrophicLevel.TopPredator);

            // Warnings sit under the list, where the choices are made.
            _warnings = EcoUIKit.Text(card.transform, "", 22f, new Color(0.96f, 0.76f, 0.36f));
            var warnRect = EcoUIKit.Rect(_warnings.gameObject);
            warnRect.anchorMin = new Vector2(0f, 0f);
            warnRect.anchorMax = new Vector2(1f, 0f);
            warnRect.pivot = new Vector2(0.5f, 0f);
            warnRect.sizeDelta = new Vector2(-64f, 148f);
            warnRect.anchoredPosition = new Vector2(0f, 96f);

            var cancel = EcoUIKit.Button(card.transform, "Cancel", 26f, EcoUIKit.Track,
                                         EcoUIKit.TextMain, Close);
            var cancelRect = EcoUIKit.Rect(cancel.gameObject);
            cancelRect.anchorMin = new Vector2(0f, 0f);
            cancelRect.anchorMax = new Vector2(0.5f, 0f);
            cancelRect.pivot = new Vector2(0f, 0f);
            cancelRect.sizeDelta = new Vector2(-40f, 62f);
            cancelRect.anchoredPosition = new Vector2(32f, 22f);

            var apply = EcoUIKit.Button(card.transform, "Apply", 26f, EcoUIKit.Accent,
                                        Color.white, Apply);
            var applyRect = EcoUIKit.Rect(apply.gameObject);
            applyRect.anchorMin = new Vector2(0.5f, 0f);
            applyRect.anchorMax = new Vector2(1f, 0f);
            applyRect.pivot = new Vector2(1f, 0f);
            applyRect.sizeDelta = new Vector2(-40f, 62f);
            applyRect.anchoredPosition = new Vector2(-32f, 22f);

            _root = scrim;
            _root.SetActive(false);
        }

        void BuildGroup(Transform column, string heading, params TrophicLevel[] levels)
        {
            var head = EcoUIKit.Empty(column, "Heading");
            head.AddComponent<LayoutElement>().preferredHeight = 44f;
            var text = EcoUIKit.Text(head.transform, heading, 26f, EcoUIKit.TextDim);
            EcoUIKit.Stretch(EcoUIKit.Rect(text.gameObject), 0f, 0f);

            var all = SpeciesLibrary.All;
            for (int i = 0; i < all.Length; i++)
            {
                bool inGroup = false;
                foreach (var level in levels) if (all[i].level == level) inGroup = true;
                if (!inGroup) continue;
                BuildRow(column, i);
            }
        }

        void BuildRow(Transform column, int species)
        {
            var def = SpeciesLibrary.Get(species);
            var row = EcoUIKit.Empty(column, "Pick_" + def.id);
            row.AddComponent<LayoutElement>().preferredHeight = 68f;

            var box = EcoUIKit.Panel(row.transform, "Box", EcoUIKit.Track);
            var boxRect = EcoUIKit.Rect(box);
            boxRect.anchorMin = new Vector2(0f, 0.5f);
            boxRect.anchorMax = new Vector2(0f, 0.5f);
            boxRect.pivot = new Vector2(0f, 0.5f);
            boxRect.sizeDelta = new Vector2(40f, 40f);
            boxRect.anchoredPosition = new Vector2(4f, 0f);
            _checkBoxes[species] = box.GetComponent<Image>();

            // A filled inner square, not a tick glyph. LiberationSans has no U+2713,
            // so "✓" renders as a missing-glyph box.
            var mark = EcoUIKit.Panel(box.transform, "Mark", Color.white);
            var markRect = EcoUIKit.Rect(mark);
            EcoUIKit.Stretch(markRect, 11f, 11f);
            _checkMarks[species] = mark;

            // Common name on the upper half, scientific name on the lower. These two
            // were anchored to overlapping bands and printed on top of each other.
            var label = EcoUIKit.Text(row.transform, def.commonName, 25f, EcoUIKit.TextMain);
            var labelRect = EcoUIKit.Rect(label.gameObject);
            labelRect.anchorMin = new Vector2(0f, 1f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.offsetMin = new Vector2(58f, -34f);
            labelRect.offsetMax = new Vector2(-8f, -4f);

            var sci = EcoUIKit.Text(row.transform, def.scientificName, 18f, EcoUIKit.TextDim);
            sci.fontStyle = FontStyles.Italic;
            var sciRect = EcoUIKit.Rect(sci.gameObject);
            sciRect.anchorMin = new Vector2(0f, 0f);
            sciRect.anchorMax = new Vector2(1f, 0f);
            sciRect.pivot = new Vector2(0f, 0f);
            sciRect.offsetMin = new Vector2(58f, 4f);
            sciRect.offsetMax = new Vector2(-8f, 26f);

            var button = row.AddComponent<Button>();
            var hit = row.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);
            button.targetGraphic = hit;
            int index = species;
            button.onClick.AddListener(() =>
            {
                _draft[index] = !_draft[index];
                PaintRow(index);
                PaintWarnings();
            });
        }

        void PaintRow(int species)
        {
            bool on = _draft[species];
            _checkBoxes[species].color = on ? EcoUIKit.Accent : EcoUIKit.Track;
            _checkMarks[species].SetActive(on);
        }

        void PaintWarnings()
        {
            var list = EcosystemWarnings.For(_draft);
            if (list.Count == 0)
            {
                _warnings.text = "";
                return;
            }
            var sb = new StringBuilder();
            for (int i = 0; i < list.Count && i < 3; i++)
                sb.Append("• ").Append(list[i]).Append(i < list.Count - 1 ? "\n" : "");
            _warnings.text = sb.ToString();
        }

        public void Open()
        {
            for (int i = 0; i < SpeciesLibrary.Count; i++)
            {
                _draft[i] = _reef.Sim.IsPresent(i);
                PaintRow(i);
            }
            PaintWarnings();
            _root.SetActive(true);
        }

        public void Close() => _root.SetActive(false);

        void Apply()
        {
            for (int i = 0; i < SpeciesLibrary.Count; i++)
                _reef.SetSpeciesPresent(i, _draft[i]);
            Close();
        }
    }
}
