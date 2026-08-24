using System;
using System.IO;
using UnityEngine;
using CreateEnv.Ecosystem.Genetics;

namespace CreateEnv.Ecosystem.Memory
{
    // The reef, written down.
    //
    // JsonUtility-friendly: only primitives and arrays of them. Anything that would
    // be bulky as JSON — the octopuses, the pedigree, the history, the journal — is
    // packed into bytes and carried as one base64 string each. Written out as plain
    // JSON objects the file came to twelve kilobytes against a four kilobyte budget;
    // packed it lands around three.
    [Serializable]
    public class ReefSave
    {
        public const int Format = 1;

        public int format = Format;
        public int rosterVersion;
        public string environmentId = "";
        public int seed;
        public int day;
        public string closedAtUtc = "";

        // Settings the learner chose
        public float temperatureC = 24f;
        public float acidityPh = 8.1f;
        public int speed = 1;
        public int startingLife = 1;
        public int predictionIndex = -1;
        public bool[] present;

        // Pools
        public float[] biomass;
        public float[] count;
        public float[] energy;
        public int[] daysHungry;
        public float detritus;

        // Octopuses and their pedigree
        public string octopuses = "";
        public string ancestors = "";
        public int nextId, nextNameIndex, highestGeneration, totalBorn, totalDied;

        // What the reef remembers
        public string history = "";
        public int historyFirstDay, historyLastDay;
        public string journal = "";

        // ── Capture ──────────────────────────────────────────────────────────

        public static ReefSave From(string environmentId, EcosystemSettings settings,
                                    EcosystemSimulation sim, OctopusPopulation octopuses,
                                    ReefChronicle chronicle)
        {
            int n = SpeciesLibrary.Count;
            var save = new ReefSave
            {
                rosterVersion = SpeciesLibrary.RosterVersion,
                environmentId = environmentId ?? "",
                seed = sim.seed,
                day = sim.day,
                closedAtUtc = DateTime.UtcNow.ToString("o"),

                temperatureC = sim.temperatureC,
                acidityPh = sim.acidityPh,
                speed = settings != null ? settings.speed : 1,
                startingLife = settings != null ? settings.startingLife : 1,
                predictionIndex = settings != null ? settings.predictionIndex : -1,

                present = new bool[n],
                biomass = new float[n],
                count = new float[n],
                energy = new float[n],
                daysHungry = new int[n],
                detritus = sim.detritus,
            };

            for (int i = 0; i < n; i++)
            {
                save.present[i] = sim.IsPresent(i);
                save.biomass[i] = sim.pools[i].biomass;
                save.count[i] = sim.pools[i].count;
                save.energy[i] = sim.pools[i].energy;
                save.daysHungry[i] = sim.pools[i].daysHungry;
            }

            if (octopuses != null)
            {
                save.octopuses = PackAgents(octopuses);
                save.ancestors = PackAncestors(octopuses);
                save.highestGeneration = octopuses.HighestGeneration;
                save.totalBorn = octopuses.TotalBorn;
                save.totalDied = octopuses.TotalDied;
                save.nextId = octopuses.NextId;
                save.nextNameIndex = octopuses.NextNameIndex;
            }

            if (chronicle != null)
            {
                save.history = chronicle.history.Pack();
                save.historyFirstDay = chronicle.history.firstDay;
                save.historyLastDay = chronicle.history.lastSampledDay;
                save.journal = chronicle.journal.Pack();
            }

            return save;
        }

        // ── Restore ──────────────────────────────────────────────────────────

        public void ApplyTo(EcosystemSettings settings, EcosystemSimulation sim,
                            OctopusPopulation octopuses, ReefChronicle chronicle)
        {
            int n = Mathf.Min(SpeciesLibrary.Count, biomass != null ? biomass.Length : 0);

            if (settings != null)
            {
                settings.temperatureC = temperatureC;
                settings.acidityPh = acidityPh;
                settings.speed = speed;
                settings.startingLife = startingLife;
                settings.predictionIndex = predictionIndex;
                if (present != null && settings.present != null)
                    for (int i = 0; i < Mathf.Min(present.Length, settings.present.Length); i++)
                        settings.present[i] = present[i];
                settings.Clamp();
            }

            sim.day = day;
            sim.temperatureC = temperatureC;
            sim.acidityPh = acidityPh;
            sim.detritus = detritus;

            for (int i = 0; i < n; i++)
            {
                if (present != null && i < present.Length && sim.present != null && i < sim.present.Length)
                    sim.present[i] = present[i];

                sim.pools[i] = default;
                sim.pools[i].biomass = biomass[i];
                sim.pools[i].count = count != null && i < count.Length ? count[i] : 0f;
                sim.pools[i].energy = energy != null && i < energy.Length ? energy[i] : 0f;
                sim.pools[i].daysHungry = daysHungry != null && i < daysHungry.Length ? daysHungry[i] : 0;
            }

            if (octopuses != null)
                octopuses.Restore(UnpackAgents(), UnpackAncestors(),
                                  nextId, nextNameIndex, highestGeneration, totalBorn, totalDied);

            if (chronicle != null)
            {
                chronicle.history.Unpack(history, historyFirstDay, historyLastDay);
                chronicle.journal.Unpack(journal);
            }
        }

        // ── Packing ──────────────────────────────────────────────────────────

        static string PackAgents(OctopusPopulation pop)
        {
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);

