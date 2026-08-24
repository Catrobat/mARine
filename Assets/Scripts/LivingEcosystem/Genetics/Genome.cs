using System;
using System.Collections.Generic;
using UnityEngine;

namespace CreateEnv.Ecosystem.Genetics
{
    public enum GeneId
    {
        Camouflage    = 0,   // dominant / recessive
        BodySize      = 1,   // additive, three levels
        HeatTolerance = 2,   // additive, three levels
    }

    // Three genes, two copies of each (Design Document 4.1).
    //
    // One byte per gene, two alleles in the low two bits: a set bit is the uppercase
    // variant. Three bytes an animal, which is what makes keeping a pedigree of dead
    // ancestors cost almost nothing.
    //
    // The three genes are deliberately not all the same kind of inheritance, because
    // that is the point of the lesson (Literature 3.2 and 3.3):
    //
    //   A  camouflage     dominant/recessive — one strong copy is enough, and a weak
    //                     copy can hide for generations and reappear
    //   B  body size      additive — each copy adds a little, giving a range rather
    //                     than two categories
    //   C  heat tolerance additive — the same, and the main thing selection acts on
    [Serializable]
    public struct Genome
    {
        public byte camouflage;
        public byte bodySize;
        public byte heatTolerance;

        public const int GeneCount = 3;

        // The letter used when writing a genotype out, e.g. "AaBBcc".
        public static char LetterOf(GeneId gene) => gene switch
        {
            GeneId.Camouflage    => 'A',
            GeneId.BodySize      => 'B',
            _                    => 'C',
        };

        public static string NameOf(GeneId gene) => gene switch
        {
            GeneId.Camouflage    => "Camouflage",
            GeneId.BodySize      => "Body size",
            _                    => "Heat tolerance",
        };

        public static string DescriptionOf(GeneId gene) => gene switch
        {
            GeneId.Camouflage =>
                "Strong camouflage is dominant: one strong copy is enough. A weak copy is not " +
                "lost when it is hidden — it can be passed on and reappear in a later generation, " +
                "which is why traits can skip generations.",
            GeneId.BodySize =>
                "Additive: each copy adds a little, so sizes spread across a range instead of " +
                "falling into two groups. A bigger octopus hunts better but needs more food.",
            _ =>
                "Additive, and the main thing selection acts on here. In warm water an octopus " +
                "with poor heat tolerance spends more energy staying alive, so it more often " +
                "dies before it breeds.",
        };

        public byte Raw(GeneId gene) => gene switch
        {
            GeneId.Camouflage    => camouflage,
            GeneId.BodySize      => bodySize,
            _                    => heatTolerance,
        };

        public void SetRaw(GeneId gene, byte value)
        {
            switch (gene)
            {
                case GeneId.Camouflage:    camouflage = value; break;
                case GeneId.BodySize:      bodySize = value; break;
                default:                   heatTolerance = value; break;
            }
        }

        // One allele: true is the uppercase variant. Copy 0 came from the mother,
        // copy 1 from the father.
        public bool Allele(GeneId gene, int copy) => (Raw(gene) & (1 << copy)) != 0;

        // How many uppercase copies this animal carries: 0, 1 or 2.
        public int CopiesOf(GeneId gene)
        {
            byte raw = Raw(gene);
            return (raw & 1) + ((raw >> 1) & 1);
        }

        public bool IsHeterozygous(GeneId gene) => CopiesOf(gene) == 1;

        // "AA", "Aa" or "aa" — always written with the uppercase copy first, so the
        // same genotype always reads the same way.
        public string Notation(GeneId gene)
        {
            char upper = LetterOf(gene);
            char lower = char.ToLowerInvariant(upper);
            return CopiesOf(gene) switch
            {
                2 => $"{upper}{upper}",
                1 => $"{upper}{lower}",
                _ => $"{lower}{lower}",
            };
        }

        // The whole genotype, e.g. "AaBBcc".
        public string Notation() =>
            Notation(GeneId.Camouflage) + Notation(GeneId.BodySize) + Notation(GeneId.HeatTolerance);

        // ── Building genomes ─────────────────────────────────────────────────

        public static Genome FromCopies(int camouflageCopies, int bodyCopies, int heatCopies)
        {
            var g = new Genome();
            g.camouflage    = PackCopies(camouflageCopies);
            g.bodySize      = PackCopies(bodyCopies);
            g.heatTolerance = PackCopies(heatCopies);
            return g;
        }

        static byte PackCopies(int copies) => copies switch
        {
            >= 2 => 0b11,
            1    => 0b01,
            _    => 0b00,
        };

        public static Genome Random(System.Random rng)
        {
            var g = new Genome();
            foreach (GeneId gene in Enum.GetValues(typeof(GeneId)))
            {
                byte raw = 0;
                if (rng.NextDouble() < 0.5) raw |= 1;
                if (rng.NextDouble() < 0.5) raw |= 2;
                g.SetRaw(gene, raw);
            }
            return g;
        }

