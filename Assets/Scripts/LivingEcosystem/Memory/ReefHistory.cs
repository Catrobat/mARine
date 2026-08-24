using System;
using UnityEngine;

namespace CreateEnv.Ecosystem.Memory
{
    // What the reef has been doing, kept as a rolling record.
    //
    // Feeds the charts in the report and the "what changed while you were away"
    // comparison. Stored as one byte per figure rather than as numbers, because the
    // save file has a 4 KB budget and a hundred days of nine species written as JSON
    // floats is six kilobytes on its own. A byte gives 1/255 resolution, which is far
    // finer than a chart drawn a few hundred pixels wide can show.
    [Serializable]
    public class ReefHistory
    {
        // A sample every few days rather than daily: three hundred days of history in
        // sixty samples, which is more than a session will ever produce and still
        // under a kilobyte.
        public const int DaysPerSample = 5;
        public const int Capacity = 60;

        // Per sample: nine species, three gene frequencies, one health level.
        public const int Stride = SpeciesLibrary.Count + 3 + 1;

        public byte[] samples = new byte[Capacity * Stride];
        public int count;          // how many samples are filled
        public int firstDay;       // simulated day of sample 0
        public int lastSampledDay = int.MinValue;

        public bool IsEmpty => count == 0;

        public void Clear()
        {
            Array.Clear(samples, 0, samples.Length);
            count = 0;
            firstDay = 0;
            lastSampledDay = int.MinValue;
        }

        // The largest value a species is ever likely to show, so its byte spans a
        // useful range instead of hugging zero.
        public static float ScaleOf(int species)
        {
            var def = SpeciesLibrary.Get(species);
            if (def == null) return 1f;
            return def.IsProducer ? Mathf.Max(1f, def.baseCapacity)
                                  : Mathf.Max(1f, def.ceiling);
        }

        public void MaybeSample(EcosystemSimulation sim, EcosystemHealth health,
                                Genetics.OctopusPopulation octopuses)
        {
            if (sim.day - lastSampledDay < DaysPerSample) return;
            Sample(sim, health, octopuses);
        }

        public void Sample(EcosystemSimulation sim, EcosystemHealth health,
                           Genetics.OctopusPopulation octopuses)
        {
            lastSampledDay = sim.day;

            // Full: drop the oldest sample and shuffle down. Sixty entries of thirteen
            // bytes is a memcpy of under a kilobyte, a few times a minute at most.
            if (count >= Capacity)
            {
                Array.Copy(samples, Stride, samples, 0, (Capacity - 1) * Stride);
                count = Capacity - 1;
                firstDay += DaysPerSample;
            }
            else if (count == 0)
            {
                firstDay = sim.day;
            }

            int at = count * Stride;
            for (int i = 0; i < SpeciesLibrary.Count; i++)
                samples[at + i] = Quantise(sim.DisplayAmount(i) / ScaleOf(i));

            int g = at + SpeciesLibrary.Count;
            if (octopuses != null)
            {
                samples[g + 0] = Quantise(octopuses.AlleleFrequency(Genetics.GeneId.Camouflage));
                samples[g + 1] = Quantise(octopuses.AlleleFrequency(Genetics.GeneId.BodySize));
                samples[g + 2] = Quantise(octopuses.AlleleFrequency(Genetics.GeneId.HeatTolerance));
            }

            samples[at + Stride - 1] = (byte)Mathf.Clamp((int)health.level, 0, 2);
            count++;
        }

        static byte Quantise(float unit) => (byte)Mathf.Clamp(Mathf.RoundToInt(unit * 255f), 0, 255);

        // ── Reading back ─────────────────────────────────────────────────────

        public int DayAt(int sample) => firstDay + sample * DaysPerSample;

        // A species' amount at a sample, back in its own units.
        public float AmountAt(int sample, int species)
        {
            if (sample < 0 || sample >= count) return 0f;
            return samples[sample * Stride + species] / 255f * ScaleOf(species);
        }

        // A gene's frequency at a sample, 0..1.
        public float GeneAt(int sample, Genetics.GeneId gene)
        {
            if (sample < 0 || sample >= count) return 0f;
            return samples[sample * Stride + SpeciesLibrary.Count + (int)gene] / 255f;
        }

        public HealthLevel HealthAt(int sample)
        {
            if (sample < 0 || sample >= count) return HealthLevel.Green;
            return (HealthLevel)samples[sample * Stride + Stride - 1];
        }

        // How much a species has changed across the whole record, as a fraction.
        public float ChangeOf(int species)
        {
            if (count < 2) return 0f;
            float first = AmountAt(0, species);
            float last = AmountAt(count - 1, species);
            if (first < 0.01f) return last > 0.01f ? 1f : 0f;
            return (last - first) / first;
        }

        // ── Persistence ──────────────────────────────────────────────────────

        public string Pack()
        {
            if (count == 0) return "";
            var used = new byte[count * Stride];
            Array.Copy(samples, used, used.Length);
            return Convert.ToBase64String(used);
        }

        public void Unpack(string packed, int firstDayValue, int lastDay)
        {
            Clear();
            if (string.IsNullOrEmpty(packed)) return;

            try
            {
                var bytes = Convert.FromBase64String(packed);
                int samplesIn = Mathf.Min(Capacity, bytes.Length / Stride);
                Array.Copy(bytes, samples, samplesIn * Stride);
                count = samplesIn;
                firstDay = firstDayValue;
                lastSampledDay = lastDay;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ReefHistory] Could not read the saved history: {e.Message}");
                Clear();
            }
        }
    }
}
