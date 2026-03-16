using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player.Scripts
{
    /// <summary>
    /// Handles player movement. Stores the input direction and applies
    /// velocity to the Rigidbody every FixedUpdate. Supports smooth
    /// speed transitions via DOTween (walk/run).
    /// </summary>
    public class PlayerMove : PlayerAbility
    {
        [SerializeField] private float walkSpeed = 4f;
        [SerializeField] private float accelerationDuration = 0.2f;

        private float moveSpeed;
        private bool isRunning;
        private Tween moveTween;
        private Vector2 inputDirection;
        
        /// <summary>
        /// Initializes the movement speed to the walk speed.
        /// </summary>
        /// <param name="_playerController">Reference to the parent PlayerController.</param>
        public override void Init(PlayerController _playerController)
        {
            base.Init(_playerController);
            moveSpeed = walkSpeed;
        }

        /// <summary>
        /// Records the input direction received from the InputAction.
        /// </summary>
        /// <param name="_context">The InputAction callback context.</param>
        public override void Execute(InputAction.CallbackContext _context)
        {
            base.Execute(_context);
            
            inputDirection = _context.ReadValue<Vector2>();
        }

        /// <summary>
        /// Applies velocity to the Rigidbody based on the input direction
        /// and current speed, preserving the vertical component.
        /// </summary>
        private void FixedUpdate()
        {
            if (!controller.Rb)
                return;

            Vector3 targetVelocity = (transform.right * inputDirection.x + transform.forward * inputDirection.y).normalized * moveSpeed;
            
            controller.Rb.linearVelocity = new Vector3(targetVelocity.x, controller.Rb.linearVelocity.y, targetVelocity.z);
        }

        /// <summary>
        /// Kills the active tween on component destruction.
        /// </summary>
        private void OnDestroy()
        {
            moveTween?.Kill();
        }
        
        /// <summary>
        /// Changes the movement speed, with an optional smooth transition via DOTween.
        /// </summary>
        /// <param name="_speed">Target speed.</param>
        /// <param name="_instant">If true, applies the speed immediately without transition.</param>
        public void SetMoveSpeed(float _speed, bool _instant = false)
        {
            if (_instant)
                moveSpeed = _speed;
            else
                DOTween.To(() => moveSpeed, v => moveSpeed = v, _speed, accelerationDuration)
                    .SetEase(Ease.OutSine);
        }

        /// <summary>
        /// Enables or disables running. Interpolates speed between walk and run via DOTween.
        /// </summary>
        /// <param name="_value">True to run, false to walk.</param>
        /// <param name="_runSpeedMultiplier">Walk speed multiplier for running.</param>
        public void SetRunning(bool _value, float _runSpeedMultiplier = 2f)
        {
            isRunning = _value;
            
            float targetSpeed = isRunning ? walkSpeed * _runSpeedMultiplier : walkSpeed;
            DOTween.To(() => moveSpeed, v => moveSpeed = v, targetSpeed, accelerationDuration)
                .SetEase(Ease.OutSine);
        }
        
        /// <summary>
        /// Returns the current running state.
        /// </summary>
        /// <returns>True if the player is currently running.</returns>
        public bool IsRunning() => isRunning;

        /// <summary>
        /// Returns the current movement speed.
        /// </summary>
        /// <returns>The player's current speed.</returns>
        public float GetCurrentSpeed()
        {
            return moveSpeed;
        }
    }
}