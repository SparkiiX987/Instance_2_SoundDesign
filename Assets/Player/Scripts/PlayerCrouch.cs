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
        [SerializeField] private float crouchDuration = 0.25f;
        [SerializeField, Tooltip("Par rapport à la scale du transform"), Range(1f, 5f)]
        private float crouchPercentage = 2f;

        private float defaultHeight;
        private float crouchHeight;

        private Transform playerTransform;
        private bool isCrouching;
        private bool isInConduit;
        private Tween crouchTween;

        /// <summary>
        /// Manually assigns the CapsuleCollider used for crouching.
        /// </summary>
        /// <param name="_collider">The player's body CapsuleCollider.</param>
        public void SetCapsuleCollider(CapsuleCollider _collider)
        {
            //capsuleCollider = _collider;
        }

        /// <summary>
        /// Initializes crouch by retrieving the collider, default height and transform.
        /// </summary>
        /// <param name="_playerController">Reference to the parent PlayerController.</param>
        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);

            playerTransform = controller.transform;
            defaultHeight = playerTransform.localScale.y;
            
            crouchHeight = defaultHeight / crouchPercentage;

            Assert.IsNotNull(playerTransform, $"[{GetType().Name}] PlayerTransform is null.");
        }

        /// <summary>
        /// Toggles the crouch state on performed and starts a DOTween animation
        /// on the collider height and player scale.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public override void Execute(InputAction.CallbackContext _context)
        {
            if (!CanExecute())
                return;

            if (_context.started)
            {
                isCrouching = true;
                EventBus.Publish(new OnPlayerCrouch());
                AnimateCrouch(crouchHeight);
            }
            else if (_context.canceled)
            {
                if (isInConduit)
                    return;

                isCrouching = false;
                EventBus.Publish(new OnPlayerUnCrouch());
                AnimateCrouch(defaultHeight);
            }
        }

        /// <summary>
        /// Animates the player's Y scale to the target height using DOTween.
        /// </summary>
        /// <param name="targetHeight">The target Y scale value.</param>
        private void AnimateCrouch(float targetHeight)
        {
            crouchTween?.Kill();
            crouchTween = DOTween.To(
                () => playerTransform.localScale.y,
                scaleY => playerTransform.localScale = new Vector3(playerTransform.localScale.x, scaleY, playerTransform.localScale.z),
                targetHeight,
                crouchDuration
            ).SetEase(AnimationHelper.IN_SMOOTH);
        }

        /// <summary>
        /// Forces the player back to standing if currently crouching.
        /// </summary>
        private void ForceUnCrouch()
        {
            if (!isCrouching)
                return;

            isCrouching = false;
            EventBus.Publish(new OnPlayerUnCrouch());
            AnimateCrouch(defaultHeight);
        }

        /// <summary>
        /// Manually sets the conduit state. When true, the player cannot uncrouch.
        /// </summary>
        /// <param name="_value">Whether the player is inside a conduit.</param>
        public void SetInConduit(bool _value)
        {
            isInConduit = _value;
        }

        /// <summary>
        /// Called when the player enters a conduit. Locks crouch state.
        /// </summary>
        private void OnEnterConduit(OnPlayerEnterConduit _event)
        {
            isInConduit = true;
        }

        /// <summary>
        /// Called when the player exits a conduit. Unlocks crouch and forces uncrouch.
        /// </summary>
        private void OnExitConduit(OnPlayerExitConduit _event)
        {
            isInConduit = false;
            ForceUnCrouch();
        }

        /// <summary>
        /// Subscribes to conduit enter/exit events.
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayerEnterConduit>(OnEnterConduit);
            EventBus.Subscribe<OnPlayerExitConduit>(OnExitConduit);
        }

        /// <summary>
        /// Unsubscribes from conduit events.
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayerEnterConduit>(OnEnterConduit);
            EventBus.Unsubscribe<OnPlayerExitConduit>(OnExitConduit);
        }

        /// <summary>
        /// Kills the active tween on destruction to prevent leaks.
        /// </summary>
        private void OnDestroy()
        {
            crouchTween?.Kill();
        }
    }
}