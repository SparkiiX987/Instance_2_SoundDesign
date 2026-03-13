using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrouch : PlayerAbility
{
    [SerializeField] private float defaultHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSpeed = 8f;

    private Transform playerTransform;

    private CapsuleCollider capsuleCollider;
    private bool isCrouching = false;

    public override void Init(PlayerController _playerController)
    {
        base.Init(_playerController);

        capsuleCollider = player.playerCollider;
        defaultHeight = capsuleCollider.height;
        playerTransform = player.playerTransform;
    }

    public override void Execute()
    {
        if (player == null || !player.canMove) return;

        float targetHeight = isCrouching ? crouchHeight : defaultHeight;

        if (capsuleCollider != null)
            capsuleCollider.height = Mathf.MoveTowards(capsuleCollider.height, targetHeight, crouchSpeed * Time.deltaTime);

        if (playerTransform != null)
        {
            float scaleY = capsuleCollider.height / defaultHeight;
            playerTransform.localScale = new Vector3(1f, scaleY, 1f);
        }
    }

    public void OnCrouch(InputAction.CallbackContext _context)
    {
        if (_context.performed)
            isCrouching = true;
        else if (_context.canceled)
            isCrouching = false;
    }
}