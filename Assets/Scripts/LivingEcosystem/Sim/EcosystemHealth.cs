using UnityEngine;

namespace CreateEnv.Ecosystem
{
    public enum HealthLevel { Green = 0, Amber = 1, Red = 2 }

    public enum CollapseStage
    {
        Healthy     = 0,
        Imbalance   = 1,  // one population climbs abnormally
        Overgrazing = 2,  // producer biomass visibly falling
        Starvation  = 3,  // the overgrown population stops growing, then falls
        Collapse    = 4,  // higher levels lose their food and decline
        Barren      = 5,  // only detritus and the slowest-growing survivors remain
    }

    // Watches the simulation and decides how it is doing. Collapse is staged rather
    // than instantaneous so each stage can be read as it passes
    // (Design Document 6.3). Stages only advance while things are getting worse;
    // recovery walks them back down, because a learner who fixes the reef should see
    // it say so.
    public class EcosystemHealth
    {
        public HealthLevel level = HealthLevel.Green;
        public CollapseStage stage = CollapseStage.Healthy;

        // Producer biomass when the reef was last healthy — the yardstick everything
        // else is measured against.
        public float baselineProducers;
        public int   stageEnteredDay;

        readonly TrendTracker _producerTrend = new TrendTracker(20);

        public float ProducerFraction { get; private set; } = 1f;

        public void Reset(EcosystemSimulation sim)
        {
            baselineProducers = Mathf.Max(1f, sim.GrazeableProducerBiomass());
            level = HealthLevel.Green;
            stage = CollapseStage.Healthy;
            stageEnteredDay = 0;
            ProducerFraction = 1f;
            _producerTrend.Reset();
        }

        public void Evaluate(EcosystemSimulation sim)
        {
            float producers = sim.GrazeableProducerBiomass();
            _producerTrend.Push(producers);

            // A reef that grows past its old baseline resets the yardstick, so a
            // recovered system is not judged against a peak it never had.
            if (producers > baselineProducers) baselineProducers = producers;
            ProducerFraction = producers / Mathf.Max(1f, baselineProducers);

            var next = ClassifyStage(sim);
            if (next != stage)
            {
                stage = next;
                stageEnteredDay = sim.day;
            }

            level = stage switch
            {
                CollapseStage.Healthy     => HealthLevel.Green,
                CollapseStage.Imbalance   => HealthLevel.Amber,
                CollapseStage.Overgrazing => HealthLevel.Amber,
                _                         => HealthLevel.Red,
            };
        }

        CollapseStage ClassifyStage(EcosystemSimulation sim)
        {
            var all = SpeciesLibrary.All;
            float fraction = ProducerFraction;

            // Stage 5 — barren. Producers all but gone.
            if (fraction < 0.12f) return CollapseStage.Barren;

            // Stage 4 — collapse. Higher levels have lost their food.
            bool huntersFailing = false;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].IsProducer || all[i].transient) continue;
                if (all[i].level != TrophicLevel.Hunter) continue;
                if (sim.IsPresent(i) && sim.pools[i].count <= 0f) huntersFailing = true;
            }
            if (fraction < 0.30f || (fraction < 0.55f && huntersFailing))
                return CollapseStage.Collapse;

            // Stage 3 — starvation. Somebody has been running an energy deficit.
            int starving = 0;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].IsProducer || all[i].transient) continue;
                if (sim.IsPresent(i) && sim.pools[i].daysHungry >= 5) starving++;
            }
            if (fraction < 0.55f || starving >= 2) return CollapseStage.Starvation;

            // Stage 2 — overgrazing. Producer biomass is visibly falling.
            if (fraction < 0.80f && _producerTrend.Direction < 0) return CollapseStage.Overgrazing;

            // Stage 1 — imbalance. One population is climbing abnormally.
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i].IsProducer || all[i].transient) continue;
                if (!sim.IsPresent(i)) continue;
                if (sim.pools[i].count > all[i].startingStock * 1.9f)
                    return CollapseStage.Imbalance;
            }
            if (fraction < 0.88f) return CollapseStage.Imbalance;

            return CollapseStage.Healthy;
        }

        public static string Describe(CollapseStage stage) => stage switch
        {
            CollapseStage.Healthy     => "Balanced",
            CollapseStage.Imbalance   => "Something is climbing",
            CollapseStage.Overgrazing => "Being eaten faster than it grows",
            CollapseStage.Starvation  => "Animals are starting to starve",
            CollapseStage.Collapse    => "The food web is coming apart",
            CollapseStage.Barren      => "Barren",
            _ => "",
        };

        public static Color Colour(HealthLevel level) => level switch
        {
            HealthLevel.Green => new Color(0.24f, 0.72f, 0.36f),
            HealthLevel.Amber => new Color(0.94f, 0.68f, 0.18f),
            _                 => new Color(0.86f, 0.30f, 0.26f),
        };
    }

    // A short rolling window, used for the trend arrows in the panel and for the
    // "is it rising or falling" half of the reasoning rules.
    public class TrendTracker
    {
        readonly float[] _values;
        int _written;

        public TrendTracker(int window)
        {
            _values = new float[Mathf.Max(2, window)];
        }

        public void Reset() => _written = 0;

        public void Push(float value)
        {
            _values[_written % _values.Length] = value;
            _written++;
        }

        public bool Ready => _written >= _values.Length;

        public float Oldest =>
            _written < _values.Length ? _values[0] : _values[_written % _values.Length];

        public float Newest => _written == 0 ? 0f : _values[(_written - 1) % _values.Length];

        // Fractional change across the window.
        public float Change
        {
            get
            {
                if (_written < 2) return 0f;
                float old = Oldest;
                return Mathf.Abs(old) < 1e-4f ? 0f : (Newest - old) / old;
            }
        }

        // -1 falling, 0 steady, +1 rising. The 4% dead band keeps the arrows from
        // flickering on noise.
        public int Direction
        {
            get
            {
                float c = Change;
                if (c > 0.04f) return 1;
                if (c < -0.04f) return -1;
                return 0;
            }
        }
    }
}
