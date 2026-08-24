
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LayerManager : MonoBehaviour
{
    public static LayerManager Instance;

    [Header("UI Buttons")]
    public GameObject editButton;
    public GameObject doneButton;
    public GameObject nextButton;
    public GameObject prevButton;
    public GameObject octoYellowButton;
    public GameObject octoBlueButton;

    [Header("Settings")]
    public SandboxSettings settings;

    [Header("Layer Root Objects — assign all 5 in Inspector")]
    public GameObject layer0Root;
    public GameObject layer1Root;
    public GameObject layer2Root;
    public GameObject layer3Root;
    public GameObject layer4Root;

    [Header("Grid Managers — assign all 5 in Inspector")]
    public GridManager gridManager0;
    public GridManager gridManager1;
    public GridManager gridManager2;
    public GridManager gridManager3;
    public GridManager gridManager4;

    [Header("Isometric Camera Settings")]
    public Camera mainCamera;
    public float isoHeight = 5f;
    public float isoDistance = 4f;
    public float isoAngleX = 45f;
    public float isoAngleY = 45f;
    public bool isoOrthographic = false;
    public float isoOrthoSize = 5f;

    [Header("Top Down Camera Settings")]
    public float topDownHeight = 8f;
    public float topDownOrthoSize = 3f;

    [Header("Sandbox Center")]
    public Vector3 sandboxCenter = Vector3.zero;

    [Header("Slide Animation")]
    public float slideSpeed = 2f;
    public float slideOffsetX = 9f;
    public float slideOffsetZ = 9f;

    [Header("Layer Colors")]
    public Color topLayerColor = new Color(0f, 1f, 0f, 0.18f);  // Green tint semi-transparent
    public Color otherLayerColor = new Color(0.23f, 0.23f, 0.23f, 1f); // Grey fully opaque

    private int activeLayer = 0;
    private int layerCount = 3; // Set from SandboxSettings
    private bool isEditMode = false;

    // Dynamic lists — sized to layerCount at runtime
    private List<GameObject> layerRoots = new List<GameObject>();
    private List<GridManager> gridManagers = new List<GridManager>();
    private List<Vector3> layerOriginPos = new List<Vector3>();
    private List<Coroutine> slideCoroutines = new List<Coroutine>();
    private List<List<GameObject>> layerActors = new List<List<GameObject>>();

    void Awake()
    {
        Instance = this;
    }

    // Called by LayerCountUI after count is confirmed
    public void InitializeLayers(int count)
    {
        layerCount = Mathf.Clamp(count, 3, 5);

        // Build root list based on count
        layerRoots.Clear();
        layerRoots.Add(layer0Root);
        layerRoots.Add(layer1Root);
        layerRoots.Add(layer2Root);
        if (layerCount >= 4) layerRoots.Add(layer3Root);
        if (layerCount >= 5) layerRoots.Add(layer4Root);

        // Build grid manager list
        gridManagers.Clear();
        gridManagers.Add(gridManager0);
        gridManagers.Add(gridManager1);
        gridManagers.Add(gridManager2);
        if (layerCount >= 4) gridManagers.Add(gridManager3);
        if (layerCount >= 5) gridManagers.Add(gridManager4);

        // Store origins
        layerOriginPos.Clear();
        foreach (var root in layerRoots)
            layerOriginPos.Add(root != null ? root.transform.position : Vector3.zero);

        // Init coroutine slots
        slideCoroutines.Clear();
        for (int i = 0; i < layerCount; i++)
            slideCoroutines.Add(null);

        // Init actor lists
        layerActors.Clear();
        for (int i = 0; i < layerCount; i++)
            layerActors.Add(new List<GameObject>());

        // Hide unused layer roots
        HideUnusedRoots();

        SetActiveLayer(0);
        SetIsometricView();
        ShowAllLayers();

        // UI initial state
        doneButton.SetActive(false);
        octoYellowButton.SetActive(false);
        octoBlueButton.SetActive(false);
        editButton.SetActive(true);
        nextButton.SetActive(true);
        prevButton.SetActive(true);

        Debug.Log($"LayerManager initialized with {layerCount} layers.");
    }

    void HideUnusedRoots()
    {
        // Hide layer roots that are beyond the current layer count
        if (layer3Root != null) layer3Root.SetActive(layerCount >= 4);
        if (layer4Root != null) layer4Root.SetActive(layerCount >= 5);
    }

    // PUBLIC ACCESSORS

    public int GetActiveLayer() => activeLayer;
    public int GetLayerCount() => layerCount;
    public bool IsEditMode() => isEditMode;

    public void SetActiveLayer(int index)
    {
        activeLayer = Mathf.Clamp(index, 0, layerCount - 1);
        Debug.Log($"Active Layer set to: {activeLayer}");
    }



    public GameObject GetRootForLayer(int index)
    {
        if (index >= 0 && index < layerRoots.Count)
            return layerRoots[index];
        return null;
    }

    public GridManager GetActiveGridManager()
    {
        if (activeLayer >= 0 && activeLayer < gridManagers.Count)
            return gridManagers[activeLayer];
        return null;
    }

    public float GetActiveLayerHeight()
    {
        if (settings == null) return activeLayer * 1.5f;
        return settings.GetLayerHeight(activeLayer);
    }

    // ACTOR REGISTRATION

    public void RegisterActor(GameObject actor, int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < layerActors.Count)
        {
            layerActors[layerIndex].Add(actor);
            Debug.Log($"Registered actor on Layer {layerIndex}.");
        }
    }

    public void UnregisterActor(GameObject actor, int layerIndex)
    {
        if (layerIndex >= 0 && layerIndex < layerActors.Count)
        {
            layerActors[layerIndex].Remove(actor);
            Debug.Log($"Unregistered actor from Layer {layerIndex}.");
        }
    }

    List<GameObject> GetActorList(int index)
    {
        if (index >= 0 && index < layerActors.Count)
            return layerActors[index];
        return new List<GameObject>();
    }

    public void NextLayer()
    {
        if (isEditMode)
        {
            Debug.Log("Exit edit mode before switching layers.");
            return;
        }

        if (activeLayer < layerCount - 1)
        {
            activeLayer++;
            UpdateIsometricVisibility();
            Debug.Log($"Switched to Layer {activeLayer}");
        }
        else
        {
            Debug.Log("Already at bottom layer.");
        }
    }

    public void PrevLayer()
    {
        if (isEditMode)
        {
            Debug.Log("Exit edit mode before switching layers.");
            return;
        }

        if (activeLayer > 0)
        {
            activeLayer--;
            UpdateIsometricVisibility();
            Debug.Log($"Switched to Layer {activeLayer}");
        }
        else
        {
            Debug.Log("Already at top layer.");
        }
    }

    // EDIT MODE

    public void EnterEditMode()
    {
        isEditMode = true;
        SetTopDownView();
        UpdateEditModeVisibility();

        editButton.SetActive(false);
        nextButton.SetActive(false);
        prevButton.SetActive(false);
        doneButton.SetActive(true);
        octoYellowButton.SetActive(true);
        octoBlueButton.SetActive(true);

        Debug.Log($"Edit mode ON — Layer {activeLayer}");
    }

    public void ExitEditMode()
    {
        isEditMode = false;

        UpdateIsometricVisibility();

        editButton.SetActive(true);
        nextButton.SetActive(true);
        prevButton.SetActive(true);
        doneButton.SetActive(false);
        octoYellowButton.SetActive(false);
        octoBlueButton.SetActive(false);
        SetIsometricView();
        Debug.Log("Edit mode OFF");
    }

    // VISIBILITY LOGIC

    void UpdateIsometricVisibility()
    {
        for (int i = 0; i < layerCount; i++)
        {
            GameObject root = GetRootForLayer(i);
            List<GameObject> actors = GetActorList(i);

            if (i < activeLayer)
            {
                // Passed layers — slide away
                Vector3 offPos = layerOriginPos[i] +
                                 new Vector3(slideOffsetX, 0f, slideOffsetZ);
                SlideLayerTo(i, offPos);
                SetActorsVisible(actors, false, 1f);
                SetActorsInteractable(actors, false);
            }
            else if (i == activeLayer)
            {
                // Top active layer — slide back, green tint
                SlideLayerTo(i, layerOriginPos[i]);
                SetGridColor(root, topLayerColor);
                RestoreUnusableCellColors(i); // keep red on blocked cells
                SetActorsVisible(actors, true, 1f);
                SetActorsInteractable(actors, true);
            }
            else
            {
                // Below active — slide back, grey
                SlideLayerTo(i, layerOriginPos[i]);
                SetGridColor(root, otherLayerColor);
                RestoreUnusableCellColors(i); // keep red on blocked cells
                SetActorsVisible(actors, true, 1f);
                SetActorsInteractable(actors, false);
            }
        }
    }

    void UpdateEditModeVisibility()
    {
        for (int i = 0; i < layerCount; i++)
        {
            GameObject root = GetRootForLayer(i);
            List<GameObject> actors = GetActorList(i);

            if (i < activeLayer)
            {
                // Passed — slide away
                Vector3 offPos = layerOriginPos[i] +
                                 new Vector3(slideOffsetX, 0f, slideOffsetZ);
                SlideLayerTo(i, offPos);
                SetActorsVisible(actors, false, 1f);
                SetActorsInteractable(actors, false);
            }
            else if (i == activeLayer)
            {
                // Active edit layer — green tint
                SlideLayerTo(i, layerOriginPos[i]);
                SetGridColor(root, topLayerColor);
                RestoreUnusableCellColors(i); // keep red on blocked cells
                SetActorsVisible(actors, true, 1f);
                SetActorsInteractable(actors, true);
            }
            else
            {
                // Below — grey, not interactable
                SlideLayerTo(i, layerOriginPos[i]);
                SetGridColor(root, otherLayerColor);
                RestoreUnusableCellColors(i); // keep red on blocked cells
                SetActorsVisible(actors, true, 1f);
                SetActorsInteractable(actors, false);
            }
        }
    }

    void ShowAllLayers()
    {
        for (int i = 0; i < layerCount; i++)
        {
            GameObject root = GetRootForLayer(i);
            if (root != null)
            {
                root.transform.position = layerOriginPos[i];
                SetGridColor(root, i == 0 ? topLayerColor : otherLayerColor);
                RestoreUnusableCellColors(i); // keep red on blocked cells
            }
            SetActorsVisible(GetActorList(i), true, 1f);
            SetActorsInteractable(GetActorList(i), i == 0);
        }
    }

   
    void SetGridColor(GameObject root, Color color)
    {
        if (root == null) return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Check if this renderer belongs to an actor - if so, SKIP IT
            bool belongsToActor = false;
            for (int i = 0; i < layerCount; i++)
            {
                foreach (GameObject actor in layerActors[i])
                {
                    if (actor == null) continue;
                    if (r.transform == actor.transform || r.transform.IsChildOf(actor.transform))
                    {
                        belongsToActor = true;
                        break;
                    }
                }
                if (belongsToActor) break;
            }

            if (belongsToActor) continue; // Don't touch actor materials!

            // Apply color only to grid renderers
            r.enabled = true;
            foreach (Material mat in r.materials)
            {
                if (color.a < 1f)
                {
                    mat.SetOverrideTag("RenderType", "Transparent");
                    mat.SetFloat("_Mode", 3f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    mat.SetInt("_ZWrite", 0);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.EnableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                }
                else
                {
                    mat.SetOverrideTag("RenderType", "Opaque");
                    mat.SetFloat("_Mode", 0f);
                    mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                    mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                    mat.SetInt("_ZWrite", 1);
                    mat.DisableKeyword("_ALPHATEST_ON");
                    mat.DisableKeyword("_ALPHABLEND_ON");
                    mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
                }

                mat.color = color;
            }
        }
    }

  
    void RestoreUnusableCellColors(int index)
    {
        if (index < 0 || index >= gridManagers.Count) return;
        GridManager gm = gridManagers[index];
        if (gm != null)
            gm.RestoreUnusableCellColors();
    }

    // SLIDE ANIMATION

    void SlideLayerTo(int layerIndex, Vector3 targetPos)
    {
        GameObject root = GetRootForLayer(layerIndex);
        if (root == null) return;

        if (slideCoroutines[layerIndex] != null)
            StopCoroutine(slideCoroutines[layerIndex]);

        slideCoroutines[layerIndex] = StartCoroutine(SlideCoroutine(root, targetPos));
    }

    IEnumerator SlideCoroutine(GameObject root, Vector3 targetPos)
    {
        while (root != null &&
               Vector3.Distance(root.transform.position, targetPos) > 0.01f)
        {
            root.transform.position = Vector3.Lerp(
                root.transform.position,
                targetPos,
                Time.deltaTime * slideSpeed
            );
            yield return null;
        }

        if (root != null)
            root.transform.position = targetPos;
    }

    // ACTOR VISIBILITY

    void SetActorsVisible(List<GameObject> actors, bool visible, float alpha)
    {
        foreach (GameObject actor in actors)
        {
            if (actor == null) continue;

            Renderer[] renderers = actor.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = visible;

                if (visible && alpha < 1f)
                {
                    foreach (Material mat in r.materials)
                    {
                        Color c = mat.color;
                        c.a = alpha;
                        mat.color = c;

                        mat.SetFloat("_Mode", 3);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                        mat.SetInt("_ZWrite", 0);
                        mat.EnableKeyword("_ALPHABLEND_ON");
                        mat.renderQueue = 3000;
                    }
                }
                else if (visible && alpha >= 1f)
                {
                    foreach (Material mat in r.materials)
                    {
                        Color c = mat.color;
                        c.a = 1f;
                        mat.color = c;

                        mat.SetFloat("_Mode", 0);
                        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                        mat.SetInt("_ZWrite", 1);
                        mat.DisableKeyword("_ALPHABLEND_ON");
                        mat.renderQueue = -1;
                    }
                }
            }
        }
    }

    void SetActorsInteractable(List<GameObject> actors, bool interactable)
    {
        foreach (GameObject actor in actors)
        {
            if (actor == null) continue;
            Collider[] colliders = actor.GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
                c.enabled = interactable;
        }
    }

    // CAMERA
    public void SetSandboxCenter(Vector3 center)
    {
        sandboxCenter = center;

        // Update origins AFTER sandbox center is known
        for (int i = 0; i < layerRoots.Count; i++)
        {
            if (layerRoots[i] != null)
                layerOriginPos[i] = layerRoots[i].transform.position;
        }

        Debug.Log($"Sandbox center: {sandboxCenter}");

        // Refresh camera with correct position
        SetIsometricView();
    }

    void SetTopDownView()
    {
        if (mainCamera == null) return;

        float activeY = GetActiveLayerHeight();

        mainCamera.transform.position = new Vector3(
            sandboxCenter.x,
            sandboxCenter.y + activeY + topDownHeight,
            sandboxCenter.z
        );
        mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        mainCamera.orthographic = true;
        mainCamera.orthographicSize = topDownOrthoSize;

        Debug.Log($"Camera → Top Down | OrthoSize:{topDownOrthoSize}");
    }

    void SetIsometricView()
    {
        if (mainCamera == null) return;

        mainCamera.transform.position = new Vector3(
            sandboxCenter.x - isoDistance,
            sandboxCenter.y + isoHeight,
            sandboxCenter.z - isoDistance
        );

        mainCamera.transform.rotation = Quaternion.Euler(isoAngleX, isoAngleY, 0f);

        mainCamera.orthographic = isoOrthographic;
        if (isoOrthographic)
            mainCamera.orthographicSize = isoOrthoSize;

        Debug.Log($"Camera → Isometric | " +
                  $"Height:{isoHeight} Distance:{isoDistance} " +
                  $"Ortho:{isoOrthographic}");
    }
}