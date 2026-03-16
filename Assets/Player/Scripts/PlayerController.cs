using UnityEngine;
using System.Linq;
    
namespace Player.Scripts
{
    /// <summary>
    /// Main player controller. Initializes all PlayerAbility components
    /// and manages input state (enable/disable).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private CapsuleCollider bodyCollider;
        private Rigidbody rb;
        private bool canInput;

        /*private PlayerLook look;
        private PlayerMove move;*/

        /// <summary>
        /// Retrieves required components and initializes all abilities attached to the GameObject.
        /// </summary>
        void Awake()
        {
            gameObject.SetActive(true);
            
            rb = GetComponent<Rigidbody>();
            
            PlayerAbility[] abilities = GetComponents<PlayerAbility>();
            abilities.ToList().ForEach(ability =>
            {
                ability.Init(this);
                
                /*if (ability is PlayerCrouch crouch)
                    crouch.SetCapsuleCollider(bodyCollider);*/
            });
            
            EnableInput();
        }
        
        /// <summary>
        /// Enables player input reception.
        /// </summary>
        public void EnableInput()
        {
            canInput = true;
        }

        /// <summary>
        /// Disables player input reception.
        /// </summary>
        public void DisableInput()
        {
            canInput = false;
        }
        
        /// <summary>Whether the player inputs are currently active.</summary>
        public bool IsInputValid => canInput;
        
        /// <summary>Reference to the player's Rigidbody.</summary>
        public Rigidbody Rb => rb;

        /// <summary>Reference to the player's body CapsuleCollider.</summary>
        public CapsuleCollider BodyCollider => bodyCollider;
    }
}
