using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DeleteActorManager : MonoBehaviour
{
    public static DeleteActorManager Instance;
    public bool isDeleteModeActive = false;
    public Button deleteButton;
    public Camera sceneCamera;

    private Image buttonImage;
    private Color defaultColor;
    private Color targetColor;
    private Color deleteColor = Color.yellow;
    private float colorLerpSpeed = 5f;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!deleteButton)
        {
            Debug.LogError("Delete button is not assigned!");
            return;
        }

        buttonImage = deleteButton.GetComponent<Image>();
        if (!buttonImage)
        {
            Debug.LogError("Delete button has no Image component!");
            return;
        }

        defaultColor = buttonImage.color;
        targetColor = defaultColor;

        deleteButton.onClick.AddListener(() =>
        {
            isDeleteModeActive = !isDeleteModeActive;
            targetColor = isDeleteModeActive ? deleteColor : defaultColor;

            Debug.Log(isDeleteModeActive ? "Delete mode on. Tap actors to delete." : "Delete mode off.");
        });
    }

    void Update()
    {

        if (buttonImage != null)
            buttonImage.color = Color.Lerp(buttonImage.color, targetColor, Time.deltaTime * colorLerpSpeed);

        if (!isDeleteModeActive) return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            TryDeleteActor(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began &&
            !EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
        {
            TryDeleteActor(Input.GetTouch(0).position);
        }
#endif
    }

    void TryDeleteActor(Vector2 screenPos)
    {
        Ray ray = sceneCamera.ScreenPointToRay(screenPos);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject obj = hit.collider.gameObject;
            if (obj.CompareTag("Actor"))
            {
                string id = obj.name;
                Destroy(obj);
                EnvironmentDataCache.RemoveActorById(id);
                Debug.Log($"Deleted actor with ID: {id}");
            }
        }
    }
}
