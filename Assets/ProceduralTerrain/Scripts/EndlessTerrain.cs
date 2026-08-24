using UnityEngine;
using System.Collections.Generic;
using System.Threading;

public class EndlessTerrain : MonoBehaviour
{
    
    const float Scale                  = 0.25f;
    const float ViewerMoveThreshold    = 0.5f;
    const float SqrViewerMoveThreshold = ViewerMoveThreshold * ViewerMoveThreshold;
    const float CullDistanceMultiplier = 1.5f;


    public LODInfo[]  detailLevels;
    public Transform  viewer;
    [Tooltip("On (default): stream immediately on Start, like the original scenes. " +
             "Off: wait for EnvironmentLoader.Initialize() so a profile is applied first.")]
    public bool autoStart = true;
    [Tooltip("Optional: scatters prefab props (coral/seaweed) on near chunks. Leave empty to disable.")]
    public TerrainDetailScatter detailScatter;
    [Tooltip("Optional: additional chunk decorators (rock formations, kelp, ...).")]
    public ChunkDecorator[] extraDecorators;

    public static float   MaxViewDst;
    public static Vector2 ViewerPosition;

    static MapGenerator     _mapGenerator;
    static ChunkDecorator[] _decorators;

    Vector2 _viewerPositionOld;
    int     _chunkSize;
    int     _chunksVisibleInViewDst;
    float   _sqrCullDistance;

    readonly Dictionary<Vector2, TerrainChunk> _chunkDictionary        = new Dictionary<Vector2, TerrainChunk>();
    static   List<TerrainChunk>                _chunksVisibleLastUpdate = new List<TerrainChunk>();
    readonly List<Vector2>                     _chunksToDestroy         = new List<Vector2>();

    bool _initialized;

    void Start()
    {
        if (autoStart) Initialize();
    }

    // Idempotent streaming bootstrap.
    public void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        _mapGenerator  = FindFirstObjectByType<MapGenerator>();
        _decorators    = CollectDecorators();
        MaxViewDst    = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        _chunkSize    = MapGenerator.mapChunkSize - 1;
        _chunksVisibleInViewDst = Mathf.RoundToInt(MaxViewDst / _chunkSize);

        ClampDecoratorDistancesToLod0();

        float cullDst    = MaxViewDst * CullDistanceMultiplier;
        _sqrCullDistance = cullDst * cullDst;

