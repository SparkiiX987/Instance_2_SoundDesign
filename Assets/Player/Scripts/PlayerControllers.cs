using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool canMove = true;

    public Vector2 MoveInput { get; set; }
    public Vector2 LookInput { get; set; }

    public Rigidbody rb;
    public CapsuleCollider playerCollider;
    public Transform playerTransform;

    private PlayerLook look;
    private PlayerMove move;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        PlayerAbility[] playerAbilities = GetComponentsInChildren<PlayerAbility>();
        foreach(PlayerAbility ability in playerAbilities)
        {
            ability.Init(this);
            if(ability is PlayerLook)
            {
                look = (PlayerLook)ability;
            }
            if (ability is PlayerMove)
            {
                move = (PlayerMove)ability;
            }
        }
    }

    void Update()
    {
        look.Execute();
    }

    void FixedUpdate()
    {
        move.Execute();
    }
}