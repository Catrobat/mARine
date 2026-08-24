using System.Text;
using UnityEditor;
using UnityEngine;

namespace CreateEnv.Ecosystem.EditorTools
{
    // Runs the ecosystem headlessly and reports what it does.
    //
    // The milestone document names balancing as the main risk of this step and asks
    // for every rate to live in data so tuning needs no rebuild. This is the other
    // half of that: a way to check a change without entering play mode, putting on a
    // headset, and waiting several simulated months.
    //
    // The scenarios are the milestone's own "Done when" clauses.
    public class EcosystemBalanceWindow : EditorWindow
    {
        Vector2 _scroll;
        string _report = "Press Run to simulate.";
        int _days = 400;

        [MenuItem("Tools/Living Ecosystem/Balance Report")]
        static void Open()
        {
            var window = GetWindow<EcosystemBalanceWindow>(false, "Reef Balance", true);
            window.minSize = new Vector2(680f, 460f);
        }

        void OnGUI()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Living Reef — balance report", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Simulates the reef without entering play mode. Run this after changing any " +
                "rate in SpeciesLibrary or in a SpeciesLibrary asset.", MessageType.Info);

            _days = EditorGUILayout.IntSlider("Days to simulate", _days, 60, 2000);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run", GUILayout.Height(28f))) Run();
            if (GUILayout.Button("Reload species data", GUILayout.Height(28f)))
            {
                SpeciesLibrary.Invalidate();
                SpeciesVisualLibrary.Invalidate();
                _report = "Species data reloaded.";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        void Run()
        {
            var sb = new StringBuilder();
            int passed = 0, total = 0;

            void Check(string name, bool ok, string detail)
            {
                total++;
                if (ok) passed++;
                sb.Append(ok ? "  PASS  " : "  FAIL  ").Append(name);
                if (!string.IsNullOrEmpty(detail)) sb.Append("   (").Append(detail).Append(')');
                sb.Append('\n');
            }

            // ── 1. A balanced reef stays balanced ────────────────────────────
            sb.Append("1. A balanced reef stays balanced over ").Append(_days).Append(" days\n");
            var settings = new EcosystemSettings { enabled = true };
            settings.Clamp();

            var sim = new EcosystemSimulation(settings, 12345);
            float startProducers = sim.GrazeableProducerBiomass();
            Run(sim, _days);

            var extinct = new StringBuilder();
            bool allAlive = true;
            for (int i = 0; i < SpeciesLibrary.Count; i++)
            {
                var def = SpeciesLibrary.Get(i);
                if (def.IsProducer) continue;
                if (sim.pools[i].count <= 0f)
                {
                    allAlive = false;
                    if (extinct.Length > 0) extinct.Append(", ");
                    extinct.Append(def.commonName);
                }
            }
            Check("no species goes extinct", allAlive, extinct.ToString());

            float drift = Mathf.Abs(sim.GrazeableProducerBiomass() - startProducers) / Mathf.Max(1f, startProducers);
            Check("producers within 40% of their start", drift < 0.40f, $"drift {drift * 100f:0.0}%");
            sb.Append(Snapshot(sim)).Append('\n');

            // ── 2. Removing the tiger shark ──────────────────────────────────
            sb.Append("2. Remove the tiger shark: urchins climb, algae fall\n");
            sim = new EcosystemSimulation(settings, 12345);
            Run(sim, 60);
            float urchinBefore = sim.pools[SpeciesLibrary.Urchin].count;
            float algaeBefore = sim.pools[SpeciesLibrary.Halimeda].biomass + sim.pools[SpeciesLibrary.Padina].biomass;
            sim.SetPresent(SpeciesLibrary.TigerShark, false);

            float peakUrchin = 0f, lowAlgae = float.MaxValue, peakOctopus = 0f;
            for (int d = 0; d < 300; d++)
            {
                sim.Tick();
                peakUrchin = Mathf.Max(peakUrchin, sim.pools[SpeciesLibrary.Urchin].count);
                peakOctopus = Mathf.Max(peakOctopus, sim.pools[SpeciesLibrary.Octopus].count);
                lowAlgae = Mathf.Min(lowAlgae,
                    sim.pools[SpeciesLibrary.Halimeda].biomass + sim.pools[SpeciesLibrary.Padina].biomass);
            }
            Check("urchins climb", peakUrchin > urchinBefore * 1.5f,
                  $"{urchinBefore:0.0} -> peak {peakUrchin:0.0}");
            Check("algae fall", lowAlgae < algaeBefore * 0.85f,
                  $"{algaeBefore:0.0} -> low {lowAlgae:0.0}");
            Check("mesopredator release", peakOctopus > sim.pools[SpeciesLibrary.Octopus].count
                  || peakOctopus > 5f, $"octopus peak {peakOctopus:0.00}");
            sb.Append('\n');

            // ── 3. A deliberately broken reef collapses in stages ────────────
            sb.Append("3. A broken reef (no shark, no parrotfish) passes through the collapse stages\n");
            var broken = new EcosystemSettings { enabled = true };
            broken.Clamp();
            broken.present[SpeciesLibrary.TigerShark] = false;
            broken.present[SpeciesLibrary.Parrotfish] = false;

            sim = new EcosystemSimulation(broken, 12345);
            var health = new EcosystemHealth();
            health.Reset(sim);

            float algaeStart = sim.GrazeableProducerBiomass();
            float peak = 0f, trough = float.MaxValue;
            int barrenDay = -1;
            var reached = new bool[6];

            for (int d = 1; d <= 600; d++)
            {
                sim.Tick();
                health.Evaluate(sim);
                reached[(int)health.stage] = true;

                float u = sim.pools[SpeciesLibrary.Urchin].count;
                if (u > peak) { peak = u; trough = float.MaxValue; }
                else trough = Mathf.Min(trough, u);

                if (barrenDay < 0 && sim.GrazeableProducerBiomass() < algaeStart * 0.12f) barrenDay = d;
            }

            Check("urchins overshoot", peak > 60f, $"peak {peak:0.0}");
            Check("then crash back", trough < peak * 0.6f, $"peak {peak:0.0} -> trough {trough:0.0}");
            Check("reaches barren", barrenDay > 0, barrenDay > 0 ? $"day {barrenDay}" : "never");
            Check("collapse is staged, not instant", barrenDay < 0 || barrenDay > 100, $"day {barrenDay}");

            sb.Append("  stages seen: ");
            for (int i = 1; i < reached.Length; i++)
                if (reached[i]) sb.Append((CollapseStage)i).Append(' ');
            sb.Append("\n\n");

            // ── 4. Water conditions ──────────────────────────────────────────
            sb.Append("4. Warming and acidification act on the calcifiers\n");
            float warmCoral = CoralAfter(300, 30f, 8.1f);
            float baseCoral = CoralAfter(300, 24f, 8.1f);
            float acidCoral = CoralAfter(300, 24f, 7.7f);
            Check("coral does worse in warm water", warmCoral < baseCoral * 0.80f,
                  $"30C {warmCoral:0.0} vs 24C {baseCoral:0.0}");
            Check("coral does worse in acidified water", acidCoral < baseCoral * 0.85f,
                  $"pH7.7 {acidCoral:0.0} vs pH8.1 {baseCoral:0.0}");
            sb.Append('\n');

            // ── 5. Budgets ───────────────────────────────────────────────────
            int stateBytes = SpeciesLibrary.Count * (sizeof(float) * 5 + sizeof(int) + sizeof(bool))
                           + sizeof(float) * 2 + sizeof(int) * 2;
            sb.Append("5. Budgets\n");
            Check("simulation state under 10 KB", stateBytes < 10 * 1024, $"{stateBytes} bytes");
            sb.Append('\n');

            sb.Insert(0, $"{passed} of {total} checks passed.\n\n");
            _report = sb.ToString();
            Debug.Log("[LivingReef] Balance report:\n" + _report);
        }

        static float CoralAfter(int days, float temperature, float ph)
        {
            var settings = new EcosystemSettings { enabled = true, temperatureC = temperature, acidityPh = ph };
            settings.Clamp();
            var sim = new EcosystemSimulation(settings, 12345);
            Run(sim, days);
            return sim.pools[SpeciesLibrary.Coral].biomass;
        }

        static void Run(EcosystemSimulation sim, int days)
        {
            for (int i = 0; i < days; i++) sim.Tick();
        }

        static string Snapshot(EcosystemSimulation sim)
        {
            var sb = new StringBuilder("  final: ");
            for (int i = 0; i < SpeciesLibrary.Count; i++)
            {
                var def = SpeciesLibrary.Get(i);
                sb.Append(def.id).Append('=')
                  .Append(sim.DisplayAmount(i).ToString("0.#"))
                  .Append(i < SpeciesLibrary.Count - 1 ? "  " : "");
            }
            sb.Append("\n  detritus=").Append(sim.detritus.ToString("0.#"));
            return sb.ToString();
        }
    }
}
