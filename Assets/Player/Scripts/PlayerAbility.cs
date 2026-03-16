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
        public virtual void Execute(InputAction.CallbackContext _context)
        {
            //Debug.Log("Executing ability: " + GetType().Name);
            if (!controller.IsInputValid)
                return;
        }
    }
}