using UnityEngine;

namespace CreateEnv
{
    // Procedural surface-style libraries for the Create-Env feature.
    public static class TerrainTextureStyles
    {
        public static readonly string[] Names =
        {
            "Classic sand", "Golden Ripples", "Coral Rubble",
            "Volcanic Ash", "Silty Mud", "Pebble Field", "Bleached Shells",
        };

        const int Size = 256; // texture side; tileable by construction

        static readonly Texture2D[] AlbedoCache = new Texture2D[Names.Length];
        static readonly Texture2D[] NormalCache = new Texture2D[Names.Length];

        // Applies style `index` to a material using Custom/UnderwaterSand.
        // Call on a runtime CLONE of the material — never on the shared asset.
        public static void Apply(Material mat, int index)
        {
            index = Mathf.Clamp(index, 0, Names.Length - 1);
            if (mat == null || index == 0) return;

            Bake(index);
            SetTex(mat, "_BaseMap",   AlbedoCache[index]);
            SetTex(mat, "_NormalMap", NormalCache[index]);

            switch (index)
            {
                case 1: // Golden Ripples — classic dunes, stronger relief
                    SetCol(mat, "_BaseColor", new Color(1f, 0.97f, 0.88f));
                    SetF(mat, "_WorldTiling", 0.35f);       SetF(mat, "_DetileStrength", 0.4f);
                    SetF(mat, "_NormalStrength", 0.8f);
                    SetF(mat, "_CausticsIntensity", 1.0f);  SetF(mat, "_SparkleIntensity", 0.6f);
                    break;
                case 2: // Coral Rubble — broken pink/white chunks
                    SetCol(mat, "_BaseColor", Color.white);
                    SetF(mat, "_WorldTiling", 0.5f);        SetF(mat, "_DetileStrength", 0.15f);
                    SetF(mat, "_NormalStrength", 1.1f);
                    SetF(mat, "_CausticsIntensity", 0.9f);  SetF(mat, "_SparkleIntensity", 0.4f);
                    break;
                case 3: // Volcanic Ash — dark floor, glinting flecks
                    SetCol(mat, "_BaseColor", new Color(0.92f, 0.92f, 0.98f));
                    SetF(mat, "_WorldTiling", 0.45f);       SetF(mat, "_DetileStrength", 0.35f);
                    SetF(mat, "_NormalStrength", 0.55f);
                    SetF(mat, "_CausticsIntensity", 0.55f); SetF(mat, "_SparkleIntensity", 1.3f);
                    break;
                case 4: // Silty Mud — smooth olive estuary bed
                    SetCol(mat, "_BaseColor", Color.white);
                    SetF(mat, "_WorldTiling", 0.25f);       SetF(mat, "_DetileStrength", 0.5f);
                    SetF(mat, "_NormalStrength", 0.35f);
                    SetF(mat, "_CausticsIntensity", 0.8f);  SetF(mat, "_SparkleIntensity", 0.15f);
                    break;
                case 5: // Pebble Field — rounded stones, hard relief
                    SetCol(mat, "_BaseColor", Color.white);
                    SetF(mat, "_WorldTiling", 0.6f);        SetF(mat, "_DetileStrength", 0.1f);
                    SetF(mat, "_NormalStrength", 1.3f);
                    SetF(mat, "_CausticsIntensity", 1.0f);  SetF(mat, "_SparkleIntensity", 0.5f);
                    break;
                case 6: // Bleached Shells — pale flat with shell flecks
                    SetCol(mat, "_BaseColor", Color.white);
                    SetF(mat, "_WorldTiling", 0.4f);        SetF(mat, "_DetileStrength", 0.3f);
                    SetF(mat, "_NormalStrength", 0.5f);
                    SetF(mat, "_CausticsIntensity", 1.2f);  SetF(mat, "_SparkleIntensity", 0.8f);
                    break;
            }
        }

        // Caustics never repeat between environments:
        static readonly float[] CausticsScaleBase = { 0.6f, 0.6f, 0.85f, 0.45f, 0.35f, 0.75f, 0.7f };
        static readonly float[] CausticsSpeedBase = { 0.5f, 0.5f, 0.6f, 0.35f, 0.3f, 0.55f, 0.65f };

