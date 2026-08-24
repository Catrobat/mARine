using System;
using System.Collections.Generic;
using System.Text;
using CreateEnv.Ecosystem.Genetics;
using UnityEngine;

namespace CreateEnv.Ecosystem.Memory
{
    // The report a learner takes away with them (Design Document 7.3).
    //
    // The point of this is not the file. It is that the reef stops being a thing that
    // happened on a phone and becomes evidence: what was set up, what was predicted,
    // what actually occurred, and why.
    //
    // Kept to about two pages, and written the way a researcher writes for themselves
    // — figures and short factual lines, not explanations. An earlier draft carried a
    // paragraph of teaching prose under every chart and ran to five pages; a report
    // nobody reads to the end has failed regardless of what is in it. What survives is
    // what the reader cannot reconstruct on their own: the numbers, the shapes, the
    // sequence of events, and the pedigree.
    //
    // Everything sits on PdfWriter's baseline grid, so line spacing is even from the
    // first page to the last.
    public static class EcosystemReport
    {
        // ── Entry points ─────────────────────────────────────────────────────

        // Everything the report needs, gathered into one argument.
        //
        // The report takes this rather than the controller so it can be built without
        // a running scene — which is what lets a test generate a real PDF on a laptop
        // and check it, instead of the report only ever being seen on a phone.
        public struct Source
        {
            public string environmentName;
            public EcosystemSimulation sim;
            public EcosystemHealth health;
            public EcosystemSettings settings;
            public OctopusPopulation octopuses;
            public ReefChronicle chronicle;
            public IReadOnlyList<Reason> reasons;
        }

        public static byte[] Build(Source source)
        {
            var pdf = new PdfWriter();
            Compose(pdf, source);
            return pdf.ToBytes();
        }

        // Dated, so a learner can keep several and see which is which — the file name
        // is the only label a PDF has in a downloads list.
        public static string FileNameFor(string environmentName) =>
            "reef-" + Slug(environmentName) + "-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".pdf";

        static string Slug(string name)
        {
            if (string.IsNullOrEmpty(name)) return "reef";
            var sb = new StringBuilder(name.Length);
            foreach (char c in name)
                sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : '-');
            return sb.ToString().Trim('-');
        }

        // ── Composition ──────────────────────────────────────────────────────

        const float Line = PdfWriter.Line;

        static void Compose(PdfWriter pdf, Source source)
        {
            var sim = source.sim;
            var octopuses = source.octopuses;
            var history = source.chronicle?.history;

            Header(pdf, source.environmentName, sim, source.health);
            Setup(pdf, source.settings, sim, source.health);
            Populations(pdf, history, sim);
            Pyramid(pdf, sim);
            WhatHappened(pdf, source.chronicle?.journal, octopuses, source.reasons);

            if (octopuses != null && sim.IsPresent(SpeciesLibrary.Octopus))
            {
                FamilyTree(pdf, octopuses);
                Genes(pdf, history, octopuses);
            }

            SpeciesList(pdf, sim);
            Footnote(pdf);
        }

        // 1. Masthead ─────────────────────────────────────────────────────────
        static void Header(PdfWriter pdf, string environmentName,
                           EcosystemSimulation sim, EcosystemHealth health)
        {
            pdf.Space(Line);
            pdf.TextAt(PdfWriter.Margin, pdf.Cursor, "REEF REPORT", 8f, true, PdfWriter.Faint);

            pdf.Space(Line * 1.6f);
            pdf.TextAt(PdfWriter.Margin, pdf.Cursor,
                       string.IsNullOrEmpty(environmentName) ? "My Reef" : environmentName,
                       18f, true, PdfWriter.Ink);

            // The verdict, right-aligned on the same line as the title: a reader who
            // gets no further than the top of the page still learns how it ended.
            string verdict = EcosystemHealth.Describe(health.stage);
            var colour = HealthColour(health.level);
            float verdictWidth = PdfWriter.Measure(verdict, 9.5f, true) + 22f;
            float verdictX = PdfWriter.PageWidth - PdfWriter.Margin - verdictWidth;

            pdf.Rect(verdictX, pdf.Cursor - 4f, verdictWidth, 20f,
                     Color.Lerp(Color.white, colour, 0.16f));
            pdf.Rect(verdictX, pdf.Cursor - 4f, 3f, 20f, colour);
            pdf.TextAt(verdictX + 11f, pdf.Cursor + 2f, verdict, 9.5f, true, PdfWriter.Ink);

            pdf.Space(Line);
            pdf.TextAt(PdfWriter.Margin, pdf.Cursor,
                       "Day " + sim.day + "  ·  " + DateTime.Now.ToString("d MMMM yyyy"),
                       9f, false, PdfWriter.Faint);
            pdf.Space(6f);
        }

