using UnityEngine;
using System.Collections.Generic;

// Scatters procedurally-generated feature meshes (rock formations, kelp plants,
// glow anemones — see ProceduralMeshLibrary) onto streamed terrain chunks.
public class ProceduralFeatureScatter : ChunkDecorator
{
    [System.Serializable]
    public class FeatureRule
    {
        public string name = "Feature";
        public bool enabled = true;
        public ProceduralMeshLibrary.FeatureKind kind = ProceduralMeshLibrary.FeatureKind.SeagrassTuft;

        [Tooltip("Material for every instance of this rule (use the Underwater* prop shaders).")]
        public Material material;
        [Tooltip("How many mesh variations to build for this rule. More = less repetition.")]
        [Range(1, 8)] public int variants = 4;

        [Header("Density")]
        [Tooltip("Average instances per 8 m chunk.")]
        [Range(0f, 30f)] public float densityPerChunk = 1.5f;
        [Tooltip("Cluster into fields using low-frequency world noise instead of spreading evenly.")]
        public bool useMask = false;
        [Tooltip("Smaller = larger fields.")]
        public float maskScale = 0.045f;
        [Range(0f, 1f)] public float maskThreshold = 0.55f;

        [Header("Where it may appear")]
        [Range(0f, 90f)] public float minSlope = 0f;
        [Range(0f, 90f)] public float maxSlope = 45f;
        public Vector2 heightRange = new Vector2(-1000f, 1000f);

        [Header("Per-instance")]
        [Tooltip("Real-world height in metres (min..max).")]
        public Vector2 sizeMeters = new Vector2(0.8f, 2f);
        [Tooltip("Shape of the size distribution between min and max. 1 = uniform, which " +
                 "gives as many boulders near the top of the range as the bottom and is why " +
                 "a field can read as a row of same-sized lumps. Real clast populations are " +
                 "heavy-tailed — lots of small material, a few big blocks — so 2-3 looks " +
                 "much more like a natural boulder field (and costs fewer triangles, since " +
                 "fewer instances clear the high-detail threshold).")]
        [Range(1f, 4f)] public float sizeDistribution = 1f;
        [Tooltip("0 = stand straight up, 1 = fully follow the ground slope.")]
        [Range(0f, 1f)] public float alignToNormal = 0.25f;
        [Tooltip("Random lean, degrees.")]
        public float randomTilt = 6f;
        [Tooltip("Fraction of height sunk into the surface so nothing floats.")]
        [Range(0f, 0.5f)] public float embed = 0.08f;
        [Tooltip("Add a MeshCollider (only worth it for big swim-through formations).")]
        public bool addCollider = false;

        [Header("Minimap")]
        public bool showOnMinimap = false;
        public Color minimapColor = new Color(0.7f, 0.75f, 0.8f, 1f);
    }

    [Header("Seed")]
    public int seed = 7331;

    [Header("Rendering")]
    [Tooltip("Decorative props rarely need shadows; off is much cheaper on mobile.")]
    public bool castShadows = false;
    [Tooltip("Safety cap on instances per chunk (across all rules).")]
    public int maxPropsPerChunk = 80;
    [Header("Feature rules")]
    public FeatureRule[] rules;

    // Mesh library: built once, shared by every instance.
    Mesh[][] _variantCache;

    // Shared by every rule and every rock in the world: a handful of colony shapes is
    // plenty once they are randomly oriented and scaled on the surface.
    const int NubVariants = 6;
    void OnValidate()
    {
        if (rules == null) return;
        foreach (var r in rules)
            if (r != null && r.maxSlope < r.minSlope) r.maxSlope = r.minSlope;
        InvalidateMeshCache();
    }

    // Meshes are built once and shared, so anything that changes their shape has
    // to drop the cache or the first chunk's meshes keep the old shape all run.
    public void InvalidateMeshCache()
    {
        _variantCache = null;
    }

    // Enable rules for the habitat the profile asked for, so a "Sandy Bottom" custom
    // environment does not grow a kelp forest.
    public void ApplyHabitat(int lifePackIndex)
    {
        if (rules == null) return;

        bool kelp   = lifePackIndex == 2;
        bool reef   = lifePackIndex == 3;

        foreach (var rule in rules)
        {
            if (rule == null) continue;
            switch (rule.kind)
            {
                case ProceduralMeshLibrary.FeatureKind.KelpPlant:
                    rule.enabled = kelp; break;
                case ProceduralMeshLibrary.FeatureKind.SeaFan:
                case ProceduralMeshLibrary.FeatureKind.GlowAnemone:
                    rule.enabled = reef; break;
                // Seagrass grows on soft sediment, so it belongs everywhere.
                case ProceduralMeshLibrary.FeatureKind.SeagrassTuft:
                    rule.enabled = true; break;
            }
        }
        InvalidateMeshCache();
    }

    Mesh GetMesh(int ruleIndex, int variant)
    {
        int slots = rules.Length;
        if (_variantCache == null || _variantCache.Length != slots)
            _variantCache = new Mesh[slots][];

        FeatureRule rule = rules[ruleIndex];
        int slot = ruleIndex;
        var set = _variantCache[slot];
        if (set == null || set.Length != rule.variants)
            _variantCache[slot] = set = new Mesh[rule.variants];

        int v = Mathf.Abs(variant) % rule.variants;
        if (set[v] == null)
            // The joint seed deliberately depends on `seed` alone, not on the rule or the variant:
            set[v] = ProceduralMeshLibrary.Build(rule.kind,
                seed * 31 + ruleIndex * 977 + v * 7919 + (int)rule.kind * 53,
                jointSeed: seed * 31 + 12007);
        return set[v];
    }

