using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private Transform checkPoint;

    private void OnTriggerEnter(Collider other)
    {
        EventBus.Publish(new OnLevelEnd
        {
            newSpawnPoint = checkPoint.position
        });
    }
}
