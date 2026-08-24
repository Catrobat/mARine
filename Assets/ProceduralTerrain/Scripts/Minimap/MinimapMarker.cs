using UnityEngine;

// Add to anything you want shown on the minimap. Self-registers on enable and
// removes itself on disable/destroy, so streamed-in/out props clean up on their own.
public class MinimapMarker : MonoBehaviour
{
    [Tooltip("Blip colour on the minimap.")]
    public Color color = new Color(0.5f, 1f, 0.65f, 1f);

    [Tooltip("What this is, for the minimap legend. Usually the scatter rule's name.")]
    public string label;

    // Set colour and label together. AddComponent runs OnEnable before the caller
    // can assign fields, so declaring the legend there always saw a null label.
    public void Configure(Color blipColor, string blipLabel)
    {
        color = blipColor;
        label = blipLabel;
        MinimapRegistry.DeclareLegend(label, color);
    }

    void OnEnable()
    {
        MinimapRegistry.Add(this);
        // Covers markers configured before this enable, and prefab-authored ones.
        MinimapRegistry.DeclareLegend(label, color);
    }

    void OnDisable() => MinimapRegistry.Remove(this);
}
