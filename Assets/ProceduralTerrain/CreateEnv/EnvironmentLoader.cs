using UnityEngine;

namespace CreateEnv
{
    // Lives on one GameObject in the Explore scene.
    public class EnvironmentLoader : MonoBehaviour
    {
        [Header("Optional explicit refs (auto-found if left empty)")]
        public MapGenerator          mapGenerator;
        public UnderwaterEnvironment underwater;
        public WaterSurface          water;
        public TerrainDetailScatter  scatter;
        public EndlessTerrain        endlessTerrain;
        public MarineSnow            snow;
        [Tooltip("Rock/feature scatters. Left empty, every one in the scene is found and " +
                 "given the profile's encrusting growth.")]
        public ProceduralFeatureScatter[] featureScatters;

        [Tooltip("Used only when Explore is entered directly (no StartScreen). " +
                 "0 = Sample, 1 = Canyon, 2 = Kelp.")]
        public int fallbackBuiltInIndex = 0;

        void Start()
        {
            var profile = ResolveProfile();
            EnvironmentBounds.Clamp(profile); // never trust the incoming object
            Apply(profile);
        }

        EnvironmentProfile ResolveProfile()
        {
            if (EnvironmentSession.Selected != null)
                return EnvironmentSession.Selected;

            // Entered Explore directly (e.g. pressing Play in the Editor): fall back
            // to a built-in so the scene still works for development.
            var builtins = BuiltInProfiles.All();
            int i = Mathf.Clamp(fallbackBuiltInIndex, 0, builtins.Length - 1);
            Debug.Log($"[EnvironmentLoader] No selected profile; using built-in '{builtins[i].displayName}'.");
            return builtins[i];
        }

        void Apply(EnvironmentProfile p)
        {
            AutoWire();

            // 1) Terrain shape — must be set before any chunk generates.
            if (mapGenerator != null) mapGenerator.ApplyProfile(p);
            else Debug.LogWarning("[EnvironmentLoader] No MapGenerator found — terrain won't match the profile.");

            // 2) Water & atmosphere (single waterLevel source drives both, invariant I-4).
            if (underwater != null)
            {
                underwater.waterColor       = p.waterColor;
                underwater.surfaceGlowColor = p.surfaceGlowColor;
                underwater.deepColor        = p.deepColor;
                underwater.sunGlowColor     = p.sunGlowColor;
                underwater.sunGlowIntensity = p.sunGlowIntensity;
                underwater.ambientIntensity = p.ambientIntensity;
                underwater.fogDensity       = p.fogDensity;
                underwater.fadeStart        = p.fadeStart;
                underwater.fadeEnd          = p.fadeEnd;
                underwater.cameraFarMargin  = p.cameraFarMargin;
                underwater.waterLevel       = p.waterLevel; // I-5 far plane derived inside Apply()

                // Only take over the scene's light when a time of day was actually
                // chosen; -1 leaves whatever lighting the scene already has.
                underwater.driveSunLight     = p.timeOfDay >= 0;
                underwater.sunElevation      = p.sunElevation;
                underwater.sunAzimuth        = p.sunAzimuth;
                underwater.sunLightColor     = p.sunLightColor;
                underwater.sunLightIntensity = p.sunLightIntensity;

                underwater.absorbTint     = p.absorbTint;
                underwater.surgeAmplitude = p.surgeAmplitude;
                underwater.surgeSpeed     = p.surgeSpeed;
                underwater.surgeDirection = new Vector2(p.surgeDirX, p.surgeDirZ);
                underwater.encrustAmount  = p.encrustAmount;
                underwater.encrustScale   = p.encrustScale;
                underwater.encrustColorA  = p.encrustColorA;
                underwater.encrustColorB  = p.encrustColorB;
                underwater.encrustColorC  = p.encrustColorC;
                underwater.encrustRelief  = p.encrustRelief;
                underwater.turfAmount     = p.turfAmount;
                underwater.turfColor      = p.turfColor;
                underwater.turfTipColor   = p.turfTipColor;
                underwater.turfScale      = p.turfScale;
                underwater.turfUpBias     = p.turfUpBias;
                underwater.turfRelief     = p.turfRelief;
            }

            // 2b) Suspended particulate. The wrap box is tied to fadeEnd so snow never
            // pops in beyond the distance the fog has already gone opaque (invariant I-2).
            if (snow != null)
            {
                float snowFar = Mathf.Clamp(p.fadeEnd * 0.35f, 3f, 9f);
                snow.nearFade = 0.5f;
                snow.farFade  = snowFar;
                snow.ApplySettings(p.snowCount, snowFar * 1.7f,
                                   Color.Lerp(p.surfaceGlowColor, Color.white, 0.55f),
                                   p.snowOpacity, p.snowSizeMin, p.snowSizeMax,
                                   p.snowDrift, p.snowSink);
            }

            // 2c) Surface styles — must precede water.Rebuild() and EndlessTerrain.Initialize().
            ApplySurfaceStyles(p);

            // 3) Water plane.
            if (water != null)
            {
                water.waterLevel = p.waterLevel;
                water.resolution = Mathf.Clamp(p.waterResolution, 8, 160);
                water.Rebuild();
            }

            // 4) Life / scatter (per-species density, capped by maxPropsPerChunk = I-6).
            if (scatter != null)
            {
                var lib  = LifePackLibrary.Load();
                var pack = lib != null ? lib.GetPack(p.lifePackIndex) : null;
                if (lib == null && p.lifePackIndex > 0)
                    Debug.LogWarning("[EnvironmentLoader] No LifePackLibrary in Resources — keeping the scene's existing scatter rules; only global density is applied.");

                if (lib != null)
                    scatter.ApplyLifePack(pack, p.lifeDensity, p.speciesDensity,
                                          p.maxPropsPerChunk, p.castShadows, p.waterTint, p.scatterSeed);
                else
                {
                    // Fallback: scale whatever rules the scene already has.
                    scatter.maxPropsPerChunk = p.maxPropsPerChunk;
                    scatter.castShadows      = p.castShadows;
                    scatter.waterTint        = p.waterTint;
                }
            }

            // 4b) Drop cached feature meshes before EndlessTerrain.Initialize() below streams the
            // first chunk:
            foreach (var fs in featureScatters)
            {
                if (fs == null) continue;
                fs.ApplyHabitat(p.lifePackIndex);   // also drops the mesh cache
            }

            // 5) Streaming — LAST, after MapGenerator is configured.
            if (endlessTerrain != null)
            {
                endlessTerrain.autoStart    = false;
                endlessTerrain.detailLevels = EndlessTerrain.BuildViewDistance(p.viewDistanceIndex);
                endlessTerrain.Initialize();
            }
            else Debug.LogWarning("[EnvironmentLoader] No EndlessTerrain found — nothing will stream.");
        }

