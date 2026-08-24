using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // The live energy pyramid: four tiers whose widths rise and fall with real
    // biomass (Design Document 1.1). The pyramid narrows because energy is lost at
    // every step — about a tenth reaches the next level, though it varies a lot
    // (Literature 2.2). It can go top-heavy, and then inverted, as a reef collapses,
    // which is the whole point of drawing it live.
    public class EnergyPyramidUI : MonoBehaviour
    {
        struct Tier
        {
            public RectTransform bar;
            public Image fill;
            public TMP_Text label;
        }

        readonly Tier[] _tiers = new Tier[4];

        static readonly TrophicLevel[] Levels =
        {
            TrophicLevel.TopPredator,
            TrophicLevel.Hunter,
            TrophicLevel.PlantEater,
            TrophicLevel.Producer,
        };

        static readonly string[] LevelNames =
        {
            "Top predator", "Hunter", "Plant eaters", "Producers",
        };

        static readonly Color[] LevelColours =
        {
            new Color(0.42f, 0.52f, 0.62f),
            new Color(0.80f, 0.42f, 0.62f),
            new Color(0.90f, 0.58f, 0.32f),
            new Color(0.44f, 0.76f, 0.42f),
        };

        public static EnergyPyramidUI Create(Transform column)
        {
            var host = EcoUIKit.Empty(column, "EnergyPyramid");
            host.AddComponent<LayoutElement>().preferredHeight = 210f;

            var ui = host.AddComponent<EnergyPyramidUI>();
            ui.Build(host.transform);
            return ui;
        }

        void Build(Transform host)
        {
            var caption = EcoUIKit.Text(host, "About a tenth passes up each step", 20f, EcoUIKit.TextDim);
            var capRect = EcoUIKit.Rect(caption.gameObject);
            capRect.anchorMin = new Vector2(0f, 1f);
            capRect.anchorMax = new Vector2(1f, 1f);
            capRect.pivot = new Vector2(0f, 1f);
            capRect.sizeDelta = new Vector2(0f, 28f);
            capRect.anchoredPosition = Vector2.zero;

            for (int i = 0; i < _tiers.Length; i++)
            {
                var rowGo = EcoUIKit.Empty(host, "Tier" + i);
                var rowRect = EcoUIKit.Rect(rowGo);
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.sizeDelta = new Vector2(0f, 40f);
                rowRect.anchoredPosition = new Vector2(0f, -34f - i * 42f);

                // Bars grow from the centre, so the shape reads as a pyramid.
                var barGo = EcoUIKit.Panel(rowGo.transform, "Bar", LevelColours[i]);
                var bar = EcoUIKit.Rect(barGo);
                bar.anchorMin = new Vector2(0.5f, 0f);
                bar.anchorMax = new Vector2(0.5f, 1f);
                bar.pivot = new Vector2(0.5f, 0.5f);
                bar.sizeDelta = new Vector2(10f, -10f);
                bar.anchoredPosition = Vector2.zero;

                var label = EcoUIKit.Text(rowGo.transform, LevelNames[i], 20f,
                                          EcoUIKit.TextMain, TextAlignmentOptions.Center);
                EcoUIKit.Stretch(EcoUIKit.Rect(label.gameObject), 4f, 0f);

                _tiers[i] = new Tier
                {
                    bar = bar,
                    fill = barGo.GetComponent<Image>(),
                    label = label,
                };
            }
        }

        public void Refresh(EcosystemSimulation sim)
        {
            // Scale every tier against the producers, because that is what the shape
            // is meant to communicate: how much of the base survives each step up.
            float producers = Mathf.Max(0.001f, sim.BiomassAtLevel(TrophicLevel.Producer));
            float widest = ((RectTransform)transform).rect.width - 20f;
            if (widest <= 0f) widest = 480f;

            for (int i = 0; i < _tiers.Length; i++)
            {
                float biomass = sim.BiomassAtLevel(Levels[i]);
                float share = biomass / producers;

                // A square-root scale so a tier holding a tenth of the base still
                // reads at a third of the width. A linear scale makes every level
                // above the producers a hairline and teaches nothing.
                float width = Mathf.Clamp(Mathf.Sqrt(Mathf.Clamp01(share)) * widest, 8f, widest);

                var size = _tiers[i].bar.sizeDelta;
                _tiers[i].bar.sizeDelta = new Vector2(Mathf.Lerp(size.x, width, 0.35f), size.y);

                var colour = LevelColours[i];
                _tiers[i].fill.color = biomass > 0.001f
                    ? colour
                    : new Color(colour.r, colour.g, colour.b, 0.18f);

                string label = biomass > 0.001f
                    ? $"{LevelNames[i]}  {biomass:0.#}"
                    : LevelNames[i] + "  —";
                if (_tiers[i].label.text != label) _tiers[i].label.text = label;
            }
        }
    }
}
