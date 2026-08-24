using UnityEngine;

[CreateAssetMenu(menuName = "MarineAR/Sandbox Settings")]
public class SandboxSettings : ScriptableObject
{
    [Header("Layer Count")]
    [Range(3, 5)]
    public int layerCount = 3;

    [Header("Layer Spacing")]
    public float layerSpacing = 1.5f; // Vertical gap between each layer
    public float bottomLayerY = 0f;   // Y position of the bottom most layer

    [Header("Sandbox Bounds")]
    public float sandboxWidth = 5f;
    public float sandboxDepth = 5f;

    [Header("Grid")]
    public int gridSizeX = 5;
    public int gridSizeY = 5;

    [Header("Layer Visuals")]
    public float activeLayerAlpha = 1f;
    public float layer1BelowAlpha = 0.4f;
    public float layer2BelowAlpha = 0.15f;

    // Auto calculate height for any layer index
    // Layer 0 = top = highest Y
    // Last layer = bottom = bottomLayerY
    public float GetLayerHeight(int index, int totalLayers)
    {
        // Top layer gets highest Y, bottom layer gets bottomLayerY
        float topY = bottomLayerY + (totalLayers - 1) * layerSpacing;
        return topY - (index * layerSpacing);
    }

    // Convenience overload using saved layerCount
    public float GetLayerHeight(int index)
    {
        return GetLayerHeight(index, layerCount);
    }

    // Total vertical height of the sandbox
    public float GetTotalHeight(int totalLayers)
    {
        return (totalLayers - 1) * layerSpacing;
    }
}