using UnityEngine;

namespace Player.Scripts
{
    public class PlayerGroundChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask;

        private void OnTriggerEnter(Collider _other)
        {
            // Check if the layer of the colliding object is in the groundMask
            if ((groundMask & (1 << _other.gameObject.layer)) == 0)
                return;
            
            EventBus.Publish(new OnPlayerDetectGround());
        }
    }
}

