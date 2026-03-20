using UnityEngine;

namespace Player.Scripts
{
    public class PlayerGroundChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask masks;

        private void OnTriggerEnter(Collider _other)
        {
            if (!IsOnGround(masks, _other) )
                return;
            
            EventBus.Publish(new OnPlayerDetectGround());
        }

        private bool IsOnGround(LayerMask _layerMask, Collider _collider)
        {
            return (_layerMask & (1 << _collider.gameObject.layer)) != 0;
        }
    }
}

