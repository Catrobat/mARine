using UnityEngine;

public class OctopusInk : MonoBehaviour
{
    public GameObject inkParticlePrefab;
    public SharkFollower shark;

    public void SpewInk()
    {
        Instantiate(inkParticlePrefab, transform.position, Quaternion.identity);
        // Add shark reaction here if needed
        shark.OnInkHit();
    }
}
