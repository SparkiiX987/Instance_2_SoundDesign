using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    public class PlayerJump : PlayerAbility
    {
        [SerializeField] private float jumpPower = 5f;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private int nbJump = 1;

        private int _jumpsRemaining;

        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);
            _jumpsRemaining = nbJump;

        }

        public override void Execute(InputAction.CallbackContext _context)
        {
            base.Execute(_context);
            if (!_context.performed || _jumpsRemaining <= 0) { return; }

            controller.Rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            _jumpsRemaining--;
        }

        private void OnTriggerEnter(Collider _other)
        {
            if ((groundMask & (1 << _other.gameObject.layer)) == 0) { return; }
            _jumpsRemaining = nbJump;
        }
    }
}