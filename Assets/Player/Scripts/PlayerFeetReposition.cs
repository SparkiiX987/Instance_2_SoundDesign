using UnityEngine;

/// <summary>
/// Repositions the player's feet transform when crouching or uncrouching.
/// Listens to OnPlayerCrouch / OnPlayerUnCrouch events and moves the
/// local position accordingly.
/// </summary>
public class PlayerFeetReposition : MonoBehaviour
{
    [SerializeField] private Vector3 crouchPos;
    [SerializeField] private Vector3 unCrouchPos;

    /// <summary>
    /// Subscribes to crouch and uncrouch events.
    /// </summary>
    private void Start()
    {
        EventBus.Subscribe<OnPlayerCrouch>(PlayerCrouch);
        EventBus.Subscribe<OnPlayerUnCrouch>(PlayerUnCrouch);
    }

    /// <summary>
    /// Unsubscribes from crouch and uncrouch events.
    /// </summary>
    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnPlayerCrouch>(PlayerCrouch);
        EventBus.Unsubscribe<OnPlayerUnCrouch>(PlayerUnCrouch);
    }

    /// <summary>
    /// Moves the feet to the crouch position.
    /// </summary>
    /// <param name="_playerCrouch">The crouch event data.</param>
    private void PlayerCrouch(OnPlayerCrouch _playerCrouch)
    {
        Debug.Log("crouch");
        transform.localPosition = crouchPos;
    }

    /// <summary>
    /// Moves the feet back to the standing position.
    /// </summary>
    /// <param name="_playerUnCrouch">The uncrouch event data.</param>
    private void PlayerUnCrouch(OnPlayerUnCrouch _playerUnCrouch)
    {
        Debug.Log("UnCrouch");
        transform.localPosition = unCrouchPos;
    }
}
