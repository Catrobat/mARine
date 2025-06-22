using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ARPlacementController : MonoBehaviour
{
    [Header("AR References")]
    public ARRaycastManager raycastManager;
    public Camera arCamera;

    public ARPlaneManager planeManager;  // AR Plane Manager reference

    [Header("Environment Prefabs")]
    public ActorDatabase actorDatabase;

    [Header("UI")]
    public FloatingJoystick joystick;                  // Joystick logic reference
    public GameObject joystickUIRoot;                  // Joystick canvas root (assign in Inspector)

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool placed = false;

    void Start()
    {
        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(false);  // Hide joystick UI by default
    }

    void Update()
    {

        if (placed)
            return;

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceAt(Input.mousePosition);
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began)
        {
            TryPlaceAt(Input.GetTouch(0).position);
        }
#endif
    }



    private void TryPlaceAt(Vector2 screenPosition)
    {
        if (raycastManager.Raycast(screenPosition, hits, TrackableType.Planes))
        {
            Pose pose = hits[0].pose;
            PlaceSavedEnvironment(pose.position);
            placed = true;
        }
    }

    private void PlaceSavedEnvironment(Vector3 position)
    {
        string envKey = PlayerPrefs.GetString("SelectedEnvironmentKey");
        string json = PlayerPrefs.GetString(envKey);

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogError("No environment data found for key: " + envKey);
            return;
        }

        EnvironmentData data = JsonUtility.FromJson<EnvironmentData>(json);
        if (data == null)
        {
            Debug.LogError("Failed to parse environment data.");
            return;
        }

        GameObject root = new GameObject(data.environmentName);
        root.transform.position = position;

        if (planeManager != null)
        {
            planeManager.enabled = false; // Disable plane detection after placement
            
            foreach (var plane in planeManager.trackables)
            {
                plane.gameObject.SetActive(false); // Hide existing planes
            }
        }

        // Instantiate environment plane
        if (!string.IsNullOrEmpty(data.environmentPlanePrefabName))
        {
            GameObject planePrefab = actorDatabase.GetActorByName(data.environmentPlanePrefabName);
            if (planePrefab != null)
            {
                GameObject plane = Instantiate(planePrefab, root.transform);
                plane.transform.localPosition = Vector3.zero;
                plane.transform.localRotation = Quaternion.identity;
            }
        }

        bool mainPlayerFound = false;

        // Instantiate actors
        foreach (var actor in data.placedActors)
        {
            GameObject prefab = actorDatabase.GetActorByName(actor.prefabName);
            if (prefab != null)
            {
                GameObject go = Instantiate(prefab, root.transform);
                go.transform.localPosition = actor.localPosition;
                go.transform.localRotation = actor.localRotation;

                // Assign unique name and tag
                if (string.IsNullOrEmpty(actor.uniqueID))
                    actor.uniqueID = System.Guid.NewGuid().ToString();

                go.name = actor.uniqueID;
                go.tag = "Actor";

                // Attach ActorIdentity script and assign uniqueId
                ActorIdentity identity = go.AddComponent<ActorIdentity>();
                identity.uniqueId = actor.uniqueID;

                // Main player setup
                if (actor.isMainPlayer)
                {
                    var controller = go.AddComponent<MovementController>();
                    controller.joystick = joystick;
                    mainPlayerFound = true;
                    Debug.Log("Main player instantiated with movement.");
                }

                // Food Consumer behavior
                if (actor.addedScripts != null && actor.addedScripts.Contains("Food Consumption"))
                {
                    FoodConsumer foodConsumer = go.AddComponent<FoodConsumer>();
                    foodConsumer.foodTargetUniqueID = actor.foodTargetUniqueID;
                }
            }
        }

        if (joystickUIRoot != null)
            joystickUIRoot.SetActive(mainPlayerFound);

        Debug.Log("Environment placed successfully.");
    }
}
