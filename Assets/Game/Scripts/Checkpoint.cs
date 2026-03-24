using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        EventBus.Publish(new OnLevelEnd
        {
            newSpawnPoint = other.transform.position,
        });
    }
}
