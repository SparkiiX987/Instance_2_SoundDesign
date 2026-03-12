using UnityEngine;

public abstract class PlayerAbility : MonoBehaviour
{
    public abstract void Init(PlayerController playerController);
    public abstract void Execute();
}