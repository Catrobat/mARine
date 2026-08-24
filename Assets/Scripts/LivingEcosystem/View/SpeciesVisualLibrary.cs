using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // Where a species' model and material come from.
    //
    //   1. A prefab at Resources/LivingEcosystem/Species/<id>, if one exists.
    //   2. Otherwise a species-shaped mesh from ReefMeshLibrary.
    //
    // Meshes AND materials are built once per species and shared by every instance.
    // That sharing is not tidiness, it is the frame rate: giving each drawn organism
    // its own Material meant fifty-odd unique materials, fifty draw calls and no
    // batching whatsoever. Anything that varies per individual — an octopus's
    // camouflage, a highlight — goes through a MaterialPropertyBlock instead, which
    // leaves the batching intact.
    public static class SpeciesVisualLibrary
    {
        const string PrefabPath = "LivingEcosystem/Species/";

        static readonly GameObject[] _prefabs = new GameObject[SpeciesLibrary.Count];
        static readonly Mesh[] _meshes = new Mesh[SpeciesLibrary.Count];
        static readonly Material[] _materials = new Material[SpeciesLibrary.Count];
        static readonly bool[] _resolved = new bool[SpeciesLibrary.Count];
        static Material _template;

        // Shader property ids, looked up once. Resolving them by string on every
        // assignment is a surprising amount of work when it happens per organism.
        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int ColorId = Shader.PropertyToID("_Color");
        public static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        public static GameObject PrefabFor(int species)
        {
            Resolve(species);
            return _prefabs[species];
        }

        public static Mesh MeshFor(int species)
        {
            Resolve(species);
            return _meshes[species];
        }

        // One material per species, shared by every one of its instances.
        public static Material MaterialFor(int species)
        {
            if (species < 0 || species >= _materials.Length) return Template;
            if (_materials[species] != null) return _materials[species];

            var def = SpeciesLibrary.Get(species);
            var mat = new Material(Template) { name = "LivingEcosystem/" + (def != null ? def.id : species.ToString()) };
            if (def != null)
            {
                if (mat.HasProperty(BaseColorId)) mat.SetColor(BaseColorId, def.tint);
                if (mat.HasProperty(ColorId)) mat.SetColor(ColorId, def.tint);
            }
            _materials[species] = mat;
            return mat;
        }

        public static void Invalidate()
        {
            for (int i = 0; i < _resolved.Length; i++)
            {
                _resolved[i] = false;
                _prefabs[i] = null;
                _meshes[i] = null;
                _materials[i] = null;
            }
        }

        static void Resolve(int species)
        {
            if (species < 0 || species >= SpeciesLibrary.Count) return;
            if (_resolved[species]) return;
            _resolved[species] = true;

            var def = SpeciesLibrary.Get(species);
            if (def == null) return;

            var prefab = Resources.Load<GameObject>(PrefabPath + def.id);
            if (prefab != null)
            {
                _prefabs[species] = prefab;
                return;
            }

            // Seeded per species so each looks the same between sessions, and built
            // to that species' own diagnostic silhouette.
            _meshes[species] = ReefMeshLibrary.Build(species, 4100 + species * 37);
        }

        // Simple Lit rather than Unlit: these meshes carry their meaning in their
        // shape, and an unlit shader flattens every one of them into a solid
        // silhouette where a limpet and an urchin look identical.
        public static Material Template
        {
            get
            {
                if (_template != null) return _template;

                var shader = Shader.Find("Universal Render Pipeline/Simple Lit")
                          ?? Shader.Find("Universal Render Pipeline/Lit")
                          ?? Shader.Find("Universal Render Pipeline/Unlit")
                          ?? Shader.Find("Diffuse");

                _template = new Material(shader) { enableInstancing = true };
                _template.name = "LivingEcosystem/Template";

                // Rough, non-metallic, no specular highlight: wet organic surfaces.
                if (_template.HasProperty("_Smoothness")) _template.SetFloat("_Smoothness", 0.12f);
                if (_template.HasProperty("_Metallic")) _template.SetFloat("_Metallic", 0f);
                if (_template.HasProperty("_SpecularHighlights")) _template.SetFloat("_SpecularHighlights", 0f);

                return _template;
            }
        }
    }
}
