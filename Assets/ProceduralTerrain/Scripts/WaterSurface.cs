using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WaterSurface : MonoBehaviour
{
    public Transform viewer;
    public float waterLevel = 8f;
    public float planeHalfSize = 50f;        // covers the camera far plane
    [Range(8, 160)] public int resolution = 96; // vertex density for the waves
    public Material waterMaterial;

    void Awake()
    {
        Rebuild();
    }

    // Rebuilds the water plane from the current waterLevel/resolution. EnvironmentLoader
    // sets those fields from the profile, then calls this. Safe to call at runtime.
    public void Rebuild()
    {
        GetComponent<MeshFilter>().sharedMesh = BuildPlaneMesh(resolution, planeHalfSize);

        MeshRenderer mr = GetComponent<MeshRenderer>();
        mr.sharedMaterial = waterMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        transform.position = new Vector3(0f, waterLevel, 0f);
    }

    void Update()
    {
        // AR fallback: follow the active camera when no viewer is wired in the Inspector.
        if (viewer == null && Camera.main != null)
            viewer = Camera.main.transform;

        if (viewer != null)
        {
            Vector3 vp = viewer.position;
            transform.position = new Vector3(vp.x, waterLevel, vp.z);
        }
    }

    static Mesh BuildPlaneMesh(int res, float halfSize)
    {
        int vertCount = (res + 1) * (res + 1);
        Vector3[] verts = new Vector3[vertCount];
        Vector2[] uvs   = new Vector2[vertCount];
        int[]     tris  = new int[res * res * 6];

        float step = (halfSize * 2f) / res;
        int vi = 0;
        for (int z = 0; z <= res; z++)
            for (int x = 0; x <= res; x++)
            {
                verts[vi] = new Vector3(-halfSize + x * step, 0f, -halfSize + z * step);
                uvs[vi]   = new Vector2((float)x / res, (float)z / res);
                vi++;
            }

        int ti = 0;
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
            {
                int a = z * (res + 1) + x;
                int b = a + 1;
                int c = a + (res + 1);
                int d = c + 1;
                tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
            }

        Mesh mesh = new Mesh { name = "WaterSurface", indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        return mesh;
    }
}