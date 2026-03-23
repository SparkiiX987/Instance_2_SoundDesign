using DG.Tweening;
using UnityEngine;

public class LaserDetection : MonoBehaviour
{
    [SerializeField] Collider laser;
    private void OnTriggerEnter(Collider other)
    {
        EventBus.Publish(new AlarmeSetActive
        {
            playerPosition = other.transform.position,
        });
        laser.enabled = false;
    }
}
