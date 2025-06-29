using UnityEngine;

public class HighLightTargetRegistry : MonoBehaviour
{
    public static HighLightTargetRegistry Instance {get; private set;} 
    
    public GameObject muteButton;
    public GameObject aiButton;
    public GameObject depthSlider; 
    public GameObject globalCanvasParent;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
}
