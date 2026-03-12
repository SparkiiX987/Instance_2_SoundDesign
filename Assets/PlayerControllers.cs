using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool canMove = true;

    public Vector2 MoveInput { get; set; }
    public Vector2 LookInput { get; set; }

    [SerializeField] private Rigidbody rb;
    [SerializeField] private PlayerMove move;
    [SerializeField] private PlayerLook look;
    [SerializeField] private PlayerCrouch crouch;
    [SerializeField] private PlayerJump jump;

    void Awake()
    {
        move?.Init(this);
        look?.Init(this);
        crouch?.Init(this);
        jump?.Init(this);
    }

    void Update()
    {
        look.Execute();
        crouch.Execute();
    }

    void FixedUpdate()
    {
        move.Execute();
        jump.Execute();
    }
}