        static Color HealthColour(HealthLevel level) => level switch
        {
            HealthLevel.Green => new Color(0.20f, 0.65f, 0.35f),
            HealthLevel.Amber => new Color(0.85f, 0.62f, 0.15f),
            _                 => new Color(0.82f, 0.28f, 0.24f),
        };

        // 2. Setup and prediction ─────────────────────────────────────────────
        static void Setup(PdfWriter pdf, EcosystemSettings settings,
                          EcosystemSimulation sim, EcosystemHealth health)
        {
            pdf.Heading("Setup");

            var included = new List<string>();
            var excluded = new List<string>();
            for (int i = 0; i < SpeciesLibrary.Count; i++)
            {
                var def = SpeciesLibrary.Get(i);
                if (def == null) continue;
                (sim.IsPresent(i) ? included : excluded).Add(def.commonName);
            }

            pdf.Field("Temperature", sim.temperatureC.ToString("0.#") + " °C");
            pdf.Field("pH", sim.acidityPh.ToString("0.00"));
            pdf.Field("Starting life",
                      EcosystemSettings.StartingLifeOptions[
                          Mathf.Clamp(settings != null ? settings.startingLife : 1, 0,
                                      EcosystemSettings.StartingLifeOptions.Length - 1)]);
            pdf.Field("Species present", included.Count + " of " + SpeciesLibrary.Count);

            if (excluded.Count > 0)
                pdf.Field("Left out", string.Join(", ", excluded));

            // The prediction belongs with the setup: it was made at the same moment,
            // and on its own it did not need a heading of its own.
            if (settings != null && settings.predictionIndex >= 0)
            {
                int actual = EcosystemWarnings.OutcomeIndex(health, sim);
                pdf.Field(settings.predictionIndex == actual ? "Prediction (correct)"
                                                             : "Prediction (missed)",
                          EcosystemWarnings.PredictionOptions[
                              Mathf.Clamp(settings.predictionIndex, 0,
                                          EcosystemWarnings.PredictionOptions.Length - 1)]);
                pdf.Field("Outcome", EcosystemWarnings.PredictionOptions[
                              Mathf.Clamp(actual, 0,
                                          EcosystemWarnings.PredictionOptions.Length - 1)]);
            }
        }

        // 3. Populations ──────────────────────────────────────────────────────
        static void Populations(PdfWriter pdf, ReefHistory history, EcosystemSimulation sim)
        {
            pdf.Heading("Populations", keepWith: 8f * Line);

            if (history == null || history.count < 2)
            {
                pdf.Paragraph("Too early for a chart — the reef has not run long enough yet.",
                              9f, false, PdfWriter.Faint);
                return;
            }

            // Animals only. Producer biomass runs orders of magnitude above a headcount,
            // and on one pair of axes it flattens every animal onto the floor.
            var series = new List<int>();
            for (int i = 0; i < SpeciesLibrary.Count; i++)
            {
                var def = SpeciesLibrary.Get(i);
                if (def == null || def.IsProducer || def.transient) continue;
                if (!sim.IsPresent(i)) continue;
                series.Add(i);
            }

            DrawSeriesChart(pdf, history, series, 9f * Line,
                            (h, s, k) => h.AmountAt(s, k),
                            k => SpeciesLibrary.NameOf(k),
                            k => SpeciesLibrary.Get(k).tint,
                            v => v.ToString("0.#"));

            pdf.Space(Line * 0.5f);

            foreach (int k in series)
            {
                var def = SpeciesLibrary.Get(k);
                float first = history.AmountAt(0, k);
                float last = history.AmountAt(history.count - 1, k);

                string figure = Mathf.Abs(last - first) < 0.05f
                    ? "steady at " + last.ToString("0.#")
                    : first.ToString("0.#") + "  →  " + last.ToString("0.#") +
                      "   (" + (last > first ? "+" : "") +
                      Mathf.RoundToInt(history.ChangeOf(k) * 100f) + "%)";

                pdf.Row(def.tint, def.commonName, figure);
            }
        }

