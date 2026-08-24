using UnityEngine;

namespace CreateEnv
{
    // The single bridge between the friendly layer (SimpleEnvironmentSettings) and the
    // technical layer (EnvironmentProfile).
    public static class SimpleEnvironmentMapper
    {
        public static void Apply(EnvironmentProfile p)
        {
            if (p == null || p.simple == null) return;
            var s = p.simple;
            s.Clamp();

            ApplySeafloor(p, s);
            ApplyHabitat(p, s);
            ApplyWater(p, s);
            ApplyTimeOfDay(p, s);
            ApplyExploration(p, s);
            ApplySurfaces(p, s);

            EnvironmentBounds.Clamp(p);
        }

        // ── 1. Seafloor ──────────────────────────────────────────────────────
        // Each profile is a hand-tuned recipe over the terrain-shape and biome-style fields;
        static void ApplySeafloor(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            switch (s.seafloorProfile)
            {
                case 0: // Flat Plain — wide, gentle dunes
                    p.styleIndex = 0;
                    p.noiseScale = 90f; p.meshHeightMultiplier = 6f; p.waterLevel = 8f;
                    break;
                case 2: // Rocky Reef — tighter, bumpier dunes
                    p.styleIndex = 0;
                    p.noiseScale = 30f; p.meshHeightMultiplier = 24f; p.waterLevel = 8f;
                    break;
                case 3: // Coral Plateau — terraced mesas, gentle ridges
                    p.styleIndex = 1;
                    p.noiseScale = 60f; p.meshHeightMultiplier = 28f; p.waterLevel = 10f;
                    p.warpStrength = 5f;   p.warpScale = 2.5f;
                    p.ridgeWeight  = 0.35f; p.ridgeSharpness = 2f;
                    p.terraceSteps = 7;    p.terraceSharpness = 5f; p.terraceStrength = 0.85f;
                    break;
                case 4: // Underwater Canyon — deep ridged walls (mirrors the Canyon built-in)
                    p.styleIndex = 1;
                    p.noiseScale = 60f; p.meshHeightMultiplier = 40f; p.waterLevel = 14f;
                    p.warpStrength = 6f;   p.warpScale = 2.5f;
                    p.ridgeWeight  = 0.7f; p.ridgeSharpness = 2.6f;
                    p.terraceSteps = 4;    p.terraceSharpness = 4f; p.terraceStrength = 0.5f;
                    break;
                default: // 1: Rolling Hills — the classic dunes baseline
                    p.styleIndex = 0;
                    p.noiseScale = 55f; p.meshHeightMultiplier = 16f; p.waterLevel = 8f;
                    break;
            }

            float c = s.terrainComplexity;
            p.octaves     = 3 + Mathf.RoundToInt(c * 4f);        // 3..7
            p.persistance = Mathf.Lerp(0.40f, 0.62f, c);
            p.lacunarity  = Mathf.Lerp(1.8f, 2.4f, c);
        }

        // ── 2. Habitat ───────────────────────────────────────────────────────
        static void ApplyHabitat(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            // Pack indices follow the LifePackLibrary convention the built-ins use:
            // 1 = sandy/sample, 2 = kelp forest, 3 = coral (see BuiltInProfiles).
            switch (s.marineHabitat)
            {
                case 1:  p.lifePackIndex = 2; break; // Kelp Forest
                case 2:  p.lifePackIndex = 1; break; // Sandy Bottom
                default: p.lifePackIndex = 3; break; // Coral Reef
            }

            // Per-species multipliers are a technical detail the simple layer never
            // sets; empty means "1x for every species in the pack" to the loader.
            p.speciesDensity = System.Array.Empty<float>();

            float d = s.habitatDensity;
            p.lifeDensity      = Mathf.Lerp(0.3f, 1.8f, d);
            p.maxPropsPerChunk = Mathf.RoundToInt(Mathf.Lerp(80f, 300f, d));

            // What grows on the rock comes from the ocean model, because it is not an art choice:
            var benthos = OceanModel.BenthosAt(s.ResolvedWaterType(), s.siteDepthMeters,
                                               s.marineHabitat, d);
            p.encrustAmount = benthos.coverage;
            p.encrustScale  = 1.6f;
            p.encrustColorA = benthos.colorA;
            p.encrustColorB = benthos.colorB;
            p.encrustColorC = benthos.colorC;
            p.encrustRelief = benthos.relief;

            p.turfAmount   = benthos.turf;
            p.turfColor    = benthos.turfBase;
            p.turfTipColor = benthos.turfTip;
            p.turfScale    = 2.6f;
            p.turfUpBias   = 0.55f;
            p.turfRelief   = 0.7f;
        }

