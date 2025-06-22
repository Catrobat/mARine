using UnityEngine;

// Global Audio Toggle Manager
public class AudioToggleManager: MonoBehaviour
{
    public static AudioToggleManager instance { get; private set; }
    
    public bool audioEnabled = true;
    
    private AudioSource _currentAudio;

    public void Awake()
    {
        if (instance && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetCurrentAudio(AudioSource newAudio)
    {
        Debug.Log("SetCurrentAudio called for: " + newAudio.name);

        // Stop previous audio
        if (_currentAudio && _currentAudio != newAudio)
        {
            Debug.Log("Stopping previous audio: " + _currentAudio.name);
            _currentAudio.Stop();
        }

        _currentAudio = newAudio;

        if (audioEnabled)
        {
            Debug.Log("Playing new audio");
            _currentAudio.Play();
        }
        else
        {
            Debug.Log("Pausing new audio because audio is disabled");
            _currentAudio.Pause();
        }
    }


    public void ToggleAudio()
    {
        audioEnabled = !audioEnabled;
        Debug.Log("Audio is now: " + (audioEnabled ? "enabled" : "disabled"));

        if (_currentAudio)
        {
            if (audioEnabled)
                _currentAudio.UnPause();
            else
                _currentAudio.Pause();
        }
    }
}