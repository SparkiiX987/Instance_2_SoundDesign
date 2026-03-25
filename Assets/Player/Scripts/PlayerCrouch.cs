using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using Utils;

namespace Player.Scripts
{
    /// <summary>
    /// Handles player crouching. Animates the transform scale
    /// using DOTween for a smooth transition.
    /// Supports hold mode and toggle mode.
    /// </summary>
    public class PlayerCrouch : PlayerAbility
    {
        [Header("Crouch Settings")]
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
            // CapsuleCollider non utilisé actuellement dans cette version.
        }

        /// <summary>
        /// Initializes crouch by retrieving the default values and transform.
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
        /// Executes crouch input according to the selected input mode.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public override void Execute(InputAction.CallbackContext _context)
        {
            if (!CanExecute())
                return;

            if (InputPrefs.IsCrouchToggleEnabled)
            {
                HandleToggleCrouch(_context);
                return;
            }

            HandleHoldCrouch(_context);
        }

        /// <summary>
        /// Handles crouch in hold mode.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        private void HandleHoldCrouch(InputAction.CallbackContext _context)
        {
            if (_context.started)
            {
                SetCrouchState(true);
            }
            else if (_context.canceled)
            {
                if (isInConduit)
                    return;

                SetCrouchState(false);
            }
        }

        /// <summary>
        /// Handles crouch in toggle mode.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        private void HandleToggleCrouch(InputAction.CallbackContext _context)
        {
            if (!_context.started)
                return;

            if (isCrouching)
            {
                if (isInConduit)
                    return;

                SetCrouchState(false);
            }
            else
            {
                SetCrouchState(true);
            }
        }

        /// <summary>
        /// Sets crouch state and updates visuals/events.
        /// </summary>
        /// <param name="_isCrouching">Target crouch state.</param>
        private void SetCrouchState(bool _isCrouching)
        {
            if (isCrouching == _isCrouching)
                return;

            isCrouching = _isCrouching;

            if (isCrouching)
            {
                EventBus.Publish(new OnPlayerCrouch());
                EventBus.Publish(new OnPlayerInputEnter
                {
                    input = TutorialVerifState.crouch
                });

                AnimateCrouch(crouchHeight);
            }
            else
            {
                EventBus.Publish(new OnPlayerUnCrouch());
                AnimateCrouch(defaultHeight);
            }
        }

        /// <summary>
        /// Animates the player's Y scale to the target height using DOTween.
        /// </summary>
        /// <param name="_targetHeight">The target Y scale value.</param>
        private void AnimateCrouch(float _targetHeight)
        {
            crouchTween?.Kill();

            crouchTween = DOTween.To(
                () => playerTransform.localScale.y,
                _scaleY => playerTransform.localScale = new Vector3(
                    playerTransform.localScale.x,
                    _scaleY,
                    playerTransform.localScale.z),
                _targetHeight,
                crouchDuration
            ).SetEase(AnimationHelper.IN_SMOOTH);
        }

        /// <summary>
        /// Forces the player back to standing if currently crouching.
        /// </summary>
        public void ForceUnCrouch()
        {
            if (!isCrouching)
                return;

            SetCrouchState(false);
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