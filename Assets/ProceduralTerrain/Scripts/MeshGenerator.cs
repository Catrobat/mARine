using UnityEngine;

// Builds terrain mesh geometry from a heightmap that carries a 1-sample border
// (used only for seamless normals, not geometry).
public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(
        float[,] borderedHeightMap,
        float heightMultiplier,
        AnimationCurve _heightCurve,
        int levelOfDetail)
    {
        // AnimationCurve is not thread-safe; copy keys before sampling off-thread.
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);

        int borderedSize = borderedHeightMap.GetLength(0);
        int meshSize = borderedSize - 2;

        // Final heights for the whole bordered map; border samples feed normals.
        float[,] heights = new float[borderedSize, borderedSize];
        for (int y = 0; y < borderedSize; y++)
            for (int x = 0; x < borderedSize; x++)
                heights[x, y] = heightCurve.Evaluate(borderedHeightMap[x, y]) * heightMultiplier;

        float topLeftX = (meshSize - 1) / -2f;
        float topLeftZ = (meshSize - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < meshSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < meshSize; x += meshSimplificationIncrement)
            {
                int bx = x + 1;
                int by = y + 1;

                meshData.Vertices[vertexIndex] = new Vector3(
                    topLeftX + x,
                    heights[bx, by],
                    topLeftZ - y);

                // Central-difference normal (map's y axis points along -Z).
                float dX = heights[bx + 1, by] - heights[bx - 1, by];
                float dY = heights[bx, by + 1] - heights[bx, by - 1];
                Vector3 normal = new Vector3(-dX * 0.5f, 1f, dY * 0.5f);
                meshData.Normals[vertexIndex] = normal.normalized;

                // UV over [0,1] per resolution so tiling is stable across LODs.
                meshData.UVs[vertexIndex] = new Vector2(
                    x / (float)(meshSize - 1),
                    y / (float)(meshSize - 1));

                if (x < meshSize - 1 && y < meshSize - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        // Taller terrain (canyon biomes) opens taller LOD seams on cliff edges,
        // so the hidden curtain grows with the height multiplier.
        float skirtDepth = Mathf.Max(1.5f, heightMultiplier * 0.25f);
        AddSkirt(meshData, verticesPerLine, skirtDepth);

        return meshData;
    }

    // Drops a hidden vertical curtain from the chunk edge so cracks/LOD gaps
    // show terrain behind them, not the background.
    static void AddSkirt(MeshData meshData, int verticesPerLine, float skirtDepth)
    {
        int loopLength = 4 * (verticesPerLine - 1);
        int skirtStart = verticesPerLine * verticesPerLine;

        for (int k = 0; k < loopLength; k++)
        {
            int border = BorderLoopIndex(k, verticesPerLine);
            int skirt = skirtStart + k;

            Vector3 v = meshData.Vertices[border];
            meshData.Vertices[skirt] = new Vector3(v.x, v.y - skirtDepth, v.z);
            meshData.Normals[skirt] = meshData.Normals[border];
            meshData.UVs[skirt] = meshData.UVs[border];

            int kNext = (k + 1) % loopLength;
            int borderNext = BorderLoopIndex(kNext, verticesPerLine);
            int skirtNext = skirtStart + kNext;

            // Outward-facing winding (border loop runs clockwise seen from above).
            meshData.AddTriangle(border, skirt, borderNext);
            meshData.AddTriangle(borderNext, skirt, skirtNext);
        }
    }

    // Grid index of the k-th border vertex, walking the edge clockwise.
    static int BorderLoopIndex(int k, int verticesPerLine)
    {
        int side = k / (verticesPerLine - 1);
        int j = k % (verticesPerLine - 1);
        int last = verticesPerLine - 1;

        int gx, gy;
        switch (side)
        {
            case 0: gx = j; gy = 0; break;          // north (+Z edge)
            case 1: gx = last; gy = j; break;        // east  (+X edge)
            case 2: gx = last - j; gy = last; break; // south (-Z edge)
            default: gx = 0; gy = last - j; break;   // west  (-X edge)
        }
        return gy * verticesPerLine + gx;
    }
}

// Off-thread mesh container; call CreateMesh() on the main thread.
public class MeshData
{
    public readonly Vector3[] Vertices;
    public readonly int[] Triangles;
    public readonly Vector2[] UVs;
    public readonly Vector3[] Normals;

    int _triangleIndex;

    public MeshData(int meshWidth, int meshHeight)
    {
        // Grid vertices plus one skirt vertex per border vertex.
        int gridVerts = meshWidth * meshHeight;
        int skirtVerts = 4 * (meshWidth - 1);

        Vertices = new Vector3[gridVerts + skirtVerts];
        UVs = new Vector2[gridVerts + skirtVerts];
        Normals = new Vector3[gridVerts + skirtVerts];
        Triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6 + skirtVerts * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        Triangles[_triangleIndex] = a;
        Triangles[_triangleIndex + 1] = b;
        Triangles[_triangleIndex + 2] = c;
        _triangleIndex += 3;
    }

    // Main thread only.
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = Vertices,
            triangles = Triangles,
            uv = UVs,
            normals = Normals
        };
        return mesh;
    }
}
