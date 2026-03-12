using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerJump : PlayerAbility
{
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private LayerMask groundMask;

    private PlayerController player;
    [SerializeField] private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool canJump = true;
    private bool isGrounded = false;
    private bool jumpRequested = false;

    public override void Init(PlayerController playerController)
    {
        player = playerController;
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    public override void Execute()
    {
        isGrounded = CheckGrounded();
        canJump = isGrounded;

        if (jumpRequested && canJump)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        }

        jumpRequested = false;
    }

    private bool CheckGrounded()
    {
        float radius = capsuleCollider != null ? capsuleCollider.radius : 0.3f;
        float distance = (capsuleCollider != null ? capsuleCollider.height / 2f : 1f) + 0.1f;
        return Physics.SphereCast(transform.position, radius * 0.9f, Vector3.down, out _, distance, groundMask);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && player != null && player.canMove && canJump)
            jumpRequested = true;
    }
}