    public override GameObject PopulateChunk(int chunkX, int chunkZ, Vector3 worldCentre,
                                             float worldHalfSize, Collider surface, Transform parent)
    {
        if (rules == null || rules.Length == 0 || surface == null) return null;

        var rng = new System.Random(HashChunk(chunkX, chunkZ, seed));
        GameObject root = null;
        int spawned = 0;
        float rayTop = worldCentre.y + 50f;


        for (int r = 0; r < rules.Length && spawned < maxPropsPerChunk; r++)
        {
            FeatureRule rule = rules[r];
            if (rule == null || !rule.enabled || rule.densityPerChunk <= 0f) continue;
            if (MaterialFor(rule) == null) continue;

            int n = PoissonCount(rule.densityPerChunk, rng);
            for (int k = 0; k < n && spawned < maxPropsPerChunk; k++)
            {
                float x = worldCentre.x + Rand11(rng) * worldHalfSize;
                float z = worldCentre.z + Rand11(rng) * worldHalfSize;

                if (rule.useMask && (float)rng.NextDouble() > MaskFactor(x, z, rule)) continue;
                if (!SampleGround(surface, x, z, rayTop, out Vector3 p, out Vector3 nrm)) continue;

                float slope = Vector3.Angle(nrm, Vector3.up);
                if (slope < rule.minSlope || slope > rule.maxSlope) continue;
                if (p.y < rule.heightRange.x || p.y > rule.heightRange.y) continue;

                EnsureRoot(ref root, parent, chunkX, chunkZ);

                float footprint = SpawnFeature(rule, r, rng, p, nrm, root.transform);
                spawned++;
            }
        }

        return root;
    }

    Material MaterialFor(FeatureRule rule)
    {
        return rule.material;
    }

    static int RandRange(System.Random rng, int lo, int hi)
    {
        if (hi < lo) hi = lo;
        return rng.Next(lo, hi + 1);
    }

    static void EnsureRoot(ref GameObject root, Transform parent, int chunkX, int chunkZ)
    {
        if (root != null) return;
        root = new GameObject($"Features ({chunkX},{chunkZ})");
        root.transform.SetParent(parent, false);
    }


    // Returns the placed formation's ground footprint radius, so talus can be
    // shed in a skirt that actually matches how big the rock turned out.
    float SpawnFeature(FeatureRule rule, int ruleIndex, System.Random rng,
                       Vector3 groundPoint, Vector3 groundNormal, Transform root)
    {
        // Draw the variant and the size in the same order as before so the world
        // layout is unchanged.
        int variantPick = rng.Next(1 << 20);
        float u = (float)rng.NextDouble();
        if (rule.sizeDistribution > 1.0001f) u = Mathf.Pow(u, rule.sizeDistribution);
        float target = Mathf.Lerp(rule.sizeMeters.x, rule.sizeMeters.y, u);

        Mesh mesh = GetMesh(ruleIndex, variantPick);

        var go = new GameObject(rule.name);
        var renderer = go.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = MaterialFor(rule);
        if (!castShadows)
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        if (rule.showOnMinimap)
            go.AddComponent<MinimapMarker>().Configure(rule.minimapColor, rule.name);

        // Orientation: partial slope alignment + random yaw + a small lean.
        Quaternion align = Quaternion.Slerp(Quaternion.identity,
            Quaternion.FromToRotation(Vector3.up, groundNormal), rule.alignToNormal);
        Quaternion yaw = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
        Quaternion tilt = Quaternion.Euler(Rand11(rng) * rule.randomTilt, 0f, Rand11(rng) * rule.randomTilt);
        go.transform.rotation = align * yaw * tilt;

        // Scale so the mesh's native height hits the target metres, then seat
        // it so its (scaled) base sits at the ground minus the embed fraction.
        Bounds b = mesh.bounds;
        float scale = (b.size.y > 0.0001f) ? target / b.size.y : 1f;
        go.transform.localScale = Vector3.one * scale;
        go.transform.position = groundPoint + Vector3.up * (-b.min.y * scale - rule.embed * target);

        if (rule.addCollider)
        {
            var col = go.AddComponent<MeshCollider>();
            col.sharedMesh = mesh; // non-convex: you can swim through arches/grottos
        }

        return Mathf.Max(b.size.x, b.size.z) * scale * 0.5f;
    }

    // Low-frequency world noise: 0 = skip here, 1 = field of this feature.
    float MaskFactor(float worldX, float worldZ, FeatureRule rule)
    {
        float nz = Mathf.PerlinNoise(worldX * rule.maskScale + 917f, worldZ * rule.maskScale + 917f);
        float t = Mathf.Clamp01((nz - rule.maskThreshold) / 0.25f);
        return t * t * (3f - 2f * t);
    }

    static bool SampleGround(Collider surface, float x, float z, float rayTop,
                             out Vector3 point, out Vector3 normal)
    {
        var ray = new Ray(new Vector3(x, rayTop, z), Vector3.down);
        if (surface.Raycast(ray, out RaycastHit hit, rayTop * 2f + 100f))
        {
            point = hit.point;
            normal = hit.normal;
            return true;
        }
        point = Vector3.zero;
        normal = Vector3.up;
        return false;
    }

    static int PoissonCount(float mean, System.Random rng)
    {
        int n = Mathf.FloorToInt(mean);
        if ((float)rng.NextDouble() < (mean - n)) n++;
        return n;
    }

    static float Rand11(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);

    static int HashChunk(int x, int z, int seed)
    {
        unchecked
        {
            int h = seed;
            h = h * 73856093 ^ x * 19349663 ^ z * 83492791;
            return h;
        }
    }
}
