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

   
    public GameObject GetSelectedActor()
    {
        return selectedActor;
    }

  
    public void ClearSelection()
    {
        selectedActor = null;
        Debug.Log("Actor selection cleared.");
    }
}
