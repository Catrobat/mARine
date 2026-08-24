using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Samples.ARStarterAssets;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

namespace MarineAR.AISpawner.Runtime
{
    /// <summary>
    /// Drives placement by reusing the scene's existing XRI pipeline: the
    /// <see cref="ObjectSpawner"/> + <see cref="ARInteractorSpawnTrigger"/> pair that
    /// already powers AR_Spawn handles the plane raycast and tap detection; this manager
    /// only injects the runtime-loaded organism template and enforces the rule that a
    /// single placement operation exists at a time. Move / rotate / scale come from the
    /// XRGrabInteractable + ARTransformer already on the organism rig; delete reuses the
    /// focus-interactable pattern from ARTemplateMenuManager.
    /// </summary>
    public sealed class PlacementManager
    {
        readonly ObjectSpawner m_Spawner;
        readonly ARInteractorSpawnTrigger m_SpawnTrigger;
        readonly XRInteractionGroup m_InteractionGroup;

        bool m_PlacementActive;

        /// <summary>Raised with the spawned instance after the user places the organism.</summary>
        public event Action<GameObject> OrganismPlaced;

        public PlacementManager(
            ObjectSpawner spawner,
            ARInteractorSpawnTrigger spawnTrigger,
            XRInteractionGroup interactionGroup)
        {
            m_Spawner = spawner != null ? spawner : throw new ArgumentNullException(nameof(spawner));
            m_SpawnTrigger = spawnTrigger != null ? spawnTrigger : throw new ArgumentNullException(nameof(spawnTrigger));
            m_InteractionGroup = interactionGroup; // optional — delete degrades gracefully

            m_Spawner.objectSpawned += OnObjectSpawned;

            // Placement is armed on demand only.
            m_SpawnTrigger.enabled = false;
            m_Spawner.objectPrefabs = new List<GameObject>();
            m_Spawner.applyRandomAngleAtSpawn = false;
        }

        /// <summary>Whether a placement operation is currently waiting for a tap.</summary>
        public bool IsPlacementActive => m_PlacementActive;

        /// <summary>Whether an already-placed organism currently has manipulation focus.</summary>
        public bool HasFocusedOrganism => m_InteractionGroup != null && m_InteractionGroup.focusInteractable != null;

        /// <summary>
        /// Arms placement with the given template. Any previous pending placement is
        /// replaced — only one placement operation may exist at a time.
        /// </summary>
        public void BeginPlacement(GameObject template)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));

            m_Spawner.objectPrefabs.Clear();
            m_Spawner.objectPrefabs.Add(template);
            m_Spawner.spawnOptionIndex = 0;
            m_SpawnTrigger.enabled = true;
            m_PlacementActive = true;
        }

        /// <summary>Disarms placement without spawning.</summary>
        public void CancelPlacement()
        {
            m_SpawnTrigger.enabled = false;
            m_Spawner.objectPrefabs.Clear();
            m_PlacementActive = false;
        }

        /// <summary>
        /// Deletes the organism that currently has manipulation focus
        /// (same interaction as the AR_Spawn delete button).
        /// </summary>
        public void DeleteFocusedOrganism()
        {
            var focused = m_InteractionGroup?.focusInteractable;
            if (focused != null)
                UnityEngine.Object.Destroy(focused.transform.gameObject);
        }

        /// <summary>Unsubscribes from spawner events. Call on scene teardown.</summary>
        public void Dispose()
        {
            if (m_Spawner != null)
                m_Spawner.objectSpawned -= OnObjectSpawned;
        }

        void OnObjectSpawned(GameObject instance)
        {
            // The template is kept inactive so it never renders at the origin;
            // clones inherit that state and are activated here, after the spawner
            // has already positioned and oriented them on the plane.
            instance.SetActive(true);

            CancelPlacement();
            OrganismPlaced?.Invoke(instance);
        }
    }
}
