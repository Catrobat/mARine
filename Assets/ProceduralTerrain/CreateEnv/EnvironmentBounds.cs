using UnityEngine;

namespace CreateEnv
{
    // The single guardrail. Every user-facing field has a Range here;
    public static class EnvironmentBounds
    {
        public struct Range
        {
            public readonly float Min, Max, Default, Step;
            public Range(float min, float max, float def, float step = 0f)
            { Min = min; Max = max; Default = def; Step = step; }

            public float Clamp(float v)
            {
                v = Mathf.Clamp(v, Min, Max);
                if (Step > 0f) v = Min + Mathf.Round((v - Min) / Step) * Step;
                return v;
            }
            public int ClampInt(float v) => Mathf.RoundToInt(Clamp(v));
        }

        // ── Group 1: Terrain shape ───────────────────────────────────────────
        public static readonly Range NoiseScale           = new Range(12f, 140f, 45f, 1f);
        public static readonly Range Octaves              = new Range(1f, 8f, 5f, 1f);
        public static readonly Range Persistance          = new Range(0.15f, 0.85f, 0.5f, 0.01f);
        public static readonly Range Lacunarity           = new Range(1.6f, 3.2f, 2f, 0.05f);
        public static readonly Range MeshHeightMultiplier = new Range(4f, 48f, 20f, 0.5f);
        public static readonly Range Offset               = new Range(-10000f, 10000f, 0f, 1f);

        // ── Group 2: Biome style (match the [Range] attributes in TerrainShaper) ─
        public static readonly Range WarpStrength     = new Range(0f, 15f, 6f, 0.1f);
        public static readonly Range WarpScale        = new Range(0.5f, 5f, 2.5f, 0.1f);
        public static readonly Range RidgeWeight      = new Range(0f, 1f, 0.55f, 0.01f);
        public static readonly Range RidgeSharpness   = new Range(1f, 6f, 2.2f, 0.1f);
        public static readonly Range TerraceSteps     = new Range(0f, 12f, 5f, 1f);
        public static readonly Range TerraceSharpness = new Range(1f, 8f, 4f, 0.1f);
        public static readonly Range TerraceStrength  = new Range(0f, 1f, 0.65f, 0.01f);

        // ── Group 3: Water & atmosphere ──────────────────────────────────────
        public static readonly Range WaterLevel      = new Range(3f, 20f, 8f, 0.5f);
        public static readonly Range SunGlowIntensity = new Range(0f, 2f, 1f, 0.05f);
        public static readonly Range AmbientIntensity = new Range(0f, 2f, 1f, 0.05f);
        public static readonly Range FogDensity      = new Range(0.02f, 0.12f, 0.055f, 0.005f);
        public static readonly Range FadeStart       = new Range(4f, 120f, 14f, 0.5f);
        public static readonly Range FadeEnd         = new Range(8f, 120f, 22f, 0.5f);
        public static readonly Range WaterResolution = new Range(32f, 128f, 96f, 8f);
        public static readonly Range CameraFarMargin = new Range(1f, 6f, 3f, 0.5f);

        // ── Group 4: Life / scatter ──────────────────────────────────────────
        public static readonly Range LifeDensity      = new Range(0f, 2f, 1f, 0.05f);
        public static readonly Range MaxPropsPerChunk = new Range(0f, 300f, 240f, 10f);
        public static readonly Range WaterTint        = new Range(0f, 1f, 0.18f, 0.01f);
        public static readonly Range SpeciesDensity   = new Range(0f, 3f, 1f, 0.05f);

