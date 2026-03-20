using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    /// <summary>
    /// Abstract base class for all player abilities.
    /// Each ability is linked to a PlayerController and receives inputs through Execute.
    /// </summary>
    public abstract class PlayerAbility : MonoBehaviour
    {
        protected PlayerController controller;

        /// <summary>
        /// Initializes the ability by assigning its PlayerController.
        /// </summary>
        /// <param name="_playerController">Reference to the parent PlayerController.</param>
        public virtual void Init(PlayerController _playerController)
        {
            controller = _playerController;
            Assert.IsNotNull(controller, $"[{GetType().Name}] PlayerController reference is null.");
        }

        /// <summary>
        /// Callback executed on an input event. Checks that inputs are enabled before proceeding.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public virtual void Execute(InputAction.CallbackContext _context) { }

        /// <summary>
        /// Checks whether this ability can be executed (enabled and input valid).
        /// </summary>
        /// <returns>True if the ability is enabled and the controller accepts input.</returns>
        protected bool CanExecute()
        {
            if (!enabled || !controller.IsInputValid)
            {
                return false;
            }
            
            return true;
        }
    }
}