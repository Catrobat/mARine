using UnityEngine;

/// <summary>
/// Runtime map data passed between threads.
public struct MapData
{
    public readonly float[,] HeightMap;

    public MapData(float[,] heightMap)
    {
        HeightMap = heightMap;
    }
}

/// <summary>
/// Defines a named terrain region with a height threshold and representative colour.
[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}