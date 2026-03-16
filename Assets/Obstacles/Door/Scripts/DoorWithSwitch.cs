using UnityEngine;

public class DoorWithSwitch : Door
{
    [SerializeField] private int connectedSwitchId;

    private void Start()
    {
        EventBus.Subscribe<OnSwitchOnEvent>(OnSwitchOn);
        EventBus.Subscribe<OnSwitchOffEvent>(OnSwitchOff);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnSwitchOnEvent>(OnSwitchOn);
        EventBus.Unsubscribe<OnSwitchOffEvent>(OnSwitchOff);
    }
    private void OnSwitchOn(OnSwitchOnEvent evt)
    {
        if (evt.switchId != connectedSwitchId)
            return;

        Open();
    }

    private void OnSwitchOff(OnSwitchOffEvent evt)
    {
        if (evt.switchId != connectedSwitchId)
            return;

        Close();
    }

    public override void Interact()
    {
        base.Interact();
    }
}
