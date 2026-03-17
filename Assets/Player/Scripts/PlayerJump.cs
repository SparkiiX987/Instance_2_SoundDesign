using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    public class PlayerJump : PlayerAbility
    {
        [SerializeField] private float jumpPower = 5f;
        [SerializeField] private int nbJump = 1;
        [SerializeField] private BoxCollider feetCollider;

        private int _jumpsRemaining;
        
        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);
            _jumpsRemaining = nbJump;
        }

        private void OnEnable()
        {
            EventBus.Subscribe<OnPlayerDetectGround>(OnDetectGround);
        }
        
        private void OnDisable()
        {
            EventBus.Unsubscribe<OnPlayerDetectGround>(OnDetectGround);
        }

        public override void Execute(InputAction.CallbackContext _context)
        {
            base.Execute(_context);
            if (!_context.performed || _jumpsRemaining <= 0 || controller.Rb.linearVelocity.y > 0) { return; }

            controller.Rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            _jumpsRemaining--;
        }

        private void OnDetectGround(OnPlayerDetectGround _detectGround)
        {
            _jumpsRemaining = nbJump;
        }
    }
}