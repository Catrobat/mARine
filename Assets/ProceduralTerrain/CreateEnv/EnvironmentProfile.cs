using System;
using UnityEngine;

namespace CreateEnv
{
    // A whole environment as plain, serializable data. This is what the three
    // built-in biomes and every user-made environment ARE — no scene, no prefab.
    [Serializable]
    public class EnvironmentProfile
    {
        // ── Meta ─────────────────────────────────────────────────────────────
        public string id = "";            // stable file id (slug). Empty => generated on save.
        public string displayName = "My Environment";
        public bool   isBuiltIn = false;  // true for the three shipped biomes (read-only)

        // ── Group 1: Terrain shape (MapGenerator) ────────────────────────────
        public float noiseScale = 45f;
        public int   octaves = 5;
        public float persistance = 0.5f;
        public float lacunarity = 2f;
        public int   seed = 0;
        public float offsetX = 0f;
        public float offsetY = 0f;
        public float meshHeightMultiplier = 20f;

        // ── Group 2: Biome style (TerrainShaper.Settings) ────────────────────
        public int   styleIndex = 0;      // 0 = Classic, 1 = Canyons
        public float warpStrength = 6f;
        public float warpScale = 2.5f;
        public float ridgeWeight = 0.55f;
        public float ridgeSharpness = 2.2f;
        public int   terraceSteps = 5;
        public float terraceSharpness = 4f;
        public float terraceStrength = 0.65f;

        // ── Group 3: Water & atmosphere (UnderwaterEnvironment + WaterSurface) ─
        public float waterLevel = 8f;
        public Color waterColor       = new Color(0.015f, 0.46f, 0.595f, 1f);
        public Color surfaceGlowColor = new Color(0.33f, 0.78f, 0.85f, 1f);
        public Color deepColor        = new Color(0.004f, 0.19f, 0.30f, 1f);
        public Color sunGlowColor     = new Color(0.75f, 0.95f, 0.90f, 1f);
        public float sunGlowIntensity = 1f;
        public float ambientIntensity = 1f;
        public float fogDensity = 0.055f;
        public float fadeStart = 14f;
        public float fadeEnd = 22f;
        public int   waterResolution = 96;
        public float cameraFarMargin = 3f;

        // ── Group 3b: Medium, surge, particulate ─────────────────────────────
        // Every default here reproduces the pre-existing look exactly, so a profile saved
        // before these fields existed deserializes to 0 and renders identically.
        public float absorbTint = 0f;
        public float surgeAmplitude = 0f;
        public float surgeSpeed = 0.55f;
        public float surgeDirX = 1f;
        public float surgeDirZ = 0.35f;

        public int   snowCount = 0;
        public float snowOpacity = 0.5f;
        public float snowSizeMin = 0.014f;
        public float snowSizeMax = 0.050f;
        public float snowDrift = 0.05f;
        public float snowSink = 0.03f;

        public float encrustAmount = 0f;
        public float encrustScale = 1.6f;
        public Color encrustColorA = new Color(0.62f, 0.28f, 0.42f, 1f);
        public Color encrustColorB = new Color(0.78f, 0.46f, 0.26f, 1f);
        // Cryptic (underside/overhang) community.
        public Color encrustColorC = new Color(0.44f, 0.20f, 0.26f, 1f);
        // 0 = the old flat tint, 1 = full crust relief. See ApplyEncrustation.
        public float encrustRelief = 0.85f;

        // Algal turf: the continuous mat the encrusting colonies sit on. See ApplyAlgalTurf.
        public float turfAmount = 0f;
        public Color turfColor = new Color(0.15f, 0.30f, 0.12f, 1f);
        public Color turfTipColor = new Color(0.46f, 0.66f, 0.20f, 1f);
        public float turfScale = 2.6f;
        public float turfUpBias = 0.55f;
        public float turfRelief = 0.7f;

        // ── Group 3c: Surface styles (SurfaceStyles.cs) ──────────────────────
        // Indices into TerrainTextureStyles.Names / WaterStyles.Names. Index 0 of both is
        // "Classic":
        public int terrainTextureStyle = 0;
        public int waterStyle = 0;

        // ── Group 3d: Time of day ────────────────────────────────────────────
        // -1 means "not set": the loader then leaves the scene's own lighting alone,
        // so every environment authored before this keeps the look it had.
        public int   timeOfDay = -1;
        public float sunElevation = 45f;
        public float sunAzimuth = 200f;
        public Color sunLightColor = Color.white;
        public float sunLightIntensity = 1f;

        // ── Group 4: Life / scatter (TerrainDetailScatter) ───────────────────
        public int     lifePackIndex = 1;     // index into LifePackLibrary (0 = None)
        public float   lifeDensity = 1f;       // global multiplier on rule densities
        public int     maxPropsPerChunk = 240; // hard per-chunk ceiling
        public bool    castShadows = false;
        public float   waterTint = 0.18f;
        public int     scatterSeed = 1337;
        // Per-species density multipliers, aligned to the chosen pack's rules.
        // Length may differ from the pack; the loader treats missing entries as 1.
        public float[] speciesDensity = Array.Empty<float>();

        // ── Group 5: Streaming (EndlessTerrain) ──────────────────────────────
        public int viewDistanceIndex = 1;      // 0 = Near, 1 = Medium, 2 = Far

        // ── User layer ───────────────────────────────────────────────────────
        // The friendly choices this profile was made from (see SimpleEnvironmentMapper).
        // Persisted so editing an environment re-opens with the same choices;
        public SimpleEnvironmentSettings simple = new SimpleEnvironmentSettings();

        // ── Group 6: Living Ecosystem ────────────────────────────────────────
        // Off by default, so an environment saved before this feature existed loads with the
        // ecosystem disabled and behaves exactly as it did.
        public Ecosystem.EcosystemSettings ecosystem = new Ecosystem.EcosystemSettings();

        public EnvironmentProfile Clone()
        {
            var c = (EnvironmentProfile)MemberwiseClone();
            c.speciesDensity = (float[])speciesDensity.Clone();
            c.simple = simple != null ? simple.Clone() : new SimpleEnvironmentSettings();
            c.ecosystem = ecosystem != null ? ecosystem.Clone() : new Ecosystem.EcosystemSettings();
            return c;
        }
    }
}
