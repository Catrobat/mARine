using System;
using System.Collections.Generic;
using UnityEngine;

namespace CreateEnv.Ecosystem.Memory
{
    // What kind of thing happened. Stored as a code rather than a sentence.
    //
    // The app already ships in English and German, and a saved sentence is saved in
    // whatever language it was generated in. Keeping the code and its subject means
    // the wording is chosen when it is shown, so a reef saved in one language reads
    // correctly in the other — and it costs a few bytes instead of sixty.
    public enum ReefEventKind : byte
    {
        None = 0,

        // Ecosystem
        SpeciesRemoved = 1,
        SpeciesReturned = 2,
        PopulationCrashed = 3,
        PopulationBoomed = 4,
        CoralBleached = 5,
        CoralRecovered = 6,
        WentBarren = 7,
        Recovering = 8,

        // Octopuses
        OctopusHatched = 20,
        OctopusDied = 21,
        OctopusMated = 22,
        OctopusSettled = 23,
        NewGeneration = 24,
    }

    [Serializable]
    public struct ReefEvent
    {
        public ReefEventKind kind;
        public int day;
        public int subject;    // species index, or an octopus id
        public int value;      // count, generation, cause of death — depends on kind
        public int other;      // a second octopus id, or a generation number

        // Births, deaths and new generations always outrank a percentage change in
        // biomass (Milestone Step 3, risks). This is that ranking, as a number.
        //
        // Above all of them sit the two things the learner did themselves. A log that
        // drops "you removed the parrotfish" but keeps every hatching has thrown away
        // the cause and kept only the consequences — and the whole point of the log is
        // that a learner can trace one to the other.
        public int Rank => kind switch
        {
            ReefEventKind.SpeciesRemoved    => 110,
            ReefEventKind.SpeciesReturned   => 105,
            ReefEventKind.NewGeneration     => 100,
            ReefEventKind.WentBarren        => 95,
            ReefEventKind.OctopusHatched    => 90,
            ReefEventKind.OctopusMated      => 85,
            ReefEventKind.OctopusDied       => 80,
            ReefEventKind.OctopusSettled    => 70,
            ReefEventKind.CoralBleached     => 65,
            ReefEventKind.Recovering        => 50,
            ReefEventKind.CoralRecovered    => 45,
            ReefEventKind.PopulationCrashed => 40,
            ReefEventKind.PopulationBoomed  => 35,
            _ => 0,
        };
    }

    // The reef's diary: the last twenty things worth remembering.
    //
    // One list, three readers — the Welcome Back card, the report's "what happened"
    // section, and the assistant's snapshot. Keeping one journal rather than three
    // means they can never disagree about what occurred.
    [Serializable]
    public class ReefJournal
    {
        public const int Capacity = 20;
        const int BytesPerEvent = 14;

        readonly List<ReefEvent> _events = new List<ReefEvent>(Capacity);

        public IReadOnlyList<ReefEvent> Events => _events;
        public int Count => _events.Count;

        public void Clear() => _events.Clear();

        public void Add(ReefEventKind kind, int day, int subject = -1, int value = 0, int other = -1)
        {
            // Newest first, so reading the list is reading backwards in time.
            _events.Insert(0, new ReefEvent
            {
                kind = kind, day = day, subject = subject, value = value, other = other,
            });
            if (_events.Count > Capacity) _events.RemoveAt(_events.Count - 1);
        }

        // The most notable events since a given day, ranked by kind first and recency
        // second — which is what stops a Welcome Back card leading with "the fan alga
        // is down four percent" when a whole generation was born.
        public List<ReefEvent> Notable(int sinceDay, int limit)
        {
            var picked = new List<ReefEvent>(limit);
            foreach (var e in _events)
                if (e.day >= sinceDay) picked.Add(e);

            picked.Sort((a, b) =>
            {
                int byRank = b.Rank.CompareTo(a.Rank);
                return byRank != 0 ? byRank : b.day.CompareTo(a.day);
            });

            if (picked.Count > limit) picked.RemoveRange(limit, picked.Count - limit);
            return picked;
        }

