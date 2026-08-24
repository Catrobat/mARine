using UnityEngine;

[System.Serializable]
public class PortalEnvironmentInfo
{
    public string EnvironmentName;

    [TextArea(3, 5)]
    public string Description;

    public Sprite PreviewImage;

    [Tooltip("Scene to load when this portal environment is selected.")]
    public string SceneName;
}