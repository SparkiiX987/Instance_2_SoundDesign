using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    /// <summary>
    /// Handles camera and player rotation based on mouse/stick input.
    /// Applies a vertical clamp to limit the viewing angle.
    /// </summary>
    public class PlayerLook : PlayerAbility
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private float lookXLimit = 80f;

        private float rotationX = 0f;

        /// <summary>
        /// Initializes look by verifying that the camera is assigned.
        /// </summary>
        /// <param name="_playerController">Reference to the parent PlayerController.</param>
        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);
            Assert.IsNotNull(playerCamera, $"[{GetType().Name}] PlayerCamera reference is null.");
        }

        /// <summary>
        /// Applies vertical rotation (camera) and horizontal rotation (transform) based on input.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public override void Execute(InputAction.CallbackContext _context)
        {
            base.Execute(_context);

            Vector2 lookInput = _context.ReadValue<Vector2>();

            rotationX -= lookInput.y * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
            transform.Rotate(Vector3.up * lookInput.x * lookSpeed);
        }
    }
}