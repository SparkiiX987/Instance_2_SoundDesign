using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : PlayerAbility
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform playerBody;
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] private float lookXLimit = 80f;

    private PlayerController player;
    private float rotationX = 0f;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public override void Init(PlayerController playerController)
    {
        player = playerController;
    }

    public override void Execute()
    {
        if (player == null || !player.canMove) return;

        Vector2 lookInput = player.LookInput;

        rotationX -= lookInput.y * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * lookInput.x * lookSpeed);
    }

    public void OnLook(InputAction.CallbackContext context)
    {
        if (player != null)
            player.LookInput = context.ReadValue<Vector2>();
    }
}