using UnityEngine;

namespace CreateEnv.Ecosystem.Genetics
{
    // Turns a genotype into what the animal actually is (Literature 3.1: the pair of
    // variants is the genotype, what it produces is the phenotype).
    //
    // A small random component is added on top of every trait, so two animals with
    // identical genes are not quite identical. One line of code, and one of the most
    // commonly misunderstood ideas in school biology: genotype is not destiny
    // (Design Document 4.1). The component is drawn once at birth and kept, so an
    // individual stays itself.
    public struct OctopusTraits
    {
        public float camouflage;    // 0 poor .. 1 excellent
        public float bodySize;      // multiplier on the model and on appetite
        public float heatTolerance; // 0 intolerant .. 1 tolerant

        // How much of each trait is individual variation rather than genotype.
        public const float CamouflageNoise = 0.08f;
        public const float BodySizeNoise   = 0.05f;
        public const float HeatNoise       = 0.07f;

        public static OctopusTraits From(Genome genome, float noiseSeed01)
        {
            // One stored number drives all three wobbles, so an individual needs a
            // single byte of "not quite its genotype" rather than three.
            float n1 = Wobble(noiseSeed01, 0.13f);
            float n2 = Wobble(noiseSeed01, 0.47f);
            float n3 = Wobble(noiseSeed01, 0.79f);

            var t = new OctopusTraits();

            // Camouflage is dominant: one strong copy is enough. The heterozygote is
            // deliberately NOT midway — that is what dominance means, and showing Aa
            // as identical to AA is what makes the recessive copy's reappearance
            // surprising later.
            bool strong = genome.CopiesOf(GeneId.Camouflage) >= 1;
            t.camouflage = Mathf.Clamp01((strong ? 0.86f : 0.30f) + n1 * CamouflageNoise);

            // Additive: 0, 1 or 2 copies spread across a range.
            int sizeCopies = genome.CopiesOf(GeneId.BodySize);
            t.bodySize = Mathf.Clamp(0.80f + sizeCopies * 0.20f + n2 * BodySizeNoise, 0.6f, 1.5f);

            int heatCopies = genome.CopiesOf(GeneId.HeatTolerance);
            t.heatTolerance = Mathf.Clamp01(heatCopies * 0.5f + n3 * HeatNoise);

            return t;
        }

        // A repeatable pseudo-random value in -1..1 from a stored 0..1 seed.
        static float Wobble(float seed01, float salt)
        {
            float v = Mathf.Sin((seed01 + salt) * 127.1f) * 43758.5453f;
            return (v - Mathf.Floor(v)) * 2f - 1f;
        }

        // ── Plain-language descriptions, for the inspector ───────────────────

        public string CamouflageWord =>
            camouflage > 0.65f ? "Strong" : camouflage > 0.45f ? "Moderate" : "Weak";

        public string BodySizeWord =>
            bodySize > 1.12f ? "Large" : bodySize > 0.95f ? "Medium" : "Small";

        public string HeatToleranceWord =>
            heatTolerance > 0.66f ? "High" : heatTolerance > 0.33f ? "Moderate" : "Low";

        public float ValueOf(GeneId gene) => gene switch
        {
            GeneId.Camouflage    => camouflage,
            GeneId.BodySize      => Mathf.InverseLerp(0.6f, 1.5f, bodySize),
            _                    => heatTolerance,
        };

        public string WordFor(GeneId gene) => gene switch
        {
            GeneId.Camouflage    => CamouflageWord,
            GeneId.BodySize      => BodySizeWord,
            _                    => HeatToleranceWord,
        };
    }
}
