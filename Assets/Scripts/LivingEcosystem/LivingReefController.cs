using CreateEnv.Ecosystem.UI;
using UnityEngine;

namespace CreateEnv.Ecosystem
{
    // Owns the running ecosystem: the simulation, the clock, the health tracker, the
    // reasoning rules, the rewind buffer, the renderer and the interface.
    //
    // One component, created at runtime by LivingReefBootstrap. Nothing else in the
    // project holds a reference to it, so if the feature is off, none of this exists
    // and Shallow Sand behaves exactly as it does today.
    [DefaultExecutionOrder(200)]
    public class LivingReefController : MonoBehaviour
    {
        public EcosystemSettings Settings { get; private set; }
        public EcosystemSimulation Sim { get; private set; }
        public EcosystemHealth Health { get; private set; }
        public ReasonEngine Reasons { get; private set; }
        public EcosystemClock Clock { get; private set; }

        // Step 2: the octopuses are individuals with genomes rather than a number.
        public Genetics.OctopusPopulation Octopuses { get; private set; }

        // Step 3: what the reef has been through, and what it remembers.
        public Memory.ReefChronicle Chronicle { get; private set; }

        RewindBuffer _rewind;
        PopulationRenderer _renderer;
        EcosystemPanelUI _panel;
        BarrenPromptUI _barrenPrompt;

        public bool CanRewind => _rewind != null && _rewind.HasAny;

        // followViewer: true in AR, where the reef is the patch of seafloor around
        // the learner. False for a fixed-camera preview, where it is pinned in front.
        public static LivingReefController Install(EcosystemSettings settings, int seed,
                                                   bool followViewer = true,
                                                   string environmentId = null)
        {
            var go = new GameObject("Living Reef");
            var reef = go.AddComponent<LivingReefController>();
            reef._followViewer = followViewer;
            reef.EnvironmentId = environmentId;
            reef.Initialise(settings, seed);
            return reef;
        }

        // Which saved reef this is. Null means nothing is written down.
        public string EnvironmentId { get; private set; }

        // What happened while the learner was away, for the Welcome Back card.
        public Memory.TimeAway.Result Resumed { get; private set; }
        public int DayOnArrival { get; private set; }
        public bool HasSomethingToReport { get; private set; }

        // The two octopuses currently chosen in the breeding tool, so the renderer
        // can light them and the learner can find them in the water.
        [System.NonSerialized] public int chosenFemaleId = -1;
        [System.NonSerialized] public int chosenMaleId = -1;

        bool _followViewer = true;
        bool _waitingForPlacement;
        float _placementPoll;

        void Initialise(EcosystemSettings settings, int seed)
        {
            Settings = settings != null ? settings.Clone() : new EcosystemSettings();
            Settings.Clamp();

            Sim = new EcosystemSimulation(Settings, seed);
            Health = new EcosystemHealth();
            Reasons = new ReasonEngine();
            Clock = new EcosystemClock { speed = Settings.speed };
            _rewind = new RewindBuffer();

            // Octopuses become individuals. The pool still speaks to the food web on
            // their behalf, so the balanced web from Step 1 is unchanged.
            Chronicle = new Memory.ReefChronicle();

            Octopuses = new Genetics.OctopusPopulation();
            Octopuses.journal = Chronicle.journal;
            Sim.agentManaged[SpeciesLibrary.Octopus] = true;
            if (Sim.IsPresent(SpeciesLibrary.Octopus))
                Octopuses.Found(Mathf.RoundToInt(Sim.pools[SpeciesLibrary.Octopus].count), seed);

            Health.Reset(Sim);
            Reasons.Reset();
            Reasons.Observe(Sim);
            Reasons.Evaluate(Sim, Health);
            Chronicle.Begin(Sim, Health, Octopuses);
            _rewind.Capture(Sim);

            LoadAndCatchUp();

            _renderer = gameObject.AddComponent<PopulationRenderer>();
            _renderer.followViewer = _followViewer;
            // The AR path waits for the learner to place the environment; a preview
            // scene already has its floor, so it can draw straight away.
            _renderer.requireGround = _followViewer;
            _renderer.octopuses = Octopuses;
            _renderer.reef = this;
            _renderer.Bind(Sim, transform);

            _panel = EcosystemPanelUI.Create(this);
            _barrenPrompt = GetComponentInChildren<BarrenPromptUI>(true);

            // In AR the whole interface stays hidden until the environment is down.
            // A preview scene has its floor already, so it starts live.
            _waitingForPlacement = _followViewer;
            if (_waitingForPlacement && _panel != null)
                _panel.gameObject.SetActive(false);
        }

