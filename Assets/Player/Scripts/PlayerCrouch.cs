using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using Utils;

namespace Player.Scripts
{
    /// <summary>
    /// Handles player crouching. Animates the CapsuleCollider height
    /// and transform scale using DOTween for a smooth transition.
    /// </summary>
    public class PlayerCrouch : PlayerAbility
    {
        [SerializeField] private float defaultHeight = 2f;
        [SerializeField] private float crouchHeight = 1f;
        [SerializeField] private float crouchDuration = 0.25f;

        private Transform playerTransform;
        private CapsuleCollider capsuleCollider;
        private bool isCrouching;
        private Tween crouchTween;

        /// <summary>
        /// Manually assigns the CapsuleCollider used for crouching.
        /// </summary>
        /// <param name="_collider">The player's body CapsuleCollider.</param>
        public void SetCapsuleCollider(CapsuleCollider _collider)
        {
            capsuleCollider = _collider;
        }

        /// <summary>
        /// Initializes crouch by retrieving the collider, default height and transform.
        /// </summary>
        /// <param name="_playerController">Reference to the parent PlayerController.</param>
        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);

            capsuleCollider = controller.BodyCollider;
            defaultHeight = capsuleCollider.height;

            playerTransform = controller.transform;

            Assert.IsNotNull(capsuleCollider, $"[{GetType().Name}] CapsuleCollider reference is null.");
            Assert.IsNotNull(playerTransform, $"[{GetType().Name}] PlayerTransform is null.");
        }

        /// <summary>
        /// Toggles the crouch state on performed and starts a DOTween animation
        /// on the collider height and player scale.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public override void Execute(InputAction.CallbackContext _context)
        {
            base.Execute(_context);

            if (_context.performed)
            {
                isCrouching = !isCrouching;
            }
            else
                return;

            if (isCrouching)
                EventBus.Publish(new OnPlayerCrouch());
            else
                EventBus.Publish(new OnPlayerUnCrouch());

            float targetHeight = isCrouching ? crouchHeight : defaultHeight;

            crouchTween?.Kill();

            crouchTween = DOTween.To(
                () => playerTransform.localScale.y * defaultHeight,
                h =>
                {
                    float scaleY = h / defaultHeight;
                    playerTransform.localScale = new Vector3(1f, scaleY, 1f);
                    capsuleCollider.transform.parent.localScale = new Vector3(scaleY, scaleY, 1f);
                },
                targetHeight,
                crouchDuration
            ).SetEase(AnimationHelper.IN_SMOOTH);
        }

        /// <summary>
        /// Kills the active tween on component destruction to prevent leaks.
        /// </summary>
        private void OnDestroy()
        {
            crouchTween?.Kill();
        }
    }
}