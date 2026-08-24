using UnityEngine;
using System;
using System.Threading;
using System.Collections.Generic;
using CreateEnv;

// Generates heightmaps off-thread and dispatches results to the main thread.
// mapChunkSize 33 = 32 segments; valid LODs are 0, 1, 2, 4 (not 3).
public class MapGenerator : MonoBehaviour
{
    public enum DrawMode { NoiseMap, ColourMap, Mesh }

    // ── Inspector ────────────────────────────────────────────────────────────
    [Header("Preview")]
    public DrawMode drawMode;

    [Header("Noise")]
    public Noise.NormalizeMode normalizeMode;
    public const int mapChunkSize = 33; // 32 segments = 8 m per chunk at Scale 0.25

    [Range(0, 4)] public int editorPreviewLOD;
    public float noiseScale;
    public int octaves;
    [Range(0, 1)] public float persistance;
    public float lacunarity;
    public int seed;
    public Vector2 offset;

    [Header("Terrain Style (biomes)")]
    [Tooltip("Classic = original dunes. Canyons = ridged + terraced cliff terrain.")]
    public TerrainShaper.Settings terrainStyle = new TerrainShaper.Settings();

    [Header("Mesh")]
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    [Header("Material")]
    public Material terrainMaterial; // applied to every chunk at runtime

    [Header("Regions (editor preview only)")]
    public TerrainType[] regions;

    public bool autoUpdate;

    // ── Threading ────────────────────────────────────────────────────────────
    readonly Queue<MapThreadInfo<MapData>> _mapDataQueue = new Queue<MapThreadInfo<MapData>>();
    readonly Queue<MapThreadInfo<MeshData>> _meshDataQueue = new Queue<MapThreadInfo<MeshData>>();

    // ── Editor preview ───────────────────────────────────────────────────────
    public void DrawMapInEditor()
    {
        MapData mapData = GenerateMapData(Vector2.zero);
        MapDisplay display = FindFirstObjectByType<MapDisplay>();

        switch (drawMode)
        {
            case DrawMode.NoiseMap:
                display.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.HeightMap));
                break;

            case DrawMode.ColourMap:
                display.DrawTexture(TextureGenerator.TextureFromColourMap(
                    BuildColourMap(mapData.HeightMap), mapChunkSize, mapChunkSize));
                break;

            case DrawMode.Mesh:
                display.DrawMesh(
                    MeshGenerator.GenerateTerrainMesh(
                        mapData.HeightMap, meshHeightMultiplier, meshHeightCurve, editorPreviewLOD),
                    TextureGenerator.TextureFromColourMap(
                        BuildColourMap(mapData.HeightMap), mapChunkSize, mapChunkSize));
                break;
        }
    }

    // ── Create-Env: apply a data profile (called once by EnvironmentLoader) ──
    // Writes every terrain-shape field from the profile.
    public void ApplyProfile(EnvironmentProfile p)
    {
        if (p == null) return;

        normalizeMode        = Noise.NormalizeMode.Global;
        noiseScale           = p.noiseScale;
        octaves              = p.octaves;
        persistance          = p.persistance;
        lacunarity           = p.lacunarity;
        seed                 = p.seed;
        offset               = new Vector2(p.offsetX, p.offsetY);
        meshHeightMultiplier = p.meshHeightMultiplier;
        meshHeightCurve      = BiomeStyles.BuildHeightCurve(p.styleIndex);
        terrainStyle         = BiomeStyles.BuildShaper(p);
    }

    // ── Public threading API ─────────────────────────────────────────────────
    public void RequestMapData(Vector2 centre, Action<MapData> callback)
    {
        // ThreadPool avoids one OS thread per chunk.
        ThreadPool.QueueUserWorkItem(_ => MapDataThread(centre, callback));
    }

    public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
    {
        ThreadPool.QueueUserWorkItem(_ => MeshDataThread(mapData, lod, callback));
    }

    // ── Thread workers ───────────────────────────────────────────────────────
    void MapDataThread(Vector2 centre, Action<MapData> callback)
    {
        MapData mapData = GenerateMapData(centre);
        lock (_mapDataQueue)
            _mapDataQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
    }

    void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
    {
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(
            mapData.HeightMap, meshHeightMultiplier, meshHeightCurve, lod);
        lock (_meshDataQueue)
            _meshDataQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
    }

    // ── Main-thread dispatch ─────────────────────────────────────────────────
    void Update()
    {
        DrainQueue(_mapDataQueue);
        DrainQueue(_meshDataQueue);
    }

    static void DrainQueue<T>(Queue<MapThreadInfo<T>> queue)
    {
        lock (queue)
        {
            while (queue.Count > 0)
            {
                MapThreadInfo<T> info = queue.Dequeue();
                info.Callback(info.Parameter);
            }
        }
    }

    // ── Data generation ──────────────────────────────────────────────────────
    MapData GenerateMapData(Vector2 centre)
    {
        // +2: a 1-sample border on each side, used only for seamless normals.
        float[,] noiseMap = (terrainStyle != null && terrainStyle.style != TerrainShaper.Style.Classic)
            ? TerrainShaper.Generate(
                mapChunkSize + 2, mapChunkSize + 2,
                seed, noiseScale, octaves, persistance, lacunarity,
                centre + offset, terrainStyle)
            : Noise.GenerateNoiseMap(
                mapChunkSize + 2, mapChunkSize + 2,
                seed, noiseScale, octaves, persistance, lacunarity,
                centre + offset, normalizeMode);

        return new MapData(noiseMap);
    }

    // Editor-preview colour map only (+1 indexing skips the normals border).
    Color[] BuildColourMap(float[,] heightMap)
    {
        Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
        for (int y = 0; y < mapChunkSize; y++)
        {
            for (int x = 0; x < mapChunkSize; x++)
            {
                float h = heightMap[x + 1, y + 1];
                for (int i = 0; i < regions.Length; i++)
                {
                    if (h >= regions[i].height)
                        colourMap[y * mapChunkSize + x] = regions[i].colour;
                    else
                        break;
                }
            }
        }
        return colourMap;
    }

    // ── Validation ───────────────────────────────────────────────────────────
    void OnValidate()
    {
        if (lacunarity < 1) lacunarity = 1;
        if (octaves < 0) octaves = 0;
    }

    // ── Thread info container ─────────────────────────────────────────────────
    struct MapThreadInfo<T>
    {
        public readonly Action<T> Callback;
        public readonly T Parameter;

        public MapThreadInfo(Action<T> callback, T parameter)
        {
            Callback = callback;
            Parameter = parameter;
        }
    }
}