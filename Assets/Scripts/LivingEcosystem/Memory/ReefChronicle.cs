using UnityEngine;

namespace CreateEnv.Ecosystem.Memory
{
    // Watches the reef and writes down what happens.
    //
    // The simulation knows its numbers but not which of their changes were worth
    // remembering. This turns a running state into a record: it samples the history
    // on a fixed cadence, and it notices the handful of transitions a learner would
    // describe out loud — a species removed, the coral bleaching, a population
    // crashing, the reef going barren.
    //
    // Deliberately edge-triggered. Recording "the coral is bleached" every day would
    // fill the twenty-entry journal in under a month and bury everything else.
    public class ReefChronicle
    {
        public readonly ReefHistory history = new ReefHistory();
        public readonly ReefJournal journal = new ReefJournal();

        // Last-seen state, so only changes are written down.
        readonly bool[] _wasPresent = new bool[SpeciesLibrary.Count];
        readonly float[] _lastNoted = new float[SpeciesLibrary.Count];
        bool _wasBleached;
        CollapseStage _lastStage = CollapseStage.Healthy;
        bool _started;

        // A population has to move by this much since it was last remarked upon
        // before it is worth remarking upon again.
        const float NotableChange = 0.45f;

        public void Begin(EcosystemSimulation sim, EcosystemHealth health,
                          Genetics.OctopusPopulation octopuses)
        {
            for (int i = 0; i < SpeciesLibrary.Count; i++)
            {
                _wasPresent[i] = sim.IsPresent(i);
                _lastNoted[i] = sim.DisplayAmount(i);
            }
            _wasBleached = AnyBleached(sim);
            _lastStage = health.stage;
            _started = true;

            if (history.IsEmpty) history.Sample(sim, health, octopuses);
        }

        // Called once per simulated day, after the simulation and the agents.
        public void Observe(EcosystemSimulation sim, EcosystemHealth health,
                            Genetics.OctopusPopulation octopuses)
        {
            if (!_started) Begin(sim, health, octopuses);

            history.MaybeSample(sim, health, octopuses);

            for (int i = 0; i < SpeciesLibrary.Count; i++)
            {
                bool present = sim.IsPresent(i);
                if (present != _wasPresent[i])
                {
                    journal.Add(present ? ReefEventKind.SpeciesReturned
                                        : ReefEventKind.SpeciesRemoved, sim.day, i);
                    _wasPresent[i] = present;
                    _lastNoted[i] = sim.DisplayAmount(i);
                    continue;
                }
                if (!present) continue;

                // The octopuses report their own births and deaths individually, so a
                // headcount change there would say the same thing twice.
                if (i == SpeciesLibrary.Octopus) continue;

                float now = sim.DisplayAmount(i);
                float was = _lastNoted[i];
                if (was < 0.5f) { _lastNoted[i] = now; continue; }

                float change = (now - was) / was;
                if (change <= -NotableChange)
                {
                    journal.Add(ReefEventKind.PopulationCrashed, sim.day, i,
                                Mathf.RoundToInt(change * 100f));
                    _lastNoted[i] = now;
                }
                else if (change >= NotableChange * 1.6f)
                {
                    journal.Add(ReefEventKind.PopulationBoomed, sim.day, i,
                                Mathf.RoundToInt(change * 100f));
                    _lastNoted[i] = now;
                }
            }

            bool bleached = AnyBleached(sim);
            if (bleached != _wasBleached)
            {
                journal.Add(bleached ? ReefEventKind.CoralBleached
                                     : ReefEventKind.CoralRecovered, sim.day, SpeciesLibrary.Coral);
                _wasBleached = bleached;
            }

            if (health.stage != _lastStage)
            {
                if (health.stage == CollapseStage.Barren)
                    journal.Add(ReefEventKind.WentBarren, sim.day);
                else if (_lastStage >= CollapseStage.Collapse && health.stage <= CollapseStage.Overgrazing)
                    journal.Add(ReefEventKind.Recovering, sim.day);
                _lastStage = health.stage;
            }
        }

        public void Reset()
        {
            history.Clear();
            journal.Clear();
            _started = false;
            _wasBleached = false;
            _lastStage = CollapseStage.Healthy;
        }

        static bool AnyBleached(EcosystemSimulation sim)
        {
            for (int i = 0; i < SpeciesLibrary.Count; i++)
                if (sim.IsPresent(i) && sim.pools[i].bleached) return true;
            return false;
        }
    }
}
