using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GLTFast;
using GLTFast.Logging;
using MarineAR.AISpawner.Models;
using UnityEngine;

namespace MarineAR.AISpawner.Runtime
{
    /// <summary>
    /// Turns a downloaded GLB file into a placeable AR template using the project's
    /// existing glTFast import pipeline (the same <see cref="GltfImport"/> flow used by
    /// <c>ModelFetcher</c>). The result is an inactive template GameObject built from the
    /// <c>OrganismRig</c> prefab — which carries the identical XRGrabInteractable +
    /// ARTransformer configuration as the existing AR_Spawn spawnables — with the model
    /// normalized to a consistent real-world size and a fitted collider.
    ///
    /// Resource lifetime: a <see cref="GltfImport"/> owns the meshes and textures it
    /// created, and spawned clones share those assets with the template. Imports are
    /// therefore retained and only disposed in <see cref="DisposeAll"/> on scene exit.
    /// </summary>
    public sealed class OrganismModelBuilder
    {
        /// <summary>Longest dimension of a freshly placed organism, in meters.</summary>
        const float k_TargetSize = 0.35f;

        readonly GameObject m_OrganismRigPrefab;
        readonly Transform m_TemplateParent;
        readonly List<GltfImport> m_RetainedImports = new List<GltfImport>();

        GameObject m_CurrentTemplate;
        string m_CurrentTemplateId;

        public OrganismModelBuilder(GameObject organismRigPrefab, Transform templateParent)
        {
            m_OrganismRigPrefab = organismRigPrefab != null
                ? organismRigPrefab
                : throw new ArgumentNullException(nameof(organismRigPrefab));
            m_TemplateParent = templateParent;
        }

        /// <summary>The template currently ready for placement, or null.</summary>
        public GameObject CurrentTemplate => m_CurrentTemplate;

        /// <summary>The organism id the current template was built from, or null.</summary>
        public string CurrentTemplateId => m_CurrentTemplateId;

        /// <summary>
        /// Loads the GLB at <paramref name="modelPath"/> and builds an inactive,
        /// spawn-ready template. Replaces (and destroys) any previous template.
        /// Throws when the file cannot be imported.
        /// </summary>
        public async Task<GameObject> BuildTemplateAsync(MarineOrganism organism, string modelPath)
        {
            if (organism == null)
                throw new ArgumentNullException(nameof(organism));
            if (string.IsNullOrEmpty(modelPath))
                throw new ArgumentException("Model path is empty.", nameof(modelPath));

            var gltf = new GltfImport(logger: new ConsoleLogger());

            bool loaded;
            try
            {
                loaded = await gltf.Load(new Uri(modelPath));
            }
            catch (Exception)
            {
                gltf.Dispose();
                throw;
            }

            if (!loaded)
            {
                gltf.Dispose();
                throw new InvalidOperationException($"glTFast could not load '{modelPath}'.");
            }

            GameObject rig = UnityEngine.Object.Instantiate(m_OrganismRigPrefab, m_TemplateParent);
            rig.name = $"Organism [{organism.id}]";
            rig.SetActive(false);

            Transform anchor = rig.transform.Find("ModelAnchor");
            if (anchor == null)
            {
                anchor = new GameObject("ModelAnchor").transform;
                anchor.SetParent(rig.transform, false);
            }

            bool instantiated;
            try
            {
                instantiated = await gltf.InstantiateMainSceneAsync(anchor);
            }
            catch (Exception)
            {
                UnityEngine.Object.Destroy(rig);
                gltf.Dispose();
                throw;
            }

            if (!instantiated)
            {
                UnityEngine.Object.Destroy(rig);
                gltf.Dispose();
                throw new InvalidOperationException($"glTFast could not instantiate '{organism.id}'.");
            }

            NormalizeAndFit(rig.transform, anchor);

            // The import now backs live meshes — retain it until scene teardown.
            m_RetainedImports.Add(gltf);
            ReplaceTemplate(rig, organism.id);
            return rig;
        }

        /// <summary>Destroys the current template (spawned clones are unaffected).</summary>
        public void ClearTemplate()
        {
            if (m_CurrentTemplate != null)
                UnityEngine.Object.Destroy(m_CurrentTemplate);

            m_CurrentTemplate = null;
            m_CurrentTemplateId = null;
        }

        /// <summary>
        /// Releases every retained glTFast import (destroying its meshes/textures).
        /// Call only on scene teardown, after all spawned organisms are gone.
        /// </summary>
        public void DisposeAll()
        {
            ClearTemplate();
            foreach (GltfImport import in m_RetainedImports)
                import?.Dispose();
            m_RetainedImports.Clear();
        }

        void ReplaceTemplate(GameObject newTemplate, string organismId)
        {
            ClearTemplate();
            m_CurrentTemplate = newTemplate;
            m_CurrentTemplateId = organismId;
        }

        /// <summary>
        /// Scales the model so its longest side equals <see cref="k_TargetSize"/>,
        /// re-centers it so it stands on the placement point, and fits the rig's
        /// BoxCollider (required by XRGrabInteractable) around it.
        /// </summary>
        static void NormalizeAndFit(Transform rig, Transform anchor)
        {
            Bounds bounds = CalculateLocalBounds(rig);
            if (bounds.size == Vector3.zero)
            {
                Debug.LogWarning("[AISpawner] Model has no renderable geometry; using default collider.");
                bounds = new Bounds(Vector3.zero, Vector3.one * 0.2f);
            }

            float maxDimension = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            float scale = maxDimension > Mathf.Epsilon ? k_TargetSize / maxDimension : 1f;
            anchor.localScale = Vector3.one * scale;

            // Move the anchor so the scaled bounds' bottom-center sits at the rig origin —
            // organisms then rest naturally on the detected plane.
            Vector3 scaledCenter = bounds.center * scale;
            Vector3 scaledSize = bounds.size * scale;
            anchor.localPosition = new Vector3(
                -scaledCenter.x,
                -(scaledCenter.y - scaledSize.y * 0.5f),
                -scaledCenter.z);

            BoxCollider collider = rig.GetComponent<BoxCollider>();
            if (collider == null)
                collider = rig.gameObject.AddComponent<BoxCollider>();

            collider.center = new Vector3(0f, scaledSize.y * 0.5f, 0f);
            collider.size = scaledSize;
        }

        /// <summary>
        /// Computes combined mesh bounds in rig-local space without relying on
        /// <c>Renderer.bounds</c>, which is unavailable while the rig is inactive.
        /// </summary>
        static Bounds CalculateLocalBounds(Transform rig)
        {
            Matrix4x4 worldToRig = rig.worldToLocalMatrix;
            bool hasBounds = false;
            Bounds combined = default;

            void Encapsulate(Mesh mesh, Transform meshTransform)
            {
                if (mesh == null)
                    return;

                Matrix4x4 toRig = worldToRig * meshTransform.localToWorldMatrix;
                Bounds b = mesh.bounds;

                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);

                    Vector3 rigSpace = toRig.MultiplyPoint3x4(corner);
                    if (!hasBounds)
                    {
                        combined = new Bounds(rigSpace, Vector3.zero);
                        hasBounds = true;
                    }
                    else
                    {
                        combined.Encapsulate(rigSpace);
                    }
                }
            }

            foreach (MeshFilter filter in rig.GetComponentsInChildren<MeshFilter>(true))
                Encapsulate(filter.sharedMesh, filter.transform);

            foreach (SkinnedMeshRenderer skinned in rig.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                Encapsulate(skinned.sharedMesh, skinned.transform);

            return hasBounds ? combined : new Bounds(Vector3.zero, Vector3.zero);
        }
    }
}
