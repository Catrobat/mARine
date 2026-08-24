using UnityEngine;

[ExecuteAlways]
public class MarineSnow : MonoBehaviour
{
    [Header("Amount")]
    [Range(0, 4000)] public int particleCount = 700;
    [Tooltip("Edge of the cube of particles that follows the camera. Keep at or below fadeEnd.")]
    public float boxSize = 12f;

    [Header("Appearance")]
    public Color tint = new Color(0.86f, 0.93f, 0.95f, 1f);
    [Range(0f, 1f)] public float opacity = 0.5f;
    public float sizeMin = 0.014f;
    public float sizeMax = 0.050f;

    [Header("Motion")]
    public float driftSpeed = 0.05f;
    public float sinkSpeed = 0.03f;

    [Header("Fade")]
    public float nearFade = 0.5f;
    public float farFade = 7f;

    [Header("Material")]
    public Shader snowShader;

    const int MaxParticles = 4000;

    Mesh _mesh;
    Material _material;
    MeshFilter _filter;
    MeshRenderer _renderer;
    int _builtCount = -1;

    void OnEnable()
    {
        EnsureComponents();
        Rebuild();
    }

    void OnValidate()
    {
        particleCount = Mathf.Clamp(particleCount, 0, MaxParticles);
        boxSize = Mathf.Max(1f, boxSize);
        if (sizeMax < sizeMin) sizeMax = sizeMin;
        if (isActiveAndEnabled) Rebuild();
    }

    void OnDisable()
    {
        if (_renderer != null) _renderer.enabled = false;
    }

    public void ApplySettings(int count, float box, Color colour, float alpha,
                              float minSize, float maxSize, float drift, float sink)
    {
        particleCount = Mathf.Clamp(count, 0, MaxParticles);
        boxSize   = Mathf.Max(1f, box);
        tint      = colour;
        opacity   = Mathf.Clamp01(alpha);
        sizeMin   = Mathf.Max(0.0005f, minSize);
        sizeMax   = Mathf.Max(sizeMin, maxSize);
        driftSpeed = drift;
        sinkSpeed  = sink;
        Rebuild();
    }

    void EnsureComponents()
    {
        _filter = GetComponent<MeshFilter>();
        if (_filter == null) _filter = gameObject.AddComponent<MeshFilter>();

        _renderer = GetComponent<MeshRenderer>();
        if (_renderer == null) _renderer = gameObject.AddComponent<MeshRenderer>();

        _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _renderer.receiveShadows = false;
        _renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        _renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    void Rebuild()
    {
        EnsureComponents();

        if (particleCount <= 0)
        {
            _renderer.enabled = false;
            return;
        }
        _renderer.enabled = true;

        if (_mesh == null || _builtCount != particleCount)
        {
            BuildMesh();
            _builtCount = particleCount;
        }

        Shader s = snowShader != null ? snowShader : Shader.Find("Custom/MarineSnow");
        if (s == null)
        {
            Debug.LogWarning("[MarineSnow] Shader 'Custom/MarineSnow' not found — snow disabled.", this);
            _renderer.enabled = false;
            return;
        }

        if (_material == null || _material.shader != s)
        {
            _material = new Material(s) { hideFlags = HideFlags.DontSave };
            _renderer.sharedMaterial = _material;
        }

        _material.SetColor("_Color", tint);
        _material.SetFloat("_BoxSize", boxSize);
        _material.SetFloat("_SizeMin", sizeMin);
        _material.SetFloat("_SizeMax", sizeMax);
        _material.SetFloat("_DriftSpeed", driftSpeed);
        _material.SetFloat("_SinkSpeed", sinkSpeed);
        _material.SetFloat("_Opacity", opacity);
        _material.SetFloat("_NearFade", nearFade);
        _material.SetFloat("_FarFade", farFade);
    }

    void BuildMesh()
    {
        int n = Mathf.Clamp(particleCount, 1, MaxParticles);

        var verts    = new Vector3[n * 4];
        var corners  = new Vector2[n * 4];
        var particle = new Vector2[n * 4];
        var tris     = new int[n * 6];

        for (int i = 0; i < n; i++)
        {
            int v = i * 4;
            int t = i * 6;
            float seed = i * 1.6180339f + 0.5f;

            corners[v + 0] = new Vector2(-1f, -1f);
            corners[v + 1] = new Vector2( 1f, -1f);
            corners[v + 2] = new Vector2( 1f,  1f);
            corners[v + 3] = new Vector2(-1f,  1f);

            for (int k = 0; k < 4; k++)
            {
                verts[v + k] = Vector3.zero;
                particle[v + k] = new Vector2(seed, 0f);
            }

            tris[t + 0] = v + 0; tris[t + 1] = v + 2; tris[t + 2] = v + 1;
            tris[t + 3] = v + 0; tris[t + 4] = v + 3; tris[t + 5] = v + 2;
        }

        if (_mesh == null)
        {
            _mesh = new Mesh { name = "MarineSnow", hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
        }
        _mesh.Clear();
        _mesh.indexFormat = n * 4 > 65000
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        _mesh.vertices = verts;
        _mesh.uv = corners;
        _mesh.uv2 = particle;
        _mesh.triangles = tris;
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 100000f);

        _filter.sharedMesh = _mesh;
    }
}