            w.Write((byte)pop.agents.Count);
            foreach (var a in pop.agents)
            {
                w.Write(a.id);
                w.Write(pop.NameIndexOf(a.id));
                w.Write(a.genome.camouflage);
                w.Write(a.genome.bodySize);
                w.Write(a.genome.heatTolerance);
                w.Write((byte)a.sex);
                w.Write((byte)Mathf.Clamp(a.generation, 0, 255));
                w.Write(a.motherId);
                w.Write(a.fatherId);
                w.Write(a.ageDays);
                w.Write(a.energy);
                w.Write((byte)a.state);
                w.Write(a.noiseSeed);
                w.Write(a.broodDaysRemaining);
                w.Write((byte)Mathf.Clamp(a.pendingYoung, 0, 255));
                w.Write(a.declineDaysRemaining);
                w.Write(a.storedMate.camouflage);
                w.Write(a.storedMate.bodySize);
                w.Write(a.storedMate.heatTolerance);
                w.Write(a.storedMateId);
                w.Write(a.bornOnDay);
            }
            w.Flush();
            return Convert.ToBase64String(stream.ToArray());
        }

        OctopusAgent[] UnpackAgents()
        {
            if (string.IsNullOrEmpty(octopuses)) return Array.Empty<OctopusAgent>();

            try
            {
                var bytes = Convert.FromBase64String(octopuses);
                using var stream = new MemoryStream(bytes);
                using var r = new BinaryReader(stream);

                int n = r.ReadByte();
                var list = new OctopusAgent[n];
                for (int i = 0; i < n; i++)
                {
                    var a = new OctopusAgent();
                    a.id = r.ReadInt32();
                    a.nameIndex = r.ReadInt32();
                    a.genome.camouflage = r.ReadByte();
                    a.genome.bodySize = r.ReadByte();
                    a.genome.heatTolerance = r.ReadByte();
                    a.sex = (Sex)r.ReadByte();
                    a.generation = r.ReadByte();
                    a.motherId = r.ReadInt32();
                    a.fatherId = r.ReadInt32();
                    a.ageDays = r.ReadSingle();
                    a.energy = r.ReadSingle();
                    a.state = (OctopusState)r.ReadByte();
                    a.noiseSeed = r.ReadSingle();
                    a.broodDaysRemaining = r.ReadSingle();
                    a.pendingYoung = r.ReadByte();
                    a.declineDaysRemaining = r.ReadSingle();
                    a.storedMate.camouflage = r.ReadByte();
                    a.storedMate.bodySize = r.ReadByte();
                    a.storedMate.heatTolerance = r.ReadByte();
                    a.storedMateId = r.ReadInt32();
                    a.bornOnDay = r.ReadInt32();
                    a.name = OctopusNames.At(a.nameIndex);
                    a.RefreshTraits();
                    list[i] = a;
                }
                return list;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ReefSave] Could not read the saved octopuses: " + e.Message);
                return Array.Empty<OctopusAgent>();
            }
        }

        static string PackAncestors(OctopusPopulation pop)
        {
            using var stream = new MemoryStream();
            using var w = new BinaryWriter(stream);

            w.Write((ushort)pop.ancestors.Count);
            foreach (var r in pop.ancestors)
            {
                w.Write(r.id);
                w.Write(r.nameIndex);
                w.Write(r.genome.camouflage);
                w.Write(r.genome.bodySize);
                w.Write(r.genome.heatTolerance);
                w.Write(r.generation);
                w.Write(r.sexAndCause);
                w.Write(r.motherId);
                w.Write(r.fatherId);
                w.Write(r.diedOnDay);
                w.Write(r.ageAtDeath);
            }
            w.Flush();
            return Convert.ToBase64String(stream.ToArray());
        }

        AncestorRecord[] UnpackAncestors()
        {
            if (string.IsNullOrEmpty(ancestors)) return Array.Empty<AncestorRecord>();

            try
            {
                var bytes = Convert.FromBase64String(ancestors);
                using var stream = new MemoryStream(bytes);
                using var r = new BinaryReader(stream);

                int n = r.ReadUInt16();
                var list = new AncestorRecord[n];
                for (int i = 0; i < n; i++)
                {
                    var rec = new AncestorRecord();
                    rec.id = r.ReadInt32();
                    rec.nameIndex = r.ReadInt32();
                    rec.genome.camouflage = r.ReadByte();
                    rec.genome.bodySize = r.ReadByte();
                    rec.genome.heatTolerance = r.ReadByte();
                    rec.generation = r.ReadByte();
                    rec.sexAndCause = r.ReadByte();
                    rec.motherId = r.ReadInt32();
                    rec.fatherId = r.ReadInt32();
                    rec.diedOnDay = r.ReadInt16();
                    rec.ageAtDeath = r.ReadInt16();
                    list[i] = rec;
                }
                return list;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ReefSave] Could not read the saved pedigree: " + e.Message);
                return Array.Empty<AncestorRecord>();
            }
        }

        // How long the learner has been away, in real hours. Negative results — a
        // device clock moved backwards — count as no time at all rather than as
        // time travel.
        public double HoursAway(DateTime nowUtc)
        {
            if (string.IsNullOrEmpty(closedAtUtc)) return 0.0;
            if (!DateTime.TryParse(closedAtUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var closed))
                return 0.0;
            return Math.Max(0.0, (nowUtc - closed).TotalHours);
        }
    }
}
