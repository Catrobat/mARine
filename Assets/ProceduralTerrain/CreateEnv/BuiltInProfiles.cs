using UnityEngine;

namespace CreateEnv
{
    // The three shipped biomes as data, not scenes. Values mirror the original
    // SampleScene / CoralCanyons / KelpForest scenes.
    public static class BuiltInProfiles
    {
        public static EnvironmentProfile[] All()
        {
            return new[] { Sample(), Canyon(), Kelp() };
        }

        static EnvironmentProfile Sample()
        {
            var p = new EnvironmentProfile
            {
                id = "builtin-sample", displayName = "Sample", isBuiltIn = true,
                styleIndex = 0,
                noiseScale = 45f, octaves = 5, persistance = 0.5f, lacunarity = 2f,
                meshHeightMultiplier = 18f, seed = 0,
                waterLevel = 8f, fogDensity = 0.055f, fadeStart = 14f, fadeEnd = 22f,
                lifePackIndex = 1, lifeDensity = 1f, viewDistanceIndex = 1,
                absorbTint = 2.0f, snowCount = 650, snowOpacity = 0.48f,
                surgeAmplitude = 0.05f, encrustAmount = 0.45f,
                encrustColorC = new Color(0.44f, 0.20f, 0.26f, 1f),
                encrustRelief = 0.30f,
                turfAmount = 0.55f,
                turfColor = new Color(0.14f, 0.22f, 0.12f, 1f),
                turfTipColor = new Color(0.44f, 0.56f, 0.24f, 1f),
            };
            EnvironmentBounds.Clamp(p);
            return p;
        }

        static EnvironmentProfile Canyon()
        {
            var p = new EnvironmentProfile
            {
                id = "builtin-canyon", displayName = "Canyon", isBuiltIn = true,
                styleIndex = 1,
                noiseScale = 60f, octaves = 6, persistance = 0.5f, lacunarity = 2.1f,
                meshHeightMultiplier = 40f, seed = 7,
                warpStrength = 6f, warpScale = 2.5f, ridgeWeight = 0.6f, ridgeSharpness = 2.4f,
                terraceSteps = 5, terraceSharpness = 4f, terraceStrength = 0.65f,
                waterLevel = 14f, fogDensity = 0.05f, fadeStart = 18f, fadeEnd = 30f,
                lifePackIndex = 3, lifeDensity = 0.8f, viewDistanceIndex = 2,
                waterColor = new Color(0.02f, 0.35f, 0.5f, 1f),
                absorbTint = 2.2f, snowCount = 560, snowOpacity = 0.44f,
                surgeAmplitude = 0.045f, encrustAmount = 0.90f,
                encrustColorA = new Color(0.62f, 0.28f, 0.42f, 1f),
                encrustColorB = new Color(0.82f, 0.48f, 0.26f, 1f),
                encrustColorC = new Color(0.50f, 0.22f, 0.28f, 1f),
                encrustRelief = 1.0f,
                turfAmount = 0.45f,
                turfColor = new Color(0.14f, 0.22f, 0.12f, 1f),
                turfTipColor = new Color(0.44f, 0.56f, 0.24f, 1f),
            };
            EnvironmentBounds.Clamp(p);
            return p;
        }

        static EnvironmentProfile Kelp()
        {
            var p = new EnvironmentProfile
            {
                id = "builtin-kelp", displayName = "Kelp Forest", isBuiltIn = true,
                styleIndex = 0,
                noiseScale = 50f, octaves = 5, persistance = 0.48f, lacunarity = 2f,
                meshHeightMultiplier = 16f, seed = 3,
                waterLevel = 10f, fogDensity = 0.06f, fadeStart = 12f, fadeEnd = 22f,
                lifePackIndex = 2, lifeDensity = 1.3f, viewDistanceIndex = 1,
                waterColor = new Color(0.03f, 0.4f, 0.42f, 1f),
                surfaceGlowColor = new Color(0.4f, 0.8f, 0.65f, 1f),
                absorbTint = 2.5f, snowCount = 820, snowOpacity = 0.55f,
                surgeAmplitude = 0.07f, encrustAmount = 0.92f,
                encrustColorA = new Color(0.36f, 0.42f, 0.22f, 1f),
                encrustColorB = new Color(0.62f, 0.44f, 0.20f, 1f),
                encrustColorC = new Color(0.40f, 0.26f, 0.24f, 1f),
                encrustRelief = 0.85f,
                turfAmount = 0.80f,
                turfColor = new Color(0.11f, 0.24f, 0.10f, 1f),
                turfTipColor = new Color(0.42f, 0.62f, 0.18f, 1f),
            };
            EnvironmentBounds.Clamp(p);
            return p;
        }
    }
}
