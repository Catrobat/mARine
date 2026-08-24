using System.Collections.Generic;

// Host-agnostic list of things that should appear on the minimap.
public static class MinimapRegistry
{
    static readonly List<MinimapMarker> _all = new List<MinimapMarker>();
    public static IReadOnlyList<MinimapMarker> All => _all;

    public static void Add(MinimapMarker m)
    {
        if (m != null && !_all.Contains(m)) _all.Add(m);
    }

    public static void Remove(MinimapMarker m) => _all.Remove(m);

    // Legend: one entry per kind of thing, not per instance.
    public struct LegendEntry { public string label; public UnityEngine.Color color; }

    static readonly List<LegendEntry> _legend = new List<LegendEntry>();
    public static IReadOnlyList<LegendEntry> Legend => _legend;

    // Bumped whenever the set changes, so the radar can rebuild only when it must.
    public static int LegendVersion { get; private set; }

    public static void DeclareLegend(string label, UnityEngine.Color color)
    {
        if (string.IsNullOrEmpty(label)) return;
        for (int i = 0; i < _legend.Count; i++)
            if (_legend[i].label == label) return;
        _legend.Add(new LegendEntry { label = label, color = color });
        LegendVersion++;
    }

    public static void ClearLegend()
    {
        _legend.Clear();
        LegendVersion++;
    }
}
