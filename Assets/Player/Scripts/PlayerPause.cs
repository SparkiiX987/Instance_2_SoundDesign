using Player.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPause : PlayerAbility
{
    private OnPaused pauseEvent;

    private PlayerAbility[] abilities;

    public override void Init(PlayerController _playerController)
    {
        base.Init(_playerController);

        abilities = GetComponents<PlayerAbility>();
    }

    public void SetPlayerInputActive(bool _active)
    {
        foreach(PlayerAbility ability in abilities)
        {
            if(ability != this)
            {
                ability.enabled = _active;
            }
        }

        Cursor.lockState = _active ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !_active;
    }

    public override void Execute(InputAction.CallbackContext _context)
    {
        if(!CanExecute()) return;

        EventBus.Publish(pauseEvent);
    }
}
