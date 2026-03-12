using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float lookSpeed = 0.3f;
    [SerializeField] private float lookXLimit = 45f;

    private float rotationX = 0f;
    public bool canMove = true;

    public void OnLook(InputAction.CallbackContext context)
    {
        if (!context.performed || !canMove) return;

        Vector2 mouseDelta = context.ReadValue<Vector2>();

        rotationX -= mouseDelta.y * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
        transform.rotation *= Quaternion.Euler(0f, mouseDelta.x * lookSpeed, 0f);
    }
    
    public void Init(PlayerController playerControlller)
    {
        
    }
}