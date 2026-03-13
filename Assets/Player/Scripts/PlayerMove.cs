using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMove : PlayerAbility
{
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 8f;

    private Rigidbody rb;
    private float moveSpeed;
    private bool isRunning = false;

    public override void Init(PlayerController _playerController)
    {
        base.Init(_playerController);
        rb = player.rb;
        moveSpeed = walkSpeed;
    }

    public override void Execute()
    {
        if (player == null || !player.canMove) return;

        moveSpeed = isRunning ? runSpeed : walkSpeed;

        Vector2 moveInput = player.MoveInput;
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        move = move.normalized * moveSpeed;

        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    public void OnMove(InputAction.CallbackContext _context)
    {
        if (player != null)
            player.MoveInput = _context.ReadValue<Vector2>();
    }

    public void OnRun(InputAction.CallbackContext _context)
    {
        if (_context.performed)
            isRunning = true;
        else if (_context.canceled)
            isRunning = false;
    }
}