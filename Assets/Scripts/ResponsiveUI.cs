using UnityEngine;

public class ResponsiveUI : MonoBehaviour
{
    [Header("Assign Scroll Containers")]
    public RectTransform[] scrollContainers;
    public float landscapeSize = 2f;
    public float portaitSize = 1f;
    private Vector2 lastScreenSize;

    void Start()
    {
        ApplyScale();
    }

    void Update()
    {
        if (lastScreenSize.x != Screen.width ||
            lastScreenSize.y != Screen.height)
        {
            lastScreenSize = new Vector2(Screen.width, Screen.height);
            ApplyScale();
        }
    }

    void ApplyScale()
    {
        bool isLandscape = Screen.width > Screen.height;

        foreach (RectTransform container in scrollContainers)
        {
            if (isLandscape)
            {
                container.localScale = Vector3.one * landscapeSize;
            }
            else
            {
                container.localScale = Vector3.one * portaitSize;
            }
        }
    }
}