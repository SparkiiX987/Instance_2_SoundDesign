using UnityEngine;

public class Switch : Interactible
{
    [SerializeField] private int switchId;

    [SerializeField] private bool isActivated;

    public override void Interact()
    {
        base.Interact();

        isActivated = !isActivated;

        if(isActivated)
        {
            SwitchOn();
        }
        else
        {
            SwitchOff();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Interact();
    }

    public void SwitchOn()
    {
        EventBus.Publish(new OnSwitchOnEvent { switchId = switchId });
    }

    public void SwitchOff()
    {
        EventBus.Publish(new OnSwitchOffEvent { switchId = switchId });
    }
}
