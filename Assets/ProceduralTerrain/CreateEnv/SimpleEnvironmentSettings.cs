using System;
using UnityEngine;

namespace CreateEnv
{
    // The user-facing half of an environment: dropdowns and 0..1 sliders, no technical
    // knobs. SimpleEnvironmentMapper derives EnvironmentProfile from these.
    [Serializable]
    public class SimpleEnvironmentSettings
    {
        // Dropdown option labels. The stored value is the index into these arrays.
        public static readonly string[] SeafloorProfiles =
            { "Flat Plain", "Rolling Hills", "Rocky Reef", "Coral Plateau", "Underwater Canyon" };
        public static readonly string[] MarineHabitats =
            { "Coral Reef", "Kelp Forest", "Sandy Bottom" };
        public static readonly string[] WaterColours =
            { "Tropical Blue", "Coastal Green", "Deep Ocean Blue", "Algae Bloom" };
        public static readonly string[] ExplorationAreas =
            { "Small", "Medium", "Large" };

        public static readonly string[] TimesOfDay =
            { "Sunrise", "Afternoon", "Sunset", "Night" };

        // Neither model is "correct": one is right about how light behaves, the other
        // is four palettes tuned until they looked good. A choice, not a migration.
        public static readonly string[] WaterModels =
            { "Ocean model (physical)", "Classic palette" };

        // The style tables themselves, not copies, so a dropdown cannot drift out of
        // index alignment with SurfaceStyles.cs.
        public static readonly string[] SeafloorSurfaces = TerrainTextureStyles.Names;
        public static readonly string[] WaterMovements   = WaterStyles.Names;

        // ── 1. Seafloor (terrain shape + biome style + surface material) ─────
        public int   seafloorProfile = 1;      // Rolling Hills
        public float terrainComplexity = 0.5f; // Simple <-> Complex
        public int   seafloorSurface = 0;      // Classic sand (scene material untouched)

        // ── 2. Habitat (life pack + scatter density) ─────────────────────────
        public int   marineHabitat = 0;        // Coral Reef
        public float habitatDensity = 0.5f;    // Sparse <-> Dense

        // ── 3. Water (optical type + depth + surface motion) ─────────────────
        // waterType is the Jerlov optical water type — see OceanModel. It is the real control:
        public int   waterType = -1;
        [Tooltip("Depth of the site in metres. Decides how much daylight reaches the " +
                 "seabed, and so whether the rock carries algal turf or the dimmer " +
                 "sponge and ascidian community.")]
        public float siteDepthMeters = 12f;

        // 0 = derive from the Jerlov type (OceanModel); 1 = the hand-tuned curve and
        // the four fixed palettes below.
        public int   waterModel = 1;   // Classic palette


        public int   timeOfDay = 1;            // Afternoon
        public float waterClarity = 0.6f;      // Murky <-> Crystal Clear
        public int   waterColour = 0;          // legacy palette; seeds waterType when unset
        public int   waterMovement = 0;        // Classic (scene material untouched)

        // Legacy palette -> nearest real water type, so the old four presets still mean
        // something once water is physical.
        public static int WaterTypeFromLegacyPalette(int palette)
        {
            switch (palette)
            {
                case 1:  return 5; // Coastal Green    -> 1C
                case 2:  return 1; // Deep Ocean Blue  -> IA
                case 3:  return 7; // Algae Bloom      -> 5C
                default: return 2; // Tropical Blue    -> IB
            }
        }

        public int ResolvedWaterType()
            => waterType >= 0 ? waterType : WaterTypeFromLegacyPalette(waterColour);

        // ── 4. Exploration (streaming reach + fog fade distance) ─────────────
        public int   explorationArea = 1;      // Medium
        // No longer a control: the fog band now follows the exploration area, so the
        // two cannot disagree. Kept so profiles saved with it still deserialise.
        public float visibility = 0.6f;

        public void Clamp()
        {
            seafloorProfile = Mathf.Clamp(seafloorProfile, 0, SeafloorProfiles.Length - 1);
            marineHabitat   = Mathf.Clamp(marineHabitat, 0, MarineHabitats.Length - 1);
            waterColour     = Mathf.Clamp(waterColour, 0, WaterColours.Length - 1);
            explorationArea = Mathf.Clamp(explorationArea, 0, ExplorationAreas.Length - 1);
            seafloorSurface = Mathf.Clamp(seafloorSurface, 0, SeafloorSurfaces.Length - 1);
            waterMovement   = Mathf.Clamp(waterMovement, 0, WaterMovements.Length - 1);
            waterModel      = Mathf.Clamp(waterModel, 0, WaterModels.Length - 1);
            timeOfDay       = Mathf.Clamp(timeOfDay, 0, TimesOfDay.Length - 1);

            // -1 is meaningful (unset -> derive from legacy palette); only clamp real values.
            if (waterType >= 0) waterType = Mathf.Clamp(waterType, 0, OceanModel.TypeCount - 1);
            siteDepthMeters = Mathf.Clamp(siteDepthMeters, 0.5f, 60f);

            terrainComplexity = Mathf.Clamp01(terrainComplexity);
            habitatDensity    = Mathf.Clamp01(habitatDensity);
            waterClarity      = Mathf.Clamp01(waterClarity);
            visibility        = Mathf.Clamp01(visibility);
        }

        public SimpleEnvironmentSettings Clone() => (SimpleEnvironmentSettings)MemberwiseClone();
    }
}
