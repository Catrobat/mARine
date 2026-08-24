using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the right-side mask selector panel.
/// Switches between mask types (Scuba Mask, Scuba Goggles, Scuba Mask Alt)
/// and highlights the currently active button.
/// </summary>
public class MaskSwitcher : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FaceARManager faceManager;

    [Header("Mask Buttons")]
    [Tooltip("Buttons in order matching FaceARManager.maskPrefabs array")]
    [SerializeField] private Button[] maskButtons;

    [Header("Colors")]
    [SerializeField] private Color activeColor = new Color(0.2f, 0.9f, 0.75f, 1f);
    [SerializeField] private Color inactiveColor = new Color(0.35f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color activeBorderColor = new Color(0.2f, 1f, 0.85f, 1f);

    private int _currentIndex = 0;

    void Start()
    {
        // Wire button clicks
        for (int i = 0; i < maskButtons.Length; i++)
        {
            int index = i; // capture for closure
            if (maskButtons[i] != null)
                maskButtons[i].onClick.AddListener(() => SelectMask(index));
        }

        // Set initial highlight
        UpdateButtonHighlights();
    }

    /// <summary>
    /// Select a mask by index. Called by button clicks or externally.
    /// </summary>
    public void SelectMask(int index)
    {
        if (index < 0 || index >= maskButtons.Length) return;

        _currentIndex = index;

        if (faceManager != null)
            faceManager.SetActiveMask(index);

        UpdateButtonHighlights();
    }

    private void UpdateButtonHighlights()
    {
        for (int i = 0; i < maskButtons.Length; i++)
        {
            if (maskButtons[i] == null) continue;

            var colors = maskButtons[i].colors;

            if (i == _currentIndex)
            {
                colors.normalColor = activeColor;
                colors.highlightedColor = activeBorderColor;
                colors.selectedColor = activeColor;
            }
            else
            {
                colors.normalColor = inactiveColor;
                colors.highlightedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                colors.selectedColor = inactiveColor;
            }

            maskButtons[i].colors = colors;
        }
    }

    /// <summary>Current active mask index.</summary>
    public int CurrentIndex => _currentIndex;
}
