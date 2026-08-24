using System;
using System.Collections.Generic;
using System.IO;
using CreateEnv.Ecosystem.Genetics;
using CreateEnv.Ecosystem.Memory;
using UnityEditor;
using UnityEngine;

namespace CreateEnv.Ecosystem.EditorTools
{
    // A bench for the parts of Milestone 3 that are otherwise only reachable on a
    // phone after a day of waiting.
    //
    // Two of the three things added in this milestone are hard to test honestly. The
    // Welcome Back card needs an absence measured in hours; the report needs a reef
    // with a long history and a family tree several generations deep. Both are here
    // as buttons: backdate a save, or run a reef forward and produce the PDF.
    //
    // Editor-only, and it touches nothing at runtime.
    public class ReefMemoryWindow : EditorWindow
    {
        Vector2 _scroll;
        string _status = "";
        int _simulatedDays = 400;
        int _seed = 4242;
        float _temperature = 27.5f;
        int _hoursAway = 6;

        [MenuItem("Tools/Living Ecosystem/Saved Reefs and Reports")]
        public static void Open()
        {
            var window = GetWindow<ReefMemoryWindow>();
            window.titleContent = new GUIContent("Reef Memory");
            window.minSize = new Vector2(460f, 420f);
        }

        void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField("Saved reefs", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(ReefSaveFile.Folder, EditorStyles.miniLabel);
            EditorGUILayout.Space(4f);

            DrawSaves();

            EditorGUILayout.Space(14f);
            EditorGUILayout.LabelField("Sample report", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Runs a reef headlessly and writes the PDF it would produce. No scene, no " +
                "device and no AR session required — this is the fastest way to see what " +
                "the report actually looks like.", MessageType.None);

            _simulatedDays = EditorGUILayout.IntSlider("Days to simulate", _simulatedDays, 1, 1200);
            _seed = EditorGUILayout.IntField("Seed", _seed);
            _temperature = EditorGUILayout.Slider("Temperature (°C)", _temperature, 20f, 32f);

            if (GUILayout.Button("Build a sample report", GUILayout.Height(26f)))
                BuildSample();

            EditorGUILayout.Space(14f);
            EditorGUILayout.LabelField("From the running game", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("Report on the reef that is playing now", GUILayout.Height(26f)))
                    ReportOnLiveReef();

                if (GUILayout.Button("Save the reef that is playing now"))
                    SaveLiveReef();
            }

