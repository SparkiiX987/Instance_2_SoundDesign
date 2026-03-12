using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float runSpeed = 12f;

    private Rigidbody rb;
    public bool canMove = true;
    public bool isCrouching = false;
    public float crouchSpeed = 3f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        Vector2 moveInput = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        bool isRunning = sprintAction != null && sprintAction.action.IsPressed();

        float currentSpeed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);

        Vector3 forward = transform.forward * moveInput.y;
        Vector3 right   = transform.right   * moveInput.x;
        Vector3 move    = (forward + right).normalized * currentSpeed;

        if (!canMove) move = Vector3.zero;

        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    public void OnMove(InputAction.CallbackContext context) { }
    
    public void Init(PlayerController playerControlller)
    {
        
    }
}