using System.Collections.Generic;
using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // One matched rule, ready to show.
    public class Reason
    {
        public string id;            // stable key, also used by the snapshot
        public string whatIsHappening;
        public string whyItIsHappening;
        public string whatHappensNext;
        public int    priority;
    }

    // The Why panel's reasoning. Not artificial intelligence: a small rule-based
    // explainer, which keeps it instant, offline, deterministic and impossible to
    // get wrong (Design Document 6.2).
    //
    // Each rule is: condition -> cause string + consequence string. They are
    // evaluated once per tick, in priority order, and the first three that match are
    // shown. Every string here should trace to the Literature Document, and every
    // rule needs a biologist's review of cause and consequence before release —
    // pre-release checklist item 6.
    //
    // Wording rule, from Literature 3.6: populations change because some individuals
    // leave more descendants than others. Never write as though an animal adapts on
    // purpose or within its own lifetime.
    public class ReasonEngine
    {
        readonly List<Reason> _active = new List<Reason>(8);
        readonly TrendTracker[] _trends = new TrendTracker[SpeciesLibrary.Count];
        readonly TrendTracker _producerTrend = new TrendTracker(20);

        public IReadOnlyList<Reason> Active => _active;

        public ReasonEngine()
        {
            for (int i = 0; i < _trends.Length; i++)
                _trends[i] = new TrendTracker(20);
        }

        public void Reset()
        {
            _active.Clear();
            _producerTrend.Reset();
            for (int i = 0; i < _trends.Length; i++) _trends[i].Reset();
        }

        public void Observe(EcosystemSimulation sim)
        {
            for (int i = 0; i < SpeciesLibrary.Count; i++)
                _trends[i].Push(sim.DisplayAmount(i));
            // Grazeable only: the ungrazed coral would otherwise mask a seafloor
            // being stripped, and "the algae are shrinking" would never fire.
            _producerTrend.Push(sim.GrazeableProducerBiomass());
        }

        public int TrendOf(int species) => _trends[species].Direction;
        public float ChangeOf(int species) => _trends[species].Change;

        // Evaluates the whole rule set and keeps the three strongest matches.
        public void Evaluate(EcosystemSimulation sim, EcosystemHealth health)
        {
            _active.Clear();
            var all = SpeciesLibrary.All;

            // ── Predator removal and its cascade ─────────────────────────────
            // grazer rising AND its predator absent -> "predator removed"
            if (!sim.IsPresent(SpeciesLibrary.TigerShark) && TrendOf(SpeciesLibrary.Urchin) > 0)
                Add("predator removed", 100,
                    $"{Cap(Name(SpeciesLibrary.Urchin))}s are increasing quickly.",
                    "You removed the tiger shark. Nothing is hunting the urchins now, so more of them survive to breed.",
                    "If this continues, the algae will run out and the urchins will starve. The coral may be smothered first.");

            if (!sim.IsPresent(SpeciesLibrary.TigerShark) && TrendOf(SpeciesLibrary.Octopus) > 0)
                Add("mesopredator release", 70,
                    "Octopuses are becoming more common.",
                    "With the tiger shark gone, the octopus is no longer hunted and no longer has to hide. A mid-level hunter freed like this is called a released mesopredator.",
                    "Lobsters and limpets will come under heavier pressure than before.");

            // ── Overgrazing ──────────────────────────────────────────────────
            // producer biomass falling AND grazer count rising -> "overgrazing"
            bool producersFalling = _producerTrend.Direction < 0;
            int risingGrazer = FirstRisingGrazer(sim);
            if (producersFalling && risingGrazer >= 0)
                Add("overgrazing", 90,
                    $"The algae are shrinking while {Name(risingGrazer)}s increase.",
                    $"{Cap(Name(risingGrazer))}s are eating the algae faster than the algae can regrow.",
                    "The algae will keep falling until there is not enough food, and then the grazers will starve.");

            if (producersFalling && risingGrazer < 0 && health.ProducerFraction < 0.8f)
                Add("producers falling", 60,
                    "The algae are shrinking.",
                    "More is being taken from the algae each day than they manage to grow back.",
                    "If nothing changes, the animals that depend on them will start to run short.");

            // ── Starvation ───────────────────────────────────────────────────
            // consumer energy negative for 5+ days -> "starvation"
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].IsProducer || all[i].transient) continue;
                if (!sim.IsPresent(i) || sim.pools[i].count <= 0f) continue;
                if (sim.pools[i].daysHungry < 5) continue;

                Add("starvation:" + all[i].id, 85,
                    $"{Cap(Name(i))}s are starting to starve.",
                    $"There is not enough food for the number of {Name(i)}s now living here, so they are using more energy than they take in.",
                    $"Their numbers will fall until the survivors can feed themselves.");
                break; // one starvation line is enough; the panel shows three reasons
            }

            // ── Water conditions ─────────────────────────────────────────────
            // producer capacity reduced AND temperature above threshold -> "water too warm"
            int bleaching = FirstBleaching(sim);
            if (bleaching >= 0)
                Add("water too warm", 95,
                    "The coral is bleaching.",
                    "The water is too warm, so the coral has expelled the algae living in its tissue. Those algae supplied most of its food, and its colour.",
                    "Bleached coral is not dead. If the water cools soon it can recover; if the heat lasts, it starves.");

            if (sim.temperatureC > 28f && bleaching < 0)
                Add("warm water", 50,
                    "The water is warm.",
                    "Warmer water raises the metabolic rate of cold-blooded animals, so they need more food simply to stay alive.",
                    "Animals will need to eat more, and the algae will grow less well above their best temperature.");

            if (sim.acidityPh < 7.95f)
                Add("acidified water", 55,
                    "The water is more acidic than normal.",
                    "Acidified water makes it harder to build skeletons and shells from calcium carbonate. Corals, calcareous algae and snails are affected first; soft-bodied animals much less so.",
                    "The coral, the green alga and the limpets will grow more slowly than the others.");

            // ── Missing pieces of the web ────────────────────────────────────
            if (!AnyPresent(sim, TrophicLevel.PlantEater))
                Add("no grazers", 88,
                    "Nothing is eating the algae.",
                    "You removed every plant eater, so nothing is grazing.",
                    "The algae will grow unchecked and can overgrow the coral, blocking its light.");

            if (!AnyPresent(sim, TrophicLevel.Producer))
                Add("no producers", 99,
                    "There are no producers left.",
                    "Nothing here is capturing energy from sunlight, so no new energy is entering the ecosystem at all.",
                    "Every animal will starve within a few dozen days. Only the detritus pool is left.");

            // ── Recovery ─────────────────────────────────────────────────────
            if (health.stage == CollapseStage.Healthy && _producerTrend.Direction > 0
                && health.ProducerFraction > 0.9f)
                Add("recovering", 20,
                    "The reef is regrowing.",
                    "Grazing has eased enough for the algae to grow back faster than they are eaten.",
                    "As the algae return, the animals that feed on them can increase again — though recovery takes longer than the collapse did.");

            if (_active.Count == 0)
                Add("balanced", 10,
                    "The reef is steady.",
                    "Each population is eating about as much as its food can regrow, so nothing is climbing or falling quickly.",
                    "Left alone, it should stay roughly like this. Change the water or remove a species to see what happens.");

            _active.Sort((a, b) => b.priority.CompareTo(a.priority));
            if (_active.Count > 3) _active.RemoveRange(3, _active.Count - 3);
        }

        // Three tappable starter questions matched to the current situation, drawn
        // from the same rule set (Design Document 7.2). Learners who are unsure what
        // to ask are the ones who most need the assistant.
        public List<string> SuggestedQuestions(EcosystemSimulation sim)
        {
            var q = new List<string>(3);
            foreach (var r in _active)
            {
                if (q.Count >= 3) break;
                switch (r.id)
                {
                    case "predator removed": q.Add("Why are my urchins increasing?"); break;
                    case "overgrazing":      q.Add("What will happen if I do nothing?"); break;
                    case "water too warm":   q.Add("Why is the coral turning white?"); break;
                    case "acidified water":  q.Add("What does acidity do to the reef?"); break;
                    case "no producers":     q.Add("How do I fix this?"); break;
                    case "no grazers":       q.Add("What happens if algae grow unchecked?"); break;
                    case "mesopredator release": q.Add("Why are there more octopuses?"); break;
                    default:
                        if (r.id.StartsWith("starvation:")) q.Add("Why are animals starving?");
                        break;
                }
            }
            if (q.Count < 3) q.Add("What is happening in my reef?");
            if (q.Count < 3) q.Add("How do I keep this reef balanced?");
            if (q.Count < 3) q.Add("Which animal matters most here?");
            return q;
        }

        void Add(string id, int priority, string what, string why, string next)
        {
            _active.Add(new Reason
            {
                id = id, priority = priority,
                whatIsHappening = what, whyItIsHappening = why, whatHappensNext = next,
            });
        }

        int FirstRisingGrazer(EcosystemSimulation sim)
        {
            var all = SpeciesLibrary.All;
            int best = -1;
            float bestChange = 0.10f;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].level != TrophicLevel.PlantEater) continue;
                if (!sim.IsPresent(i) || sim.pools[i].count <= 0f) continue;
                float c = ChangeOf(i);
                if (c > bestChange) { bestChange = c; best = i; }
            }
            return best;
        }

        int FirstBleaching(EcosystemSimulation sim)
        {
            for (int i = 0; i < SpeciesLibrary.Count; i++)
                if (sim.IsPresent(i) && sim.pools[i].bleached) return i;
            return -1;
        }

        static bool AnyPresent(EcosystemSimulation sim, TrophicLevel level)
        {
            var all = SpeciesLibrary.All;
            for (int i = 0; i < all.Length; i++)
                if (all[i].level == level && sim.IsAlive(i)) return true;
            return false;
        }

        static string Name(int i) => SpeciesLibrary.NameOf(i).ToLowerInvariant();

        static string Cap(string s) =>
            string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}
