using UnityEngine;

public class ActorSelector : MonoBehaviour
{
    [System.Serializable]
    public class ActorOption
    {
        public string name;
        public GameObject prefab;
    }

    public ActorOption[] actors; // List of available actor prefabs
    private GameObject selectedActor;

    /// <summary>
    /// Call this function from UI Buttons to select an actor by index.
    /// </summary>
    /// <param name="index">Index of the actor in the array</param>
    public void SelectActor(int index)
    {
        if (index >= 0 && index < actors.Length)
        {
            selectedActor = actors[index].prefab;
            Debug.Log($"Actor selected: {actors[index].name}");
        }
        else
        {
            Debug.LogWarning("Invalid actor index selected.");
        }
    }

    /// <summary>
    /// Get the currently selected actor prefab.
    /// </summary>
    public GameObject GetSelectedActor()
    {
        return selectedActor;
    }

    /// <summary>
    /// Clear the current actor selection.
    /// </summary>
    public void ClearSelection()
    {
        selectedActor = null;
        Debug.Log("Actor selection cleared.");
    }
}
