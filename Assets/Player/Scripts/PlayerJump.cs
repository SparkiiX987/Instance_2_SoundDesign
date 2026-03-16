using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    /// <summary>
    /// Handles player jumping. Applies a vertical impulse through the Rigidbody
    /// only when the player is grounded.
    /// </summary>
    public class PlayerJump : PlayerAbility
    {
        [SerializeField] private float jumpPower = 5f;
        [SerializeField] private LayerMask groundMask;

        /// <summary>
        /// Executes the jump if the action is performed and the player is grounded.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public override void Execute(InputAction.CallbackContext _context)
        {
            base.Execute(_context);
            
            if (_context.performed)
            {
                if (!CheckGrounded())
                    return; 
                
                controller.Rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            }
        }

        /// <summary>
        /// Checks whether the player is touching the ground using a downward SphereCast.
        /// </summary>
        /// <returns>True if the player is grounded, false otherwise.</returns>
        private bool CheckGrounded()
        {
            /*if (controller)
                Debug.Log("Checking grounded with radius: " + (controller.BodyCollider ? controller.BodyCollider.radius : 0.3f) + " and distance: " + ((controller.BodyCollider ? controller.BodyCollider.height / 2f : 1f) + 0.1f));*/
            
            float radius = controller.BodyCollider ? controller.BodyCollider.radius : 0.3f;
            float distance = (controller.BodyCollider ? controller.BodyCollider.height / 2f : 1f) + 0.1f;
            return Physics.SphereCast(transform.position, radius * 0.9f, Vector3.down, out _, distance, groundMask);
        }
    }
}