using System.Collections.Generic;
using CreateEnv;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FreeExploreConfigController : MonoBehaviour
{
    [Header("Mode")]
    [SerializeField] private Button boundlessButton;
    [SerializeField] private Button portalButton;
    [SerializeField] private TMP_Text boundlessLabel;
    [SerializeField] private TMP_Text portalLabel;

    [SerializeField] private Color selectedColor = new Color32(0x2B, 0xA8, 0x4A, 0xFF);
    [SerializeField] private Color unselectedColor = Color.white;
    [SerializeField] private Color selectedTextColor = Color.white;
    [SerializeField] private Color unselectedTextColor = new Color32(0x66, 0x78, 0x8C, 0xFF);

    [Header("Environment")]
    [SerializeField] private Image previewImage;
    [SerializeField] private TMP_Text environmentName;
    [SerializeField] private TMP_Text descriptionText;

    [SerializeField] private Button leftArrow;
    [SerializeField] private Button rightArrow;

    [SerializeField] private Button playButton;

    [Header("Page dots")]
    [SerializeField] private RectTransform dotsContainer;
    [Tooltip("Inactive dot Image inside dotsContainer, cloned per environment.")]
    [SerializeField] private GameObject dotTemplate;
    [SerializeField] private Color dotActiveColor = new Color32(0x2B, 0xA8, 0x4A, 0xFF);
    [SerializeField] private Color dotInactiveColor = new Color32(0xC9, 0xD4, 0xE0, 0xFF);

    [Header("Custom environments (Boundless only)")]
    [Tooltip("The 'New Environment' button that opens the CustomEnvBuilder scene. " +
             "Hidden while Portal mode is selected. Optional.")]
    [SerializeField] private GameObject customizeButton;
    [Tooltip("Star icon shown beside the name of a user-made environment.")]
    [SerializeField] private GameObject customStar;
    [Tooltip("'Custom' tag shown beside the name of a user-made environment.")]
    [SerializeField] private GameObject customTag;
    [Tooltip("Opens the builder pre-filled with the selected user-made environment.")]
    [SerializeField] private Button editButton;
    [SerializeField] private string builderSceneName = "CustomEnvBuilder";

    [Header("Data")]
    [SerializeField] private EnvironmentInfo[] boundlessEnvironments;
    [SerializeField] private PortalEnvironmentInfo[] portalEnvironments;

    [Header("User-made environments")]
    [Tooltip("The ConfigurableTerrainEnvironment prefab that FreeExploreEndless AR-places. " +
             "Its EnvironmentLoader applies the selected user profile. Required for user envs to play.")]
    [SerializeField] private GameObject configurablePrefab;
    [Tooltip("Fallback preview shown for user-made environments (they have no authored sprite). Optional.")]
    [SerializeField] private Sprite customEnvPreview;
    [SerializeField] private string endlessSceneName = "FreeExploreEndless";

    // User-saved environments, loaded from disk. They are appended AFTER the
    // built-in boundless environments so the carousel reads built-ins first, then
    // "your own" environments made in CustomEnvBuilder.
    private readonly List<EnvironmentProfile> userEnvironments = new List<EnvironmentProfile>();

    private int boundlessIndex = 0;
    private int portalIndex = 0;

    private bool isBoundless = true;

    private void Start()
    {
        LoadUserEnvironments();

        leftArrow.onClick.AddListener(PreviousEnvironment);
        rightArrow.onClick.AddListener(NextEnvironment);

        boundlessButton.onClick.AddListener(SelectBoundless);
        portalButton.onClick.AddListener(SelectPortal);

        playButton.onClick.AddListener(Play);
        if (editButton != null)
            editButton.onClick.AddListener(EditSelected);

        RefreshModeUI();
        RefreshUI();
    }

    private void LoadUserEnvironments()
    {
        userEnvironments.Clear();
        userEnvironments.AddRange(EnvironmentRepository.ListUser());
    }

    // Total number of selectable boundless entries: authored built-ins first,
    // then every user-saved environment.
    private int BoundlessCount =>
        (boundlessEnvironments != null ? boundlessEnvironments.Length : 0) + userEnvironments.Count;

    private int BuiltInCount =>
        boundlessEnvironments != null ? boundlessEnvironments.Length : 0;

    // The user-made profile currently shown in the carousel, or null when a
    // built-in (or a portal environment) is selected.
    private EnvironmentProfile SelectedUserEnvironment =>
        isBoundless && boundlessIndex >= BuiltInCount && boundlessIndex < BoundlessCount
            ? userEnvironments[boundlessIndex - BuiltInCount]
            : null;

    private void SelectBoundless()
    {
        if (isBoundless)
            return;

        isBoundless = true;

        RefreshModeUI();
        RefreshUI();
    }

    private void SelectPortal()
    {
        if (!isBoundless)
            return;

        isBoundless = false;

        RefreshModeUI();
        RefreshUI();
    }

    private void PreviousEnvironment()
    {
        if (isBoundless)
        {
            int count = BoundlessCount;
            if (count == 0)
                return;

            boundlessIndex--;

            if (boundlessIndex < 0)
                boundlessIndex = count - 1;
        }
        else
        {
            if (portalEnvironments.Length == 0)
                return;

            portalIndex--;

            if (portalIndex < 0)
                portalIndex = portalEnvironments.Length - 1;
        }

        RefreshUI();
    }

    private void NextEnvironment()
    {
        if (isBoundless)
        {
            int count = BoundlessCount;
            if (count == 0)
                return;

            boundlessIndex++;

            if (boundlessIndex >= count)
                boundlessIndex = 0;
        }
        else
        {
            if (portalEnvironments.Length == 0)
                return;

            portalIndex++;

            if (portalIndex >= portalEnvironments.Length)
                portalIndex = 0;
        }

        RefreshUI();
    }

    private void RefreshUI()
    {
        if (isBoundless)
        {
            if (BoundlessCount == 0)
                return;

            boundlessIndex = Mathf.Clamp(boundlessIndex, 0, BoundlessCount - 1);

            var profile = SelectedUserEnvironment;
            if (profile == null)
            {
                var env = boundlessEnvironments[boundlessIndex];

                previewImage.sprite = env.PreviewImage;
                environmentName.text = env.EnvironmentName;
                descriptionText.text = env.Description;
            }
            else
            {
                previewImage.sprite = customEnvPreview;
                environmentName.text = profile.displayName;
                descriptionText.text = Describe(profile);
            }
        }
        else
        {
            if (portalEnvironments.Length == 0)
                return;

            var env = portalEnvironments[portalIndex];

            previewImage.sprite = env.PreviewImage;
            environmentName.text = env.EnvironmentName;
            descriptionText.text = env.Description;
        }

        RefreshCustomAccents();
        RefreshDots();
    }

    private void RefreshModeUI()
    {
        bool showArrows = isBoundless
            ? BoundlessCount > 1
            : portalEnvironments.Length > 1;

        leftArrow.gameObject.SetActive(showArrows);
        rightArrow.gameObject.SetActive(showArrows);

        // The custom-environment builder only applies to boundless mode.
        if (customizeButton != null)
            customizeButton.SetActive(isBoundless);

        boundlessButton.image.color = isBoundless ? selectedColor : unselectedColor;
        portalButton.image.color = isBoundless ? unselectedColor : selectedColor;
        if (boundlessLabel != null) boundlessLabel.color = isBoundless ? selectedTextColor : unselectedTextColor;
        if (portalLabel != null)    portalLabel.color    = isBoundless ? unselectedTextColor : selectedTextColor;
    }

    // Star + "Custom" tag on the card and the Edit button only make sense for
    // user-made environments.
    private void RefreshCustomAccents()
    {
        bool isUserEnv = SelectedUserEnvironment != null;
        if (customStar != null) customStar.SetActive(isUserEnv);
        if (customTag != null)  customTag.SetActive(isUserEnv);
        if (editButton != null) editButton.gameObject.SetActive(isUserEnv);
    }

    // One dot per carousel entry, current entry highlighted. Dots are cloned from
    // an inactive template so the count can follow the user's saved environments.
    private void RefreshDots()
    {
        if (dotsContainer == null || dotTemplate == null)
            return;

        int count = isBoundless ? BoundlessCount : (portalEnvironments != null ? portalEnvironments.Length : 0);
        int current = isBoundless ? boundlessIndex : portalIndex;

        while (dotsContainer.childCount - 1 < count)
            Instantiate(dotTemplate, dotsContainer);

        for (int i = 1; i < dotsContainer.childCount; i++)
        {
            var dot = dotsContainer.GetChild(i).gameObject;
            int index = i - 1;
            dot.SetActive(index < count);
            if (dot.TryGetComponent(out Image image))
                image.color = index == current ? dotActiveColor : dotInactiveColor;
        }
    }

    private async void EditSelected()
    {
        var profile = SelectedUserEnvironment;
        if (profile == null)
            return;

        EnvironmentSession.EditRequestId = profile.id;
        await SceneLoaderBackend.LoadAddressableSceneAsync(builderSceneName);
    }

    private async void Play()
    {
        if (isBoundless)
        {
            if (BoundlessCount == 0)
                return;

            var profile = SelectedUserEnvironment;
            if (profile == null)
            {
                // Authored built-in environment: play its own prefab, and clear any
                // stale user profile so EnvironmentLoader uses the authored scene as-is.
                EnvironmentSession.Selected = null;
                ExploreSession.SelectedBoundlessEnvironment = boundlessEnvironments[boundlessIndex];
            }
            else
            {
                // User-made environment: hand the profile to the session and play it
                // through the shared configurable prefab (mirrors CustomEnvBuilder's
                // former PlayProfile path).
                EnvironmentSession.Selected = profile.Clone();
                ExploreSession.SelectedBoundlessEnvironment = new EnvironmentInfo
                {
                    EnvironmentName = profile.displayName,
                    Description = profile.displayName,
                    EnvironmentPrefab = configurablePrefab
                };
            }

            await SceneLoaderBackend.LoadAddressableSceneAsync(endlessSceneName);
        }
        else
        {
            if (portalEnvironments.Length == 0)
                return;

            await SceneLoaderBackend.LoadAddressableSceneAsync(portalEnvironments[portalIndex].SceneName);
        }
    }

    // Bullet summary of a user-made environment, in the same friendly words the
    // builder uses. (p.simple is never null and in range after EnvironmentBounds.Clamp.)
    private static string Describe(EnvironmentProfile p)
    {
        var s = p.simple;
        return $"• {SimpleEnvironmentSettings.SeafloorProfiles[s.seafloorProfile]} seafloor\n" +
               $"• {SimpleEnvironmentSettings.MarineHabitats[s.marineHabitat]} habitat\n" +
               $"• {SimpleEnvironmentSettings.WaterColours[s.waterColour]} water\n" +
               $"• {SimpleEnvironmentSettings.ExplorationAreas[s.explorationArea]} exploration area";
    }
}
