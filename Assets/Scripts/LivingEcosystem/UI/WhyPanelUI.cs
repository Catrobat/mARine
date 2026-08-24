using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // The Why panel (Design Document 6.2). Every change the learner can see must
    // have an explanation available in one tap. Answers three questions in plain
    // language: what is happening, why is it happening, what happens next.
    //
    // Doubles as the organism inspector: tapping a species row opens its card, which
    // extends the info card the app already uses rather than inventing a new one.
    public class WhyPanelUI : MonoBehaviour
    {
        LivingReefController _reef;
        GameObject _root;
        TMP_Text _title, _body;
        RectTransform _content;

        public static WhyPanelUI Create(Transform parent, LivingReefController reef)
        {
            var host = EcoUIKit.Empty(parent, "WhyPanel");
            var rect = EcoUIKit.Rect(host);
            EcoUIKit.Stretch(rect, 0f, 0f);

            var ui = host.AddComponent<WhyPanelUI>();
            ui._reef = reef;
            ui.Build(host.transform);
            return ui;
        }

        void Build(Transform parent)
        {
            // Dimmed backdrop; tapping it closes.
            var scrim = EcoUIKit.Panel(parent, "Scrim", new Color(0f, 0f, 0f, 0.62f));
            EcoUIKit.Stretch(EcoUIKit.Rect(scrim), 0f, 0f);
            var scrimButton = scrim.AddComponent<Button>();
            scrimButton.targetGraphic = scrim.GetComponent<Image>();
            scrimButton.onClick.AddListener(Close);

            var card = EcoUIKit.Panel(scrim.transform, "Card", EcoUIKit.PanelBgSoft);
            var cardRect = EcoUIKit.Rect(card);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(880f, 1080f);

            _title = EcoUIKit.Text(card.transform, "Why?", 36f, EcoUIKit.TextMain);
            var titleRect = EcoUIKit.Rect(_title.gameObject);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-64f, 56f);
            titleRect.anchoredPosition = new Vector2(0f, -28f);

            var scrollHost = EcoUIKit.Empty(card.transform, "Scroll");
            var scrollRect = EcoUIKit.Rect(scrollHost);
            scrollRect.anchorMin = new Vector2(0f, 0f);
            scrollRect.anchorMax = new Vector2(1f, 1f);
            scrollRect.offsetMin = new Vector2(40f, 116f);
            scrollRect.offsetMax = new Vector2(-40f, -104f);

            _content = EcoUIKit.ScrollColumn(scrollHost.transform, out _);
            EcoUIKit.Stretch(EcoUIKit.Rect(_content.parent.gameObject), 0f, 0f);

            _body = EcoUIKit.Text(_content, "", 25f, EcoUIKit.TextMain);
            var bodyFitter = _body.gameObject.AddComponent<ContentSizeFitter>();
            bodyFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _body.gameObject.AddComponent<LayoutElement>().minHeight = 60f;

            var close = EcoUIKit.Button(card.transform, "Close", 26f, EcoUIKit.Accent, Color.white, Close);
            var closeRect = EcoUIKit.Rect(close.gameObject);
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(320f, 62f);
            closeRect.anchoredPosition = new Vector2(0f, 24f);

            _root = scrim;
            _root.SetActive(false);
        }

        public void Close() => _root.SetActive(false);

        // The three-question explanation of the reef's current state.
        public void Open()
        {
            _title.text = "Why?";
            var sb = new StringBuilder();
            var reasons = _reef.Reasons.Active;

            if (reasons.Count == 0)
            {
                sb.Append("Nothing notable is happening yet. Give it a few days, or change something.");
            }
            else
            {
                for (int i = 0; i < reasons.Count; i++)
                {
                    var r = reasons[i];
                    sb.Append("<b>What is happening</b>\n").Append(r.whatIsHappening).Append("\n\n");
                    sb.Append("<b>Why</b>\n").Append(r.whyItIsHappening).Append("\n\n");
                    sb.Append("<b>What happens next</b>\n").Append(r.whatHappensNext);
                    if (i < reasons.Count - 1) sb.Append("\n\n<color=#5A6C7C>———</color>\n\n");
                }
            }

            _body.text = sb.ToString();
            _root.SetActive(true);
        }

        // The organism inspector: what this species is, and what it is doing here.
        public void OpenSpecies(int species)
        {
            var def = SpeciesLibrary.Get(species);
            if (def == null) return;

            var sim = _reef.Sim;
            _title.text = def.commonName;

            var sb = new StringBuilder();
            sb.Append("<i>").Append(def.scientificName).Append("</i>\n\n");

            sb.Append("<b>In your reef right now</b>\n");
            if (!sim.IsPresent(species))
            {
                sb.Append("Removed. It is not part of this ecosystem.\n\n");
            }
            else
            {
                float amount = sim.DisplayAmount(species);
                sb.Append(def.IsProducer ? "Biomass: " : "Number: ")
                  .Append(Mathf.RoundToInt(amount));

                int dir = _reef.Reasons.TrendOf(species);
                sb.Append(dir > 0 ? "  (rising)" : dir < 0 ? "  (falling)" : "  (steady)");

                if (sim.pools[species].bleached) sb.Append("\nBleached — the water is too warm.");
                if (!def.IsProducer && sim.pools[species].daysHungry >= 5)
                    sb.Append("\nUnderfed for ").Append(sim.pools[species].daysHungry).Append(" days.");
                sb.Append("\n\n");
            }

            sb.Append("<b>Level</b>\n").Append(LevelName(def.level)).Append("\n\n");
            sb.Append("<b>What it looks like</b>\n").Append(def.appearance).Append("\n\n");
            sb.Append("<b>Where it lives</b>\n").Append(def.habitat).Append("\n\n");
            sb.Append("<b>What it eats</b>\n").Append(def.diet).Append("\n\n");
            sb.Append("<b>Its role here</b>\n").Append(def.roleInModel).Append("\n\n");

            // Conservation status and regional protection as separate labelled
            // fields — pre-release checklist item 3.
            sb.Append("<b>IUCN Red List</b>\n").Append(def.iucnStatus).Append("\n\n");
            if (!string.IsNullOrEmpty(def.regionalStatus))
                sb.Append("<b>Regionally</b>\n").Append(def.regionalStatus).Append("\n\n");

            sb.Append("<b>Worth knowing</b>\n").Append(def.oneThingWorthTelling).Append("\n\n");

            if (!string.IsNullOrEmpty(def.simplificationNote))
                sb.Append("<color=#9FB2C4><b>What this simulation simplifies</b>\n")
                  .Append(def.simplificationNote).Append("</color>");

            _body.text = sb.ToString();
            _root.SetActive(true);
        }

        static string LevelName(TrophicLevel level) => level switch
        {
            TrophicLevel.Producer    => "Producer — it makes its own food from sunlight",
            TrophicLevel.PlantEater  => "Plant eater — it eats producers",
            TrophicLevel.Hunter      => "Hunter — it eats plant eaters",
            _                        => "Top predator — nothing here regularly hunts it",
        };
    }
}
