using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    /// <summary>
    /// Handles player running. Enables/disables running on PlayerMove
    /// based on the Sprint key press/release.
    /// </summary>
    [RequireComponent(typeof(PlayerMove))]
    public class PlayerRun : PlayerAbility
    {
        [SerializeField] private float runSpeedMultiplier = 2f;
        private PlayerMove playerMove;
        
        /// <summary>
        /// Initializes the reference to the required PlayerMove to modify speed.
        /// </summary>
        /// <param name="_playerController">Reference to the parent PlayerController.</param>
        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);
            playerMove = GetComponent<PlayerMove>();
        }

        /// <summary>
        /// Enables running on started (key pressed) and disables it on canceled (key released).
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public override void Execute(InputAction.CallbackContext _context)
        {
            base.Execute(_context);

            if (_context.started)
                playerMove.SetRunning(true, runSpeedMultiplier);
            else if (_context.canceled)
                playerMove.SetRunning(false, runSpeedMultiplier);
        }
    }
}