        // Runtime material clones created for the chosen surface styles.
        Material _styledTerrainMaterial;
        Material _styledWaterMaterial;

        // Applies the sea-floor texture style and the water style.
        void ApplySurfaceStyles(EnvironmentProfile p)
        {
            if (p.terrainTextureStyle > 0 && mapGenerator != null && mapGenerator.terrainMaterial != null)
            {
                _styledTerrainMaterial = new Material(mapGenerator.terrainMaterial)
                {
                    name = mapGenerator.terrainMaterial.name + " (styled)"
                };
                TerrainTextureStyles.Apply(_styledTerrainMaterial, p.terrainTextureStyle);
                // Caustic character is seeded from the environment's own seed and
                // palette, so two environments on the same sea floor still differ.
                TerrainTextureStyles.ApplyCaustics(_styledTerrainMaterial, p.terrainTextureStyle,
                                                   p.seed, p.surfaceGlowColor);
                mapGenerator.terrainMaterial = _styledTerrainMaterial;
            }

            if (p.waterStyle > 0 && water != null && water.waterMaterial != null)
            {
                _styledWaterMaterial = new Material(water.waterMaterial)
                {
                    name = water.waterMaterial.name + " (styled)"
                };
                WaterStyles.Apply(_styledWaterMaterial, p.waterStyle, p.waterColor, p.seed);
                water.waterMaterial = _styledWaterMaterial;
            }
        }

        void OnDestroy()
        {
            if (_styledTerrainMaterial != null) Destroy(_styledTerrainMaterial);
            if (_styledWaterMaterial != null)   Destroy(_styledWaterMaterial);
        }

        void AutoWire()
        {
            if (mapGenerator   == null) mapGenerator   = FindFirstObjectByType<MapGenerator>();
            if (underwater     == null) underwater     = FindFirstObjectByType<UnderwaterEnvironment>();
            if (water          == null) water          = FindFirstObjectByType<WaterSurface>();
            if (scatter        == null) scatter        = FindFirstObjectByType<TerrainDetailScatter>();
            if (endlessTerrain == null) endlessTerrain = FindFirstObjectByType<EndlessTerrain>();
            // Plural, and never left null: a scene can carry several feature scatters
            // (one for boulders, one for spires) and every one of them needs the crust.
            if (featureScatters == null || featureScatters.Length == 0)
                featureScatters = FindObjectsByType<ProceduralFeatureScatter>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (snow           == null) snow           = FindFirstObjectByType<MarineSnow>();
        }
    }
}