        // ── Wording ──────────────────────────────────────────────────────────
        //
        // Chosen at display time from the code, never stored. When the string tables
        // are wired up, this is the single place that has to change.
        public static string Describe(ReefEvent e, Genetics.OctopusPopulation octopuses)
        {
            string species = e.subject >= 0 && e.subject < SpeciesLibrary.Count
                ? SpeciesLibrary.NameOf(e.subject).ToLowerInvariant()
                : "something";

            string who = octopuses != null && e.subject >= 0
                ? octopuses.NameOf(e.subject) : "An octopus";
            string mate = octopuses != null && e.other >= 0
                ? octopuses.NameOf(e.other) : "another";

            switch (e.kind)
            {
                case ReefEventKind.SpeciesRemoved:    return "You removed the " + species;
                case ReefEventKind.SpeciesReturned:   return "The " + species + " was put back";
                case ReefEventKind.PopulationCrashed: return "The " + species + " population fell sharply";
                case ReefEventKind.PopulationBoomed:  return "The " + species + " population climbed sharply";
                case ReefEventKind.CoralBleached:     return "The coral began to bleach";
                case ReefEventKind.CoralRecovered:    return "The coral recovered its colour";
                case ReefEventKind.WentBarren:        return "The reef went barren";
                case ReefEventKind.Recovering:        return "The reef began to recover";

                case ReefEventKind.OctopusHatched:
                    return e.value > 1
                        ? who + " hatched a brood of " + e.value + " (generation " + e.other + ")"
                        : who + " hatched (generation " + e.other + ")";

                case ReefEventKind.OctopusDied:
                    return who + " " + Genetics.OctopusAgent.CauseWord((Genetics.CauseOfDeath)e.value);

                case ReefEventKind.OctopusMated:      return who + " and " + mate + " mated";
                case ReefEventKind.OctopusSettled:    return who + " settled here from the plankton";
                case ReefEventKind.NewGeneration:     return "A new generation hatched (generation " + e.value + ")";

                default: return "";
            }
        }

        // ── Persistence ──────────────────────────────────────────────────────
        // Five small numbers per event, packed rather than written as JSON objects.

        public string Pack()
        {
            if (_events.Count == 0) return "";

            var bytes = new byte[_events.Count * BytesPerEvent];
            int at = 0;
            foreach (var e in _events)
            {
                bytes[at++] = (byte)e.kind;
                WriteInt(bytes, ref at, e.day);
                WriteInt(bytes, ref at, e.subject);
                WriteInt(bytes, ref at, e.value);
                bytes[at++] = (byte)Mathf.Clamp(e.other + 1, 0, 255);
            }
            return Convert.ToBase64String(bytes);
        }

        public void Unpack(string packed)
        {
            Clear();
            if (string.IsNullOrEmpty(packed)) return;

            try
            {
                var bytes = Convert.FromBase64String(packed);
                int at = 0;
                while (at + BytesPerEvent <= bytes.Length && _events.Count < Capacity)
                {
                    var e = new ReefEvent { kind = (ReefEventKind)bytes[at++] };
                    e.day = ReadInt(bytes, ref at);
                    e.subject = ReadInt(bytes, ref at);
                    e.value = ReadInt(bytes, ref at);
                    e.other = bytes[at++] - 1;
                    _events.Add(e);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[ReefJournal] Could not read the saved journal: " + ex.Message);
                Clear();
            }
        }

        static void WriteInt(byte[] b, ref int at, int v)
        {
            b[at++] = (byte)(v & 0xFF);
            b[at++] = (byte)((v >> 8) & 0xFF);
            b[at++] = (byte)((v >> 16) & 0xFF);
            b[at++] = (byte)((v >> 24) & 0xFF);
        }

        static int ReadInt(byte[] b, ref int at)
        {
            int v = b[at] | (b[at + 1] << 8) | (b[at + 2] << 16) | (b[at + 3] << 24);
            at += 4;
            return v;
        }
    }
}
