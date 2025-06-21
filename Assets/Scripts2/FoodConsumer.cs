using UnityEngine;

public class FoodConsumer : MonoBehaviour
{
    public string foodTargetUniqueID;

    void Update()
    {
        GameObject[] targets = GameObject.FindGameObjectsWithTag("Actor");

        foreach (GameObject target in targets)
        {
            ActorIdentity identity = target.GetComponent<ActorIdentity>();
            if (identity != null && identity.uniqueId == foodTargetUniqueID)
            {
                float dist = Vector3.Distance(transform.position, target.transform.position);
                if (dist < 1f)
                {
                    Destroy(target);
                    Debug.Log($"{gameObject.name} consumed {target.name} (ID: {identity.uniqueId})");
                }
            }
        }
    }
}