        // 4. Energy ───────────────────────────────────────────────────────────
        static readonly TrophicLevel[] PyramidLevels =
        {
            TrophicLevel.TopPredator,
            TrophicLevel.Hunter,
            TrophicLevel.PlantEater,
            TrophicLevel.Producer,
        };

        static void Pyramid(PdfWriter pdf, EcosystemSimulation sim)
        {
            // Heading and bars are one figure and never split across a page.
            pdf.Heading("Energy", keepWith: 5f * Line);

            float baseline = Mathf.Max(0.001f, sim.BiomassAtLevel(TrophicLevel.Producer));
            float widest = PdfWriter.ContentWidth - 190f;

            foreach (var level in PyramidLevels)
            {
                float biomass = sim.BiomassAtLevel(level);
                float width = Mathf.Max(2f, Mathf.Clamp01(biomass / baseline) * widest);

                pdf.Space(Line);
                pdf.TextAt(PdfWriter.Margin, pdf.Cursor, LevelWord(level), 9f, false, PdfWriter.Ink);
                pdf.Rect(PdfWriter.Margin + 110f, pdf.Cursor - 1f, width, 9f, LevelColour(level));
                pdf.TextAt(PdfWriter.Margin + 116f + width, pdf.Cursor,
                           biomass.ToString("0.#") + " kg", 8f, false, PdfWriter.Faint);
            }
        }

        static string LevelWord(TrophicLevel level) => level switch
        {
            TrophicLevel.Producer   => "Seaweed and coral",
            TrophicLevel.PlantEater => "Plant eaters",
            TrophicLevel.Hunter     => "Hunters",
            _                       => "Top predator",
        };

        // The pyramid band is a group; a species row needs the singular role.
        static string RoleWord(TrophicLevel level) => level switch
        {
            TrophicLevel.Producer   => "producer",
            TrophicLevel.PlantEater => "plant eater",
            TrophicLevel.Hunter     => "hunter",
            _                       => "top predator",
        };

        static Color LevelColour(TrophicLevel level) => level switch
        {
            TrophicLevel.Producer   => new Color(0.35f, 0.68f, 0.38f),
            TrophicLevel.PlantEater => new Color(0.38f, 0.63f, 0.78f),
            TrophicLevel.Hunter     => new Color(0.72f, 0.52f, 0.30f),
            _                       => new Color(0.45f, 0.42f, 0.50f),
        };

        // 5. What happened ────────────────────────────────────────────────────

        // The log is the one section that earns its length: it is the only record of
        // the order things happened in, and order is what makes a cause a cause.
        const int MaximumEvents = 12;

