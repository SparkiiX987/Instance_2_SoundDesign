using UnityEngine;

public class PlayerFeetReposition : MonoBehaviour
{
    [SerializeField] private Vector3 crouchPos;
    [SerializeField] private Vector3 unCrouchPos;
    private void Start()
    {
        EventBus.Subscribe<OnPlayerCrouch>(PlayerCrouch);
        EventBus.Subscribe<OnPlayerUnCrouch>(PlayerUnCrouch);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnPlayerCrouch>(PlayerCrouch);
        EventBus.Unsubscribe<OnPlayerUnCrouch>(PlayerUnCrouch);
    }
    private void PlayerCrouch(OnPlayerCrouch _playerCrouch)
    {
        Debug.Log("crouch");
        transform.localPosition = crouchPos;
    }

    private void PlayerUnCrouch(OnPlayerUnCrouch _playerUnCrouch)
    {
        Debug.Log("UnCrouch");
        transform.localPosition = unCrouchPos;
    }
}
