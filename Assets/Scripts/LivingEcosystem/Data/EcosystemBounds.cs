using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // The guardrail for the ecosystem half of a profile, matching the role
    // EnvironmentBounds plays for the terrain half. Every learner-facing value has
    // a Range here; the UI builds its sliders from these Ranges, so the form cannot
    // express an out-of-range value, and Clamp() is the second line of defence on
    // every load and every save.
    public static class EcosystemBounds
    {
        public struct Range
        {
            public readonly float Min, Max, Default, Step;
            public Range(float min, float max, float def, float step = 0f)
            { Min = min; Max = max; Default = def; Step = step; }

            public float Clamp(float v)
            {
                v = Mathf.Clamp(v, Min, Max);
                if (Step > 0f) v = Min + Mathf.Round((v - Min) / Step) * Step;
                return Mathf.Clamp(v, Min, Max);
            }

            // Where v sits in the range, 0..1. Used to drive the 0..1 sliders the
            // builder form is made of.
            public float Normalize(float v) => Max > Min ? Mathf.Clamp01((v - Min) / (Max - Min)) : 0f;
            public float Denormalize(float t) => Clamp(Mathf.Lerp(Min, Max, Mathf.Clamp01(t)));
        }

        // Shallow Cabo Verde water runs roughly 22–27 °C through the year. The range
        // is widened at both ends so the learner can push the system into bleaching
        // territory and see it respond — the point of the control.
        public static readonly Range Temperature = new Range(18f, 32f, 24f, 0.5f);

        // Present-day surface pH is about 8.1. 7.7 is the low end of end-of-century
        // projections; 8.3 is roughly pre-industrial.
        public static readonly Range Acidity = new Range(7.6f, 8.3f, 8.1f, 0.05f);

        // ── Environmental response curves ────────────────────────────────────
        // Kept here, next to the ranges they read from, so retuning the biology and
        // retuning the limits happen in one file.

        // Growth multiplier on producers from temperature. Peaks near 26 °C and
        // falls away on both sides; above the bleaching threshold the coral's
        // symbionts are expelled and its capacity collapses (handled per-species
        // through SpeciesDefinition.heatSensitivity).
        public static float TemperatureFactor(float celsius, float optimum, float tolerance)
        {
            float d = (celsius - optimum) / Mathf.Max(0.01f, tolerance);
            return Mathf.Clamp01(Mathf.Exp(-d * d));
        }

        // Metabolic cost rises with temperature for cold-blooded animals: warmer
        // water means more food is needed simply to stay alive (Literature §2.9).
        // ~10 % more demand per degree above the reference, Q10-flavoured but linear
        // enough to stay legible.
        public static float MetabolicFactor(float celsius)
        {
            return Mathf.Clamp(1f + (celsius - 24f) * 0.075f, 0.5f, 2.2f);
        }

        // Acidification makes calcium carbonate skeletons harder to build, so it hits
        // calcifiers first and leaves soft-bodied organisms nearly untouched
        // (Literature §2.9). 1 = unaffected at present-day pH.
        public static float CalcificationFactor(float ph, float calcifierWeight)
        {
            if (calcifierWeight <= 0f) return 1f;
            float deficit = Mathf.Clamp01((8.1f - ph) / 0.5f); // 0 at pH 8.1, 1 at pH 7.6
            return Mathf.Clamp01(1f - deficit * calcifierWeight);
        }

        // Light at 5–20 m on a bright shallow Cabo Verde reef. Held constant in this
        // release — it exists as a term so the equations match the design document
        // and a day/night cycle can be added later without touching them.
        public const float Light = 1f;
    }
}