        static void WhatHappened(PdfWriter pdf, ReefJournal journal,
                                 OctopusPopulation octopuses, IReadOnlyList<Reason> reasons)
        {
            pdf.Heading("What happened");

            var events = journal != null ? journal.Notable(0, MaximumEvents) : null;
            if (events == null || events.Count == 0)
            {
                pdf.Paragraph("Nothing worth recording yet.", 9f, false, PdfWriter.Faint);
            }
            else
            {
                // Notable() ranks by importance so the right events survive the cut;
                // reading them back in date order is what turns a list into a story.
                events.Sort((a, b) => b.day.CompareTo(a.day));

                foreach (var e in events)
                {
                    string line = ReefJournal.Describe(e, octopuses);
                    if (string.IsNullOrEmpty(line)) continue;

                    pdf.EnsureRoom(Line);
                    pdf.Space(Line);
                    pdf.TextAt(PdfWriter.Margin, pdf.Cursor, "Day " + e.day, 8.5f, false, PdfWriter.Faint);
                    pdf.TextAt(PdfWriter.Margin + 46f, pdf.Cursor, line, 9f, false, PdfWriter.Ink);
                }
            }

            // Why it is like this now, in the same words the Why panel uses on screen,
            // so the report and the app can never tell a learner two different stories.
            if (reasons != null && reasons.Count > 0)
            {
                pdf.Space(Line * 0.5f);
                for (int i = 0; i < reasons.Count && i < 2; i++)
                {
                    pdf.EnsureRoom(Line * 3f);
                    pdf.Space(Line * 0.5f);
                    pdf.Paragraph(reasons[i].whatIsHappening, 9f, true);
                    pdf.Paragraph(reasons[i].whyItIsHappening, 9f, false, PdfWriter.Faint);
                }
            }
        }

        // 6. Family tree ──────────────────────────────────────────────────────
        struct TreeNode
        {
            public int id;
            public string name;
            public Genome genome;
            public Sex sex;
            public int generation;
            public int motherId, fatherId;
            public bool alive;
            public CauseOfDeath cause;
        }

        const float BoxWidth = 108f;
        const float BoxHeight = 3f * Line;

        static void FamilyTree(PdfWriter pdf, OctopusPopulation octopuses)
        {
            pdf.Heading("Octopus pedigree", keepWith: 6f * Line);

            pdf.Field("Hatched / died / alive",
                      octopuses.TotalBorn + " / " + octopuses.TotalDied + " / " + octopuses.AliveCount);
            pdf.Field("Generations", Mathf.Max(1, octopuses.HighestGeneration).ToString());
            pdf.Field("Gene letters", "camouflage · body size · heat tolerance, capital = stronger copy");

            // Everyone, living and dead, sorted into generations.
            var rows = new Dictionary<int, List<TreeNode>>();
            void Place(TreeNode node)
            {
                if (!rows.TryGetValue(node.generation, out var list))
                    rows[node.generation] = list = new List<TreeNode>();
                list.Add(node);
            }

            foreach (var a in octopuses.agents)
            {
                if (!a.IsAlive) continue;
                Place(new TreeNode
                {
                    id = a.id, name = a.name, genome = a.genome, sex = a.sex,
                    generation = a.generation, motherId = a.motherId, fatherId = a.fatherId,
                    alive = true,
                });
            }
            foreach (var r in octopuses.ancestors)
            {
                Place(new TreeNode
                {
                    id = r.id, name = r.Name, genome = r.genome, sex = r.Sex,
                    generation = r.generation, motherId = r.motherId, fatherId = r.fatherId,
                    alive = false, cause = r.Cause,
                });
            }

            var generations = new List<int>(rows.Keys);
            generations.Sort();
            if (generations.Count == 0)
            {
                pdf.Paragraph("No octopuses have lived here yet.", 9f, false, PdfWriter.Faint);
                return;
            }

            // A box's centre-top, so the next generation can draw a line up to it.
            // Cleared when a generation lands on a new page: a line to a box on the
            // previous sheet would run off this one.
            var placed = new Dictionary<int, Vector2>();
            int lastPage = pdf.PageCount;

            int perRow = Mathf.Max(1, Mathf.FloorToInt(PdfWriter.ContentWidth / (BoxWidth + 8f)));

            foreach (int gen in generations)
            {
                var row = rows[gen];

                pdf.EnsureRoom(BoxHeight + 3f * Line);
                if (pdf.PageCount != lastPage)
                {
                    placed.Clear();
                    lastPage = pdf.PageCount;
                }

                pdf.Space(Line);
                pdf.TextAt(PdfWriter.Margin, pdf.Cursor,
                           gen == 0 ? "Founders" : "Generation " + gen, 8f, true, PdfWriter.Faint);

                // A generation wider than the page wraps onto another line rather than
                // being cut off. A pedigree with animals missing from it is not a
                // pedigree, and it is the founders — the largest generation — that
                // would lose members.
                var newlyPlaced = new List<KeyValuePair<int, Vector2>>(row.Count);

                for (int start = 0; start < row.Count; start += perRow)
                {
                    int shown = Mathf.Min(perRow, row.Count - start);

                    if (start > 0) pdf.EnsureRoom(BoxHeight + Line);
                    pdf.Space(BoxHeight + Line);
                    float boxBottom = pdf.Cursor;

                    float span = shown * BoxWidth + (shown - 1) * 8f;
                    float startX = PdfWriter.Margin +
                                   Mathf.Max(0f, (PdfWriter.ContentWidth - span) * 0.5f);

                    for (int i = 0; i < shown; i++)
                    {
                        var node = row[start + i];
                        float x = startX + i * (BoxWidth + 8f);
                        DrawOctopusBox(pdf, node, x, boxBottom);

                        var centreTop = new Vector2(x + BoxWidth * 0.5f, boxBottom + BoxHeight);
                        newlyPlaced.Add(new KeyValuePair<int, Vector2>(node.id, centreTop));

                        LinkToParent(pdf, placed, node.motherId, centreTop);
                        LinkToParent(pdf, placed, node.fatherId, centreTop);
                    }
                }

                // Added after the links are drawn, so a sibling in the same generation
                // is never mistaken for a parent.
                foreach (var entry in newlyPlaced) placed[entry.Key] = entry.Value;
            }
        }

