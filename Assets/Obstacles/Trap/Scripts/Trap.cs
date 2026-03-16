using UnityEngine;

public class Trap : Interactible
{
    public override void Interact()
    {
        base.Interact();
        EventBus.Publish(new OnTrapEnter());
    }

    private void OnTriggerEnter(Collider other)
    {
        Interact();
    }
}
