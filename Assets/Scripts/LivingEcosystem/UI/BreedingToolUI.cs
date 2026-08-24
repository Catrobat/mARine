using System.Collections.Generic;
using System.Text;
using CreateEnv.Ecosystem.Genetics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // The breeding tool: pick two octopuses, see the prediction, breed them, compare
    // the result (Milestone Step 2, Breeding tool).
    //
    // The prediction comes from PunnettPrediction, which derives it from the same
    // rule Genome.Inherit actually uses, so the grid cannot drift away from the maths
    // underneath it.
    //
    // The lesson hidden in the tool is the mismatch: a grid predicts ratios over many
    // offspring, and a brood of two to four will frequently not match. Letting the
    // learner see that teaches more about probability than a tidy result would, so
    // the tool says it in advance and then shows what actually happened.
    public class BreedingToolUI : MonoBehaviour
    {
        LivingReefController _reef;
        GameObject _root;
        TMP_Text _title, _body;
        RectTransform _content;
        Button _actionButton;

        int _motherId = -1, _fatherId = -1;
        string _outcome;

        public static BreedingToolUI Create(Transform parent, LivingReefController reef)
        {
            var host = EcoUIKit.Empty(parent, "BreedingTool");
            EcoUIKit.Stretch(EcoUIKit.Rect(host), 0f, 0f);

            var ui = host.AddComponent<BreedingToolUI>();
            ui._reef = reef;
            ui.Build(host.transform);
            return ui;
        }

        void Build(Transform parent)
        {
            var scrim = EcoUIKit.Panel(parent, "Scrim", new Color(0f, 0f, 0f, 0.66f));
            EcoUIKit.Stretch(EcoUIKit.Rect(scrim), 0f, 0f);

            var card = EcoUIKit.Panel(scrim.transform, "Card", EcoUIKit.PanelBgSoft);
            var cardRect = EcoUIKit.Rect(card);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(920f, 1320f);

            _title = EcoUIKit.Text(card.transform, "Breeding", 34f, EcoUIKit.TextMain);
            var titleRect = EcoUIKit.Rect(_title.gameObject);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-60f, 48f);
            titleRect.anchoredPosition = new Vector2(0f, -26f);

            var scrollHost = EcoUIKit.Empty(card.transform, "Scroll");
            var scrollRect = EcoUIKit.Rect(scrollHost);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(40f, 182f);
            scrollRect.offsetMax = new Vector2(-40f, -100f);

            _content = EcoUIKit.ScrollColumn(scrollHost.transform, out _);
            EcoUIKit.Stretch(EcoUIKit.Rect(_content.parent.gameObject), 0f, 0f);

            _body = EcoUIKit.Text(_content, "", 23f, EcoUIKit.TextMain);
            _body.gameObject.AddComponent<LayoutElement>().minHeight = 60f;
            var fitter = _body.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _actionButton = EcoUIKit.Button(card.transform, "Breed them", 26f,
                                            EcoUIKit.Accent, Color.white, Breed);
            var actionRect = EcoUIKit.Rect(_actionButton.gameObject);
            actionRect.anchorMin = new Vector2(0f, 0f);
            actionRect.anchorMax = new Vector2(1f, 0f);
            actionRect.pivot = new Vector2(0.5f, 0f);
            actionRect.sizeDelta = new Vector2(-60f, 60f);
            actionRect.anchoredPosition = new Vector2(0f, 92f);

            var close = EcoUIKit.Button(card.transform, "Close", 24f, EcoUIKit.Track,
                                        EcoUIKit.TextMain, Close);
            var closeRect = EcoUIKit.Rect(close.gameObject);
            closeRect.anchorMin = new Vector2(0.5f, 0f);
            closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.sizeDelta = new Vector2(320f, 54f);
            closeRect.anchoredPosition = new Vector2(0f, 24f);

            _root = scrim;
            _root.SetActive(false);
        }

        public void Close()
        {
            _root.SetActive(false);
            // Stop lighting them once the learner has left the tool; a brooding
            // female keeps her own glow from the renderer.
            _reef.chosenFemaleId = -1;
            _reef.chosenMaleId = -1;
        }

        void PushSelection()
        {
            _reef.chosenFemaleId = _motherId;
            _reef.chosenMaleId = _fatherId;
        }

        // Opens with one parent already chosen; the other is picked from the list.
        public void Open(int firstParentId)
        {
            var pop = _reef.Octopuses;
            if (pop == null) return;

            var first = pop.ById(firstParentId);
            _motherId = _fatherId = -1;
            if (first != null)
            {
                if (first.sex == Sex.Female) _motherId = first.id;
                else _fatherId = first.id;
            }

            _outcome = null;
            PushSelection();
            Refresh();
            _root.SetActive(true);
        }

        void Refresh()
        {
            var pop = _reef.Octopuses;
            var mother = _motherId >= 0 ? pop.ById(_motherId) : null;
            var father = _fatherId >= 0 ? pop.ById(_fatherId) : null;

            _title.text = "Breeding";

            var sb = new StringBuilder();

            if (_outcome != null)
            {
                // The card already has a Close at the bottom. Turning the action
                // button into a second one just asks the learner which Close to press.
                _actionButton.gameObject.SetActive(false);
                sb.Append(_outcome);
                _body.text = sb.ToString();
                return;
            }

            _actionButton.gameObject.SetActive(true);

            // ── Who is chosen ────────────────────────────────────────────────
            sb.Append("<b>The pair</b>\n");
            sb.Append("Mother: ").Append(mother != null ? $"{mother.name}  ({mother.genome.Notation()})" : "— choose below —").Append('\n');
            sb.Append("Father: ").Append(father != null ? $"{father.name}  ({father.genome.Notation()})" : "— choose below —").Append("\n\n");

            if (mother == null || father == null)
            {
                sb.Append("<b>Choose the other parent</b>\n");
                sb.Append("<color=#9FB2C4>Both must be mature, and one of each sex.</color>\n\n");
                BuildCandidateButtons(mother == null ? Sex.Female : Sex.Male);
                SetAction("Breed them", Breed, false);
                _body.text = sb.ToString();
                return;
            }

            ClearCandidates();

            // ── The prediction ───────────────────────────────────────────────
            sb.Append("<b>What their young could inherit</b>\n\n");

            foreach (GeneId gene in System.Enum.GetValues(typeof(GeneId)))
            {
                sb.Append("<b>").Append(Genome.NameOf(gene)).Append("</b>   ")
                  .Append(mother.genome.Notation(gene)).Append("  x  ")
                  .Append(father.genome.Notation(gene)).Append('\n');

                sb.Append(GridArt(mother.genome, father.genome, gene));

                foreach (var outcome in PunnettPrediction.Outcomes(mother.genome, father.genome, gene))
                {
                    sb.Append("   ").Append(outcome.notation).Append("  ")
                      .Append(Mathf.RoundToInt(outcome.probability * 100f)).Append("%   ")
                      .Append("<color=#9FB2C4>").Append(outcome.phenotype).Append("</color>\n");
                }
                sb.Append("   ratio ").Append(PunnettPrediction.RatioText(mother.genome, father.genome, gene))
                  .Append("\n\n");
            }

            sb.Append("<color=#9FB2C4>").Append(PunnettPrediction.SmallBroodCaveat).Append("</color>\n\n");
            sb.Append("<color=#C27A14>Both parents die after breeding. This is real, and it is " +
                      "what makes an octopus generation short enough to watch.</color>");

            SetAction("Breed them", Breed, true);
            _body.text = sb.ToString();
        }

        // The classic two-by-two, drawn in text so it reads on any screen width.
        static string GridArt(Genome mother, Genome father, GeneId gene)
        {
            char upper = Genome.LetterOf(gene);
            char lower = char.ToLowerInvariant(upper);

            string Allele(Genome g, int copy) => g.Allele(gene, copy) ? upper.ToString() : lower.ToString();

            string f0 = Allele(father, 0), f1 = Allele(father, 1);
            string m0 = Allele(mother, 0), m1 = Allele(mother, 1);

            string Cell(string a, string b)
            {
                // Always written uppercase-first, so the same genotype always reads
                // the same way.
                bool aUpper = a[0] == upper;
                bool bUpper = b[0] == upper;
                return aUpper == bUpper ? a + b : upper.ToString() + lower.ToString();
            }

            var sb = new StringBuilder();
            sb.Append("<color=#9FB2C4><mspace=1.1em>");
            sb.Append("      ").Append(f0).Append("   ").Append(f1).Append('\n');
            sb.Append("   ").Append(m0).Append("  ").Append(Cell(m0, f0)).Append("  ").Append(Cell(m0, f1)).Append('\n');
            sb.Append("   ").Append(m1).Append("  ").Append(Cell(m1, f0)).Append("  ").Append(Cell(m1, f1)).Append('\n');
            sb.Append("</mspace></color>");
            return sb.ToString();
        }

        readonly List<GameObject> _candidates = new List<GameObject>(8);

        void ClearCandidates()
        {
            for (int i = 0; i < _candidates.Count; i++)
                if (_candidates[i] != null) Destroy(_candidates[i]);
            _candidates.Clear();
        }

        void BuildCandidateButtons(Sex wanted)
        {
            ClearCandidates();
            var pop = _reef.Octopuses;
            if (pop == null) return;

            foreach (var a in pop.agents)
            {
                if (!a.IsAlive || a.sex != wanted) continue;
                if (!a.IsMature(OctopusPopulation.MaturityDays) || a.IsBrooding) continue;

                int id = a.id;
                var button = EcoUIKit.Button(_content, $"{a.name}   {a.genome.Notation()}", 23f,
                                             EcoUIKit.Track, EcoUIKit.TextMain,
                                             () =>
                                             {
                                                 if (wanted == Sex.Female) _motherId = id;
                                                 else _fatherId = id;
                                                 PushSelection();
                                                 Refresh();
                                             });
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 56f;
                _candidates.Add(button.gameObject);
            }

            if (_candidates.Count == 0)
            {
                var none = EcoUIKit.Text(_content,
                    wanted == Sex.Female
                        ? "No mature female is available right now."
                        : "No mature male is available right now.",
                    22f, new Color(0.96f, 0.76f, 0.36f));
                none.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
                _candidates.Add(none.gameObject);
            }
        }

        void SetAction(string label, System.Action action, bool enabled)
        {
            var text = _actionButton.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = label;
            _actionButton.interactable = enabled;
            _actionButton.onClick.RemoveAllListeners();
            if (action != null) _actionButton.onClick.AddListener(() => action());
        }

        void Breed()
        {
            var pop = _reef.Octopuses;
            var mother = pop?.ById(_motherId);
            var father = pop?.ById(_fatherId);
            if (mother == null || father == null) return;

            // Record what was predicted before the dice are rolled, so the comparison
            // afterwards is honest.
            var predicted = new Dictionary<GeneId, List<PunnettPrediction.Outcome>>();
            foreach (GeneId gene in System.Enum.GetValues(typeof(GeneId)))
                predicted[gene] = PunnettPrediction.Outcomes(mother.genome, father.genome, gene);

            string motherName = mother.name, fatherName = father.name;
            int generation = mother.generation + 1;

            if (!pop.StartBrood(mother, father, _reef.Sim, SpeciesLibrary.Get(SpeciesLibrary.Octopus)))
            {
                _outcome = "They could not breed. Both must be mature, and she must not already be brooding.";
                Refresh();
                return;
            }

            var sb = new StringBuilder();
            sb.Append("<size=115%><b>").Append(motherName).Append(" and ").Append(fatherName)
              .Append(" have mated</b></size>\n\n");

            sb.Append(motherName).Append(" has gone to her den. She will guard her eggs for about a " +
                      "month and will not eat at all while she does — and within days of them " +
                      "hatching, she dies. ").Append(fatherName)
              .Append(" is already fading.\n\n");

            sb.Append("<b>Watch for generation ").Append(generation).Append("</b>\n");
            sb.Append("Tap one of the young when they hatch and see what it inherited.\n\n");

            sb.Append("<b>You predicted</b>\n");
            foreach (var pair in predicted)
            {
                sb.Append("<color=#9FB2C4>").Append(Genome.NameOf(pair.Key)).Append("</color>   ");
                for (int i = 0; i < pair.Value.Count; i++)
                {
                    if (i > 0) sb.Append(" · ");
                    sb.Append(pair.Value[i].notation).Append(' ')
                      .Append(Mathf.RoundToInt(pair.Value[i].probability * 100f)).Append('%');
                }
                sb.Append('\n');
            }
            sb.Append("\n<color=#9FB2C4>A brood of two to four often will not match those numbers. " +
                      "That is chance, not a mistake.</color>");

            _outcome = sb.ToString();
            Refresh();
        }
    }
}
