using UnityEngine;

// Ties the lifetime of a runtime-built Mesh to the GameObject that renders it.
[DisallowMultipleComponent]
public class GeneratedMeshOwner : MonoBehaviour
{
    [SerializeField] Mesh _mesh;

    public static void Attach(GameObject go, Mesh mesh)
    {
        if (go == null || mesh == null) return;
        go.AddComponent<GeneratedMeshOwner>()._mesh = mesh;
    }

    void OnDestroy()
    {
        if (_mesh == null) return;
        if (Application.isPlaying) Destroy(_mesh);
        else DestroyImmediate(_mesh);
        _mesh = null;
    }
}