        static void LinkToParent(PdfWriter pdf, Dictionary<int, Vector2> placed,
                                 int parentId, Vector2 childTop)
        {
            if (parentId < 0 || !placed.TryGetValue(parentId, out var parentTop)) return;

            // The stored point is the parent's top; its box hangs BoxHeight below, and
            // the line has to meet the bottom edge or it runs through the parent's text.
            float parentBottom = parentTop.y - BoxHeight;
            if (parentBottom <= childTop.y) return;

            float mid = (childTop.y + parentBottom) * 0.5f;
            pdf.Stroke(childTop.x, childTop.y, childTop.x, mid, PdfWriter.Rule, 0.6f);
            pdf.Stroke(childTop.x, mid, parentTop.x, mid, PdfWriter.Rule, 0.6f);
            pdf.Stroke(parentTop.x, mid, parentTop.x, parentBottom, PdfWriter.Rule, 0.6f);
        }

        static void DrawOctopusBox(PdfWriter pdf, TreeNode node, float x, float y)
        {
            pdf.Rect(x, y, BoxWidth, BoxHeight,
                     node.alive ? new Color(0.93f, 0.97f, 0.94f) : new Color(0.95f, 0.95f, 0.96f));
            pdf.Rect(x, y, 3f, BoxHeight,
                     node.alive ? PdfWriter.Accent : new Color(0.72f, 0.75f, 0.78f));

            pdf.TextAt(x + 8f, y + BoxHeight - 12f, Fit(node.name, 8.5f, true), 8.5f, true, PdfWriter.Ink);

            string subtitle = node.sex == Sex.Female ? "female" : "male";
            if (!node.alive && node.cause != CauseOfDeath.StillAlive)
                subtitle += ", " + OctopusAgent.CauseWord(node.cause);
            pdf.TextAt(x + 8f, y + BoxHeight - 22f, Fit(subtitle, 7f, false), 7f, false, PdfWriter.Faint);

            var sb = new StringBuilder();
            for (int g = 0; g < Genome.GeneCount; g++)
            {
                if (g > 0) sb.Append(' ');
                sb.Append(node.genome.Notation((GeneId)g));
            }
            pdf.TextAt(x + 8f, y + 7f, sb.ToString(), 7.5f, false, PdfWriter.Ink);
        }

