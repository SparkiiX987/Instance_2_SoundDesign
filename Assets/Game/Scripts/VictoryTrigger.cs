using UnityEngine;

public class VictoryTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        EventBus.Publish<OnVictory>(new OnVictory());
    }
}
