using UnityEngine;

public class CageCollisionDetection : MonoBehaviour
{
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private GameObject Cage;
    [SerializeField] private Rigidbody cageRb;

    private void OnEnable()
    {
        EventBus.Subscribe<OnTutorialFinish>(DetachCage);
    }
    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnTutorialFinish>(DetachCage);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(Cage);
        }
    }
    private void DetachCage(OnTutorialFinish _tutorialFinish)
    {
        cageRb.isKinematic = false;
    }
}
