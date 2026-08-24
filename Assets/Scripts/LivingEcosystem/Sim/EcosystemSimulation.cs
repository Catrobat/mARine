using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // Each species is a small set of numbers, not a crowd of creatures
    // (Design Document 3.1). The renderer draws a sample and scales it to the pool.
    public struct Pool
    {
        public float biomass;     // total energy held, in kg over the patch
        public float count;       // how many individuals (0 for producers)
        public float energy;      // running energy balance, read by the Why panel
        public float intake;      // biomass eaten this tick
        public float demand;      // biomass wanted this tick
        public float removed;     // biomass taken from this pool by predators this tick
        public bool  bleached;    // producers only
        public int   daysHungry;  // consecutive ticks with a negative energy balance
    }

    // The number-pool model. Plain arithmetic, one pass per simulated day, no
    // pathfinding, no spatial partitioning, no jobs — everything on the main thread
    // in microseconds (Design Document 3.1–3.3).
    //
    // The equations are the ones in the design document, with three additions that
    // the design document's shape implies but does not spell out, and without which
    // the web does not survive a session:
    //
    //   * a multi-prey Holling type II response, so feeding saturates when prey is
    //     plentiful AND demand switches to what is actually there when it is not;
    //   * a refuge per prey species, so predation fades before the prey is gone
    //     instead of chasing the last individual to extinction;
    //   * recruitment for producers, so a grazed-out alga can return once the
    //     pressure lifts rather than being stuck at zero for ever.
    //
    // All three are ordinary reef ecology and all three are data, not code.
    public class EcosystemSimulation
    {
        public readonly Pool[] pools = new Pool[SpeciesLibrary.Count];
        public bool[] present = SpeciesLibrary.AllPresent();

        public float detritus;
        public int   day;
        public float temperatureC = 24f;
        public float acidityPh    = 8.1f;

        // Seeded so a shared environment replays identically and bugs are
        // reproducible (Design Document 3.2).
        public int seed;
        System.Random _rng;

        readonly float[] _termScratch = new float[SpeciesLibrary.Count];
        readonly float[] _linkWant = new float[SpeciesLibrary.Web.Length];

        public EcosystemSimulation(EcosystemSettings settings, int seed = 0)
        {
            this.seed = seed;
            _rng = new System.Random(seed);
            Reset(settings);
        }

        public void Reset(EcosystemSettings settings)
        {
            settings?.Clamp();
            day = 0;
            detritus = 120f;
            _rng = new System.Random(seed);

            temperatureC = settings != null ? settings.temperatureC : 24f;
            acidityPh    = settings != null ? settings.acidityPh : 8.1f;
            present      = settings != null && settings.present != null
                         ? (bool[])settings.present.Clone()
                         : SpeciesLibrary.AllPresent();

            float lifeScale = settings != null
                ? EcosystemSettings.StartingLifeScale[Mathf.Clamp(settings.startingLife, 0, 2)]
                : 1f;

            var all = SpeciesLibrary.All;
            for (int i = 0; i < all.Length; i++)
            {
                pools[i] = default;
                if (!IsPresent(i)) continue;

                var s = all[i];
                if (s.IsProducer)
                {
                    pools[i].biomass = s.startingStock * lifeScale;
                }
                else
                {
                    // A transient visitor is present or it is not; "starting life"
                    // does not make half a shark.
                    pools[i].count = s.transient
                        ? Mathf.Round(s.startingStock)
                        : Mathf.Max(1f, s.startingStock * lifeScale);
                    pools[i].biomass = pools[i].count * s.unitMass;
                }
            }
        }

        public bool IsPresent(int i) =>
            present != null && i >= 0 && i < present.Length && present[i];

        // Species whose individuals are simulated one at a time rather than as a
        // number. Set by whoever owns those agents; nothing here needs to know what
        // they are, only to leave their births and deaths alone.
        public readonly bool[] agentManaged = new bool[SpeciesLibrary.Count];

        public bool IsAgentManaged(int i) =>
            i >= 0 && i < agentManaged.Length && agentManaged[i];

        // Removing a species mid-scene is deliberately abrupt (Design Document 6.1).
        public void SetPresent(int i, bool value)
        {
            if (present == null || i < 0 || i >= present.Length) return;
            if (present[i] == value) return;

            present[i] = value;
            var s = SpeciesLibrary.Get(i);
            if (s == null) return;

            if (!value)
            {
                // Whatever was alive becomes detritus; the web reorganises around the gap.
                detritus += pools[i].biomass * SpeciesLibrary.DetritusFromDeath;
                pools[i] = default;
            }
            else
            {
                // Adding one back reintroduces it at a small founding population,
                // which is itself instructive: recovery is slower than removal.
                if (s.IsProducer)
                    pools[i].biomass = s.startingStock * 0.15f;
                else
                {
                    pools[i].count = s.transient ? 1f : Mathf.Max(1f, Mathf.Round(s.startingStock * 0.20f));
                    pools[i].biomass = pools[i].count * s.unitMass;
                }
            }
        }

        public void Tick()
        {
            day++;
            var all = SpeciesLibrary.All;
            var web = SpeciesLibrary.Web;
            float metabolic = EcosystemBounds.MetabolicFactor(temperatureC);

            for (int i = 0; i < all.Length; i++)
            {
                pools[i].intake = 0f;
                pools[i].removed = 0f;
                pools[i].demand = 0f;
            }
            for (int k = 0; k < _linkWant.Length; k++)
                _linkWant[k] = 0f;

            // ── 1. Who wants what ────────────────────────────────────────────
            // Two passes: everyone states a want, then short supply is shared out in
            // proportion to demand. Allocating in array order instead would quietly
            // let whichever species is evaluated first eat before the others.
            float detritusWanted = 0f;
            for (int c = 0; c < all.Length; c++)
            {
                if (!IsPresent(c) || pools[c].count <= 0f) continue;
                var s = all[c];
                if (s.IsProducer) continue;

                float demand = pools[c].count * s.appetite * metabolic * Mathf.Max(0.01f, s.presence);
                pools[c].demand = demand;
                if (s.detritusFraction > 0f)
                    detritusWanted += demand * s.detritusFraction;
            }

            float detritusScale = detritusWanted > 1e-6f
                ? Mathf.Min(1f, detritus / detritusWanted)
                : 0f;

            for (int c = 0; c < all.Length; c++)
            {
                if (!IsPresent(c) || pools[c].count <= 0f) continue;
                var s = all[c];
                if (s.IsProducer) continue;

                if (s.detritusFraction > 0f)
                {
                    float take = pools[c].demand * s.detritusFraction * detritusScale;
                    pools[c].intake += take;
                    detritus -= take;
                }

                float liveDemand = pools[c].demand * (1f - s.detritusFraction);
                if (liveDemand <= 0f) continue;

                // Multi-prey Holling type II (the disc equation). One expression gives
                // both saturation — a full predator stops hunting — and switching:
                // demand aimed at prey that has run out moves to prey that has not,
                // instead of being thrown away on an empty larder.
                float denom = 1f;
                for (int k = 0; k < web.Length; k++)
                {
                    var link = web[k];
                    if (link.predator != c) continue;
                    int p = link.prey;
                    _termScratch[p] = 0f;
                    if (!IsPresent(p) || pools[p].biomass <= 1e-6f) continue;

                    var preyDef = all[p];
                    float reachable = Mathf.Max(0f, pools[p].biomass - preyDef.refuge);
                    if (reachable <= 0f) continue;

                    float term = link.preference * reachable / Mathf.Max(0.01f, link.halfSaturation);
                    _termScratch[p] = term;
                    denom += term;
                }

                for (int k = 0; k < web.Length; k++)
                {
                    var link = web[k];
                    if (link.predator != c) continue;
                    float term = _termScratch[link.prey];
                    if (term <= 0f) continue;

                    float want = liveDemand * term / denom;
                    _linkWant[k] = want;
                    pools[link.prey].removed += want;
                }
            }

            // Nothing may be eaten twice: where several predators claim the same pool,
            // scale every claim back to what is actually there, then hand each
            // predator only what it really got.
            for (int k = 0; k < web.Length; k++)
            {
                float want = _linkWant[k];
                if (want <= 0f) continue;

                var link = web[k];
                float claimed = pools[link.prey].removed;
                float scale = claimed > pools[link.prey].biomass && claimed > 0f
                    ? pools[link.prey].biomass / claimed
                    : 1f;

                pools[link.predator].intake += want * scale;
            }

            for (int p = 0; p < all.Length; p++)
                pools[p].removed = Mathf.Min(pools[p].removed, pools[p].biomass);

            // ── 2. Producers ─────────────────────────────────────────────────
            for (int i = 0; i < all.Length; i++)
            {
                var s = all[i];
                if (!s.IsProducer) continue;
                if (!IsPresent(i)) { pools[i] = default; continue; }

                float capacity = s.baseCapacity
                               * EcosystemBounds.Light
                               * EcosystemBounds.TemperatureFactor(temperatureC, s.optimumTemperature, s.temperatureTolerance)
                               * EcosystemBounds.CalcificationFactor(acidityPh, s.calcifierWeight);
                capacity = Mathf.Max(1f, capacity);

                float rate = s.growthRate;
                float lossRate = s.naturalLoss;

                // Bleaching: above its threshold the coral expels the symbionts that
                // supply most of its energy. It is not dead, and it recovers if the
                // water cools, but it grows barely at all and wastes away meanwhile.
                pools[i].bleached = false;
                if (s.CanBleach && temperatureC > s.bleachingThresholdC)
                {
                    float severity = Mathf.Clamp01((temperatureC - s.bleachingThresholdC) / 2.5f);
                    rate *= 1f - 0.85f * severity;
                    lossRate *= 1f + 3f * severity;
                    pools[i].bleached = true;
                }

                float biomass = pools[i].biomass;
                float growth = rate * biomass * (1f - biomass / capacity);

                // Spore and drift recruitment from the surrounding reef.
                if (biomass < capacity * 0.05f)
                    growth += s.recruitment * capacity;

                float loss = lossRate * biomass;
                float grazed = pools[i].removed;

                biomass += growth - grazed - loss;
                pools[i].biomass = Mathf.Max(0f, biomass);

                // The detritus loop: decomposition returning nutrients to the base.
                detritus += loss * SpeciesLibrary.DetritusFromDeath
                          + grazed * SpeciesLibrary.DetritusFromGrazing;
            }

            // ── 3. Consumers ─────────────────────────────────────────────────
            for (int c = 0; c < all.Length; c++)
            {
                var s = all[c];
                if (s.IsProducer) continue;
                if (!IsPresent(c)) { pools[c] = default; continue; }

                if (s.transient)
                {
                    // A transient apex predator is present or it is not. It does not
                    // breed here and it does not starve here — it swims elsewhere.
                    pools[c].count = 1f;
                    pools[c].biomass = s.unitMass;
                    pools[c].energy = 0f;
                    pools[c].daysHungry = 0;
                    continue;
                }

                if (pools[c].count <= 0f) continue;

                float gain = pools[c].intake * s.assimilation;
                float cost = pools[c].count * s.livingCost * metabolic;

                // Energy is kept as a running balance because the reasoning rules read
                // it ("consumer energy negative for 5+ days -> starvation"), while
                // births and deaths run off the supply/need ratio, which responds at once.
                pools[c].energy += gain - cost;
                pools[c].energy = Mathf.Clamp(pools[c].energy, -cost * 12f, cost * 12f);
                pools[c].daysHungry = gain < cost ? pools[c].daysHungry + 1 : 0;

                float ratio = cost > 1e-9f ? gain / cost : (gain > 0f ? 2f : 0f);

                // A species that has been promoted to individual agents stops here.
                // The pool still states its demand, takes its intake and suffers its
                // predation — so the food web around it is exactly the web that was
                // balanced — but who eats, who breeds and who dies is decided one
                // animal at a time by the agent layer, which writes the count back.
                if (IsAgentManaged(c)) continue;

                if (ratio >= 1f)
                {
                    float surplus = Mathf.Min(1f, (ratio - 1f) / SpeciesLibrary.SurplusForFullBreeding);
                    float room = Mathf.Max(0f, 1f - pools[c].count / Mathf.Max(1f, s.ceiling));
                    pools[c].count += pools[c].count * s.birthRate * surplus * room;
                }
                else
                {
                    float deficit = Mathf.Min(1f, (1f - ratio) / SpeciesLibrary.DeficitForFullDeath);
                    float deaths = pools[c].count * s.starveRate * deficit;
                    pools[c].count -= deaths;
                    detritus += deaths * s.unitMass * SpeciesLibrary.DetritusFromDeath;
                }

                // Predation removal, converted from biomass back into individuals.
                if (pools[c].removed > 0f && s.unitMass > 1e-6f)
                {
                    pools[c].count = Mathf.Max(0f, pools[c].count - pools[c].removed / s.unitMass);
                    detritus += pools[c].removed * 0.15f;
                }

                pools[c].count = Mathf.Clamp(pools[c].count, 0f, s.ceiling * 1.15f);
                if (pools[c].count < 0.35f)
                {
                    pools[c].count = 0f;
                    pools[c].energy = 0f;
                }
                pools[c].biomass = pools[c].count * s.unitMass;
            }

            // Bacteria return nutrients to the water; energy is genuinely lost at each
            // step and cannot be recycled, but nutrients are used again and again.
            detritus = Mathf.Max(0f, detritus * (1f - SpeciesLibrary.DetritusRemineralised));
        }

        // ── Readouts ─────────────────────────────────────────────────────────
        public float DisplayAmount(int i)
        {
            var s = SpeciesLibrary.Get(i);
            if (s == null) return 0f;
            return s.IsProducer ? pools[i].biomass : pools[i].count;
        }

        public bool IsAlive(int i) => IsPresent(i) && DisplayAmount(i) > 0.05f;

        // Total biomass at each trophic level, for the energy pyramid.
        public float BiomassAtLevel(TrophicLevel level)
        {
            float total = 0f;
            var all = SpeciesLibrary.All;
            for (int i = 0; i < all.Length; i++)
                if (all[i].level == level && IsPresent(i))
                    total += pools[i].biomass;
            return total;
        }

        public float TotalProducerBiomass() => BiomassAtLevel(TrophicLevel.Producer);

        // Producer biomass that something actually eats.
        //
        // The coral is a producer but is not grazed in this model, and it is the
        // slowest-growing thing here — so it survives a collapse almost untouched.
        // Judging the reef's health on total producer biomass would let the coral
        // mask a seafloor grazed down to bare rock, and the barren stage could never
        // be reached. Health is measured against the part of the base that is
        // actually under pressure.
        public float GrazeableProducerBiomass()
        {
            float total = 0f;
            var all = SpeciesLibrary.All;
            var web = SpeciesLibrary.Web;

            for (int i = 0; i < all.Length; i++)
            {
                if (!all[i].IsProducer || !IsPresent(i)) continue;

                bool grazed = false;
                for (int k = 0; k < web.Length; k++)
                    if (web[k].prey == i) { grazed = true; break; }

                if (grazed) total += pools[i].biomass;
            }
            return total;
        }
    }
}
