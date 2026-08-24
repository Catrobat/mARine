using System;

namespace CreateEnv.Ecosystem.Genetics
{
    public enum Sex { Female = 0, Male = 1 }

    // What an octopus is doing. The design document's state machine
    // (rest, hunt, flee, mate, brood, die), kept exactly that small.
    public enum OctopusState
    {
        Resting  = 0,
        Hunting  = 1,
        Fleeing  = 2,
        Mating   = 3,
        Brooding = 4,
        Dead     = 5,
    }

    public enum CauseOfDeath
    {
        StillAlive = 0,
        Starved    = 1,
        Predated   = 2,
        AfterBreeding = 3,   // semelparity: it bred, and that is what kills it
        OldAge     = 4,
    }

    // One octopus. Individuals rather than a number pool, because these are the
    // animals the learner taps, inspects and breeds (Design Document 3.1).
    [Serializable]
    public class OctopusAgent
    {
        public int id;
        public int nameIndex;
        public string name;
        public Genome genome;
        public Sex sex;

        public int generation;
        public int motherId = -1;
        public int fatherId = -1;

        public float ageDays;
        public float energy = 1f;        // 0 starving .. 2 well fed
        public OctopusState state = OctopusState.Resting;

        // Individual variation on top of the genotype, drawn once at birth.
        public float noiseSeed;

        // Brooding
        public float broodDaysRemaining;
        public int   pendingYoung;
        public int   mateId = -1;

        // The female stores the sperm packets she was given, and can hold them for
        // days or months before using them (Literature 4.8). Keeping the father's
        // genome here is what lets him die — as he always does — well before the eggs
        // hatch, without the brood losing its father.
        public Genome storedMate;
        public int    storedMateId = -1;

        // Days left before a mated male dies. He has been declining since shortly
        // after mating and is usually dead well before the eggs hatch (Literature
        // 4.8) — but he does not drop on the spot, and in a population this small
        // that difference is the difference between a lineage continuing and dying
        // out for want of a mate.
        public float declineDaysRemaining;
        public bool HasMated => declineDaysRemaining > 0f;

        // Share of the day's catch this animal competed for, recomputed each tick.
        [NonSerialized] public float huntingShare;

        public CauseOfDeath causeOfDeath = CauseOfDeath.StillAlive;
        public int diedOnDay = -1;
        public int bornOnDay;

        [NonSerialized] public OctopusTraits traits;

        public bool IsAlive => state != OctopusState.Dead;

        // Brooding is a condition, not a pose. Deriving it from `state` meant any
        // transient state change clobbered it — an escape from the shark set
        // Fleeing, the female silently stopped brooding, and her eggs never hatched.
        // The countdown is the authority; `state` is only what the interface says
        // she is doing.
        public bool IsBrooding => broodDaysRemaining > 0f;

        public void RefreshTraits() => traits = OctopusTraits.From(genome, noiseSeed);

        public bool IsMature(float maturityDays) => ageDays >= maturityDays;

        // Body mass scales with the size gene, so a large octopus really is a larger
        // share of the pool's biomass — and therefore a bigger meal for the shark.
        public float Mass(float baseUnitMass) => baseUnitMass * traits.bodySize;

        public string SexWord => sex == Sex.Female ? "Female" : "Male";

        public string StateWord => state switch
        {
            OctopusState.Resting  => "Resting in its den",
            OctopusState.Hunting  => "Hunting",
            OctopusState.Fleeing  => "Hiding from the shark",
            OctopusState.Mating   => "Mating",
            OctopusState.Brooding => "Brooding her eggs, not eating",
            _                     => "Dead",
        };

        public static string CauseWord(CauseOfDeath cause) => cause switch
        {
            CauseOfDeath.Starved       => "starved",
            CauseOfDeath.Predated      => "taken by the tiger shark",
            CauseOfDeath.AfterBreeding => "died after breeding",
            CauseOfDeath.OldAge        => "died of old age",
            _                          => "alive",
        };
    }

    // A dead octopus, kept so the family tree survives it.
    //
    // Roughly twenty bytes: name index, genes, generation, parents and cause of
    // death. Dead animals are kept as records, not as agents, which is what keeps an
    // eight-generation pedigree well under a kilobyte (Design Document 4.4).
    [Serializable]
    public struct AncestorRecord
    {
        public int id;
        public int nameIndex;
        public Genome genome;
        public byte generation;
        public byte sexAndCause;   // low nibble: sex, high nibble: cause of death
        public int motherId;
        public int fatherId;
        public short diedOnDay;
        public short ageAtDeath;

        public Sex Sex => (Sex)(sexAndCause & 0x0F);
        public CauseOfDeath Cause => (CauseOfDeath)((sexAndCause >> 4) & 0x0F);
        public string Name => OctopusNames.At(nameIndex);

        public static AncestorRecord From(OctopusAgent a, int nameIndex)
        {
            return new AncestorRecord
            {
                id = a.id,
                nameIndex = nameIndex,
                genome = a.genome,
                generation = (byte)Math.Min(255, a.generation),
                sexAndCause = (byte)(((int)a.sex & 0x0F) | (((int)a.causeOfDeath & 0x0F) << 4)),
                motherId = a.motherId,
                fatherId = a.fatherId,
                diedOnDay = (short)Math.Min(short.MaxValue, a.diedOnDay),
                ageAtDeath = (short)Math.Min(short.MaxValue, (int)a.ageDays),
            };
        }
    }
}
