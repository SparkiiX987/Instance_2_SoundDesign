using DG.Tweening;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    /// <summary>
    /// Cat jump ability. Press C to ready the jump (disables PlayerJump),
    /// then press Space while C is held to launch an arc jump via DOTween.
    /// Inputs are disabled during the jump and restored on landing.
    /// </summary>
    [RequireComponent(typeof(PlayerJump))]
    public class PlayerLeap : PlayerAbility
    {
        [Header("Leap Settings")]
        [Tooltip("Horizontal distance covered by the leap jump.")]
        [SerializeField] private float distHoriJump = 4f;
        [SerializeField] private float height = 2f;
        [SerializeField] private float angle = 45f;
        [Tooltip("Cooldown in seconds before the player can leap again after landing. 0 = no cooldown.")]
        [SerializeField] private float cooldown = 0f;

        private PlayerJump playerJump;
        private bool isCKeyHeld;
        private bool isJumping;
        private bool isOnCooldown;
        private Tween cooldownTween;

        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);
            playerJump = GetComponent<PlayerJump>();
            Assert.IsNotNull(playerJump, $"[{GetType().Name}] PlayerJump component is required for PlayerCatJump.");
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayerDetectGround>(OnDetectGround);
            EventBus.Subscribe<OnPlayerCrouch>(OnCrouch);
            EventBus.Subscribe<OnPlayerUnCrouch>(OnUnCrouch);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayerDetectGround>(OnDetectGround);
            EventBus.Unsubscribe<OnPlayerCrouch>(OnCrouch);
            EventBus.Unsubscribe<OnPlayerUnCrouch>(OnUnCrouch);
            cooldownTween?.Kill();
        }

        private void OnCrouch(OnPlayerCrouch _)
        {
            isCKeyHeld = true;
            playerJump.enabled = false;
        }

        private void OnUnCrouch(OnPlayerUnCrouch _)
        {
            isCKeyHeld = false;
            if (!isJumping)
                playerJump.enabled = true;
        }

        /// <summary>
        /// Called by the Space key input action.
        /// Triggers the cat jump only if C is held and not already jumping.
        /// </summary>
        public override void Execute(InputAction.CallbackContext _context)
        {
            base.Execute(_context);
            
            if (!_context.performed || !isCKeyHeld || isJumping || isOnCooldown)
                return;

            PerformLeapJump();
        }

        private void PerformLeapJump()
        {
            isJumping = true;

            EventBus.Publish(new OnDisableInput());
            controller.DisableInput();

            float g = Mathf.Abs(Physics.gravity.y);
            float duration = 2f * Mathf.Sqrt(2f * height / g);

            Vector3 targetPosition = controller.transform.position
                + controller.transform.forward * distHoriJump;

            controller.Rb.DOJump(targetPosition, height, 1, duration)
                .SetEase(Ease.Linear);
        }

        private void OnDetectGround(OnPlayerDetectGround _)
        {
            if (!isJumping)
                return;

            isJumping = false;

            if (cooldown > 0f)
            {
                isOnCooldown = true;
                cooldownTween?.Kill();
                cooldownTween = DOVirtual.DelayedCall(cooldown, () => isOnCooldown = false);
            }

            if (!isCKeyHeld)
                playerJump.enabled = true;

            controller.EnableInput();
            //EventBus.Publish(new OnEnableInput());
        }
    }
}