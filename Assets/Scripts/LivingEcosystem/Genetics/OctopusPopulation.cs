using System.Collections.Generic;
using UnityEngine;

namespace CreateEnv.Ecosystem.Genetics
{
    // The octopuses, one animal at a time.
    //
    // This sits on top of the pool rather than replacing it. Each tick the pool has
    // already worked out how much food the octopuses collectively got and how much
    // biomass the shark took from them; this layer decides which individuals ate,
    // which were caught, which bred and which died, then writes the head count back.
    // The food web the balance work produced is therefore untouched.
    //
    // Selection is not scripted (Design Document 4.3). There is no fitness score
    // anywhere in this file. A heat-intolerant octopus spends more energy staying
    // alive in warm water, so it more often starves before it breeds. A poorly
    // camouflaged one is chosen by the shark more often. Gene frequencies shift
    // because some individuals leave more descendants than others — which is the
    // whole of natural selection (Literature 3.6).
    public class OctopusPopulation
    {
        // ── Tunables. The milestone names generation speed as the main risk, and
        // maturity is the one number that controls it. ────────────────────────
        public const float MaturityDays = 92f;    // ~3 simulated months (real: about a year)
        public const float MaxAgeDays   = 330f;   // about a year; most breed and die first
        public const float BroodDays    = 34f;    // she fasts for the whole of it
        public const float MaleDeclineDays = 26f; // he declines after mating, then dies
        public const int   MaxAgents    = 5;      // with the shark, 9 or fewer live agents
                                          // (the design allows 4-8; this reef feeds about five,
                                          //  which is exactly where Step 1's pool balanced)
        public const int   MinBrood     = 2;
        public const int   MaxBrood     = 4;
        public const float BreedingCondition = 1.20f;  // of a maximum of 2
        public const float PreyNeededToBreed = 0.45f;  // share of a healthy reef's prey

        // Chance per day that a female who could breed actually does. Multiplied by
        // up to four as she ages, so most breed before they die of old age but at
        // scattered times rather than all at once.
        public const float DailyBreedingChance = 0.014f;
        public const float Reserves = 12f;             // days of upkeep an octopus carries

        // Settlement from the plankton.
        //
        // Octopus young drift for around sixty-five days before settling to the
        // seafloor (Literature 4.8), which means a reef is not a closed box: it
        // receives young spawned elsewhere. Modelling that is not a convenience, it
        // is the mechanism that keeps a real patch of reef occupied.
        //
        // It matters doubly here. Five agents is a very small population, and a
        // semelparous one breeds and dies in cohorts — so a single failed brood, or
        // one generation ageing out together, ends the line for good.
        //
        // Deliberately a last resort, triggered only when the reef is down to its
        // final animal. Settlers carry whatever the wider sea carries, which is an
        // even mix, so a steady trickle of them drags every allele frequency towards
        // fifty percent — strongly enough, at this population size, to cancel out the
        // selection the learner is supposed to be watching.
        public const float SettlementIntervalDays = 38f;
        public const int   SettlementBelow = 2;

        // Raised far above reality so new variation is visible within a session, but
        // not so far that it drowns the thing it is meant to reveal. Mutation pushes
        // an allele towards an even split; at 3.5% per transmission that pressure was
        // comparable to the selection acting on heat tolerance, and warming barely
        // moved the frequency. Around a hundredth is still thousands of times the real
        // rate. The interface says the rate is raised wherever it matters
        // (Literature 3.5).
        public const float MutationRate = 0.012f;

        public const int GenerationCap = 8;       // older generations collapse to a summary

        public readonly List<OctopusAgent> agents = new List<OctopusAgent>(MaxAgents);
        public readonly List<AncestorRecord> ancestors = new List<AncestorRecord>(64);
        // Where life-cycle events are recorded. Set by whoever owns this
        // population; nothing here depends on it existing.
        public Memory.ReefJournal journal;

        System.Random _rng;
        int _nextId;
        int _nextNameIndex;
        readonly Dictionary<int, int> _nameIndexById = new Dictionary<int, int>(64);

        // Prey biomass a healthy reef carries, worked out once from the species
        // table. Breeding is measured against this rather than against a bare count.
        float _preyBaseline;
        float _settlementTimer;

