using UnityEngine;
using UnityEngine.EventSystems;



public class UpDownButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum SwimDirection { Up, Down }

    [Header("Configuration")]
    public SwimDirection direction = SwimDirection.Up;

    [Tooltip("Optional — leave empty to auto-find the main player at runtime")]
    public MovementController targetController;

    void Start()
    {
        if (targetController == null)
            FindMainPlayerController();
    }

    void FindMainPlayerController()
    {
        GameObject[] actors = GameObject.FindGameObjectsWithTag("Actor");
        foreach (var actor in actors)
        {
            MovementController mc = actor.GetComponent<MovementController>();
            if (mc != null)
            {
                targetController = mc;
                return;
            }
        }

        Debug.LogWarning("[UpDownButtonHandler] No main player with MovementController found yet. " +
                         "Will retry when pressed.");
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (targetController == null) FindMainPlayerController();
        if (targetController == null) return;

        if (direction == SwimDirection.Up)
            targetController.StartMovingUp();
        else
            targetController.StartMovingDown();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (targetController == null) return;

        if (direction == SwimDirection.Up)
            targetController.StopMovingUp();
        else
            targetController.StopMovingDown();
    }

    // Safety: stop movement if the button is disabled/destroyed while held
    void OnDisable()
    {
        if (targetController == null) return;

        if (direction == SwimDirection.Up)
            targetController.StopMovingUp();
        else
            targetController.StopMovingDown();
    }
}