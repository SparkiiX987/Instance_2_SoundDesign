using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    /// <summary>
    /// Handles player running.
    /// Supports hold mode and toggle mode.
    /// </summary>
    [RequireComponent(typeof(PlayerMove))]
    public class PlayerRun : PlayerAbility
    {
        [Header("Run Settings")]
        [SerializeField] private float runSpeedMultiplier = 2f;

        private PlayerMove playerMove;
        private bool isRunning;

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
        /// Executes run input according to the selected input mode.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public override void Execute(InputAction.CallbackContext _context)
        {
            if (!CanExecute())
                return;

            EventBus.Publish(new OnPlayerInputEnter
            {
                input = TutorialVerifState.sprint
            });

            if (InputPrefs.IsSprintToggleEnabled)
            {
                HandleToggleSprint(_context);
                return;
            }

            HandleHoldSprint(_context);
        }

        /// <summary>
        /// Handles sprint in hold mode.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        private void HandleHoldSprint(InputAction.CallbackContext _context)
        {
            if (_context.started)
            {
                isRunning = true;
                playerMove.SetRunning(true, runSpeedMultiplier);
            }
            else if (_context.canceled)
            {
                isRunning = false;
                playerMove.SetRunning(false, runSpeedMultiplier);
            }
        }

        /// <summary>
        /// Handles sprint in toggle mode.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        private void HandleToggleSprint(InputAction.CallbackContext _context)
        {
            if (!_context.started)
                return;

            isRunning = !isRunning;
            playerMove.SetRunning(isRunning, runSpeedMultiplier);
        }

        /// <summary>
        /// Forces sprint off.
        /// </summary>
        public void ForceStopRun()
        {
            isRunning = false;
            playerMove.SetRunning(false, runSpeedMultiplier);
        }
    }
}