        // ── 3. Water ─────────────────────────────────────────────────────────
        static void ApplyWater(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            // Two models, user's choice. Both write the same technical fields.
            if (s.waterModel == 1) { ApplyWaterClassic(p, s); return; }

            // Water is derived, not picked. The Jerlov optical type fixes the attenuation
            // spectrum;
            int   type    = s.ResolvedWaterType();
            float clarity = s.waterClarity;
            float depth   = s.siteDepthMeters;

            float density = OceanModel.FogDensityFor(type);
            // +-25% within the type, murky end thicker.
            p.fogDensity = density * Mathf.Lerp(1.25f, 0.75f, clarity);
            p.absorbTint = OceanModel.AbsorbTintFor(type);

            // Brightness follows the light that actually reaches this depth.
            float light = OceanModel.LightAtDepth(type, depth);
            p.sunGlowIntensity = Mathf.Lerp(0.35f, 1.35f, Mathf.Sqrt(light));
            p.ambientIntensity = Mathf.Lerp(0.45f, 1.25f, Mathf.Sqrt(light));

            // Turbidity is suspended particulate, so marine snow tracks the water
            // type directly rather than being dialled separately.
            float turbid = Mathf.InverseLerp(0.02f, 0.80f, density);
            p.snowCount   = Mathf.RoundToInt(Mathf.Lerp(250f, 1100f, turbid));
            p.snowOpacity = Mathf.Lerp(0.30f, 0.62f, turbid);
            p.snowDrift   = 0.05f;
            p.snowSink    = 0.03f;

            p.surgeAmplitude = 0.05f;
            p.surgeSpeed     = 0.55f;
            p.surgeDirX      = 1f;
            p.surgeDirZ      = 0.35f;

            // Colour of the medium: whatever survives the path, at three path lengths.
            float vis = OceanModel.VisibilityMetres(type);
            Color medium  = OceanModel.WaterColour(type, vis * 0.30f, 0.62f);
            Color deep    = OceanModel.WaterColour(type, vis * 0.85f, 0.24f);
            Color surface = OceanModel.WaterColour(type, vis * 0.10f, 0.88f);

            // Sun glow is the near-surface end of the same ramp, warmed slightly:
            // the shaft you are looking up into has travelled the least water.
            Color sun = OceanModel.WaterColour(type, vis * 0.04f, 1f);
            sun = Color.Lerp(sun, Color.white, 0.55f);

            SetColours(p, medium, surface, deep, sun);
        }

        // The original hand-tuned mapping: clarity does the work of the optical model
        // and colour is a straight palette pick. Physically arbitrary, but authored.
        static void ApplyWaterClassic(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            float clarity = s.waterClarity;
            p.fogDensity       = Mathf.Lerp(0.11f, 0.03f, clarity);
            p.sunGlowIntensity = Mathf.Lerp(0.7f, 1.3f, clarity);
            p.ambientIntensity = Mathf.Lerp(0.8f, 1.2f, clarity);

            p.absorbTint  = Mathf.Lerp(2.6f, 1.3f, clarity);
            p.snowCount   = Mathf.RoundToInt(Mathf.Lerp(900f, 400f, clarity));
            p.snowOpacity = Mathf.Lerp(0.58f, 0.34f, clarity);
            p.snowDrift   = 0.05f;
            p.snowSink    = 0.03f;

            p.surgeAmplitude = 0.05f;
            p.surgeSpeed     = 0.55f;
            p.surgeDirX      = 1f;
            p.surgeDirZ      = 0.35f;

            switch (s.waterColour)
            {
                case 1: // Coastal Green (kelp-forest palette)
                    SetColours(p, new Color(0.03f, 0.40f, 0.42f), new Color(0.40f, 0.80f, 0.65f),
                                  new Color(0.01f, 0.22f, 0.20f), new Color(0.70f, 0.95f, 0.80f));
                    break;
                case 2: // Deep Ocean Blue
                    SetColours(p, new Color(0.01f, 0.25f, 0.45f), new Color(0.20f, 0.55f, 0.80f),
                                  new Color(0.002f, 0.10f, 0.22f), new Color(0.60f, 0.80f, 0.95f));
                    break;
                case 3: // Algae Bloom
                    SetColours(p, new Color(0.10f, 0.42f, 0.25f), new Color(0.45f, 0.75f, 0.40f),
                                  new Color(0.03f, 0.18f, 0.10f), new Color(0.80f, 0.90f, 0.60f));
                    break;
                default: // 0: Tropical Blue (the shipped default palette)
                    SetColours(p, new Color(0.015f, 0.46f, 0.595f), new Color(0.33f, 0.78f, 0.85f),
                                  new Color(0.004f, 0.19f, 0.30f), new Color(0.75f, 0.95f, 0.90f));
                    break;
            }
        }