        public static void ApplyCaustics(Material mat, int index, int seed, Color surfaceGlow)
        {
            if (mat == null) return;
            index = Mathf.Clamp(index, 0, Names.Length - 1);

            var rng = new System.Random(seed * 7919 + index * 131 + 17);
            float J(float lo, float hi) => Mathf.Lerp(lo, hi, (float)rng.NextDouble());

            SetF(mat, "_CausticsScale",  CausticsScaleBase[index] * J(0.75f, 1.4f));
            SetF(mat, "_CausticsSpeed",  CausticsSpeedBase[index] * J(0.7f, 1.35f));
            SetF(mat, "_CausticsChroma", J(0.4f, 1.6f));
            SetCol(mat, "_CausticsColor", Color.Lerp(surfaceGlow, Color.white, J(0.45f, 0.7f)));
        }

        // ── Baking ───────────────────────────────────────────────────────────
        static void Bake(int index)
        {
            if (AlbedoCache[index] != null) return;

            var albedo = new Color[Size * Size];
            var height = new float[Size * Size];
            int seed   = 1000 + index * 733; // fixed per style → deterministic

            for (int y = 0; y < Size; y++)
            {
                float v = (float)y / Size;
                for (int x = 0; x < Size; x++)
                {
                    float u = (float)x / Size;
                    int   i = y * Size + x;
                    Pixel(index, u, v, x, y, seed, out albedo[i], out height[i]);
                }
            }

            AlbedoCache[index] = ToTexture(albedo, false, $"TerrainAlbedo_{Names[index]}");
            NormalCache[index] = ToTexture(HeightToNormals(height), true, $"TerrainNormal_{Names[index]}");
        }

