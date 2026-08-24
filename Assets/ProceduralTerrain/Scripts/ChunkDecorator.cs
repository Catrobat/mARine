using UnityEngine;

// Base class for anything that decorates streamed terrain chunks with extra objects
// (prefab scatter, procedural rock formations, kelp, ...).
public abstract class ChunkDecorator : MonoBehaviour
{
    [Tooltip("Props spawn on chunks within this world distance of the viewer. " +
             "Keep ≤ the LOD0 distance so a collider exists to seat on.")]
    public float placementDistance = 16f;

    [Tooltip("Extra distance beyond placementDistance before a chunk's props are destroyed.")]
    public float placementHysteresis = 4f;

    // Builds this decorator's objects for one chunk.
    public abstract GameObject PopulateChunk(int chunkX, int chunkZ, Vector3 worldCentre,
                                             float worldHalfSize, Collider surface, Transform parent);
}