        public int NextId => _nextId;
        public int NextNameIndex => _nextNameIndex;

        public int NameIndexOf(int id) =>
            _nameIndexById.TryGetValue(id, out int index) ? index : 0;

        public int HighestGeneration { get; private set; }
        public int TotalBorn { get; private set; }
        public int TotalDied { get; private set; }

        public int AliveCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < agents.Count; i++) if (agents[i].IsAlive) n++;
                return n;
            }
        }

        // ── Founding ─────────────────────────────────────────────────────────

        // Seeds the founders with every variant present.
        //
        // Selection can only act on variation that already exists (Literature 3.7).
        // A founding population that happens to be all one genotype can never evolve,
        // and the feature would look broken rather than finished — so the founders are
        // laid out to guarantee variety rather than left to chance.
        public void Found(int count, int seed)
        {
            agents.Clear();
            ancestors.Clear();
            _nameIndexById.Clear();
            _rng = new System.Random(seed ^ 0x0C7095);
            _nextId = 0;
            _nextNameIndex = 0;
            HighestGeneration = 0;
            TotalBorn = 0;
            TotalDied = 0;

            count = Mathf.Clamp(count, 2, MaxAgents);

            // A spread that guarantees both camouflage variants and all three levels
            // of each additive gene are present from day zero.
            // Laid out so that no variant is confined to one sex. Sexes alternate
            // down this list, and an earlier arrangement happened to give both
            // heat-tolerant genotypes to females — so in warm water the only animals
            // that survived could not pass the tolerant allele on, and the population
            // died out having never evolved. Every gene now has both its variants
            // represented among the males and among the females.
            var plan = new (int cam, int size, int heat)[]
            {
                (1, 1, 1),   // F  Aa Bb Cc — carries everything
                (2, 0, 2),   // M  AA bb CC — tolerant male
                (0, 2, 0),   // F  aa BB cc — the weak-camouflage variant, in the open
                (1, 2, 1),   // M
                (2, 1, 2),   // F  tolerant female
                (0, 1, 1),   // M  carries the weak-camouflage variant
                (1, 0, 0),   // F
                (2, 2, 1),   // M
            };

            for (int i = 0; i < count; i++)
            {
                var p = plan[i % plan.Length];
                var genome = Genome.FromCopies(p.cam, p.size, p.heat);
                // Alternate the sexes so a founding population always has both.
                var sex = (i % 2 == 0) ? Sex.Female : Sex.Male;
                var agent = Create(genome, sex, generation: 1, motherId: -1, fatherId: -1, day: 0);

                // Founders start as adults of mixed ages, so they do not all mature,
                // breed and die in lockstep.
                agent.ageDays = MaturityDays * (0.55f + 0.5f * (float)_rng.NextDouble());
                agents.Add(agent);
            }
            HighestGeneration = 1;

            _settlementTimer = 0f;
            _preyBaseline = 0f;
            var web = SpeciesLibrary.Web;
            for (int k = 0; k < web.Length; k++)
            {
                if (web[k].predator != SpeciesLibrary.Octopus) continue;
                var prey = SpeciesLibrary.Get(web[k].prey);
                if (prey != null) _preyBaseline += prey.startingStock * prey.unitMass;
            }
        }

        OctopusAgent Create(Genome genome, Sex sex, int generation, int motherId, int fatherId, int day)
        {
            int nameIndex = _nextNameIndex++;
            var agent = new OctopusAgent
            {
                id = _nextId++,
                nameIndex = nameIndex,
                name = OctopusNames.At(nameIndex),
                genome = genome,
                sex = sex,
                generation = generation,
                motherId = motherId,
                fatherId = fatherId,
                noiseSeed = (float)_rng.NextDouble(),
                energy = 1f,
                bornOnDay = day,
            };
            agent.RefreshTraits();
            _nameIndexById[agent.id] = nameIndex;
            TotalBorn++;
            if (generation > HighestGeneration) HighestGeneration = generation;
            return agent;
        }

        // Puts a saved population back exactly as it was.
        //
        // The pedigree is keyed by id, so the counters have to come back too: restart
        // them at zero and the next octopus born would reuse a dead one's id, and the
        // family tree would quietly graft one lineage onto another.
        public void Restore(OctopusAgent[] living, AncestorRecord[] pedigree,
                            int nextId, int nextNameIndex,
                            int highestGeneration, int totalBorn, int totalDied)
        {
            agents.Clear();
            ancestors.Clear();
            _nameIndexById.Clear();

            if (living != null)
                foreach (var a in living)
                {
                    agents.Add(a);
                    _nameIndexById[a.id] = a.nameIndex;
                }

            if (pedigree != null)
                foreach (var r in pedigree)
                {
                    ancestors.Add(r);
                    if (!_nameIndexById.ContainsKey(r.id)) _nameIndexById[r.id] = r.nameIndex;
                }

            _nextId = Mathf.Max(nextId, 0);
            _nextNameIndex = Mathf.Max(nextNameIndex, 0);
            HighestGeneration = Mathf.Max(1, highestGeneration);
            TotalBorn = totalBorn;
            TotalDied = totalDied;

            // Seeded from where the reef left off, so a restored session does not
            // replay the same rolls the last one made.
            _rng = new System.Random(nextId * 7919 + highestGeneration * 104729 + totalBorn);
            _settlementTimer = 0f;

            _preyBaseline = 0f;
            var web = SpeciesLibrary.Web;
            for (int k = 0; k < web.Length; k++)
            {
                if (web[k].predator != SpeciesLibrary.Octopus) continue;
                var prey = SpeciesLibrary.Get(web[k].prey);
                if (prey != null) _preyBaseline += prey.startingStock * prey.unitMass;
            }
        }

        // ── One simulated day ────────────────────────────────────────────────

        public void Tick(EcosystemSimulation sim, int speciesIndex)
        {
            var def = SpeciesLibrary.Get(speciesIndex);
            if (def == null) return;

            if (!sim.IsPresent(speciesIndex))
            {
                if (agents.Count > 0)
                {
                    for (int i = 0; i < agents.Count; i++)
                        Retire(agents[i], CauseOfDeath.Predated, sim.day);
                    agents.Clear();
                }
                WriteBack(sim, speciesIndex, def);
                return;
            }

            float metabolic = EcosystemBounds.MetabolicFactor(sim.temperatureC);

            if (agents.Count > 0)
            {
                Feed(sim, speciesIndex, def, metabolic);
                Predation(sim, speciesIndex, def);
                LiveAndDie(sim, def, metabolic);
                Breed(sim, def);
            }

            // Settlement runs even when the reef is empty. A patch of seafloor that
            // has lost its octopuses is not sterile — it is somewhere young drifting
            // in from elsewhere can settle, which is how a real reef is recolonised.
            Settle(sim, def);

            WriteBack(sim, speciesIndex, def);
        }

        // Shares out what the pool actually caught. A bigger octopus is the better
        // hunter, so it takes a larger share — and pays for it in the next step.
        void Feed(EcosystemSimulation sim, int speciesIndex, SpeciesDefinition def, float metabolic)
        {
            float caught = sim.pools[speciesIndex].intake;

            float totalShare = 0f;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                // A brooding female does not eat at all. She fans the eggs, cleans
                // them, defends them, and starves while she does it (Literature 4.8).
                // Size decides how good a hunter it is, but a day's hunting also
                // has luck in it. Without that variation every octopus runs an
                // identical energy budget and, when food tightens, they all cross
                // zero on the same day and the species vanishes in one step instead
                // of thinning to what the reef can feed.
                a.huntingShare = a.IsBrooding
                    ? 0f
                    : a.traits.bodySize * (0.75f + 0.5f * (float)_rng.NextDouble());
                totalShare += a.huntingShare;
            }

            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];

                // A brooding female is outside the feeding economy entirely. She is
                // living off reserves, and that drain is applied once, in LiveAndDie.
                // Running her through the normal equation as well — no intake, full
                // cost — burned her out in two days and no brood ever hatched.
                if (a.IsBrooding)
                {
                    a.state = OctopusState.Brooding;
                    continue;
                }

                float got = totalShare > 1e-6f ? caught * (a.huntingShare / totalShare) : 0f;
                float gain = got * def.assimilation;

                // What it costs this individual to stay alive today. Heat tolerance
                // enters here and nowhere else: in warm water an intolerant animal
                // simply burns more, and the consequences follow on their own.
                // HeatCost already carries the whole temperature effect, tolerance
                // included, so the pool's shared metabolic factor is not applied again.
                float cost = def.livingCost * a.traits.bodySize
                           * HeatCost(sim.temperatureC, a.traits.heatTolerance);

                // Energy is a running store, not a bank: it saturates, so a well-fed
                // octopus cannot hoard its way through an arbitrarily long famine.
                //
                // Reserves is how many days of its own upkeep an animal carries. At
                // one day's worth a single thin week killed the whole population and
                // there was nothing left to evolve; a real octopus goes weeks without
                // eating, and a brooding female does exactly that on purpose.
                float delta = (gain - cost) / Mathf.Max(1e-4f, def.livingCost * Reserves);
                a.energy = Mathf.Clamp(a.energy + delta, 0f, 2f);
                a.state = a.IsBrooding ? OctopusState.Brooding
                        : got > 0f ? OctopusState.Hunting
                        : OctopusState.Resting;
            }
        }

        // What a day of staying alive costs this individual, relative to a
        // comfortable reef.
        //
        // Warming raises the metabolic rate of a cold-blooded animal, so it needs
        // more food simply to exist (Literature 2.9) — and heat tolerance is
        // precisely the ability to feel that less. Applying the temperature rise
        // equally to everyone and then adding a separate penalty for the intolerant
        // was wrong twice over: a perfectly heat-tolerant octopus still starved at
        // 30 C, so the trait it is being selected for did not actually save it.
        //
        // Below the comfortable temperature nothing is charged, which is why warming
        // the water is what spreads the tolerant variant and leaving it alone is not.
        public static float HeatCost(float temperatureC, float heatTolerance)
        {
            const float comfortable = 24f;
            float excess = Mathf.Max(0f, temperatureC - comfortable);
            if (excess <= 0f) return 1f;

            // A tolerant animal pays a quarter of what an intolerant one pays.
            float perDegree = 0.10f * (1f - 0.78f * Mathf.Clamp01(heatTolerance));
            return 1f + excess * perDegree;
        }

        // The shark took a certain amount of octopus biomass. Which octopuses?
        // The poorly camouflaged ones, more often.
        void Predation(EcosystemSimulation sim, int speciesIndex, SpeciesDefinition def)
        {
            float taken = sim.pools[speciesIndex].removed;
            if (taken <= 0f) return;

            int guard = 0;
            while (taken > 0f && agents.Count > 0 && guard++ < MaxAgents * 2)
            {
                // Weighted draw: exposure is the inverse of camouflage, so a weakly
                // camouflaged animal is several times likelier to be the one caught.
                float total = 0f;
                for (int i = 0; i < agents.Count; i++)
                    total += Exposure(agents[i]);
                if (total <= 1e-6f) break;

                float roll = (float)_rng.NextDouble() * total;
                int victim = agents.Count - 1;
                for (int i = 0; i < agents.Count; i++)
                {
                    roll -= Exposure(agents[i]);
                    if (roll <= 0f) { victim = i; break; }
                }

                var caught = agents[victim];
                float mass = caught.Mass(def.unitMass);
                // Only actually taken if the shark's remaining appetite covers a
                // decent share of this animal; otherwise it got away.
                if (taken < mass * 0.5f)
                {
                    // It got away. Never overwrite a brooding female's state — she is
                    // in her den, and what she is doing matters more than the near miss.
                    if (!caught.IsBrooding) caught.state = OctopusState.Fleeing;
                    break;
                }

                taken -= mass;
                Retire(caught, CauseOfDeath.Predated, sim.day);
                agents.RemoveAt(victim);
            }
        }

        static float Exposure(OctopusAgent a)
        {
            // A brooding female is in her den and much harder to find, which is one
            // reason brooding is worth the starvation.
            float hidden = a.IsBrooding ? 0.35f : 1f;
            return Mathf.Max(0.05f, (1.05f - a.traits.camouflage)) * hidden;
        }

        void LiveAndDie(EcosystemSimulation sim, SpeciesDefinition def, float metabolic)
        {
            for (int i = agents.Count - 1; i >= 0; i--)
            {
                var a = agents[i];
                a.ageDays += 1f;

                if (a.IsBrooding)
                {
                    a.broodDaysRemaining -= 1f;
                    // She loses a large fraction of her body weight while brooding.
                    a.energy = Mathf.Max(0f, a.energy - 0.028f);
                    if (a.broodDaysRemaining <= 0f)
                    {
                        Hatch(a, sim, def);
                        // Within days of the eggs hatching, she dies. This is real,
                        // and it is what makes the feature work.
                        Retire(a, CauseOfDeath.AfterBreeding, sim.day);
                        agents.RemoveAt(i);
                        continue;
                    }
                    continue;
                }

                if (a.HasMated)
                {
                    a.declineDaysRemaining -= 1f;
                    // He eats less and less as he declines; this is programmed, not
                    // accidental (Literature 4.8, the optic gland).
                    a.energy = Mathf.Max(0f, a.energy - 0.012f);
                    if (a.declineDaysRemaining <= 0f)
                    {
                        Retire(a, CauseOfDeath.AfterBreeding, sim.day);
                        agents.RemoveAt(i);
                        continue;
                    }
                }

                if (a.energy <= 0.02f)
                {
                    Retire(a, CauseOfDeath.Starved, sim.day);
                    agents.RemoveAt(i);
                    continue;
                }

                if (a.ageDays >= MaxAgeDays)
                {
                    Retire(a, CauseOfDeath.OldAge, sim.day);
                    agents.RemoveAt(i);
                }
            }
        }

        void Breed(EcosystemSimulation sim, SpeciesDefinition def)
        {
            if (agents.Count >= MaxAgents) return;

            // A floor, not the main regulator: octopuses do not breed on a reef
            // that has been stripped. The real density control is the condition
            // requirement above — now that an octopus carries a dozen days of
            // reserves, its energy takes months to climb when food is tight and only
            // days when it is plentiful, which makes it an honest measure of how
            // crowded the reef is. Judging instead against a fixed healthy-reef
            // figure blocked breeding outright in warm water, where the reef simply
            // never reaches that figure.
            float preyNow = 0f;
            var web = SpeciesLibrary.Web;
            for (int k = 0; k < web.Length; k++)
            {
                if (web[k].predator != SpeciesLibrary.Octopus) continue;
                if (!sim.IsPresent(web[k].prey)) continue;
                preyNow += sim.pools[web[k].prey].biomass;
            }
            if (_preyBaseline > 0f && preyNow < _preyBaseline * PreyNeededToBreed) return;

            OctopusAgent female = null, male = null;
            for (int i = 0; i < agents.Count; i++)
            {
                var a = agents[i];
                if (!a.IsAlive || a.IsBrooding) continue;
                if (!a.IsMature(MaturityDays)) continue;
                // She must be in real condition, not merely fed: brooding means a
                // month without eating. Requiring a surplus rather than a living wage
                // is also what stops the population breeding itself up to the cap and
                // straight into a famine.
                if (a.energy < BreedingCondition) continue;

                if (a.sex == Sex.Female && female == null) female = a;
                else if (a.sex == Sex.Male && male == null) male = a;
            }

            if (female == null || male == null) return;

            // Being able to breed is not the same as breeding today.
            //
            // Pairing off the first eligible female with the first eligible male on
            // the very tick both qualified meant the whole reef bred within days of
            // maturing, brooded together and died together — one synchronised cohort,
            // which is neither what a real population does nor something a learner
            // can read. It also drowned out the pair they had deliberately chosen.
            //
            // Instead each ready female has a modest chance each day, rising as she
            // ages: breeding once is the last thing an octopus does, and one running
            // out of time is likelier to do it. Animals therefore breed at their own
            // times, and the pair the learner picks is the event that stands out.
            float pastMaturity = Mathf.Clamp01((female.ageDays - MaturityDays) /
                                               Mathf.Max(1f, MaxAgeDays - MaturityDays));
            float chanceToday = DailyBreedingChance * (1f + pastMaturity * 3f);
            if (_rng.NextDouble() > chanceToday) return;

            StartBrood(female, male, sim, def);
        }

        // Made public so the breeding tool can pair two animals the learner chose.
        public bool StartBrood(OctopusAgent female, OctopusAgent male,
                               EcosystemSimulation sim, SpeciesDefinition def)
        {
            if (female == null || male == null) return false;
            if (female.sex != Sex.Female || male.sex != Sex.Male) return false;
            if (!female.IsAlive || !male.IsAlive) return false;
            if (female.IsBrooding) return false;
            if (!female.IsMature(MaturityDays) || !male.IsMature(MaturityDays)) return false;

            female.state = OctopusState.Brooding;
            female.broodDaysRemaining = BroodDays;
            female.mateId = male.id;
            female.pendingYoung = _rng.Next(MinBrood, MaxBrood + 1);
            // The genome she will breed from is fixed at mating: the male transfers
            // sperm packets with his hectocotylus and she stores them, so his dying
            // first does not stop the brood.
            female.storedMate = male.genome;
            female.storedMateId = male.id;

            // He starts declining, rather than dropping where he stands. He is still
            // usually dead well before the eggs hatch, but he may father another
            // brood first — which is what real males do, and what keeps a population
            // of half a dozen from stalling the moment its one male mates.
            male.state = OctopusState.Mating;
            if (!male.HasMated) male.declineDaysRemaining = MaleDeclineDays;

            Record(Memory.ReefEventKind.OctopusMated, sim.day, female.id, 0, male.id);
            return true;
        }

        // A young octopus arrives from the plankton and settles on this reef.
        void Settle(EcosystemSimulation sim, SpeciesDefinition def)
        {
            // The clock runs whether or not the reef needs a settler, so one is
            // already due the moment the population drops. Resetting it on every
            // recovery meant a crash from five to none — which takes about six weeks
            // — always outran the settlement it was supposed to trigger.
            _settlementTimer += 1f;

            if (agents.Count >= SettlementBelow || agents.Count >= MaxAgents) return;
            if (_settlementTimer < SettlementIntervalDays) return;
            _settlementTimer = 0f;

            // It only settles where there is something to eat.
            float preyNow = 0f;
            var web = SpeciesLibrary.Web;
            for (int k = 0; k < web.Length; k++)
            {
                if (web[k].predator != SpeciesLibrary.Octopus) continue;
                if (!sim.IsPresent(web[k].prey)) continue;
                preyNow += sim.pools[web[k].prey].biomass;
            }
            if (_preyBaseline > 0f && preyNow < _preyBaseline * 0.35f) return;

            // A settler carries whatever the wider sea carries.
            var genome = Genome.Random(_rng);

            // Its sex is a coin toss, except that a reef holding only one sex gets
            // the other — a disclosed nudge, because with a population this small an
            // unlucky run of one sex is a dead end the learner can do nothing about.
            Sex sex;
            bool anyFemale = false, anyMale = false;
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].sex == Sex.Female) anyFemale = true; else anyMale = true;
            }
            if (anyFemale && !anyMale) sex = Sex.Male;
            else if (anyMale && !anyFemale) sex = Sex.Female;
            else sex = _rng.NextDouble() < 0.5 ? Sex.Female : Sex.Male;

            var settler = Create(genome, sex, Mathf.Max(1, HighestGeneration), -1, -1, sim.day);
            settler.ageDays = MaturityDays * 0.4f;   // it has already drifted for weeks
            agents.Add(settler);
            Record(Memory.ReefEventKind.OctopusSettled, sim.day, settler.id);
        }

        void Hatch(OctopusAgent mother, EcosystemSimulation sim, SpeciesDefinition def)
        {
            int room = Mathf.Max(0, MaxAgents - (agents.Count - 1));
            int young = Mathf.Min(mother.pendingYoung, room);
            int generation = mother.generation + 1;

            // Sexes alternate within a brood rather than being drawn independently.
            // The expected ratio is still even, but in a brood of two or three an
            // independent draw quite often produces one sex only, and in a population
            // this small that ends the line. A disclosed simplification.
            var firstSex = _rng.NextDouble() < 0.5 ? Sex.Female : Sex.Male;

            for (int i = 0; i < young; i++)
            {
                var genome = Genome.Inherit(mother.storedMate, mother.genome, _rng, MutationRate);
                var sex = (i % 2 == 0) ? firstSex
                                       : (firstSex == Sex.Female ? Sex.Male : Sex.Female);
                var child = Create(genome, sex, generation, mother.id, mother.storedMateId, sim.day);
                agents.Add(child);
            }

            if (young > 0)
            {
                Record(Memory.ReefEventKind.OctopusHatched, sim.day, mother.id, young, generation);
                Record(Memory.ReefEventKind.NewGeneration, sim.day, -1, generation);
            }
        }

        void Retire(OctopusAgent a, CauseOfDeath cause, int day)
        {
            a.state = OctopusState.Dead;
            a.causeOfDeath = cause;
            a.diedOnDay = day;
            TotalDied++;

            int nameIndex = _nameIndexById.TryGetValue(a.id, out int idx) ? idx : 0;
            ancestors.Add(AncestorRecord.From(a, nameIndex));
            TrimAncestors();

            Record(Memory.ReefEventKind.OctopusDied, day, a.id, (int)cause);
        }

        // History is capped at the most recent eight generations, which keeps the
        // pedigree bounded at well under a kilobyte however long the session runs.
        void TrimAncestors()
        {
            int oldestKept = HighestGeneration - GenerationCap;
            if (oldestKept <= 0) return;
            ancestors.RemoveAll(r => r.generation < oldestKept);
        }

        void Record(Memory.ReefEventKind kind, int day, int subject, int value = 0, int other = -1)
        {
            journal?.Add(kind, day, subject, value, other);
        }

        // Hands the head count and biomass back to the pool, so the rest of the food
        // web sees the octopuses exactly as it did before they became individuals.
        void WriteBack(EcosystemSimulation sim, int speciesIndex, SpeciesDefinition def)
        {
            float biomass = 0f;
            for (int i = 0; i < agents.Count; i++) biomass += agents[i].Mass(def.unitMass);

            sim.pools[speciesIndex].count = agents.Count;
            sim.pools[speciesIndex].biomass = biomass;
        }

        // ── Readouts for the interface ───────────────────────────────────────

        public OctopusAgent ById(int id)
        {
            for (int i = 0; i < agents.Count; i++) if (agents[i].id == id) return agents[i];
            return null;
        }

        public bool TryAncestor(int id, out AncestorRecord record)
        {
            for (int i = 0; i < ancestors.Count; i++)
                if (ancestors[i].id == id) { record = ancestors[i]; return true; }
            record = default;
            return false;
        }

        public string NameOf(int id)
        {
            var live = ById(id);
            if (live != null) return live.name;
            if (TryAncestor(id, out var rec)) return rec.Name;
            return "unknown";
        }

        // Share of alleles in the living population that are the uppercase variant.
        // This is the number that visibly moves when the learner warms the water.
        public float AlleleFrequency(GeneId gene)
        {
            int copies = 0, total = 0;
            for (int i = 0; i < agents.Count; i++)
            {
                if (!agents[i].IsAlive) continue;
                copies += agents[i].genome.CopiesOf(gene);
                total += 2;
            }
            return total > 0 ? copies / (float)total : 0f;
        }

        // Share of living animals showing the dominant phenotype, which for
        // camouflage is not the same as the allele frequency — a distinction the
        // inspector makes explicitly.
        public float StrongCamouflageShare()
        {
            int strong = 0, total = 0;
            for (int i = 0; i < agents.Count; i++)
            {
                if (!agents[i].IsAlive) continue;
                total++;
                if (agents[i].genome.CopiesOf(GeneId.Camouflage) >= 1) strong++;
            }
            return total > 0 ? strong / (float)total : 0f;
        }

        public float AverageTrait(GeneId gene)
        {
            float sum = 0f; int n = 0;
            for (int i = 0; i < agents.Count; i++)
            {
                if (!agents[i].IsAlive) continue;
                sum += agents[i].traits.ValueOf(gene);
                n++;
            }
            return n > 0 ? sum / n : 0f;
        }

        // True when every variant of every gene is present somewhere in the living
        // population. The milestone asks for an automated check that variety exists
        // on day zero, because without it nothing can ever evolve.
        public bool HasFullVariety()
        {
            foreach (GeneId gene in System.Enum.GetValues(typeof(GeneId)))
            {
                bool sawUpper = false, sawLower = false;
                for (int i = 0; i < agents.Count; i++)
                {
                    if (!agents[i].IsAlive) continue;
                    int copies = agents[i].genome.CopiesOf(gene);
                    if (copies >= 1) sawUpper = true;
                    if (copies <= 1) sawLower = true;
                }
                if (!sawUpper || !sawLower) return false;
            }
            return true;
        }
    }
}