            if (!string.IsNullOrEmpty(_status))
            {
                EditorGUILayout.Space(10f);
                EditorGUILayout.HelpBox(_status, MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        // ── Saved reefs ──────────────────────────────────────────────────────

        void DrawSaves()
        {
            if (!Directory.Exists(ReefSaveFile.Folder))
            {
                EditorGUILayout.HelpBox("No reef has been saved yet. Play FreeExploreEndless with " +
                                        "an environment that has the ecosystem switched on, then " +
                                        "leave play mode.", MessageType.None);
                return;
            }

            var files = Directory.GetFiles(ReefSaveFile.Folder, "*.json");
            if (files.Length == 0)
            {
                EditorGUILayout.HelpBox("The folder exists but holds no saves.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField("An hour away is a day in the ocean, up to " +
                                       TimeAway.MaximumDays + " days.", EditorStyles.miniLabel);
            EditorGUILayout.Space(2f);

            foreach (var file in files)
            {
                string id = Path.GetFileNameWithoutExtension(file);
                var save = ReefSaveFile.Read(id);

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (save == null)
                    {
                        EditorGUILayout.LabelField(id, "unreadable, or written by another build");
                        if (GUILayout.Button("Delete")) Delete(id);
                        continue;
                    }

                    double hours = save.HoursAway(DateTime.UtcNow);
                    var away = TimeAway.Measure(hours);

                    EditorGUILayout.LabelField(id, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(
                        $"day {save.day}   ·   {new FileInfo(file).Length} bytes of " +
                        $"{ReefSaveFile.BudgetBytes}   ·   generation {save.highestGeneration}");
                    EditorGUILayout.LabelField(
                        $"closed {hours:0.0} h ago, which would run {away.daysToRun} days" +
                        (away.wasCapped ? " (capped)" : "") +
                        (away.worthReporting ? "" : " — too short to show the card"),
                        EditorStyles.miniLabel);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _hoursAway = EditorGUILayout.IntField("Pretend hours away", _hoursAway);
                        if (GUILayout.Button("Backdate", GUILayout.Width(90f)))
                            Backdate(id, save, _hoursAway);
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Show the file"))
                            EditorUtility.RevealInFinder(file);
                        if (GUILayout.Button("Delete"))
                            Delete(id);
                    }
                }
            }
        }

        // Moves the save's clock backwards so the next play session behaves as though
        // the learner had been away that long. The alternative is to wait.
        void Backdate(string id, ReefSave save, int hours)
        {
            save.closedAtUtc = DateTime.UtcNow.AddHours(-Math.Max(0, hours)).ToString("O");
            ReefSaveFile.Write(save);
            _status = $"'{id}' now reads as {hours} hours old. Enter play mode to see the " +
                      "Welcome Back card.";
        }

        void Delete(string id)
        {
            if (!EditorUtility.DisplayDialog("Delete this reef?",
                    $"'{id}' will start again from scratch the next time it is played.",
                    "Delete", "Keep")) return;

            ReefSaveFile.Delete(id);
            _status = $"Deleted '{id}'.";
        }

        // ── Sample report ────────────────────────────────────────────────────

        void BuildSample()
        {
            try
            {
                var source = RunAReef(_simulatedDays, _seed, _temperature);
                WriteAndReveal(EcosystemReport.Build(source),
                               "sample-reef-report.pdf",
                               $"Simulated {_simulatedDays} days at {_temperature:0.#} °C.");
            }
            catch (Exception e)
            {
                _status = "The sample could not be built: " + e;
                Debug.LogException(e);
            }
        }

        // The same loop LivingReefController runs, minus everything to do with drawing.
        static EcosystemReport.Source RunAReef(int days, int seed, float temperature)
        {
            var settings = new EcosystemSettings
            {
                enabled = true,
                temperatureC = temperature,
                predictionIndex = 0,
            };
            settings.Clamp();

            var sim = new EcosystemSimulation(settings, seed);
            sim.temperatureC = temperature;
            sim.agentManaged[SpeciesLibrary.Octopus] = true;

            var health = new EcosystemHealth();
            health.Reset(sim);

            var reasons = new ReasonEngine();
            reasons.Reset();

            var octopuses = new OctopusPopulation();
            octopuses.Found(5, seed);

            var chronicle = new ReefChronicle();
            octopuses.journal = chronicle.journal;
            chronicle.Begin(sim, health, octopuses);

            for (int d = 0; d < days; d++)
            {
                sim.Tick();
                octopuses.Tick(sim, SpeciesLibrary.Octopus);
                health.Evaluate(sim);
                reasons.Observe(sim);
                reasons.Evaluate(sim, health);
                chronicle.Observe(sim, health, octopuses);
            }

            return new EcosystemReport.Source
            {
                environmentName = "Sample Reef",
                sim = sim,
                health = health,
                settings = settings,
                octopuses = octopuses,
                chronicle = chronicle,
                reasons = reasons.Active,
            };
        }

        // ── The live reef ────────────────────────────────────────────────────

        void ReportOnLiveReef()
        {
            var reef = FindObjectOfType<LivingReefController>();
            if (reef == null)
            {
                _status = "No Living Reef is running. It installs itself only in " +
                          LivingReefBootstrap.TargetScene + ", and only when the chosen " +
                          "environment has the ecosystem switched on.";
                return;
            }

            var profile = EnvironmentSession.Selected;
            string name = profile != null ? profile.displayName : "My Reef";
            var result = ReportShare.ShareReport(reef, name);

            _status = result.ok
                ? "Written to " + result.path
                : "Could not write the report: " + result.failure;
        }

        void SaveLiveReef()
        {
            var reef = FindObjectOfType<LivingReefController>();
            if (reef == null) { _status = "No Living Reef is running."; return; }

            reef.Save();
            _status = "Saved. It will appear in the list above.";
        }

        void WriteAndReveal(byte[] pdf, string fileName, string note)
        {
            string folder = Path.Combine(Application.persistentDataPath, "reports");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, fileName);

            File.WriteAllBytes(path, pdf);
            EditorUtility.RevealInFinder(path);

            _status = note + $"\n{pdf.Length:N0} bytes written to\n{path}";
        }

        // FindObjectOfType is deprecated in Unity 6, but the replacement is only worth
        // reaching for in code that runs every frame. This runs when a button is clicked.
        static T FindObjectOfType<T>() where T : UnityEngine.Object =>
            UnityEngine.Object.FindFirstObjectByType<T>();
    }
}
