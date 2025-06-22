using UnityEngine;
using UnityEngine.UI;

public class EnvironmentButton : MonoBehaviour
{
    public Text nameText;
    public Button selectButton;
    public Image background;

    [HideInInspector] public string prefabName;

    public void SetData(string prefabName, System.Action<string> onClick, bool isSelected)
    {
        this.prefabName = prefabName;
        nameText.text = prefabName;
        selectButton.onClick.RemoveAllListeners();
        selectButton.onClick.AddListener(() => onClick(prefabName));
        background.color = isSelected ? Color.green : Color.white;
    }
}
