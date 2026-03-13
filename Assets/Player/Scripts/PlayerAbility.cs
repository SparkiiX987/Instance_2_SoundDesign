using UnityEngine;

public abstract class PlayerAbility : MonoBehaviour
{

    protected PlayerController player;

    public virtual void Init(PlayerController _playerController)
    {
        player = _playerController;
    }

    public abstract void Execute();
}