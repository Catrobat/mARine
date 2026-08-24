using System.Collections.Generic;
using System.Text;
using CreateEnv.Ecosystem.Genetics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CreateEnv.Ecosystem.UI
{
    // The family tree (Design Document 4.4).
    //
    // A column per generation, living animals solid and dead ones faded, the selected
    // animal highlighted, and a toggle that colours by gene instead of by status so
    // the learner can literally watch a variant spread down the generations or vanish.
    //
    // This is what converts a statistic into a story. A chart showing a gene rising
    // from 30% to 70% is information; a tree showing that Nova, Tide and their six
    // descendants all carry their grandmother's camouflage variant while the others
    // died out is something a learner will remember and retell.
    public class FamilyTreeUI : MonoBehaviour
    {
        LivingReefController _reef;
        GameObject _root;
        TMP_Text _title, _body;
        RectTransform _content;
        Button[] _colourButtons;

        int _selectedId = -1;
        int _colourMode;   // 0 = by status, 1..3 = by gene

        public System.Action<int> onAnimalTapped;

        static readonly string[] ColourModes = { "Alive / dead", "Camouflage", "Body size", "Heat tolerance" };

        public static FamilyTreeUI Create(Transform parent, LivingReefController reef)
        {
            var host = EcoUIKit.Empty(parent, "FamilyTree");
            EcoUIKit.Stretch(EcoUIKit.Rect(host), 0f, 0f);

            var ui = host.AddComponent<FamilyTreeUI>();
            ui._reef = reef;
            ui.Build(host.transform);
            return ui;
        }

        void Build(Transform parent)
        {
            var scrim = EcoUIKit.Panel(parent, "Scrim", new Color(0f, 0f, 0f, 0.68f));
            EcoUIKit.Stretch(EcoUIKit.Rect(scrim), 0f, 0f);

            var card = EcoUIKit.Panel(scrim.transform, "Card", EcoUIKit.PanelBgSoft);
            var cardRect = EcoUIKit.Rect(card);
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.sizeDelta = new Vector2(940f, 1360f);

            _title = EcoUIKit.Text(card.transform, "Family tree", 34f, EcoUIKit.TextMain);
            var titleRect = EcoUIKit.Rect(_title.gameObject);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.sizeDelta = new Vector2(-60f, 48f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);

            // Colour-by toggle.
            var strip = EcoUIKit.Empty(card.transform, "ColourBy");
            var stripRect = EcoUIKit.Rect(strip);
            stripRect.anchorMin = new Vector2(0f, 1f);
            stripRect.anchorMax = new Vector2(1f, 1f);
            stripRect.pivot = new Vector2(0.5f, 1f);
            stripRect.sizeDelta = new Vector2(-56f, 46f);
            stripRect.anchoredPosition = new Vector2(0f, -80f);

            var group = strip.AddComponent<HorizontalLayoutGroup>();
            group.spacing = 6f;
            group.childForceExpandWidth = true;
            group.childForceExpandHeight = true;
            group.childControlWidth = true;
            group.childControlHeight = true;

            _colourButtons = new Button[ColourModes.Length];
            for (int i = 0; i < ColourModes.Length; i++)
            {
                int mode = i;
                _colourButtons[i] = EcoUIKit.Button(strip.transform, ColourModes[i], 20f,
                                                    EcoUIKit.Track, EcoUIKit.TextDim,
                                                    () => { _colourMode = mode; Refresh(); });
            }

            var scrollHost = EcoUIKit.Empty(card.transform, "Scroll");
            var scrollRect = EcoUIKit.Rect(scrollHost);
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(38f, 112f);
            scrollRect.offsetMax = new Vector2(-38f, -142f);

            _content = EcoUIKit.ScrollColumn(scrollHost.transform, out _);
            EcoUIKit.Stretch(EcoUIKit.Rect(_content.parent.gameObject), 0f, 0f);

            _body = EcoUIKit.Text(_content, "", 22f, EcoUIKit.TextMain);
            _body.gameObject.AddComponent<LayoutElement>().minHeight = 60f;
            var fitter = _body.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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

        public void Open(int selectedId)
        {
            _selectedId = selectedId;
            Refresh();
            _root.SetActive(true);
        }

        struct Entry
        {
            public int id;
            public string name;
            public Genome genome;
            public int generation;
            public bool alive;
            public Sex sex;
            public CauseOfDeath cause;
            public int motherId, fatherId;
        }

        void Refresh()
        {
            for (int i = 0; i < _colourButtons.Length; i++)
            {
                var img = _colourButtons[i].GetComponent<Image>();
                if (img != null) img.color = i == _colourMode ? EcoUIKit.Accent : EcoUIKit.Track;
                var label = _colourButtons[i].GetComponentInChildren<TMP_Text>();
                if (label != null) label.color = i == _colourMode ? Color.white : EcoUIKit.TextDim;
            }

            var pop = _reef.Octopuses;
            if (pop == null) { _body.text = "No octopuses on this reef."; return; }

            // Gather the living and the remembered dead into one list.
            var entries = new List<Entry>(pop.agents.Count + pop.ancestors.Count);
            foreach (var a in pop.agents)
            {
                if (!a.IsAlive) continue;
                entries.Add(new Entry
                {
                    id = a.id, name = a.name, genome = a.genome, generation = a.generation,
                    alive = true, sex = a.sex, cause = CauseOfDeath.StillAlive,
                    motherId = a.motherId, fatherId = a.fatherId,
                });
            }
            foreach (var r in pop.ancestors)
            {
                entries.Add(new Entry
                {
                    id = r.id, name = r.Name, genome = r.genome, generation = r.generation,
                    alive = false, sex = r.Sex, cause = r.Cause,
                    motherId = r.motherId, fatherId = r.fatherId,
                });
            }

            if (entries.Count == 0) { _body.text = "No octopuses yet."; return; }

            int newest = pop.HighestGeneration;
            int oldestShown = Mathf.Max(1, newest - OctopusPopulation.GenerationCap + 1);

            var sb = new StringBuilder();
            sb.Append("<color=#9FB2C4>")
              .Append(pop.AliveCount).Append(" alive · ")
              .Append(pop.TotalBorn).Append(" born · ")
              .Append(pop.TotalDied).Append(" died · generation ").Append(newest)
              .Append("</color>\n\n");

            // Anything older than the cap is one summary row, so a long session's tree
            // stays readable instead of scrolling for ever.
            int olderCount = 0;
            foreach (var e in entries) if (e.generation < oldestShown) olderCount++;
            if (olderCount > 0)
            {
                sb.Append("<color=#66788C>Generations 1 to ").Append(oldestShown - 1)
                  .Append(" — ").Append(olderCount).Append(" earlier octopuses, no longer shown")
                  .Append("</color>\n\n");
            }

            for (int gen = oldestShown; gen <= newest; gen++)
            {
                var inGeneration = new List<Entry>();
                foreach (var e in entries) if (e.generation == gen) inGeneration.Add(e);
                if (inGeneration.Count == 0) continue;

                sb.Append("<b>Generation ").Append(gen).Append("</b>\n");

                foreach (var e in inGeneration)
                {
                    string colour = ColourFor(e);
                    string marker = e.alive ? "●" : "○";
                    bool selected = e.id == _selectedId;

                    sb.Append("  <color=").Append(colour).Append('>').Append(marker).Append(' ');
                    if (selected) sb.Append("<b>");
                    sb.Append(e.name);
                    if (selected) sb.Append("</b>");
                    sb.Append("</color>");

                    sb.Append("  <color=#9FB2C4>").Append(e.genome.Notation());
                    sb.Append(e.sex == Sex.Female ? "  F" : "  M");

                    if (e.motherId >= 0 || e.fatherId >= 0)
                        sb.Append("  ← ").Append(pop.NameOf(e.motherId))
                          .Append(" + ").Append(pop.NameOf(e.fatherId));
                    else if (gen > 1)
                        sb.Append("  ← settled from the plankton");

                    if (!e.alive) sb.Append("  · ").Append(OctopusAgent.CauseWord(e.cause));
                    sb.Append("</color>\n");
                }
                sb.Append('\n');
            }

            sb.Append(Legend());
            _body.text = sb.ToString();

            BuildTapTargets(entries, oldestShown, newest);
        }

        string ColourFor(Entry e)
        {
            if (_colourMode == 0)
                return e.alive ? "#F2F6FA" : "#66788C";

            var gene = (GeneId)(_colourMode - 1);
            int copies = e.genome.CopiesOf(gene);

            // Two copies, one copy, none — three steps, so a variant spreading down
            // the generations is visible as the column changing colour.
            string full = copies switch { 2 => "#2BA84A", 1 => "#C9A227", _ => "#9A4B3F" };
            return e.alive ? full : Fade(full);
        }

        // Dead animals are shown in the same hue, dimmed, so a lineage reads as one
        // thread whether or not its members are still alive.
        static string Fade(string hex) => hex switch
        {
            "#2BA84A" => "#1C6B31",
            "#C9A227" => "#816819",
            _         => "#63302A",
        };

        string Legend()
        {
            if (_colourMode == 0)
                return "<color=#9FB2C4>● alive   ○ dead</color>";

            var gene = (GeneId)(_colourMode - 1);
            char upper = Genome.LetterOf(gene);
            char lower = char.ToLowerInvariant(upper);
            return $"<color=#2BA84A>■ {upper}{upper}</color>   " +
                   $"<color=#C9A227>■ {upper}{lower}</color>   " +
                   $"<color=#9A4B3F>■ {lower}{lower}</color>   " +
                   $"<color=#9FB2C4>(faded = dead)</color>\n" +
                   $"<color=#9FB2C4>{Genome.DescriptionOf(gene)}</color>";
        }

        readonly List<GameObject> _taps = new List<GameObject>(32);

        // A row of buttons under the tree, so any animal in it — living or dead — can
        // be opened in the inspector.
        void BuildTapTargets(List<Entry> entries, int oldestShown, int newest)
        {
            for (int i = 0; i < _taps.Count; i++) if (_taps[i] != null) Destroy(_taps[i]);
            _taps.Clear();

            var header = EcoUIKit.Text(_content, "\nTap any octopus to inspect it", 22f, EcoUIKit.TextDim);
            header.gameObject.AddComponent<LayoutElement>().preferredHeight = 44f;
            _taps.Add(header.gameObject);

            foreach (var e in entries)
            {
                if (e.generation < oldestShown) continue;
                int id = e.id;
                string label = $"{e.name}   {e.genome.Notation()}   gen {e.generation}" +
                               (e.alive ? "" : "   (dead)");
                var button = EcoUIKit.Button(_content, label, 21f,
                                             e.alive ? EcoUIKit.Track : new Color32(0x1A, 0x24, 0x2E, 0xFF),
                                             e.alive ? EcoUIKit.TextMain : EcoUIKit.TextDim,
                                             () => onAnimalTapped?.Invoke(id));
                button.gameObject.AddComponent<LayoutElement>().preferredHeight = 50f;
                _taps.Add(button.gameObject);
            }
        }
    }
}
