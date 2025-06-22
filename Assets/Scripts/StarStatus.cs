using UnityEngine;

public class StarStatus : MonoBehaviour
{
    public bool unlockStatus = false;
    public GameObject lockedPanel;
    public GameObject unlockedPanel;
    private Transform player; // auto-assigned at runtime
    public float showDistance = 2f;

    void Start()
    {
        Camera arCamera = Camera.main;  // MainCamera Tag
        player = arCamera.transform;


        // Load unlock status from PlayerPrefs
        unlockStatus = PlayerPrefs.GetInt("StarfishUnlocked", 0) == 1;

        if (player == null)
        {
            Debug.LogWarning("StarStatus: Could not find AR camera!");
        }

        lockedPanel.SetActive(false);
        unlockedPanel.SetActive(false);
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.position);
        bool shouldShow = dist < showDistance;

        lockedPanel.SetActive(shouldShow && !unlockStatus);
        unlockedPanel.SetActive(shouldShow && unlockStatus);
    }

    public void StarUnlocked()
    {
        unlockStatus = true;
        PlayerPrefs.SetInt("StarfishUnlocked", 1);
        PlayerPrefs.Save();
    }
}
