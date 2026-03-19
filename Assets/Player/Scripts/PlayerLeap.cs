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
    [RequireComponent(typeof(PlayerJump), typeof(PlayerCrouch))]
    public class PlayerLeap : PlayerAbility
    {
        [Header("Leap Settings")]
        [Tooltip("Maximum horizontal distance of the leap (meters).")]
        [SerializeField] private float maxDistance = 5f;
        [Tooltip("Maximum maxHeight reached during the leap (meters).")]
        [SerializeField] private float maxHeight = 2f;
        [Tooltip("Launch launchAngle in degrees.")]
        [SerializeField] private float launchAngle = 45f;
        [Tooltip("Cooldown in seconds before the player can leap again after landing. 0 = no leapCooldown.")]
        [SerializeField] private float leapCooldown = 0.5f;

        private PlayerJump playerJump;
        private PlayerMove playerMove;
        private PlayerCrouch playerCrouch;
        private bool isCKeyHeld;
        private bool isJumping;
        private bool isOnCooldown;
        private Tween leapCooldownTween;

        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);
            playerJump = GetComponent<PlayerJump>();
            playerMove = GetComponent<PlayerMove>();
            playerCrouch = GetComponent<PlayerCrouch>();
            
            Assert.IsNotNull(playerJump, $"[{GetType().Name}] PlayerJump component is required for PlayerCatJump.");
            Assert.IsNotNull(playerCrouch, $"[{GetType().Name}] PlayerCrouch component is required for PlayerCatJump.");
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
            leapCooldownTween?.Kill();
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
            if (!CanExecute()) return;

            Vector3 horizontalVelocity = new Vector3(controller.Rb.linearVelocity.x, 0f, controller.Rb.linearVelocity.z);
            if (!_context.performed || !isCKeyHeld || isJumping || isOnCooldown || horizontalVelocity.magnitude > 0.1f)
                return;

            PerformLeapJump();
        }

        private void PerformLeapJump()
        {
            isJumping = true;

            if (playerMove)
                playerMove.enabled = false;
            EventBus.Publish(new OnDisableInput());
            controller.DisableInput();

            float g = Mathf.Abs(Physics.gravity.y);
            float rad = launchAngle * Mathf.Deg2Rad;
            float sinA = Mathf.Sin(rad);
            float cosA = Mathf.Cos(rad);

            // Speed to reach max maxHeight: h = (v*sinθ)² / (2g)  =>  v = sqrt(2gh) / sinθ
            float speedFromHeight = Mathf.Sqrt(2f * g * maxHeight) / sinA;
            // Speed to reach max distance: d = v²*sin(2θ) / g  =>  v = sqrt(dg / sin(2θ))
            float speedFromDist = Mathf.Sqrt(maxDistance * g / Mathf.Sin(2f * rad));
            // Take the smaller so neither limit is exceeded
            float speed = Mathf.Min(speedFromHeight, speedFromDist);

            Vector3 velocity = controller.transform.forward * (speed * cosA)
                             + Vector3.up * (speed * sinA);

            controller.Rb.linearVelocity = velocity;
        }

        private void OnDetectGround(OnPlayerDetectGround _)
        {
            if (!isJumping)
                return;
            
            isJumping = false;

            if (leapCooldown > 0f)
            {
                isOnCooldown = true;
                leapCooldownTween?.Kill();
                leapCooldownTween = DOVirtual.DelayedCall(leapCooldown, () => isOnCooldown = false);
            }

            if (playerMove)
                playerMove.enabled = true;

            if (!isCKeyHeld)
                playerJump.enabled = true;

            controller.EnableInput();
            //EventBus.Publish(new OnEnableInput());
        }
    }
}