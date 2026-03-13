using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : PlayerAbility
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookSpeed = 2f;
    [SerializeField] private float lookXLimit = 80f;
    private Transform playerTransform;

    private float rotationX = 0f;

    public override void Init(PlayerController _playerController)
    {
        base.Init(_playerController);
        playerTransform = player.playerTransform;
    }

    public override void Execute()
    {
        if (player == null || !player.canMove) return;

        Vector2 lookInput = player.LookInput;

        rotationX -= lookInput.y * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        if (playerCamera != null)
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);

        if (playerTransform != null)
            playerTransform.Rotate(Vector3.up * lookInput.x * lookSpeed);
    }

    public void OnLook(InputAction.CallbackContext _context)
    {
        if (player != null)
            player.LookInput = _context.ReadValue<Vector2>();
    }
}