using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // A lightweight snapshot of the pool state, written every 30 simulated days,
    // last three retained. Well under 1 KB, and it gives the learner a way back
    // without punishing experimentation (Design Document 6.3).
    public class RewindBuffer
    {
        public const int IntervalDays = 30;
        public const int Capacity = 3;

        public class Snapshot
        {
            public int day;
            public float detritus;
            public float[] biomass;
            public float[] count;
            public bool[] present;
            public float temperatureC;
            public float acidityPh;
        }

        readonly Snapshot[] _slots = new Snapshot[Capacity];
        int _written;
        int _lastCaptureDay = -IntervalDays;

        public int StoredCount => Mathf.Min(_written, Capacity);
        public bool HasAny => StoredCount > 0;

        public void Reset()
        {
            _written = 0;
            _lastCaptureDay = -IntervalDays;
            for (int i = 0; i < _slots.Length; i++) _slots[i] = null;
        }

        public void MaybeCapture(EcosystemSimulation sim)
        {
            if (sim.day - _lastCaptureDay < IntervalDays) return;
            _lastCaptureDay = sim.day;
            Capture(sim);
        }

        public void Capture(EcosystemSimulation sim)
        {
            int n = SpeciesLibrary.Count;
            var snap = new Snapshot
            {
                day = sim.day,
                detritus = sim.detritus,
                biomass = new float[n],
                count = new float[n],
                present = new bool[n],
                temperatureC = sim.temperatureC,
                acidityPh = sim.acidityPh,
            };
            for (int i = 0; i < n; i++)
            {
                snap.biomass[i] = sim.pools[i].biomass;
                snap.count[i] = sim.pools[i].count;
                snap.present[i] = sim.IsPresent(i);
            }
            _slots[_written % Capacity] = snap;
            _written++;
        }

        // The most recent snapshot, or null when nothing has been captured yet.
        public Snapshot Latest =>
            _written == 0 ? null : _slots[(_written - 1) % Capacity];

        // Restores the reef to a previous day. Returns the snapshot used, or null.
        public Snapshot Restore(EcosystemSimulation sim)
        {
            var snap = Latest;
            if (snap == null) return null;

            for (int i = 0; i < SpeciesLibrary.Count; i++)
            {
                sim.pools[i] = default;
                sim.pools[i].biomass = snap.biomass[i];
                sim.pools[i].count = snap.count[i];
                if (sim.present != null && i < sim.present.Length)
                    sim.present[i] = snap.present[i];
            }
            sim.detritus = snap.detritus;
            sim.day = snap.day;
            sim.temperatureC = snap.temperatureC;
            sim.acidityPh = snap.acidityPh;

            // Consume it, so repeated presses walk further back rather than sticking.
            _slots[(_written - 1) % Capacity] = null;
            _written--;
            _lastCaptureDay = snap.day;
            return snap;
        }
    }
}
