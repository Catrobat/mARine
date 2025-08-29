using UnityEngine;

public class BubbleController : MonoBehaviour
{
    public ParticleSystem[] bubbleSystems;
    public TemperatureController temperatureController;

    void Update()
    {
        if (bubbleSystems != null && temperatureController != null)
        {
            foreach (var bubble in bubbleSystems)
            {
                if (bubble != null)
                {
                    var emission = bubble.emission;
                    emission.rateOverTime = Mathf.Lerp(0f, 100f, temperatureController.BubbleStrength);
                }
            }
        }
    }
}
