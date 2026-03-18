using UnityEngine;

namespace Player.Scripts
{
    public class PlayerGroundChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask objectMask;

        private void OnTriggerEnter(Collider _other)
        {
            if (IsOnGround(groundMask, _other) || IsOnGround(objectMask, _other))
                return;
            
            EventBus.Publish(new OnPlayerDetectGround());
        }

        private bool IsOnGround(LayerMask _layerMask, Collider _collider)
        {
            return (_layerMask & (1 << _collider.gameObject.layer)) != 0;
        }
    }
}

