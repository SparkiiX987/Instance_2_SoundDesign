using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    Rigidbody rb;
    PlayerMove move;
    PlayerLook look;
    PlayerCrouch crouch;
    PlayerJump jump;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        move = GetComponent<PlayerMove>();
        look = GetComponent<PlayerLook>();
        crouch = GetComponent<PlayerCrouch>();
        
        move.Init(this);
        jump.Init(this);
        look.Init(this);
        crouch.Init(this);
    }
}