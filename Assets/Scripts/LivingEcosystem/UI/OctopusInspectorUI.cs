using System.Text;
using CreateEnv.Ecosystem.Genetics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // Tap an octopus to see its genes, its traits against the population average, its
    // age, its generation and its parents (Milestone Step 2, Inspector).
    //
    // The card is deliberately explicit about the difference between genotype and
    // phenotype, because that distinction is the whole lesson: it prints the pair of
    // variants the animal carries AND what those variants produced, side by side.
    public class OctopusInspectorUI : MonoBehaviour
    {
        LivingReefController _reef;
        GameObject _root;
        TMP_Text _title, _subtitle, _body;
        RectTransform _content;
        Button _breedButton, _treeButton;

        int _shownId = -1;

        public System.Action<int> onBreedRequested;
        public System.Action<int> onTreeRequested;

        public int ShownId => _shownId;
        public bool IsOpen => _root != null && _root.activeSelf;

        public static OctopusInspectorUI Create(Transform parent, LivingReefController reef)
        {
            var host = EcoUIKit.Empty(parent, "OctopusInspector");
            EcoUIKit.Stretch(EcoUIKit.Rect(host), 0f, 0f);

            var ui = host.AddComponent<OctopusInspectorUI>();
            ui._reef = reef;
            ui.Build(host.transform);
            return ui;
        }

        void Build(Transform parent)
        {
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
            cardRect.sizeDelta = new Vector2(900f, 1280f);

            _title = EcoUIKit.Text(card.transform, "", 38f, EcoUIKit.TextMain);
            var titleRect = EcoUIKit.Rect(_title.gameObject);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-64f, 50f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);

            _subtitle = EcoUIKit.Text(card.transform, "", 24f, EcoUIKit.TextDim);
            var subRect = EcoUIKit.Rect(_subtitle.gameObject);
            subRect.anchorMin = new Vector2(0f, 1f);
            subRect.anchorMax = new Vector2(1f, 1f);
            subRect.pivot = new Vector2(0.5f, 1f);
            subRect.sizeDelta = new Vector2(-64f, 34f);
            subRect.anchoredPosition = new Vector2(0f, -74f);

            var scrollHost = EcoUIKit.Empty(card.transform, "Scroll");
            var scrollRect = EcoUIKit.Rect(scrollHost);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(40f, 190f);
            scrollRect.offsetMax = new Vector2(-40f, -124f);

            _content = EcoUIKit.ScrollColumn(scrollHost.transform, out _);
            EcoUIKit.Stretch(EcoUIKit.Rect(_content.parent.gameObject), 0f, 0f);

            _body = EcoUIKit.Text(_content, "", 24f, EcoUIKit.TextMain);
            _body.gameObject.AddComponent<LayoutElement>().minHeight = 60f;
            var fitter = _body.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _breedButton = EcoUIKit.Button(card.transform, "Breed this octopus", 24f,
                                           EcoUIKit.Accent, Color.white,
                                           () => onBreedRequested?.Invoke(_shownId));
            var breedRect = EcoUIKit.Rect(_breedButton.gameObject);
            breedRect.anchorMin = new Vector2(0f, 0f);
            breedRect.anchorMax = new Vector2(0.5f, 0f);
            breedRect.pivot = new Vector2(0f, 0f);
            breedRect.sizeDelta = new Vector2(-38f, 58f);
            breedRect.anchoredPosition = new Vector2(30f, 96f);

            _treeButton = EcoUIKit.Button(card.transform, "Family tree", 24f,
                                          EcoUIKit.Track, EcoUIKit.TextMain,
                                          () => onTreeRequested?.Invoke(_shownId));
            var treeRect = EcoUIKit.Rect(_treeButton.gameObject);
            treeRect.anchorMin = new Vector2(0.5f, 0f);
            treeRect.anchorMax = new Vector2(1f, 0f);
            treeRect.pivot = new Vector2(1f, 0f);
            treeRect.sizeDelta = new Vector2(-38f, 58f);
            treeRect.anchoredPosition = new Vector2(-30f, 96f);

            var close = EcoUIKit.Button(card.transform, "Close", 24f, EcoUIKit.Track,
                                        EcoUIKit.TextMain, Close);
            var closeRect = EcoUIKit.Rect(close.gameObject);
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(320f, 56f);
            closeRect.anchoredPosition = new Vector2(0f, 26f);

            _root = scrim;
            _root.SetActive(false);
        }

        public void Close() => _root.SetActive(false);

        public void Open(int agentId)
        {
            var pop = _reef.Octopuses;
            if (pop == null) return;

            _shownId = agentId;
            var agent = pop.ById(agentId);

            if (agent != null) ShowLiving(agent, pop);
            else if (pop.TryAncestor(agentId, out var record)) ShowAncestor(record, pop);
            else return;

            _root.SetActive(true);
        }

        void ShowLiving(OctopusAgent a, OctopusPopulation pop)
        {
            _title.text = a.name;
            _subtitle.text = $"Octopus vulgaris · {a.SexWord} · generation {a.generation}";

            _breedButton.gameObject.SetActive(true);
            bool canBreed = a.IsMature(OctopusPopulation.MaturityDays) && !a.IsBrooding;
            _breedButton.interactable = canBreed;
            var label = _breedButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = a.IsBrooding ? "Already brooding"
                           : canBreed ? "Breed this octopus"
                           : "Not mature yet";

            var sb = new StringBuilder();

            sb.Append("<b>Right now</b>\n").Append(a.StateWord).Append('\n');
            sb.Append("Age ").Append(Mathf.RoundToInt(a.ageDays)).Append(" days");
            if (a.IsMature(OctopusPopulation.MaturityDays)) sb.Append(" · mature");
            else sb.Append(" · matures at ").Append(Mathf.RoundToInt(OctopusPopulation.MaturityDays));
            sb.Append('\n');
            sb.Append("Condition ").Append(ConditionWord(a.energy)).Append("\n\n");

            // ── The genome, gene by gene ─────────────────────────────────────
            sb.Append("<b>Its genes</b>   <size=130%><b>").Append(a.genome.Notation())
              .Append("</b></size>\n");
            sb.Append("<color=#9FB2C4>Two copies of each gene, one from each parent.</color>\n\n");

            foreach (GeneId gene in System.Enum.GetValues(typeof(GeneId)))
            {
                sb.Append("<b>").Append(Genome.NameOf(gene)).Append("</b>  ")
                  .Append(a.genome.Notation(gene)).Append("  —  ")
                  .Append(a.traits.WordFor(gene)).Append('\n');
                sb.Append(Bar(a.traits.ValueOf(gene))).Append("  it\n");
                sb.Append(Bar(pop.AverageTrait(gene))).Append("  <color=#9FB2C4>the others</color>\n");

                if (a.genome.IsHeterozygous(gene) && gene == GeneId.Camouflage)
                    sb.Append("<color=#9FB2C4>Carries a hidden weak copy — it can reappear " +
                              "in its young.</color>\n");

                sb.Append('\n');
            }

            // ── Where it came from ───────────────────────────────────────────
            sb.Append("<b>Parents</b>\n");
            if (a.motherId < 0 && a.fatherId < 0)
                sb.Append(a.generation <= 1
                    ? "None here — it was among the first octopuses on this reef.\n\n"
                    : "None here — it settled from the plankton, spawned on another reef.\n\n");
            else
                sb.Append("Mother: ").Append(pop.NameOf(a.motherId))
                  .Append("\nFather: ").Append(pop.NameOf(a.fatherId)).Append("\n\n");

            sb.Append("<color=#9FB2C4>Real octopuses have thousands of genes; three are shown here " +
                      "for clarity. Mutation and growing up are both sped up so you can watch " +
                      "several generations in one sitting.</color>");

            _body.text = sb.ToString();
        }

        void ShowAncestor(AncestorRecord r, OctopusPopulation pop)
        {
            _title.text = r.Name;
            _subtitle.text = $"Octopus vulgaris · {r.Sex} · generation {r.generation} · died";
            _breedButton.gameObject.SetActive(false);

            var sb = new StringBuilder();
            sb.Append("<b>This octopus is dead</b>\n");
            sb.Append("It ").Append(OctopusAgent.CauseWord(r.Cause))
              .Append(" at ").Append(r.ageAtDeath).Append(" days old, on day ")
              .Append(r.diedOnDay).Append(".\n\n");

            sb.Append("<b>Its genes</b>\n");
            sb.Append("<size=130%><b>").Append(r.genome.Notation()).Append("</b></size>\n\n");
            foreach (GeneId gene in System.Enum.GetValues(typeof(GeneId)))
            {
                sb.Append("<b>").Append(Genome.NameOf(gene)).Append("</b>   ")
                  .Append(r.genome.Notation(gene)).Append("  ")
                  .Append(PunnettPrediction.PhenotypeWord(gene, r.genome.CopiesOf(gene)))
                  .Append('\n');
            }
            sb.Append('\n');

            sb.Append("<b>Parents</b>\n");
            if (r.motherId < 0 && r.fatherId < 0)
                sb.Append("None here — a founder or a settler.\n\n");
            else
                sb.Append("Mother: ").Append(pop.NameOf(r.motherId))
                  .Append("\nFather: ").Append(pop.NameOf(r.fatherId)).Append("\n\n");

            sb.Append("<color=#9FB2C4>Its genes are kept after death so the family tree still " +
                      "shows where its descendants came from.</color>");

            _body.text = sb.ToString();
        }

        static string ConditionWord(float energy) =>
            energy > 1.5f ? "well fed" : energy > 0.9f ? "feeding normally"
            : energy > 0.4f ? "thin" : "close to starving";

        // A plain text bar, so trait strength reads at a glance without a chart.
        static string Bar(float value01)
        {
            const int width = 12;
            int filled = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(value01) * width), 0, width);
            var sb = new StringBuilder("<color=#2BA84A>");
            for (int i = 0; i < filled; i++) sb.Append('=');
            sb.Append("</color><color=#2C3C4C>");
            for (int i = filled; i < width; i++) sb.Append('=');
            sb.Append("</color>");
            return sb.ToString();
        }
    }
}