        // ── Group 3b: Medium, surge, particulate ─────────────────────────────
        public static readonly Range AbsorbTint     = new Range(0f, 4f, 0f, 0.05f);
        public static readonly Range SurgeAmplitude = new Range(0f, 1f, 0f, 0.01f);
        public static readonly Range SurgeSpeed     = new Range(0f, 3f, 0.55f, 0.05f);
        public static readonly Range SnowCount      = new Range(0f, 4000f, 0f, 10f);
        public static readonly Range SnowOpacity    = new Range(0f, 1f, 0.5f, 0.01f);
        public static readonly Range SnowSize       = new Range(0.001f, 0.15f, 0.012f, 0.001f);
        public static readonly Range SnowSpeed      = new Range(0f, 0.5f, 0.05f, 0.005f);
        public static readonly Range EncrustAmount  = new Range(0f, 1f, 0f, 0.01f);
        public static readonly Range EncrustScale   = new Range(0.1f, 8f, 1.6f, 0.1f);
        public static readonly Range EncrustRelief  = new Range(0f, 1f, 0.85f, 0.01f);
        public static readonly Range TurfAmount     = new Range(0f, 1f, 0f, 0.01f);
        public static readonly Range TurfScale      = new Range(0.1f, 12f, 2.6f, 0.1f);
        public static readonly Range TurfUpBias     = new Range(0f, 1f, 0.55f, 0.01f);
        public static readonly Range TurfRelief     = new Range(0f, 1f, 0.7f, 0.01f);

        public const float TerrainScale = 0.25f; // EndlessTerrain.Scale — world size of a chunk unit

        // World distance (metres) the terrain streams to for each view-distance preset.
        // Fog must fully hide the streaming edge inside this reach (invariant I-2).
        public static float ViewDistanceWorldReach(int index)
        {
            switch (Mathf.Clamp(index, 0, 2))
            {
                case 0:  return 96f  * TerrainScale; // Near   = 24 m
                case 2:  return 256f * TerrainScale; // Far    = 64 m
                default: return 160f * TerrainScale; // Medium = 40 m
            }
        }

