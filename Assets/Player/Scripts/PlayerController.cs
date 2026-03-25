using UnityEngine;
using System.Linq;
    
namespace Player.Scripts
{
   
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Collider bodyCollider;
        private Rigidbody rb;
        private bool canInput;

       
        void Awake()
        {
            gameObject.SetActive(true);
            
            rb = GetComponent<Rigidbody>();
            
            PlayerAbility[] abilities = GetComponents<PlayerAbility>();
            abilities.ToList().ForEach(ability =>
            {
                ability.Init(this);
                
               
            });
            
            EnableInput();
        }
        
       
        public void EnableInput()
        {
            canInput = true;
        }

        
        public void DisableInput()
        {
            canInput = false;
            Debug.Log("Player input disabled.");
        }
        
       
        public bool IsInputValid => canInput;
        
       
        public Rigidbody Rb => rb;

        
        public Collider BodyCollider => bodyCollider;
    }
}
