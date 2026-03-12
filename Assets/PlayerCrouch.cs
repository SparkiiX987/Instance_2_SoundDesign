using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrouch : PlayerAbility
{
    [SerializeField] private float defaultHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSpeed = 8f;

    private PlayerController player;
    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private Transform modelTransform;
    private bool isCrouching = false;

    void Awake()
    {
        player = GetComponentInParent<PlayerController>();

        if (capsuleCollider != null)
            defaultHeight = capsuleCollider.height;
    }

    public override void Init(PlayerController playerController)
    {
        player = playerController;
    }

    public override void Execute()
    {
        if (player == null || !player.canMove) return;

        float targetHeight = isCrouching ? crouchHeight : defaultHeight;

        if (capsuleCollider != null)
            capsuleCollider.height = Mathf.MoveTowards(capsuleCollider.height, targetHeight, crouchSpeed * Time.deltaTime);

        if (modelTransform != null)
        {
            float scaleY = capsuleCollider.height / defaultHeight;
            modelTransform.localScale = new Vector3(1f, scaleY, 1f);
        }
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (context.performed)
            isCrouching = true;
        else if (context.canceled)
            isCrouching = false;
    }
}