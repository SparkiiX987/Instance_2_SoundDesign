using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerJump : MonoBehaviour
{
    [SerializeField] private float jumpPower = 5f;
    [SerializeField] private LayerMask groundMask;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool jumpRequested = false;
    public bool canMove = true;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
    }

    void FixedUpdate()
    {
        if (jumpRequested && IsGrounded())
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
        }

        jumpRequested = false;
    }

    private bool IsGrounded()
    {
        float radius = capsuleCollider != null ? capsuleCollider.radius : 0.3f;
        float distance = (capsuleCollider != null ? capsuleCollider.height / 2f : 1f) + 0.1f;
        return Physics.SphereCast(transform.position, radius * 0.9f, Vector3.down, out _, distance, groundMask);
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && canMove && IsGrounded())
        {
            jumpRequested = true;
        }
    }

    public void Init(PlayerController playerController)
    {
        
    }
    
}