using UnityEngine;

// Styled heightmap generation for biome terrains. Classic delegates to Noise.cs (bit-
// identical with the original scenes);
public static class TerrainShaper
{
    public enum Style
    {
        Classic,  // original rolling dunes (Noise.GenerateNoiseMap untouched)
        Canyons   // ridged + terraced: canyon walls, mesas, cliff bands
    }

    [System.Serializable]
    public class Settings
    {
        public Style style = Style.Classic;

        [Header("Domain warp (bends canyons into organic curves)")]
        [Tooltip("How far samples are pushed around, in noise-space units. 0 = off.")]
        public float warpStrength = 6f;
        [Tooltip("Feature size of the warp field relative to the main noise scale.")]
        public float warpScale = 2.5f;

        [Header("Ridges (sharp crests and canyon walls)")]
        [Range(0f, 1f)]
        [Tooltip("0 = plain rolling fBm, 1 = fully ridged mountains.")]
        public float ridgeWeight = 0.55f;
        [Range(1f, 6f)]
        [Tooltip("Higher = knife-edge crests, lower = soft ridges.")]
        public float ridgeSharpness = 2.2f;

        [Header("Terracing (flat mesa tops with cliff steps between)")]
        [Range(0, 12)]
        [Tooltip("Number of height bands. 0 = no terracing.")]
        public int terraceSteps = 5;
        [Range(1f, 8f)]
        [Tooltip("Higher = harder cliff edge between bands.")]
        public float terraceSharpness = 4f;
        [Range(0f, 1f)]
        [Tooltip("Blend between smooth terrain (0) and fully terraced (1).")]
        public float terraceStrength = 0.65f;
    }

    // Same signature contract as Noise.GenerateNoiseMap: values normalized to
    // ~[0,1] deterministically (global normalization), so streamed chunks match.
    public static float[,] Generate(
        int mapWidth, int mapHeight, int seed, float scale,
        int octaves, float persistance, float lacunarity,
        Vector2 offset, Settings settings)
    {
        float[,] map = new float[mapWidth, mapHeight];
        if (scale <= 0f) scale = 0.0001f;

        // Two independent offset sets: one for the main fBm, one for the warp
        // field. Seeded like Noise.cs so the layout is stable per seed.
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        float maxPossibleHeight = 0f;
        float amplitude = 1f;

        for (int i = 0; i < octaves; i++)
        {
            octaveOffsets[i] = new Vector2(
                prng.Next(-100000, 100000),
                prng.Next(-100000, 100000));
            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        Vector2 warpOffsetA = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));
        Vector2 warpOffsetB = new Vector2(prng.Next(-100000, 100000), prng.Next(-100000, 100000));

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;
        float warpFeatureScale = scale * Mathf.Max(settings.warpScale, 0.01f);

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // Identical grouping to Noise.cs: small numbers first, so shared
                // border samples between chunks are bit-identical (no seams).
                float baseX = x - halfWidth + offset.x;
                float baseY = y - halfHeight - offset.y;

                // Domain warp: push the sample point around by a low-frequency
                // vector field. Computed from absolute coordinates only.
                if (settings.warpStrength > 0f)
                {
                    float wx = Mathf.PerlinNoise((baseX + warpOffsetA.x) / warpFeatureScale,
                                                 (baseY + warpOffsetA.y) / warpFeatureScale) * 2f - 1f;
                    float wy = Mathf.PerlinNoise((baseX + warpOffsetB.x) / warpFeatureScale,
                                                 (baseY + warpOffsetB.y) / warpFeatureScale) * 2f - 1f;
                    baseX += wx * settings.warpStrength;
                    baseY += wy * settings.warpStrength;
                }

                amplitude = 1f;
                float frequency = 1f;
                float smoothSum = 0f;   // plain fBm, range ~[-max, max]
                float ridgeSum = 0f;    // ridged fBm, range [0, max]

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (baseX + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (baseY + octaveOffsets[i].y) / scale * frequency;
                    // PerlinNoise may return slightly outside [0,1];
                    float n = Mathf.Clamp01(Mathf.PerlinNoise(sampleX, sampleY));

                    smoothSum += (n * 2f - 1f) * amplitude;

                    // Ridge: fold the noise so its midline becomes a sharp crest.
                    float r = 1f - Mathf.Abs(n * 2f - 1f);
                    ridgeSum += Mathf.Pow(r, settings.ridgeSharpness) * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                // Both components normalized deterministically to ~[0,1]
                // (matches the Global mode maths in Noise.cs).
                float smooth01 = Mathf.Clamp01((smoothSum + 1f) / (maxPossibleHeight / 0.9f));
                float ridge01 = Mathf.Clamp01(ridgeSum / maxPossibleHeight);

                float h = Mathf.Lerp(smooth01, ridge01, settings.ridgeWeight);

                if (settings.terraceSteps > 0 && settings.terraceStrength > 0f)
                    h = Mathf.Lerp(h, Terrace(h, settings.terraceSteps, settings.terraceSharpness),
                                   settings.terraceStrength);

                map[x, y] = h;
            }
        }

        return map;
    }

    // Quantizes height into bands with a sharp (but continuous) transition —
    // flat mesa tops, near-vertical cliff faces between them.
    static float Terrace(float h, int steps, float sharpness)
    {
        float t = h * steps;
        float band = Mathf.Floor(t);
        float f = t - band;

        // Gain curve: pushes f toward 0 or 1, keeping f(0)=0, f(0.5)=0.5, f(1)=1.
        float p = Mathf.Pow(f, sharpness);
        f = p / (p + Mathf.Pow(1f - f, sharpness));

        return (band + f) / steps;
    }
}
