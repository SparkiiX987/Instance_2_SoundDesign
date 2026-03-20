using UnityEngine;

namespace Player.Scripts
{
    /// <summary>
    /// Detects when the player touches the ground using a trigger collider.
    /// Publishes OnPlayerDetectGround when a collider on the specified layer enters the trigger.
    /// </summary>
    public class PlayerGroundChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask masks;

        /// <summary>
        /// Called when a collider enters the ground-check trigger.
        /// Publishes OnPlayerDetectGround if the collider matches the ground layer mask.
        /// </summary>
        /// <param name="_other">The collider that entered the trigger.</param>
        private void OnTriggerEnter(Collider _other)
        {
            if (!IsOnGround(masks, _other) )
                return;
            
            EventBus.Publish(new OnPlayerDetectGround());
        }

        /// <summary>
        /// Checks if the given collider belongs to a layer included in the specified mask.
        /// </summary>
        /// <param name="_layerMask">The layer mask to test against.</param>
        /// <param name="_collider">The collider to check.</param>
        /// <returns>True if the collider's layer is in the mask.</returns>
        private bool IsOnGround(LayerMask _layerMask, Collider _collider)
        {
            return (_layerMask & (1 << _collider.gameObject.layer)) != 0;
        }
    }
}
