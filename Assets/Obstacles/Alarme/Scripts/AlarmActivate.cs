using FMODUnity;
using UnityEngine;
using DG.Tweening;
using FMOD.Studio;

public class AlarmActivate : MonoBehaviour
{
    [Header("Alarme")]
    [SerializeField] private int duration = 5;
    [SerializeField] private float loopDuration = 5;


    [Header("Detection")]
    [SerializeField] private string detectableTag = "default";
    [SerializeField] private bool isActiveOnStart = true;

    [Header("Son FMOD")]
    [Tooltip("Evenement FMOD joue quand l'onde radar touche cet objet.")]
    [SerializeField] private EventReference sound;

    [Header("Volume selon la proximite")]
    [Range(0f, 1f)][SerializeField] private float volumeMin = 0.1f;
    [Range(0f, 1f)][SerializeField] private float targetVolume = 0.1f;
    [Range(0f, 1f)][SerializeField] private float volumeMax = 1.0f;

    [Header("Pitch selon la proximite")]
    [Range(0.1f, 3f)][SerializeField] private float pitchMin = 0.5f;
    [Range(0.1f, 3f)][SerializeField] private float targetPitch = 0.5f;
    [Range(0.1f, 3f)][SerializeField] private float pitchMax = 2.0f;

    [Header("Enveloppe sonore")]
    [SerializeField] private float fadeInDuration = 0.08f;
    [SerializeField] private float sustainDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.5f;

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
        if (!sound.IsNull)
        {
            EventInstance instance = RuntimeManager.CreateInstance(sound);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            instance.setPitch(targetPitch);
            instance.setVolume(0f);
            instance.start();

            float currentVolume = 0f;
            DOTween.Sequence()
                .Append(DOTween.To(
                    () => currentVolume,
                    v => { currentVolume = v; instance.setVolume(v); },
                    targetVolume,
                    fadeInDuration).SetEase(Ease.OutQuad))
                .AppendInterval(sustainDuration)
                .Append(DOTween.To(
                    () => currentVolume,
                    v => { currentVolume = v; instance.setVolume(v); },
                    0f,
                    fadeOutDuration).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    instance.release();
                });
            if (duration > 0)
            {
                duration--;
                DOVirtual.DelayedCall(loopDuration, () => SetAlarmeOn(player));
            }
            return;

        }
    }
    private void SetAlarmeOff()
    {

    }

    public void SetActive(bool _value) => _active = _value;
}