        static void Pixel(int style, float u, float v, int px, int py, int seed,
                          out Color col, out float h)
        {
            switch (style)
            {
                case 2: // Coral Rubble
                {
                    Worley(u, v, 12, seed, out float f1, out float f2, out float id);
                    float chunk = Mathf.Clamp01(1f - f1 * 1.8f);
                    h = chunk * 0.8f + Fbm(u, v, 32, 2, seed + 9) * 0.2f;
                    Color cell = Pick(id,
                        new Color(0.95f, 0.92f, 0.88f), new Color(0.93f, 0.72f, 0.72f),
                        new Color(0.90f, 0.60f, 0.50f), new Color(0.80f, 0.72f, 0.85f));
                    Color gap = new Color(0.42f, 0.38f, 0.36f);
                    col = Color.Lerp(gap, cell * (0.6f + 0.4f * chunk),
                                     Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.35f, chunk)));
                    return;
                }
                case 3: // Volcanic Ash
                {
                    float grain = Fbm(u, v, 24, 4, seed);
                    h = grain;
                    col = Color.Lerp(new Color(0.07f, 0.07f, 0.08f),
                                     new Color(0.24f, 0.24f, 0.27f), grain);
                    if (Hash(px, py, seed + 3) > 0.996f) // sparse mineral specks
                    {
                        col = new Color(0.62f, 0.57f, 0.50f);
                        h   = Mathf.Min(1f, h + 0.35f);
                    }
                    return;
                }
                case 4: // Silty Mud
                {
                    float blotch = Fbm(u, v, 6, 3, seed);
                    float fine   = Fbm(u, v, 24, 2, seed + 4);
                    h = blotch * 0.7f + fine * 0.3f;
                    col = Color.Lerp(new Color(0.30f, 0.27f, 0.18f),
                                     new Color(0.52f, 0.47f, 0.30f), h);
                    return;
                }
                case 5: // Pebble Field
                {
                    Worley(u, v, 16, seed, out float f1, out float f2, out float id);
                    float pebble = Mathf.Clamp01(1f - f1 * 2f);
                    h = Mathf.Pow(pebble, 0.8f);
                    Color stone = id > 0.7f
                        ? new Color(0.55f, 0.45f, 0.35f)
                        : Color.Lerp(new Color(0.45f, 0.42f, 0.38f),
                                     new Color(0.64f, 0.62f, 0.57f), id / 0.7f);
                    Color gapSand = new Color(0.75f, 0.68f, 0.52f);
                    col = Color.Lerp(gapSand, stone * (0.6f + 0.4f * pebble),
                                     Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.25f, pebble)));
                    return;
                }
                case 6: // Bleached Shells
                {
                    float grain = Fbm(u, v, 32, 3, seed);
                    h = grain * 0.5f;
                    col = Color.Lerp(new Color(0.90f, 0.87f, 0.78f),
                                     new Color(0.99f, 0.97f, 0.90f), grain);
                    Worley(u, v, 20, seed + 31, out float f1, out float f2, out float id);
                    if (id > 0.55f) // only some cells carry a shell fleck
                    {
                        float fleck = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.16f, f1));
                        col = Color.Lerp(col, new Color(0.72f, 0.63f, 0.48f), fleck * 0.8f);
                        h  += fleck * 0.4f;
                    }
                    return;
                }
                default: // 1: Golden Ripples
                {
                    float warp  = Fbm(u, v, 8, 3, seed);
                    float band  = 0.5f + 0.5f * Mathf.Sin((v * 14f + warp * 2.2f) * 2f * Mathf.PI);
                    float grain = Fbm(u, v, 32, 3, seed + 5);
                    h = band * 0.55f + grain * 0.45f;
                    col = Color.Lerp(new Color(0.78f, 0.63f, 0.42f),
                                     new Color(0.98f, 0.92f, 0.72f), h);
                    return;
                }
            }
        }

        // Central-difference normals from the height field, packed for
        // UnpackNormalScale (x in R×A with A=1, y in G).
        static Color[] HeightToNormals(float[] h)
        {
            const float k = 2.2f;
            var outCols = new Color[Size * Size];
            for (int y = 0; y < Size; y++)
            {
                int ym = (y - 1 + Size) % Size, yp = (y + 1) % Size;
                for (int x = 0; x < Size; x++)
                {
                    int xm = (x - 1 + Size) % Size, xp = (x + 1) % Size;
                    float dx = h[y * Size + xp] - h[y * Size + xm];
                    float dy = h[yp * Size + x] - h[ym * Size + x];
                    Vector3 n = new Vector3(-dx * k, -dy * k, 1f).normalized;
                    outCols[y * Size + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, 1f, 1f);
                }
            }
            return outCols;
        }

        static Texture2D ToTexture(Color[] pixels, bool linear, string name)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true, linear)
            {
                name       = name,
                wrapMode   = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            tex.SetPixels(pixels);
            tex.Apply(true, true);
            return tex;
        }

        // ── Tileable noise (integer-hash based, deterministic on all platforms) ─
        static float Hash(int x, int y, int seed)
        {
            unchecked
            {
                uint n = (uint)(x * 374761393 + y * 668265263 + seed * 1013904223);
                n = (n ^ (n >> 13)) * 1274126177u;
                n ^= n >> 16;
                return (n & 0xFFFFFF) / 16777215f;
            }
        }

        static int Wrap(int i, int n) => ((i % n) + n) % n;

        static float ValueNoise(float u, float v, int cells, int seed)
        {
            float x = u * cells, y = v * cells;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = Hash(Wrap(x0,     cells), Wrap(y0,     cells), seed);
            float b = Hash(Wrap(x0 + 1, cells), Wrap(y0,     cells), seed);
            float c = Hash(Wrap(x0,     cells), Wrap(y0 + 1, cells), seed);
            float d = Hash(Wrap(x0 + 1, cells), Wrap(y0 + 1, cells), seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float Fbm(float u, float v, int baseCells, int octaves, int seed)
        {
            float sum = 0f, amp = 0.5f, tot = 0f;
            int cells = baseCells;
            for (int o = 0; o < octaves; o++)
            {
                sum += ValueNoise(u, v, cells, seed + o * 101) * amp;
                tot += amp;
                amp *= 0.5f;
                cells *= 2;
            }
            return tot > 0f ? sum / tot : 0f;
        }

        // Tileable Worley: F1/F2 distances plus a stable per-cell id for palettes.
        static void Worley(float u, float v, int cells, int seed,
                           out float f1, out float f2, out float cellId)
        {
            float x = u * cells, y = v * cells;
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y);
            f1 = f2 = 9f; cellId = 0f;

            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    int cx = xi + dx, cy = yi + dy;
                    int wx = Wrap(cx, cells), wy = Wrap(cy, cells);
                    float px = cx + Hash(wx, wy, seed);
                    float py = cy + Hash(wx, wy, seed + 77);
                    float d = (px - x) * (px - x) + (py - y) * (py - y);
                    if (d < f1) { f2 = f1; f1 = d; cellId = Hash(wx, wy, seed + 123); }
                    else if (d < f2) f2 = d;
                }

            f1 = Mathf.Sqrt(f1);
            f2 = Mathf.Sqrt(f2);
        }

        static Color Pick(float id, params Color[] palette)
            => palette[Mathf.Min((int)(id * palette.Length), palette.Length - 1)];

        static void SetF(Material m, string p, float v)     { if (m.HasProperty(p)) m.SetFloat(p, v); }
        static void SetCol(Material m, string p, Color v)   { if (m.HasProperty(p)) m.SetColor(p, v); }
        static void SetTex(Material m, string p, Texture v) { if (m.HasProperty(p)) m.SetTexture(p, v); }
    }

    // Wave/pattern presets for Custom/StylizedWaterSurface. Pattern styles:
    public static class WaterStyles
    {
        public static readonly string[] Names =
        {
            "Classic", "Calm Lagoon", "Gentle Swell",
            "Choppy Sea", "Glassy Ripples", "Drifting Current",
        };

        // Call on a runtime CLONE of the water material — never on the shared asset.
        public static void Apply(Material mat, int index, Color waterColor, int seed)
        {
            index = Mathf.Clamp(index, 0, Names.Length - 1);
            if (mat == null || index == 0) return;

            // The surface picks up the profile's water palette (slightly lifted so
            // it reads brighter than the fog it melts into).
            if (mat.HasProperty("_WaterColor"))
                mat.SetColor("_WaterColor", Color.Lerp(waterColor, Color.white, 0.15f));

            switch (index)
            {
                case 1: Set(mat, 0.05f, 6.0f, 0.45f, 0, 0.22f, 0.50f, 0.30f, 0.35f, 4.0f); break; // Calm Lagoon
                case 2: Set(mat, 0.14f, 4.5f, 0.80f, 0, 0.30f, 0.70f, 0.50f, 0.45f, 3.0f); break; // Gentle Swell
                case 3: Set(mat, 0.24f, 2.4f, 1.50f, 0, 0.42f, 0.90f, 0.90f, 0.55f, 2.2f); break; // Choppy Sea
                case 4: Set(mat, 0.05f, 5.0f, 0.50f, 1, 1.40f, 1.10f, 0.50f, 0.40f, 4.5f); break; // Glassy Ripples
                case 5: Set(mat, 0.11f, 3.2f, 1.00f, 2, 0.80f, 0.85f, 0.70f, 0.48f, 2.6f); break; // Drifting Current
            }

            // Seeded per-environment jitter so two envs with the same water style
            // still don't share the exact same surface pattern.
            var rng = new System.Random(seed * 4243 + index * 97 + 5);
            float J(float lo, float hi) => Mathf.Lerp(lo, hi, (float)rng.NextDouble());
            Jitter(mat, "_PatternScale", J(0.8f, 1.25f));
            Jitter(mat, "_PatternSpeed", J(0.8f, 1.25f));
            Jitter(mat, "_WaveSpeed",    J(0.9f, 1.15f));
        }

        static void Jitter(Material m, string p, float factor)
        {
            if (m.HasProperty(p)) m.SetFloat(p, m.GetFloat(p) * factor);
        }

        static void Set(Material m, float waveAmp, float waveLen, float waveSpeed,
                        int patternStyle, float patScale, float patIntensity, float patSpeed,
                        float opacity, float fresnel)
        {
            SetF(m, "_WaveAmplitude",    waveAmp);
            SetF(m, "_WaveLength",       waveLen);
            SetF(m, "_WaveSpeed",        waveSpeed);
            SetF(m, "_PatternStyle",     patternStyle);
            SetF(m, "_PatternScale",     patScale);
            SetF(m, "_PatternIntensity", patIntensity);
            SetF(m, "_PatternSpeed",     patSpeed);
            SetF(m, "_Opacity",          opacity);
            SetF(m, "_FresnelPower",     fresnel);
        }

        static void SetF(Material m, string p, float v) { if (m.HasProperty(p)) m.SetFloat(p, v); }
    }
}
