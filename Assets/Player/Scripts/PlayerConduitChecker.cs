using DG.Tweening;
using UnityEngine;
using Utils;

namespace Player.Scripts
{
    /// <summary>
    /// Checks if the player is still inside a conduit by casting two raycasts downward
    /// (front center and back center) based on the player's collider bounds.
    /// Uses DOTween delayed calls instead of Update for periodic checks.
    /// Activates on crouch, deactivates on uncrouch.
    /// </summary>
    public class PlayerConduitChecker : MonoBehaviour
    {
        [SerializeField] private Collider playerCollider;
        [SerializeField] private LayerMask conduitLayer;
        [SerializeField] private float checkDelay = 0.2f;
        [SerializeField] private float rayDistance = 1.5f;

        private Bounds colliderBounds;
        private Tween checkLoop;
        private bool isActive;
        private bool isInConduit;

        /// <summary>
        /// Snapshots the collider bounds and starts the DOTween check loop.
        /// </summary>
        private void Activate()
        {
            if (isActive)
                return;

            colliderBounds = playerCollider.bounds;
            isActive = true;

            StartCheckLoop();
        }

        /// <summary>
        /// Stops the check loop and resets the conduit state.
        /// </summary>
        private void Deactivate()
        {
            isActive = false;
            isInConduit = false;
            checkLoop?.Kill();
        }

        /// <summary>
        /// Called when the player crouches. Starts conduit detection.
        /// </summary>
        private void OnCrouch(OnPlayerCrouch _)
        {
            Activate();
        }

        /// <summary>
        /// Called when the player uncrouches. Stops conduit detection.
        /// </summary>
        private void OnUnCrouch(OnPlayerUnCrouch _)
        {
            Deactivate();
        }

        /// <summary>
        /// Recursively schedules the next conduit check using DOVirtual.DelayedCall.
        /// </summary>
        private void StartCheckLoop()
        {
            checkLoop?.Kill();
            checkLoop = DOVirtual.DelayedCall(checkDelay, () =>
            {
                if (!isActive)
                    return;

                CheckConduit();
                StartCheckLoop();
            });
        }

        /// <summary>
        /// Casts three raycasts downward (front, center, back) to detect conduit ground.
        /// Publishes OnPlayerEnterConduit / OnPlayerExitConduit on state change.
        /// </summary>
        private void CheckConduit()
        {
            Vector3 center = playerCollider.bounds.center;
            float halfExtentZ = colliderBounds.extents.z;

            Vector3 centerOrigin = center;
            Vector3 frontOrigin = center + transform.forward * halfExtentZ;
            Vector3 backOrigin = center - transform.forward * halfExtentZ;

            bool centerHit = Physics.Raycast(centerOrigin, Vector3.down, rayDistance, conduitLayer);
            bool frontHit = Physics.Raycast(frontOrigin, Vector3.down, rayDistance, conduitLayer);
            bool backHit = Physics.Raycast(backOrigin, Vector3.down, rayDistance, conduitLayer);
            bool isOnConduit = centerHit || frontHit || backHit;

            if (isOnConduit && !isInConduit)
            {
                isInConduit = true;
                EventBus.Publish(new OnPlayerEnterConduit());
            }
            else if (!isOnConduit && isInConduit)
            {
                isInConduit = false;
                EventBus.Publish(new OnPlayerExitConduit());
            }
        }

        /// <summary>
        /// Draws the raycast lines in the Scene view for debugging.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!playerCollider)
                return;

            Vector3 center = playerCollider.bounds.center;
            float halfExtentZ = playerCollider.bounds.extents.z;

            Vector3 centerOrigin = center;
            Vector3 frontOrigin = center + transform.forward * halfExtentZ;
            Vector3 backOrigin = center - transform.forward * halfExtentZ;

            Gizmos.color = Color.blue;
            Gizmos.DrawLine(centerOrigin, centerOrigin + Vector3.down * rayDistance);
            Gizmos.DrawLine(frontOrigin, frontOrigin + Vector3.down * rayDistance);
            Gizmos.DrawLine(backOrigin, backOrigin + Vector3.down * rayDistance);
        }

        /// <summary>
        /// Subscribes to crouch/uncrouch events.
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayerCrouch>(OnCrouch);
            EventBus.Subscribe<OnPlayerUnCrouch>(OnUnCrouch);
        }

        /// <summary>
        /// Unsubscribes from events and stops the check loop.
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayerCrouch>(OnCrouch);
            EventBus.Unsubscribe<OnPlayerUnCrouch>(OnUnCrouch);
            Deactivate();
        }

        /// <summary>
        /// Kills the active tween on destruction to prevent leaks.
        /// </summary>
        private void OnDestroy()
        {
            checkLoop?.Kill();
        }
    }
}