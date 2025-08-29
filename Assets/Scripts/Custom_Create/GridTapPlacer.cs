using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.EventSystems;

public class GridTapPlacer : MonoBehaviour
{
    public Camera arCamera;                     // AR Camera from AR Session Origin
    public ActorSelector actorSelector;        // Reference to ActorSelector script

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = arCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0))
            {
                Debug.Log("Hit: " + hit.collider.gameObject.name);
                Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

                GridCellScript cell = hit.collider.GetComponent<GridCellScript>();

                if (cell != null && !cell.isOccupied)
                {
                    GameObject selectedActor = actorSelector.GetSelectedActor();
                    if (selectedActor == null)
                    {
                        Debug.Log("No actor selected!");
                        return;
                    }

                    cell.PlaceActor(selectedActor);
                    Debug.Log($"Placed actor {selectedActor.name} at {cell.gameObject.name}");

                    actorSelector.ClearSelection();
                }
            }
        }
#else
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            if (EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                return;

            Ray ray = arCamera.ScreenPointToRay(Input.GetTouch(0).position);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ~0))
            {
                Debug.Log("Touch Hit: " + hit.collider.gameObject.name);
                Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 1f);

                GridCellScript cell = hit.collider.GetComponent<GridCellScript>();

                if (cell != null && !cell.isOccupied)
                {
                    GameObject selectedActor = actorSelector.GetSelectedActor();
                    if (selectedActor == null)
                    {
                        Debug.Log("No actor selected!");
                        return;
                    }

                    cell.PlaceActor(selectedActor);
                    Debug.Log($"Placed actor {selectedActor.name} at {cell.gameObject.name}");

                    actorSelector.ClearSelection();
                }
            }
        }
#endif
    }
}
