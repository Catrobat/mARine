using System;
using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // The user-facing half of the Living Ecosystem: five controls and nothing else.
    // Mirrors SimpleEnvironmentSettings exactly — the editor UI edits this, the
    // simulation reads it, and it rides inside EnvironmentProfile so it is saved,
    // loaded and cloned by machinery that already exists.
    //
    // Design Document §5. Five controls is the whole set; additions are out of scope.
    [Serializable]
    public class EcosystemSettings
    {
        public static readonly string[] StartingLifeOptions = { "Few", "Balanced", "Many" };
        public static readonly string[] SpeedOptions        = { "Paused", "Normal", "Fast" };

        // Seconds of real time per simulated day, indexed by speed. Paused never ticks.
        public static readonly float[] SecondsPerDay = { 0f, 2f, 0.25f };

        // Multiplier on every pool's starting stock, indexed by startingLife.
        public static readonly float[] StartingLifeScale = { 0.45f, 1f, 1.6f };

        // ── The five controls ────────────────────────────────────────────────
        public bool enabled = false;

        // One flag per species, indexed by SpeciesLibrary order. Unchecking a
        // species seeds it at zero and locks it there — no new simulation logic.
        public bool[] present = SpeciesLibrary.AllPresent();

        public int   startingLife = 1;     // Balanced
        public float temperatureC = 24f;   // Cabo Verde shallow-water annual mean
        public float acidityPh    = 8.1f;  // present-day open-ocean surface pH
        public int   speed        = 1;     // Normal

        // ── Prediction prompt (Design Document §5.1) ─────────────────────────
        // What the learner predicted before entering, remembered so the outcome can
        // be compared against it afterwards. -1 = no prediction made.
        public int predictionIndex = -1;

        public float SecondsPerSimDay => SecondsPerDay[Mathf.Clamp(speed, 0, SpeedOptions.Length - 1)];
        public bool  IsPaused         => Mathf.Clamp(speed, 0, SpeedOptions.Length - 1) == 0;

        public bool IsPresent(int speciesIndex)
        {
            if (present == null || speciesIndex < 0 || speciesIndex >= present.Length)
                return false;
            return present[speciesIndex];
        }

        public void Clamp()
        {
            startingLife = Mathf.Clamp(startingLife, 0, StartingLifeOptions.Length - 1);
            speed        = Mathf.Clamp(speed, 0, SpeedOptions.Length - 1);
            temperatureC = EcosystemBounds.Temperature.Clamp(temperatureC);
            acidityPh    = EcosystemBounds.Acidity.Clamp(acidityPh);

            // The species list is the one field that can go stale: a profile saved
            // before a species was added would carry a short array. Grow it, keeping
            // the learner's existing choices and defaulting anything new to present.
            int n = SpeciesLibrary.Count;
            if (present == null || present.Length != n)
            {
                var grown = SpeciesLibrary.AllPresent();
                if (present != null)
                    for (int i = 0; i < Mathf.Min(present.Length, n); i++)
                        grown[i] = present[i];
                present = grown;
            }

            if (predictionIndex < -1) predictionIndex = -1;
        }

        public EcosystemSettings Clone()
        {
            var c = (EcosystemSettings)MemberwiseClone();
            c.present = present != null ? (bool[])present.Clone() : SpeciesLibrary.AllPresent();
            return c;
        }
    }
}