        // Trims to the width of a box rather than letting it overrun into its neighbour.
        static string Fit(string text, float size, bool bold)
        {
            if (string.IsNullOrEmpty(text)) return "";
            while (text.Length > 3 && PdfWriter.Measure(text, size, bold) > BoxWidth - 14f)
                text = text.Substring(0, text.Length - 1);
            return text;
        }

        // 7. Genes ────────────────────────────────────────────────────────────
        static void Genes(PdfWriter pdf, ReefHistory history, OctopusPopulation octopuses)
        {
            pdf.Heading("Gene frequencies", keepWith: 6f * Line);

            if (history != null && history.count >= 2)
            {
                DrawSeriesChart(pdf, history, new List<int> { 0, 1, 2 }, 7f * Line,
                                (h, s, k) => h.GeneAt(s, (GeneId)k) * 100f,
                                k => Genome.NameOf((GeneId)k),
                                k => GeneColour((GeneId)k),
                                v => v.ToString("0") + "%");
                pdf.Space(Line * 0.5f);
            }

            for (int g = 0; g < Genome.GeneCount; g++)
            {
                var gene = (GeneId)g;
                float share = octopuses.AlleleFrequency(gene);
                pdf.Row(GeneColour(gene), Genome.NameOf(gene),
                        (share * 100f).ToString("0") + "%   " + Fixation(share), 9f, 120f);
            }
        }

        // With five animals a gene can vanish through nothing but luck. Naming which of
        // the two happened is the whole lesson of drift against selection, and it is the
        // one thing a learner cannot read off the chart for themselves.
        static string Fixation(float share)
        {
            if (share >= 0.999f) return "fixed — the alternative is lost";
            if (share <= 0.001f) return "lost from the population";
            if (share >= 0.85f)  return "nearly fixed";
            if (share <= 0.15f)  return "nearly lost";
            return "both versions still present";
        }

        static Color GeneColour(GeneId gene) => gene switch
        {
            GeneId.Camouflage => new Color(0.30f, 0.55f, 0.72f),
            GeneId.BodySize   => new Color(0.76f, 0.48f, 0.28f),
            _                 => new Color(0.68f, 0.33f, 0.55f),
        };

        // 8. Species ──────────────────────────────────────────────────────────

        // One line each: what it is called, what it is, and how it is doing in the
        // wild. The long natural-history notes live in the app, where a learner can
        // read them when they are curious about one animal rather than all nine.
        static void SpeciesList(PdfWriter pdf, EcosystemSimulation sim)
        {
            pdf.Heading("Species");

            for (int i = 0; i < SpeciesLibrary.Count; i++)
            {
                if (!sim.IsPresent(i)) continue;
                var def = SpeciesLibrary.Get(i);
                if (def == null) continue;

                pdf.EnsureRoom(Line);
                pdf.Space(Line);
                pdf.Rect(PdfWriter.Margin, pdf.Cursor + 1f, 6f, 6f, def.tint);
                pdf.TextAt(PdfWriter.Margin + 13f, pdf.Cursor, def.commonName, 9f, false, PdfWriter.Ink);
                pdf.TextAt(PdfWriter.Margin + 130f, pdf.Cursor, def.scientificName, 8.5f, false, PdfWriter.Faint);

                string status = string.IsNullOrEmpty(def.iucnStatus) ? "" : def.iucnStatus;
                pdf.TextAt(PdfWriter.Margin + 290f, pdf.Cursor,
                           RoleWord(def.level) + (status.Length > 0 ? "  ·  " + status : ""),
                           8.5f, false, PdfWriter.Faint);
            }
        }

        // 9. Footnote ─────────────────────────────────────────────────────────
        static void Footnote(PdfWriter pdf)
        {
            pdf.Space(Line * 0.5f);
            pdf.EnsureRoom(Line * 3f);
            pdf.Stroke(PdfWriter.Margin, pdf.Cursor, PdfWriter.PageWidth - PdfWriter.Margin,
                       pdf.Cursor, PdfWriter.Rule, 0.6f);
            pdf.Space(Line * 0.5f);

            // Short, but not dropped. A model that does not say where it stops being
            // true is teaching something false.
            pdf.Paragraph("A teaching model of one patch of Cabo Verde reef, from published rates: faster " +
                          "than life, far fewer animals, the rest of the ocean held still. Broods are two " +
                          "to four rather than thousands, the shark visits rather than lives here, and " +
                          "some species are given at genus level.",
                          8f, false, PdfWriter.Faint);
        }

