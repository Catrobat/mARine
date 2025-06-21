using UnityEngine;
using UnityEngine.UI;

public class AudioToggleButton : MonoBehaviour
{
    public Sprite audioOnSprite;
    public Sprite audioOffSprite;
    public Image buttonImage;

    public void ToggleAudio()
    {
        if (AudioToggleManager.instance)
        {
            AudioToggleManager.instance.ToggleAudio();
            UpdateIcon();
        }
    }

    private void Start()
    {
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (AudioToggleManager.instance)
        {
            if (AudioToggleManager.instance.audioEnabled)
                buttonImage.sprite = audioOnSprite;
            else
                buttonImage.sprite = audioOffSprite;
        }
    }
}