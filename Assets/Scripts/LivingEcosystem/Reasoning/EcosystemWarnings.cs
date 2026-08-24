using System.Collections.Generic;

namespace CreateEnv.Ecosystem
{
    // Guard rails, not blocks. The learner is always allowed to build an unbalanced
    // ecosystem — breaking it is the lesson. A one-line warning as choices are made
    // turns a broken ecosystem into a taught one (Design Document 5.1).
    //
    // A lookup table of about a dozen strings and no new simulation logic.
    public static class EcosystemWarnings
    {
        // Every warning that applies to the current selection, most important first.
        public static List<string> For(bool[] present)
        {
            var list = new List<string>(4);
            if (present == null || present.Length < SpeciesLibrary.Count) return list;

            bool On(int i) => present[i];

            bool anyProducer = On(SpeciesLibrary.Halimeda) || On(SpeciesLibrary.Padina) || On(SpeciesLibrary.Coral);
            bool anyAlga     = On(SpeciesLibrary.Halimeda) || On(SpeciesLibrary.Padina);
            bool anyGrazer   = On(SpeciesLibrary.Parrotfish) || On(SpeciesLibrary.Urchin)
                            || On(SpeciesLibrary.Limpet) || On(SpeciesLibrary.Lobster);

            if (!anyProducer)
                list.Add("With no producers, nothing captures energy from sunlight. Everything will starve within a few dozen days.");
            else if (!anyAlga)
                list.Add("With only coral, there is almost nothing to graze. The coral grows far too slowly to feed anything.");

            if (!anyGrazer && anyAlga)
                list.Add("With no plant eaters, algae will grow unchecked and can smother the coral.");

            if (!On(SpeciesLibrary.TigerShark) && On(SpeciesLibrary.Urchin))
                list.Add("With no tiger shark, nothing hunts the urchins. Expect them to multiply and strip the algae.");

            if (!On(SpeciesLibrary.Parrotfish) && On(SpeciesLibrary.Urchin) && !On(SpeciesLibrary.TigerShark))
                list.Add("Neither the shark nor the parrotfish is present. Nothing at all checks the urchins — a barren is very likely.");

            if (!On(SpeciesLibrary.Octopus) && On(SpeciesLibrary.Lobster))
                list.Add("With no octopus, lobsters have no predator here.");

            if (On(SpeciesLibrary.Octopus) && !On(SpeciesLibrary.Lobster) && !On(SpeciesLibrary.Limpet))
                list.Add("The octopus has nothing to hunt. It will starve.");

            if (On(SpeciesLibrary.TigerShark) && !On(SpeciesLibrary.Parrotfish)
                && !On(SpeciesLibrary.Urchin) && !On(SpeciesLibrary.Octopus))
                list.Add("The tiger shark has nothing to hunt here.");

            if (On(SpeciesLibrary.Coral) && !anyGrazer)
                list.Add("Without grazers, fan alga tends to overgrow the coral and block its light.");

            return list;
        }

        // The prediction prompt shown before entering: predict, observe, compare —
        // the scientific method in three taps (Design Document 5.1).
        public static readonly string[] PredictionOptions =
        {
            "The reef will stay balanced",
            "One species will take over",
            "The reef will collapse",
        };

        public const string PredictionQuestion = "What do you think will happen?";

        // Which outcome actually occurred, as an index into PredictionOptions, so the
        // app can compare it against what the learner predicted.
        public static int OutcomeIndex(EcosystemHealth health, EcosystemSimulation sim)
        {
            if (health.stage >= CollapseStage.Collapse) return 2;

            var all = SpeciesLibrary.All;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].IsProducer || all[i].transient) continue;
                if (!sim.IsPresent(i)) continue;
                if (sim.pools[i].count > all[i].startingStock * 2.2f) return 1;
            }
            return health.stage <= CollapseStage.Imbalance ? 0 : 1;
        }

        public static string CompareToPrediction(int predicted, int actual)
        {
            if (predicted < 0) return null;
            string actualText = actual switch
            {
                0 => "the reef stayed balanced",
                1 => "one species took over",
                _ => "the reef collapsed",
            };
            return predicted == actual
                ? $"You predicted this: {actualText}."
                : $"You predicted \"{PredictionOptions[predicted]}\", and {actualText}.";
        }
    }
}
