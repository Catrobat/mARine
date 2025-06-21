using UnityEngine;

public class LimitFramerate : MonoBehaviour
{
    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0; // Disable V-Sync to allow manual control
    }
}