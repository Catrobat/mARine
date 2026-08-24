using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityButtonUI : MonoBehaviour
{
    [Header("UI References")]
    public Button button;
    public TMP_Text label;          // Shows ability name
    public Image buttonBackground;  // Changes color when active

    [Header("Colors")]
    public Color inactiveColor = new Color(1f, 1f, 1f, 0.85f);
    public Color activeColor = new Color(0.3f, 0.85f, 1f, 0.95f); // Teal highlight when on

    private ActorAbility boundAbility;

    public void Bind(ActorAbility ability)
    {
        boundAbility = ability;

        if (label != null)
            label.text = ability.AbilityName;

        if (button != null)
            button.onClick.AddListener(OnButtonPressed);

        RefreshVisual();
    }

    void OnButtonPressed()
    {
        if (boundAbility == null) return;
        boundAbility.Activate();
        RefreshVisual();
    }

    // Called every frame so the button color stays in sync
    // (in case ability state changes from outside, e.g. future automation)
    void Update()
    {
        if (boundAbility != null)
            RefreshVisual();
    }

    void RefreshVisual()
    {
        if (buttonBackground == null || boundAbility == null) return;
        buttonBackground.color = boundAbility.IsActive ? activeColor : inactiveColor;
    }

    void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnButtonPressed);
    }
}