        static void SetColours(EnvironmentProfile p, Color water, Color glow, Color deep, Color sun)
        {
            water.a = glow.a = deep.a = sun.a = 1f;
            p.waterColor = water; p.surfaceGlowColor = glow;
            p.deepColor = deep;   p.sunGlowColor = sun;
        }

        // ── 5. Surface styles ────────────────────────────────────────────────
        // A straight pass-through, unlike every other mapping here. These two are genuinely
        // independent aesthetic choices:
        static void ApplySurfaces(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            p.terrainTextureStyle = s.seafloorSurface;
            p.waterStyle          = s.waterMovement;
        }

        // ── 3d. Time of day ──────────────────────────────────────────────────
        // Runs AFTER ApplyWater, because it scales the colours that produced. Underwater, time
        // of day is not a tint — it is how much light gets in at all.
        static void ApplyTimeOfDay(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            p.timeOfDay = s.timeOfDay;

            float lightScale;
            switch (s.timeOfDay)
            {
                case 0: // Sunrise — low and warm, long shafts through the surface
                    p.sunElevation = 8f;   p.sunAzimuth = 90f;
                    p.sunLightColor = new Color(1f, 0.72f, 0.45f, 1f);
                    p.sunLightIntensity = 0.75f;
                    p.ambientIntensity  = 0.65f;
                    p.sunGlowIntensity  = 1.25f;
                    lightScale = 0.80f;
                    break;

                case 2: // Sunset — lower and redder still
                    p.sunElevation = 6f;   p.sunAzimuth = 280f;
                    p.sunLightColor = new Color(1f, 0.55f, 0.32f, 1f);
                    p.sunLightIntensity = 0.65f;
                    p.ambientIntensity  = 0.55f;
                    p.sunGlowIntensity  = 1.35f;
                    lightScale = 0.68f;
                    break;

                case 3: // Night — moonlight only. Deliberately dark: this is the one
                        // setting where the reef stops being readable at distance.
                    p.sunElevation = 25f;  p.sunAzimuth = 20f;
                    p.sunLightColor = new Color(0.55f, 0.68f, 1f, 1f);
                    p.sunLightIntensity = 0.18f;
                    p.ambientIntensity  = 0.22f;
                    p.sunGlowIntensity  = 0.15f;
                    lightScale = 0.30f;
                    break;

                default: // 1: Afternoon — sun high, near-white, the brightest case
                    p.sunElevation = 70f;  p.sunAzimuth = 200f;
                    p.sunLightColor = new Color(1f, 0.98f, 0.92f, 1f);
                    p.sunLightIntensity = 1.2f;
                    p.ambientIntensity  = 1f;
                    p.sunGlowIntensity  = 0.9f;
                    lightScale = 1f;
                    break;
            }

            // Scale the medium itself, not just the lights: at night the water is not
            // a lit blue with the lamps turned down, it is nearly black.
            if (lightScale < 0.999f)
            {
                p.waterColor        = Dim(p.waterColor, lightScale);
                p.deepColor         = Dim(p.deepColor, lightScale * 0.85f);
                p.surfaceGlowColor  = Dim(p.surfaceGlowColor, lightScale);
                p.sunGlowColor      = Dim(p.sunGlowColor, lightScale);
            }
        }

        static Color Dim(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        // ── 4. Exploration ───────────────────────────────────────────────────
        static void ApplyExploration(EnvironmentProfile p, SimpleEnvironmentSettings s)
        {
            p.viewDistanceIndex = s.explorationArea; // Small/Medium/Large = Near/Medium/Far

            // The fog band follows the streaming reach rather than being set apart from it:
            float reach = EnvironmentBounds.ViewDistanceWorldReach(s.explorationArea);
            p.fadeEnd   = reach * 0.85f;
            p.fadeStart = p.fadeEnd * 0.55f;
        }
    }
}