        // Each parent passes one randomly chosen copy of each gene (Literature 3.4).
        // That single rule is what the Punnett grid predicts, and what makes a small
        // brood so often fail to match the prediction.
        public static Genome Inherit(Genome mother, Genome father, System.Random rng, float mutationRate)
        {
            var child = new Genome();

            foreach (GeneId gene in Enum.GetValues(typeof(GeneId)))
            {
                bool fromMother = mother.Allele(gene, rng.NextDouble() < 0.5 ? 0 : 1);
                bool fromFather = father.Allele(gene, rng.NextDouble() < 0.5 ? 0 : 1);

                // Mutation is the only source of genuinely new variation; everything
                // else just reshuffles what is already there (Literature 3.5). The
                // real rate is far too low to see in a session, so it is raised, and
                // the interface says so.
                if (rng.NextDouble() < mutationRate) fromMother = !fromMother;
                if (rng.NextDouble() < mutationRate) fromFather = !fromFather;

                byte raw = 0;
                if (fromMother) raw |= 1;
                if (fromFather) raw |= 2;
                child.SetRaw(gene, raw);
            }
            return child;
        }

        public bool Equals(Genome other) =>
            camouflage == other.camouflage &&
            bodySize == other.bodySize &&
            heatTolerance == other.heatTolerance;
    }

    // The inheritance grid, named after Reginald Punnett (Literature 3.4).
    //
    // Lives beside Genome deliberately: it predicts what Genome.Inherit will do,
    // so keeping the two in one file makes it obvious that they have to agree.
    // The harness checks that they do, against forty thousand sampled crosses per
    // genotype pair.
    //
    // Because each parent passes one randomly chosen copy of each gene, the possible
    // combinations lay out in a two-by-two grid and the expected ratios read straight
    // off it. This computes exactly that, from the same rule Genome.Inherit uses, so
    // the prediction the learner sees and the maths the simulation runs cannot drift
    // apart. The harness checks that they agree.
    //
    // The lesson hidden inside the tool: a grid predicts ratios over many offspring.
    // A brood of three will frequently not match, and that is not a bug — it is how
    // chance works with small numbers.
    public static class PunnettPrediction
    {
        public struct Cell
        {
            public bool fromMother;   // the allele this parent contributed
            public bool fromFather;
            public int copies;        // 0, 1 or 2 uppercase copies in the offspring
            public string notation;   // "AA", "Aa", "aa"
        }

        public struct Outcome
        {
            public int copies;
            public string notation;
            public float probability;   // 0..1
            public string phenotype;    // what that genotype actually looks like
        }

        // The four cells of the grid, in reading order.
        public static Cell[] Grid(Genome mother, Genome father, GeneId gene)
        {
            var cells = new Cell[4];
            int i = 0;
            for (int m = 0; m < 2; m++)
            {
                for (int f = 0; f < 2; f++)
                {
                    bool am = mother.Allele(gene, m);
                    bool af = father.Allele(gene, f);
                    int copies = (am ? 1 : 0) + (af ? 1 : 0);

                    var g = new Genome();
                    byte raw = 0;
                    if (am) raw |= 1;
                    if (af) raw |= 2;
                    g.SetRaw(gene, raw);

                    cells[i++] = new Cell
                    {
                        fromMother = am,
                        fromFather = af,
                        copies = copies,
                        notation = g.Notation(gene),
                    };
                }
            }
            return cells;
        }

        // The distinct outcomes and how likely each is, most likely first.
        public static List<Outcome> Outcomes(Genome mother, Genome father, GeneId gene)
        {
            var grid = Grid(mother, father, gene);
            var counts = new Dictionary<int, int>();
            foreach (var cell in grid)
                counts[cell.copies] = counts.TryGetValue(cell.copies, out int n) ? n + 1 : 1;

            var list = new List<Outcome>(3);
            foreach (var pair in counts)
            {
                var g = Genome.FromCopies(
                    gene == GeneId.Camouflage ? pair.Key : 0,
                    gene == GeneId.BodySize ? pair.Key : 0,
                    gene == GeneId.HeatTolerance ? pair.Key : 0);

                list.Add(new Outcome
                {
                    copies = pair.Key,
                    notation = g.Notation(gene),
                    probability = pair.Value / 4f,
                    phenotype = PhenotypeWord(gene, pair.Key),
                });
            }

            list.Sort((a, b) => b.probability.CompareTo(a.probability));
            return list;
        }

        // What a given number of uppercase copies produces, in plain words. Mirrors
        // OctopusTraits: camouflage is dominant so one copy is already strong, while
        // the additive genes step through three levels.
        public static string PhenotypeWord(GeneId gene, int copies)
        {
            if (gene == GeneId.Camouflage)
                return copies >= 1 ? "Strong camouflage" : "Weak camouflage";

            if (gene == GeneId.BodySize)
                return copies switch { 2 => "Large", 1 => "Medium", _ => "Small" };

            return copies switch { 2 => "High heat tolerance", 1 => "Moderate heat tolerance", _ => "Low heat tolerance" };
        }

        // A ratio written the way a textbook would: "3 : 1", "1 : 2 : 1", "all".
        public static string RatioText(Genome mother, Genome father, GeneId gene)
        {
            var grid = Grid(mother, father, gene);
            var byCopies = new int[3];
            foreach (var cell in grid) byCopies[cell.copies]++;

            var parts = new List<string>(3);
            for (int copies = 2; copies >= 0; copies--)
                if (byCopies[copies] > 0)
                    parts.Add($"{byCopies[copies]}");

            if (parts.Count == 1) return "all the same";
            return string.Join(" : ", parts);
        }

        // The line the breeding tool prints under the grid, so the learner is told in
        // advance that a small brood often will not match.
        public const string SmallBroodCaveat =
            "A grid predicts ratios over many offspring. A brood of two to four will often not " +
            "match it — that is not a mistake, it is how chance works with small numbers.";
    }
}