        // Applies every per-field cap, then re-derives the cross-field invariants.
        public static void Clamp(EnvironmentProfile p)
        {
            if (p == null) return;

            // User layer (dropdown indices / 0..1 sliders) — kept valid here too so
            // a hand-edited file can't hold an out-of-range choice.
            if (p.simple == null) p.simple = new SimpleEnvironmentSettings();
            p.simple.Clamp();

            // Living Ecosystem layer — same rule: never trust the incoming object.
            if (p.ecosystem == null) p.ecosystem = new Ecosystem.EcosystemSettings();
            p.ecosystem.Clamp();

            // Group 1
            p.noiseScale  = NoiseScale.Clamp(p.noiseScale);
            p.octaves     = Octaves.ClampInt(p.octaves);
            p.persistance = Persistance.Clamp(p.persistance);
            p.lacunarity  = Lacunarity.Clamp(p.lacunarity);
            p.offsetX     = Offset.Clamp(p.offsetX);
            p.offsetY     = Offset.Clamp(p.offsetY);
            p.meshHeightMultiplier = MeshHeightMultiplier.Clamp(p.meshHeightMultiplier);

            // Group 2
            p.styleIndex       = Mathf.Clamp(p.styleIndex, 0, 1);
            p.warpStrength     = WarpStrength.Clamp(p.warpStrength);
            p.warpScale        = WarpScale.Clamp(p.warpScale);
            p.ridgeWeight      = RidgeWeight.Clamp(p.ridgeWeight);
            p.ridgeSharpness   = RidgeSharpness.Clamp(p.ridgeSharpness);
            p.terraceSteps     = TerraceSteps.ClampInt(p.terraceSteps);
            p.terraceSharpness = TerraceSharpness.Clamp(p.terraceSharpness);
            p.terraceStrength  = TerraceStrength.Clamp(p.terraceStrength);

            // Group 3
            p.waterLevel       = WaterLevel.Clamp(p.waterLevel);
            p.sunGlowIntensity = SunGlowIntensity.Clamp(p.sunGlowIntensity);
            p.ambientIntensity = AmbientIntensity.Clamp(p.ambientIntensity);
            p.fogDensity       = FogDensity.Clamp(p.fogDensity);
            p.fadeStart        = FadeStart.Clamp(p.fadeStart);
            p.fadeEnd          = FadeEnd.Clamp(p.fadeEnd);
            p.waterResolution  = WaterResolution.ClampInt(p.waterResolution);
            p.cameraFarMargin  = CameraFarMargin.Clamp(p.cameraFarMargin);

            // Group 4
            p.viewDistanceIndex  = Mathf.Clamp(p.viewDistanceIndex, 0, 2);
            p.lifeDensity        = LifeDensity.Clamp(p.lifeDensity);
            p.maxPropsPerChunk   = MaxPropsPerChunk.ClampInt(p.maxPropsPerChunk);
            p.waterTint          = WaterTint.Clamp(p.waterTint);
            if (p.lifePackIndex < 0) p.lifePackIndex = 0;
            if (p.speciesDensity == null) p.speciesDensity = new float[0];
            for (int i = 0; i < p.speciesDensity.Length; i++)
                p.speciesDensity[i] = SpeciesDensity.Clamp(p.speciesDensity[i]);

            // Group 3b
            p.absorbTint     = AbsorbTint.Clamp(p.absorbTint);
            p.surgeAmplitude = SurgeAmplitude.Clamp(p.surgeAmplitude);
            p.surgeSpeed     = SurgeSpeed.Clamp(p.surgeSpeed);
            p.snowCount      = SnowCount.ClampInt(p.snowCount);
            p.snowOpacity    = SnowOpacity.Clamp(p.snowOpacity);
            p.snowSizeMin    = SnowSize.Clamp(p.snowSizeMin);
            p.snowSizeMax    = SnowSize.Clamp(p.snowSizeMax);
            p.snowDrift      = SnowSpeed.Clamp(p.snowDrift);
            p.snowSink       = SnowSpeed.Clamp(p.snowSink);
            p.encrustAmount  = EncrustAmount.Clamp(p.encrustAmount);
            p.encrustScale   = EncrustScale.Clamp(p.encrustScale);
            p.encrustRelief  = EncrustRelief.Clamp(p.encrustRelief);
            p.turfAmount     = TurfAmount.Clamp(p.turfAmount);
            p.turfScale      = TurfScale.Clamp(p.turfScale);
            p.turfUpBias     = TurfUpBias.Clamp(p.turfUpBias);
            p.turfRelief     = TurfRelief.Clamp(p.turfRelief);

            // Group 3d — time of day. -1 stays -1; anything else is a real index.
            if (p.timeOfDay >= 0)
                p.timeOfDay = Mathf.Clamp(p.timeOfDay, 0,
                                          SimpleEnvironmentSettings.TimesOfDay.Length - 1);
            p.sunElevation      = Mathf.Clamp(p.sunElevation, -10f, 90f);
            p.sunAzimuth        = Mathf.Repeat(p.sunAzimuth, 360f);
            p.sunLightIntensity = Mathf.Clamp(p.sunLightIntensity, 0f, 3f);

            // Group 3c — surface styles.
            p.terrainTextureStyle = Mathf.Clamp(p.terrainTextureStyle, 0,
                                                TerrainTextureStyles.Names.Length - 1);
            p.waterStyle          = Mathf.Clamp(p.waterStyle, 0,
                                                WaterStyles.Names.Length - 1);

            if (p.snowSizeMax < p.snowSizeMin) p.snowSizeMax = p.snowSizeMin;
            if (Mathf.Abs(p.surgeDirX) < 1e-4f && Mathf.Abs(p.surgeDirZ) < 1e-4f)
            {
                p.surgeDirX = 1f;
                p.surgeDirZ = 0f;
            }

            // ── Cross-field invariants (re-derived, never trusted) ───────────
            float reach = ViewDistanceWorldReach(p.viewDistanceIndex);

            // I-2: fog must fully hide the streaming edge.
            p.fadeEnd = Mathf.Min(p.fadeEnd, reach - 2f);
            // I-1: fog needs a real fade band.
            p.fadeStart = Mathf.Min(p.fadeStart, p.fadeEnd - 2f);
            if (p.fadeStart < FadeStart.Min) p.fadeStart = FadeStart.Min;

            // I-3: keep terrain peaks below the water surface (no eruptions).
            float peak = p.meshHeightMultiplier * TerrainScale;
            if (peak > p.waterLevel - 1f)
                p.meshHeightMultiplier = Mathf.Max(MeshHeightMultiplier.Min,
                                                   (p.waterLevel - 1f) / TerrainScale);
        }

        public static EnvironmentProfile Default()
        {
            var p = new EnvironmentProfile();
            Clamp(p);
            return p;
        }
    }
}