        void Update()
        {
            // Nothing exists until the learner has actually placed the environment.
            // Detecting a scanned plane is not enough: the reef has no seafloor to
            // live on until the terrain prefab is instantiated by the placement tap,
            // and a panel offering to edit an ecosystem that is not there yet reads
            // as broken. The clock is held too, so day one is the day they arrive.
            if (_waitingForPlacement)
            {
                _placementPoll -= Time.unscaledDeltaTime;
                if (_placementPoll > 0f) return;
                _placementPoll = 0.25f;

                if (FindFirstObjectByType<EndlessTerrain>() == null) return;

                _waitingForPlacement = false;
                if (_panel != null) _panel.gameObject.SetActive(true);
                Debug.Log("[LivingReef] Environment placed — the reef is now live.");
                return;
            }

            int days = Clock.Advance(Time.deltaTime);
            for (int i = 0; i < days; i++)
            {
                Sim.Tick();
                // The pool has stated its demand and suffered its predation; the
                // agent layer now decides which individuals that happened to.
                Octopuses.Tick(Sim, SpeciesLibrary.Octopus);
                Reasons.Observe(Sim);
                Health.Evaluate(Sim);
                Chronicle.Observe(Sim, Health, Octopuses);
                _rewind.MaybeCapture(Sim);
            }

            if (days > 0)
            {
                Reasons.Evaluate(Sim, Health);
                if (Health.stage <= CollapseStage.Imbalance && _barrenPrompt != null)
                    _barrenPrompt.NotifyRecovered();
            }
        }

        // ── Memory (Design Document 8) ───────────────────────────────────────

        // Picks up where the reef left off, then runs the days that passed while the
        // learner was elsewhere.
        void LoadAndCatchUp()
        {
            if (string.IsNullOrEmpty(EnvironmentId)) return;

            var save = Memory.ReefSaveFile.Read(EnvironmentId);
            if (save == null) return;

            save.ApplyTo(Settings, Sim, Octopuses, Chronicle);
            Clock.speed = Settings.speed;

            Health.Reset(Sim);
            Reasons.Reset();
            Reasons.Observe(Sim);

            DayOnArrival = Sim.day;
            Resumed = Memory.TimeAway.Measure(save.HoursAway(System.DateTime.UtcNow));

            // The days that passed in the ocean while the app was closed. Capped at a
            // fortnight however long they were gone, so this is at most fourteen
            // ordinary ticks — the same arithmetic the live clock runs, just without
            // anyone watching.
            for (int i = 0; i < Resumed.daysToRun; i++)
            {
                Sim.Tick();
                Octopuses.Tick(Sim, SpeciesLibrary.Octopus);
                Reasons.Observe(Sim);
                Health.Evaluate(Sim);
                Chronicle.Observe(Sim, Health, Octopuses);
            }

            Reasons.Evaluate(Sim, Health);
            HasSomethingToReport = Resumed.worthReporting;

            Debug.Log($"[LivingReef] Resumed '{EnvironmentId}' at day {DayOnArrival}; " +
                      $"{Resumed.hoursAway:0.0} hours away ran {Resumed.daysToRun} days" +
                      (Resumed.wasCapped ? " (capped)" : "") + ".");
        }

        // Written on pause, on backgrounding and on quit — the three ways a session
        // ends on a phone.
        public void Save()
        {
            if (string.IsNullOrEmpty(EnvironmentId)) return;
            if (Sim == null) return;

            Memory.ReefSaveFile.Write(
                Memory.ReefSave.From(EnvironmentId, Settings, Sim, Octopuses, Chronicle));
        }

        void OnApplicationPause(bool paused) { if (paused) Save(); }
        void OnApplicationFocus(bool focused) { if (!focused) Save(); }
        void OnApplicationQuit() => Save();
        void OnDestroy() => Save();

        public void ClearReport() => HasSomethingToReport = false;

        // ── Live controls (Design Document 6.1) ──────────────────────────────
        public void SetTemperature(float celsius)
        {
            Settings.temperatureC = EcosystemBounds.Temperature.Clamp(celsius);
            Sim.temperatureC = Settings.temperatureC;
        }

        public void SetAcidity(float ph)
        {
            Settings.acidityPh = EcosystemBounds.Acidity.Clamp(ph);
            Sim.acidityPh = Settings.acidityPh;
        }

        public void SetSpeed(int speed)
        {
            Settings.speed = Mathf.Clamp(speed, 0, EcosystemSettings.SpeedOptions.Length - 1);
            Clock.speed = Settings.speed;
        }

        public void SetSpeciesPresent(int species, bool present)
        {
            if (Sim.IsPresent(species) == present) return;
            Sim.SetPresent(species, present);
            if (Settings.present != null && species < Settings.present.Length)
                Settings.present[species] = present;
            if (_panel != null) _panel.RefreshOrganismRows();
        }

        // ── Barren-state options ─────────────────────────────────────────────
        public void RestoreAllSpecies()
        {
            for (int i = 0; i < SpeciesLibrary.Count; i++)
                SetSpeciesPresent(i, true);
        }

        public void Rewind()
        {
            var snapshot = _rewind.Restore(Sim);
            if (snapshot == null) return;

            Health.Reset(Sim);
            Reasons.Reset();
            Reasons.Observe(Sim);
            Reasons.Evaluate(Sim, Health);
            if (_panel != null) _panel.RefreshOrganismRows();
        }

        public void RestartEcosystem()
        {
            Sim.Reset(Settings);
            Health.Reset(Sim);
            Reasons.Reset();
            Reasons.Observe(Sim);
            Reasons.Evaluate(Sim, Health);
            _rewind.Reset();
            _rewind.Capture(Sim);
            Clock.Reset();
            if (_panel != null) _panel.RefreshOrganismRows();
        }
    }
}
