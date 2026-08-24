using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // A development harness for looking at the reef without AR.
    //
    // The real feature installs itself only in FreeExploreEndless, and only once the
    // learner has chosen an environment with the ecosystem switched on. That is the
    // right behaviour for the app, but it makes the reef awkward to inspect while
    // building it: pressing Play on the AR scene needs a tracked plane and a tap
    // before anything happens.
    //
    // Drop this on a GameObject in any ordinary scene and press Play. It builds the
    // same LivingReefController the real bootstrap builds, with the same simulation,
    // the same meshes and the same panel — just without the AR placement step.
    //
    // It does nothing unless you add it deliberately, so it cannot affect the app.
    public class LivingReefPreview : MonoBehaviour
    {
        [Header("Starting conditions")]
        public bool enableEcosystem = true;

        [Tooltip("0 = Few, 1 = Balanced, 2 = Many")]
        [Range(0, 2)] public int startingLife = 1;

        [Range(18f, 32f)] public float temperatureC = 24f;
        [Range(7.6f, 8.3f)] public float acidityPh = 8.1f;

        [Tooltip("0 = Paused, 1 = Normal (2s per day), 2 = Fast (0.25s per day)")]
        [Range(0, 2)] public int speed = 2;

        [Header("Which organisms are present")]
        [Tooltip("Untick one to watch the food web reorganise around the gap. " +
                 "Unticking the tiger shark is the trophic-cascade demonstration.")]
        public bool calcareousGreenAlga = true;
        public bool fanAlga = true;
        public bool lesserStarletCoral = true;
        public bool parrotfish = true;
        public bool longSpinedSeaUrchin = true;
        public bool keyholeLimpet = true;
        public bool brownSpinyLobster = true;
        public bool commonOctopus = true;
        public bool tigerShark = true;

        [Header("Repeatability")]
        [Tooltip("The same seed replays identically, so a problem can be reproduced.")]
        public int seed = 12345;

        LivingReefController _reef;

        void Start()
        {
            if (!enableEcosystem) return;

            // Guard against both this and the real bootstrap installing a reef.
            if (FindFirstObjectByType<LivingReefController>() != null)
            {
                Debug.LogWarning("[LivingReefPreview] A Living Reef is already running; " +
                                 "the preview will not add a second one.", this);
                return;
            }

            var settings = new EcosystemSettings
            {
                enabled = true,
                startingLife = startingLife,
                temperatureC = temperatureC,
                acidityPh = acidityPh,
                speed = speed,
            };
            settings.Clamp();

            settings.present[SpeciesLibrary.Halimeda]   = calcareousGreenAlga;
            settings.present[SpeciesLibrary.Padina]     = fanAlga;
            settings.present[SpeciesLibrary.Coral]      = lesserStarletCoral;
            settings.present[SpeciesLibrary.Parrotfish] = parrotfish;
            settings.present[SpeciesLibrary.Urchin]     = longSpinedSeaUrchin;
            settings.present[SpeciesLibrary.Limpet]     = keyholeLimpet;
            settings.present[SpeciesLibrary.Lobster]    = brownSpinyLobster;
            settings.present[SpeciesLibrary.Octopus]    = commonOctopus;
            settings.present[SpeciesLibrary.TigerShark] = tigerShark;

            // Pinned to this object rather than to the camera, so a fixed preview
            // camera looks across the reef instead of standing in the middle of it.
            _reef = LivingReefController.Install(settings, seed, followViewer: false);
            _reef.transform.position = transform.position;
            Debug.Log("[LivingReefPreview] Reef running. Open the 'Reef' tab on the right " +
                      "of the screen for the panel.");
        }

        // Convenience for testing from the inspector's context menu while playing.
        [ContextMenu("Remove the tiger shark")]
        void RemoveShark()
        {
            if (_reef == null) { Debug.LogWarning("Not playing."); return; }
            _reef.SetSpeciesPresent(SpeciesLibrary.TigerShark, false);
            Debug.Log("[LivingReefPreview] Tiger shark removed. Watch the urchins climb.");
        }

        [ContextMenu("Put every species back")]
        void RestoreAll()
        {
            if (_reef == null) { Debug.LogWarning("Not playing."); return; }
            _reef.RestoreAllSpecies();
        }
    }
}