        UpdateVisibleChunks();
    }

    // Props are seated against the LOD0 collider, so decorators must stop before the
    // distance where a coarser LOD takes over or they float.
    void ClampDecoratorDistancesToLod0()
    {
        if (_decorators == null || detailLevels == null || detailLevels.Length == 0) return;

        float lod0World = detailLevels[0].visibleDstThreshold * Scale;
        float safe = lod0World * 0.9f;

        for (int i = 0; i < _decorators.Length; i++)
        {
            ChunkDecorator d = _decorators[i];
            if (d == null) continue;

            float despawn = d.placementDistance + Mathf.Max(0.5f, d.placementHysteresis);
            if (despawn <= safe) continue;

            float hyst = Mathf.Min(Mathf.Max(0.5f, d.placementHysteresis), safe * 0.25f);
            d.placementHysteresis = hyst;
            d.placementDistance   = Mathf.Max(1f, safe - hyst);

            Debug.Log($"[EndlessTerrain] '{d.GetType().Name}' props would have outlived LOD0 " +
                      $"(despawn {despawn:0.#} m vs LOD0 {lod0World:0.#} m) and floated over the " +
                      $"coarser mesh. Trimmed to spawn {d.placementDistance:0.#} m / " +
                      $"despawn {d.placementDistance + hyst:0.#} m.", d);
        }
    }

    // View-distance presets (index 0 Near, 1 Medium, 2 Far). The last threshold is
    // the world-reach used by EnvironmentBounds invariant I-2 (fog hides the edge).
    public static LODInfo[] BuildViewDistance(int index)
    {
        switch (Mathf.Clamp(index, 0, 2))
        {
            case 0: return new[] { // Near — reach 96 (24 m)
                new LODInfo { lod = 0, visibleDstThreshold = 48f },
                new LODInfo { lod = 1, visibleDstThreshold = 72f },
                new LODInfo { lod = 2, visibleDstThreshold = 96f } };
            case 2: return new[] { // Far — reach 256 (64 m)
                new LODInfo { lod = 0, visibleDstThreshold = 80f },
                new LODInfo { lod = 1, visibleDstThreshold = 160f },
                new LODInfo { lod = 2, visibleDstThreshold = 256f } };
            default: return new[] { // Medium — reach 160 (40 m)
                new LODInfo { lod = 0, visibleDstThreshold = 64f },
                new LODInfo { lod = 1, visibleDstThreshold = 112f },
                new LODInfo { lod = 2, visibleDstThreshold = 160f } };
        }
    }

    void Update()
    {
        // AR fallback: when no viewer is wired in the Inspector (e.g. spawned as a
        // prefab into the Marine AR scene), stream around the active camera instead.
        if (viewer == null)
        {
            if (Camera.main == null) return;
            viewer = Camera.main.transform;
        }

        ViewerPosition = new Vector2(viewer.position.x, viewer.position.z) / Scale;

        if ((_viewerPositionOld - ViewerPosition).sqrMagnitude > SqrViewerMoveThreshold)
        {
            _viewerPositionOld = ViewerPosition;
            UpdateVisibleChunks();
            CullDistantChunks();
        }
    }

    void UpdateVisibleChunks()
    {
        foreach (var chunk in _chunksVisibleLastUpdate)
            chunk.SetVisible(false);
        _chunksVisibleLastUpdate.Clear();

        int coordX = Mathf.RoundToInt(ViewerPosition.x / _chunkSize);
        int coordY = Mathf.RoundToInt(ViewerPosition.y / _chunkSize);

        for (int yOff = -_chunksVisibleInViewDst; yOff <= _chunksVisibleInViewDst; yOff++)
        for (int xOff = -_chunksVisibleInViewDst; xOff <= _chunksVisibleInViewDst; xOff++)
        {
            var coord = new Vector2(coordX + xOff, coordY + yOff);
            if (_chunkDictionary.TryGetValue(coord, out TerrainChunk existing))
                existing.UpdateTerrainChunk();
            else
                _chunkDictionary.Add(coord, new TerrainChunk(
                    coord, _chunkSize, detailLevels, transform, _mapGenerator.terrainMaterial));
        }
    }

    // Merges the legacy single-scatter slot and the extras into one list.
    ChunkDecorator[] CollectDecorators()
    {
        // Safety net: if the Inspector reference was dropped (e.g.
        if (detailScatter == null) detailScatter = FindFirstObjectByType<TerrainDetailScatter>();

        var list = new List<ChunkDecorator>();
        if (detailScatter != null) list.Add(detailScatter);
        if (extraDecorators != null)
            foreach (var d in extraDecorators)
                if (d != null && !list.Contains(d)) list.Add(d);

        // Fallback: catch any other decorators (feature scatter, etc.) left unwired.
        foreach (var d in FindObjectsByType<ChunkDecorator>(FindObjectsSortMode.None))
            if (d != null && !list.Contains(d)) list.Add(d);

        return list.ToArray();
    }

    void CullDistantChunks()
    {
        _chunksToDestroy.Clear();
        foreach (var pair in _chunkDictionary)
            if (pair.Value.SqrDistanceFromViewer() > _sqrCullDistance)
                _chunksToDestroy.Add(pair.Key);

        foreach (var key in _chunksToDestroy)
        {
            _chunkDictionary[key].DestroyChunk();
            _chunkDictionary.Remove(key);
        }
    }

    // One streamed terrain tile: mesh, LODs, collider and its scattered props.
    public class TerrainChunk
    {
        readonly GameObject   _meshObject;
        readonly Vector2      _coord;
        readonly Vector2      _position;
        readonly Bounds       _bounds;
        readonly MeshRenderer _meshRenderer;
        readonly MeshFilter   _meshFilter;
        readonly Transform    _parent;
        readonly LODInfo[]    _detailLevels;
        readonly LODMesh[]    _lodMeshes;

        MeshCollider _meshCollider;
        GameObject[] _detailRoots;   // per-decorator prop roots; spawned/destroyed by distance
        bool[]       _detailsBuilt;  // true while the matching root exists for this approach
        MapData      _mapData;
        bool         _mapDataReceived;
        bool         _isDestroyed; // guard against late thread callbacks after Destroy()
        int          _previousLODIndex = -1;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels,
                            Transform parent, Material material)
        {
            _coord        = coord;
            _detailLevels = detailLevels;
            _parent       = parent;
            _position     = coord * size;
            _bounds       = new Bounds(_position, Vector2.one * size);

            _meshObject              = new GameObject("Terrain Chunk");
            _meshRenderer            = _meshObject.AddComponent<MeshRenderer>();
            _meshFilter              = _meshObject.AddComponent<MeshFilter>();
            _meshRenderer.material   = material;

            // AR fix: keep X/Z in world space (terrain streams around the camera) but lift the
            // seabed to the parent/anchor floor height instead of pinning it to world Y = 0.
            _meshObject.transform.position   = new Vector3(_position.x * Scale,
                                                           parent != null ? parent.position.y : 0f,
                                                           _position.y * Scale);
            _meshObject.transform.parent     = parent;
            _meshObject.transform.localScale = Vector3.one * Scale;
            SetVisible(false);

            _lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
                _lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);

            _mapGenerator.RequestMapData(_position, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            _mapData         = mapData;
            _mapDataReceived = true;
            UpdateTerrainChunk();
        }

        public void UpdateTerrainChunk()
        {
            if (_isDestroyed)     return;
            if (!_mapDataReceived) return;

            float viewerDst = Mathf.Sqrt(_bounds.SqrDistance(ViewerPosition));
            bool  visible   = viewerDst <= MaxViewDst;

            
            SetVisible(visible);

            if (visible)
            {
                int lodIndex = 0;
                for (int i = 0; i < _detailLevels.Length - 1; i++)
                {
                    if (viewerDst > _detailLevels[i].visibleDstThreshold) lodIndex = i + 1;
                    else break;
                }

                if (lodIndex != _previousLODIndex)
                {
                    LODMesh lodMesh = _lodMeshes[lodIndex];
                    if (lodMesh.HasMesh)
                    {
                        _previousLODIndex = lodIndex;
                        _meshFilter.mesh  = lodMesh.Mesh;

                        if (lodIndex == 0)
                        {
                            if (_meshCollider == null)
                                _meshCollider = _meshObject.AddComponent<MeshCollider>();
                            _meshCollider.sharedMesh = lodMesh.Mesh;
                            _meshCollider.enabled    = true;
                        }
                        else if (_meshCollider != null)
                            _meshCollider.enabled = false;
                    }
                    else if (!lodMesh.HasRequestedMesh)
                        lodMesh.RequestMesh(_mapData);
                }

                UpdateDetails(viewerDst);
                _chunksVisibleLastUpdate.Add(this);
            }
        }

        // Build props when the viewer is near (and the LOD0 collider exists),
        // destroy them once it leaves. Deterministic, so reefs rebuild on return.
        void UpdateDetails(float viewerDstScaled)
        {
            if (_decorators == null || _decorators.Length == 0) return;

            if (_detailRoots == null)
            {
                _detailRoots  = new GameObject[_decorators.Length];
                _detailsBuilt = new bool[_decorators.Length];
            }

            float worldDst = viewerDstScaled * Scale;

            // Props may only exist while the chunk is drawing the same LOD0 mesh they were seated
            // against;
            bool atLod0 = _previousLODIndex == 0 && _meshCollider != null && _meshCollider.enabled;

            for (int i = 0; i < _decorators.Length; i++)
            {
                ChunkDecorator decorator = _decorators[i];
                if (decorator == null) continue;

                float spawnDst   = decorator.placementDistance;
                float despawnDst = spawnDst + Mathf.Max(0.5f, decorator.placementHysteresis);

                if (!_detailsBuilt[i])
                {
                    if (atLod0 && worldDst <= spawnDst)
                    {
                        _detailsBuilt[i] = true; // latch even if the chunk is bare
                        Vector3 worldCentre = new Vector3(_position.x * Scale,
                                                          _parent != null ? _parent.position.y : 0f,
                                                          _position.y * Scale);
                        float   worldHalf   = (_bounds.size.x * Scale) * 0.5f;
                        _detailRoots[i] = decorator.PopulateChunk(
                            Mathf.RoundToInt(_coord.x), Mathf.RoundToInt(_coord.y),
                            worldCentre, worldHalf, _meshCollider, _parent);
                    }
                }
                else if (!atLod0 || worldDst > despawnDst)
                {
                    if (_detailRoots[i] != null) { Object.Destroy(_detailRoots[i]); _detailRoots[i] = null; }
                    _detailsBuilt[i] = false;
                }
            }
        }

        public float SqrDistanceFromViewer() => _bounds.SqrDistance(ViewerPosition);

        public void DestroyChunk()
        {
            _isDestroyed = true;
            if (_detailRoots != null)
                foreach (var root in _detailRoots)
                    if (root != null) Object.Destroy(root);
            if (_meshObject != null) Object.Destroy(_meshObject);
        }

        public void SetVisible(bool v)
        {
            if (_isDestroyed || _meshObject == null) return;
            _meshObject.SetActive(v);
            if (_detailRoots != null)
                foreach (var root in _detailRoots)
                    if (root != null) root.SetActive(v);
        }

        public bool IsVisible() => _meshObject.activeSelf;
    }

    // Requests and holds the mesh for one LOD level.
    class LODMesh
    {
        public Mesh Mesh             { get; private set; }
        public bool HasRequestedMesh { get; private set; }
        public bool HasMesh          { get; private set; }

        readonly int           _lod;
        readonly System.Action _updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            _lod            = lod;
            _updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            Mesh    = meshData.CreateMesh();
            HasMesh = true;
            _updateCallback();
        }

        public void RequestMesh(MapData mapData)
        {
            HasRequestedMesh = true;
            _mapGenerator.RequestMeshData(mapData, _lod, OnMeshDataReceived);
        }
    }

    [System.Serializable]
    public struct LODInfo
    {
        public int   lod;
        public float visibleDstThreshold;
    }
}