using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    /// <summary>
    /// Handles player jumping. Applies an upward impulse to the Rigidbody
    /// when the jump input is performed. Supports a configurable number of
    /// consecutive jumps, reset on ground detection.
    /// </summary>
    public class PlayerJump : PlayerAbility
    {
        [SerializeField] private float jumpPower = 5f;
        [SerializeField] private int nbJump = 1;
        [SerializeField] private BoxCollider feetCollider;

        private int _jumpsRemaining;
        
        /// <summary>
        /// Initializes jump count from the configured maximum.
        /// </summary>
        /// <param name="_playerController">Reference to the parent PlayerController.</param>
        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);
            _jumpsRemaining = nbJump;
        }

        /// <summary>
        /// Subscribes to ground detection events.
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayerDetectGround>(OnDetectGround);
        }
        
        /// <summary>
        /// Unsubscribes from ground detection events.
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayerDetectGround>(OnDetectGround);
        }

        /// <summary>
        /// Applies an upward impulse if the player can jump, has remaining jumps,
        /// and is not already moving upward.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public override void Execute(InputAction.CallbackContext _context)
        {
            if (!CanExecute()) return;
            if (!_context.performed || _jumpsRemaining <= 0 || controller.Rb.linearVelocity.y > 0) return;

            controller.Rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            _jumpsRemaining--;           
            EventBus.Publish(new OnPlayerInputEnter
            {
                input = "jump"
            });
        }

        /// <summary>
        /// Resets the remaining jump count when the player touches the ground.
        /// </summary>
        /// <param name="_detectGround">The ground detection event data.</param>
        private void OnDetectGround(OnPlayerDetectGround _detectGround)
        {
            _jumpsRemaining = nbJump;
        }
    }
}