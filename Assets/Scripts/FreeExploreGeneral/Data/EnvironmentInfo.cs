using UnityEngine;

[System.Serializable]
public class EnvironmentInfo
{
    public string EnvironmentName;

    [TextArea(3, 5)]
    public string Description;

    public Sprite PreviewImage;

    public GameObject EnvironmentPrefab;
}