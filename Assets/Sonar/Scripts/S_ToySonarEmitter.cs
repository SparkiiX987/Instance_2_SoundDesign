using DG.Tweening;
using UnityEngine;
using FMODUnity;

public class S_ToySonarEmitter : MonoBehaviour
{
    [Header("Sonar Settings")]
    [SerializeField] private float range    = 15f;
    [SerializeField] private float speed    = 10f;
    [SerializeField] private float cooldown = 3f;
    [SerializeField] private Color waveColor = Color.red;

    [Header("Auto Trigger")]
    [SerializeField] private bool  autoTrigger = true;
    [SerializeField] private float interval    = 3f;

    [Header("FMOD Sound")]
    [SerializeField] private EventReference sonarLoopSound;


    [HideInInspector] public int emitterIndex = -1;

    private float _currentRadius;
    private float _cooldownTimer;
    private float _autoTimer;
    private Tween _waveTween;


    private FMOD.Studio.EventInstance _loopInstance;

    /// <summary>Temps de propagation de l'onde sur toute sa portee.</summary>
    private float TravelDuration => range / Mathf.Max(speed, 0.01f);

    /// <summary>
    /// Periode reelle entre deux tirs automatiques. Elle ne peut pas
    /// descendre sous la duree de propagation, sinon l'onde precedente est
    /// tuee en plein vol et repart de zero ; ni sous le cooldown, qui
    /// avalerait le tir sans rien declencher.
    /// </summary>
    private float AutoPeriod => Mathf.Max(Mathf.Max(interval, cooldown), TravelDuration);

    private void Start()
    {
        SonarEmitterManager.Register(this);

        if (!sonarLoopSound.IsNull)
        {
            _loopInstance = RuntimeManager.CreateInstance(sonarLoopSound);
            _loopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            _loopInstance.start();
        }

        // Dephasage initial pour que les emetteurs ne tirent pas en choeur.
        _autoTimer = Random.Range(0f, AutoPeriod);
    }

    private void OnDestroy()
    {
        SonarEmitterManager.Unregister(this);

        _waveTween?.Kill();

        if (_loopInstance.isValid())
        {
            _loopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            _loopInstance.release();
        }
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;
        SonarEmitterManager.PushEmitter(this, _currentRadius, waveColor);

        if (!autoTrigger) { return; }

        // Un seul chrono, au lieu d'un InvokeRepeating double-barre par le
        // cooldown : les deux se desynchronisaient et une tick sur deux
        // etait avalee, donnant un rythme irregulier.
        _autoTimer -= Time.deltaTime;
        if (_autoTimer <= 0f)
        {
            _autoTimer = AutoPeriod;
            TriggerWave();
        }
    }

    public float CurrentRadius => _currentRadius;

    public void TriggerWave()
    {
        if (_cooldownTimer > 0f) { return; }

        float duration = TravelDuration;
        _cooldownTimer = Mathf.Max(cooldown, duration);
        _currentRadius = 0f;

        SonarEmitterManager.PushFireTime(this, Time.timeSinceLevelLoad, range, duration);

        _waveTween?.Kill();
        _waveTween = DOTween.To(
                () => _currentRadius,
                r  => _currentRadius = r,
                range, duration
            ).SetEase(Ease.Linear)
            .OnComplete(() => _currentRadius = 0f);
    }
}
