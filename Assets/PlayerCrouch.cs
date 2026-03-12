using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCrouch : MonoBehaviour
{
    [SerializeField] private float defaultHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSpeed = 3f;

    private CapsuleCollider capsuleCollider;
    private PlayerMove playerMove;
    public bool canMove = true;

    void Start()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        playerMove = GetComponent<PlayerMove>();
    }

    public void OnCrouch(InputAction.CallbackContext context)
    {
        if (!canMove || capsuleCollider == null) return;

        if (context.performed)
        {
            capsuleCollider.height = crouchHeight;
            capsuleCollider.center = Vector3.up * (crouchHeight / 2f);
            if (playerMove != null) playerMove.isCrouching = true;
        }
        else if (context.canceled)
        {
            capsuleCollider.height = defaultHeight;
            capsuleCollider.center = Vector3.up * (defaultHeight / 2f);
            if (playerMove != null) playerMove.isCrouching = false;
        }
    }
    
    public void Init(PlayerController playerControlller)
    {
        
    }
}