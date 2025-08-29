using UnityEngine;
using UnityEngine.EventSystems;

public class PlayAudioOnTap : MonoBehaviour
{
    public AudioSource audioSource;
    private Camera _arCamera;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        _arCamera = Camera.main;
    }

    void Update()
    {
        if (Input.touchCount == 0 || Input.GetTouch(0).phase != TouchPhase.Began)
            return;
        
        Touch touch = Input.GetTouch(0);
        
        // Ignore touch if over UI
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId))
            return;

        Ray ray = _arCamera.ScreenPointToRay(touch.position);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.transform == transform && audioSource && !audioSource.isPlaying)
            {
                AudioToggleManager.instance.SetCurrentAudio(audioSource);
            }
        }
    }
}