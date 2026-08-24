using System;
using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // How an organism moves, which is a property of the animal and not of its
    // trophic level. A coral is a producer in this model but it is a colony of
    // polyps on a rigid limestone skeleton: it does not sway, and a swaying one
    // reads as a plant, which is the exact misconception the info card corrects.
    public enum MotionKind
    {
        Fixed = 0,   // rigid and rooted: the coral
        Sway  = 1,   // flexible and rooted: both algae
        Crawl = 2,   // moves slowly over the seabed: urchin, limpet, lobster
        Swim  = 3,   // free in the water column: parrotfish, octopus, shark
    }

    public enum TrophicLevel
    {
        Producer   = 0,
        PlantEater = 1,
        Hunter     = 2,
        TopPredator = 3,
    }

    // One species, as data. Everything the simulation needs and everything the info
    // card shows, in one place, so retuning the biology never means touching code.
    //
    // Units: one biomass unit is 1 kg wet mass over the modelled reef patch (about
    // 100 m2 of Cabo Verde shallow sand and rock). Appetites are per individual per
    // day and sit close to real fractions of body mass. Rates are per simulated day.
    [Serializable]
    public class SpeciesDefinition
    {
        // ── Identity ─────────────────────────────────────────────────────────
        public string id;
        public string commonName;      // what the interface calls it
        public string scientificName;
        public TrophicLevel level;

        // ── Producer terms (level == Producer) ───────────────────────────────
        public float growthRate;       // logistic r, per day
        public float baseCapacity;     // K before light/temperature/acidity
        public float naturalLoss;      // fraction shed per day, feeds the detritus pool
        public float recruitment;      // spore/drift arrival as a fraction of K per day
        public float optimumTemperature;
        public float temperatureTolerance;
        public float bleachingThresholdC;   // 0 = does not bleach

        // ── Consumer terms (everything else) ─────────────────────────────────
        public float appetite;         // biomass wanted per individual per day
        public float assimilation;     // share of intake that becomes usable energy
        public float livingCost;       // energy spent per individual per day
        public float birthRate;        // max share added per day when well fed
        public float starveRate;       // max share lost per day when underfed
        public float ceiling;          // most individuals the patch can hold
        public float unitMass;         // biomass of one individual
        public float presence;         // share of days actually on this patch
        public float detritusFraction; // share of demand met from detritus
        public bool  transient;        // present or absent, never a local population

        // ── Shared ───────────────────────────────────────────────────────────
        public float calcifierWeight;  // 0 = soft-bodied, 1 = fully carbonate-skeletoned
        public float refuge;           // biomass no predator can ever reach
        public float startingStock;    // biomass for producers, count for consumers

        // ── Presentation ─────────────────────────────────────────────────────
        public Color tint;             // pyramid bars, readout dots, minimap
        public float modelHeightMeters;// drawn size, used by the renderer
        public float individualsPerModel; // how many animals one drawn model stands for

        // Where this species lives in the water column.
        //
        // Attached life — both algae, the coral, the urchin, the limpet, the lobster —
        // is fixed to the bottom: seated with its underside on the seabed and tilted
        // to the slope. Everything else swims at a height above the seabed, in metres.
        //
        // Measured from the seabed rather than as a share of the water column, because
        // the seabed is directly underfoot and always known, whereas the water surface
        // is placed independently of the terrain and the two can disagree.
        public bool  attachedToSeabed;
        public float seabedClearanceMin;
        public float seabedClearanceMax;

        public MotionKind motion;

        [Tooltip("How far a crawler wanders from where it settled, in metres, and " +
                 "how much a swimmer rises and falls within its band.")]
        public float roamRadius;
        public float verticalRoam;

        // ── Info card (traceable to the Literature Document) ─────────────────
        public string appearance;
        public string habitat;
        public string diet;
        public string roleInModel;
        public string iucnStatus;
        public string regionalStatus;
        [TextArea] public string oneThingWorthTelling;
        [TextArea] public string simplificationNote;

        public bool IsProducer => level == TrophicLevel.Producer;
        public bool IsConsumer => level != TrophicLevel.Producer;
        public bool CanBleach  => bleachingThresholdC > 0f;

        public SpeciesDefinition Clone() => (SpeciesDefinition)MemberwiseClone();
    }

    // One predator-prey edge. Kept as a table so designers can retune the web
    // without a code change (Design Document 2.3).
    [Serializable]
    public class FoodLink
    {
        public int predator;
        public int prey;

        [Tooltip("Relative share of this predator's demand aimed at this prey.")]
        public float preference;

        [Tooltip("Prey biomass at which this predator gets half of what it wants. " +
                 "Low means a relentless forager that keeps finding food even when it " +
                 "is thin on the ground.")]
        public float halfSaturation;

        public FoodLink(int predator, int prey, float preference, float halfSaturation)
        {
            this.predator = predator;
            this.prey = prey;
            this.preference = preference;
            this.halfSaturation = halfSaturation;
        }
    }
}
