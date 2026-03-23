using FMODUnity;
using UnityEngine;
using DG.Tweening;
using FMOD.Studio;

public class AlarmActivate : Interactible
{
    [Header("Alarme")]
    [SerializeField] private int duration = 5;
    [SerializeField] private float loopDuration = 5;


    [Header("Detection")]
    [SerializeField] private string detectableTag = "default";
    [SerializeField] private bool isActiveOnStart = true;

    

    private bool _active;

    // ---------------------------------------------------------------

    private void Awake()
    {
        _active = isActiveOnStart;
    }

    public Vector3 GetPosition() => transform.position;

    private void Start()
    {
        EventBus.Subscribe<AlarmeSetActive>(SetAlarmeOn);
    }
    private void OnDestroy()
    {
        EventBus.Unsubscribe<AlarmeSetActive>(SetAlarmeOn);
    }
    private void SetAlarmeOn(AlarmeSetActive player)
    {
        Interact();
            
            if (duration > 0)
            {
                duration--;
                DOVirtual.DelayedCall(loopDuration, () => SetAlarmeOn(player));
            }
            return;
    }
    private void SetAlarmeOff()
    {

    }

    public void SetActive(bool _value) => _active = _value;
}