        // ── Charting ─────────────────────────────────────────────────────────

        // One routine draws both the population lines and the gene lines. They differ
        // only in what a series means, so they share axes, gridlines and legend — and a
        // reader who has learned to read one has learned to read the other.
        static void DrawSeriesChart(PdfWriter pdf, ReefHistory history, List<int> series,
                                    float height,
                                    Func<ReefHistory, int, int, float> valueAt,
                                    Func<int, string> nameOf,
                                    Func<int, Color> colourOf,
                                    Func<float, string> formatValue)
        {
            if (series.Count == 0 || history.count < 2) return;

            float legendRows = Mathf.Ceil(series.Count / 3f);
            pdf.EnsureRoom(height + (legendRows + 2f) * Line);

            pdf.Space(height);
            float bottom = pdf.Cursor;
            float left = PdfWriter.Margin + 30f;
            float right = PdfWriter.PageWidth - PdfWriter.Margin;
            float plotWidth = right - left;

            // The tallest value across every series, rounded up so the top gridline is a
            // number a reader can hold in their head.
            float peak = 0f;
            foreach (int k in series)
                for (int s = 0; s < history.count; s++)
                    peak = Mathf.Max(peak, valueAt(history, s, k));
            peak = NiceCeiling(peak);

            for (int g = 0; g <= 2; g++)
            {
                float y = bottom + height * g / 2f;
                pdf.Stroke(left, y, right, y, PdfWriter.Rule, 0.4f);
                pdf.TextAt(PdfWriter.Margin, y - 2.5f, formatValue(peak * g / 2f),
                           7f, false, PdfWriter.Faint);
            }

            // Day labels at each end. Two are enough — the chart is about shape.
            pdf.TextAt(left, bottom - 10f, "day " + history.DayAt(0), 7f, false, PdfWriter.Faint);
            string lastLabel = "day " + history.DayAt(history.count - 1);
            pdf.TextAt(right - PdfWriter.Measure(lastLabel, 7f, false), bottom - 10f,
                       lastLabel, 7f, false, PdfWriter.Faint);

            foreach (int k in series)
            {
                var colour = colourOf(k);
                float previousX = 0f, previousY = 0f;

                for (int s = 0; s < history.count; s++)
                {
                    float x = left + plotWidth * s / (history.count - 1f);
                    float y = bottom + Mathf.Clamp01(valueAt(history, s, k) / peak) * height;
                    if (s > 0) pdf.Stroke(previousX, previousY, x, y, colour, 1.1f);
                    previousX = x;
                    previousY = y;
                }
            }

            // Legend, three across, on the grid like everything else.
            pdf.Space(Line * 1.5f);
            for (int i = 0; i < series.Count; i++)
            {
                int column = i % 3;
                if (column == 0 && i > 0) pdf.Space(Line);

                float x = PdfWriter.Margin + column * (PdfWriter.ContentWidth / 3f);
                pdf.Rect(x, pdf.Cursor + 1f, 6f, 6f, colourOf(series[i]));
                pdf.TextAt(x + 11f, pdf.Cursor, nameOf(series[i]), 8f, false, PdfWriter.Ink);
            }
        }

        // Rounds an axis maximum up to 1, 2 or 5 times a power of ten.
        static float NiceCeiling(float value)
        {
            if (value <= 0f) return 1f;
            float magnitude = Mathf.Pow(10f, Mathf.Floor(Mathf.Log10(value)));
            float normalised = value / magnitude;
            float step = normalised <= 1f ? 1f : normalised <= 2f ? 2f : normalised <= 5f ? 5f : 10f;
            return step * magnitude;
        }
    }
}
