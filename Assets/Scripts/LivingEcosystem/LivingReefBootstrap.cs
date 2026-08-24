using UnityEngine;
using UnityEngine.SceneManagement;

namespace CreateEnv.Ecosystem
{
    // Installs the Living Reef into FreeExploreEndless, and nowhere else.
    //
    // This runs itself. There is no component to add to a scene, no prefab to edit
    // and no Addressable group to touch — which matters, because the target scene is
    // an Addressable and every widget added by hand would be a change to its
    // serialized data. The whole feature is therefore additive: if it is switched
    // off, or if this file were deleted, the scene behaves exactly as it does today.
    //
    // Scope is deliberately narrow. Only FreeExploreEndless is touched. FreeExplore
    // is a separate scene with its own logic and is explicitly left alone.
    public static class LivingReefBootstrap
    {
        // The one scene the ecosystem may install itself into.
        //
        // Matched case-insensitively on purpose: the asset on disk is
        // "freeExploreEndless.unity" while every reference in code spells it
        // "FreeExploreEndless". Scene.name comes from the file, so an ordinary ==
        // never matches and the reef silently never installs.
        public const string TargetScene = "FreeExploreEndless";

        static LivingReefController _active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Hook()
        {
            // Statics survive between play sessions when domain reload is turned off,
            // so this could otherwise start holding a destroyed controller.
            _active = null;

            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            // Cover the case where the target scene is already the one running (for
            // example, pressing Play on it directly in the Editor).
            var active = SceneManager.GetActiveScene();
            if (IsTarget(active)) TryInstall();
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // A single-mode load has already destroyed the previous reef.
            if (mode == LoadSceneMode.Single) _active = null;

            if (!IsTarget(scene)) return;
            TryInstall();
        }

        static bool IsTarget(Scene scene) =>
            scene.IsValid() &&
            string.Equals(scene.name, TargetScene, System.StringComparison.OrdinalIgnoreCase);

        static void TryInstall()
        {
            if (_active != null) return;

            var settings = ResolveSettings();
            if (settings == null || !settings.enabled) return;

            // Guard against a second install if the scene is reloaded quickly.
            if (Object.FindFirstObjectByType<LivingReefController>() != null) return;

            int seed = ResolveSeed();
            _active = LivingReefController.Install(settings, seed, true, ResolveEnvironmentId());
            Debug.Log($"[LivingReef] Installed in {TargetScene} with seed {seed}. " +
                      "Open the 'Reef' tab on the right-hand edge of the screen.");
        }

        // The ecosystem rides inside the environment profile the learner chose, so it
        // arrives through machinery that already exists. When no profile is selected
        // (a built-in environment, or entering the scene directly), the feature stays
        // off and nothing changes.
        //
        // Every way of declining says why. A feature that silently does nothing is
        // indistinguishable from a broken one, and this one has several perfectly
        // legitimate reasons not to start.
        static EcosystemSettings ResolveSettings()
        {
            var profile = EnvironmentSession.Selected;
            if (profile == null)
            {
                Debug.Log("[LivingReef] No environment profile selected, so the ecosystem is not " +
                          "running. Built-in environments do not carry one — make an environment in " +
                          "Create Environment and play that instead.");
                return null;
            }

            if (profile.ecosystem == null)
            {
                Debug.Log($"[LivingReef] '{profile.displayName}' was saved before the Living Ecosystem " +
                          "existed. Open it in Create Environment, switch the ecosystem on, and save.");
                return null;
            }

            if (!profile.ecosystem.enabled)
            {
                Debug.Log($"[LivingReef] The Living Ecosystem is switched off for " +
                          $"'{profile.displayName}'. Open it in Create Environment, scroll to " +
                          "'Living Ecosystem' and set 'Enable living ecosystem' to On.");
                return null;
            }

            var settings = profile.ecosystem.Clone();
            settings.Clamp();
            return settings;
        }

        // Which saved reef to pick up. Each environment keeps its own, so a learner
        // with several worlds finds each of them as they left it.
        static string ResolveEnvironmentId()
        {
            var profile = EnvironmentSession.Selected;
            return profile != null && !string.IsNullOrEmpty(profile.id) ? profile.id : null;
        }

        // Seeded from the environment, so the same environment replays identically
        // and bugs are reproducible (Design Document 3.2).
        static int ResolveSeed()
        {
            var profile = EnvironmentSession.Selected;
            return profile != null ? profile.seed : 0;
        }
    }
}
