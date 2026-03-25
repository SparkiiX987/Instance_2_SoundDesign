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

   
    [HideInInspector] public int emitterIndex = 0;

    private float _currentRadius;
    private float _cooldownTimer;
    private Tween _waveTween;


    private FMOD.Studio.EventInstance _loopInstance;

    private void Start()
    {
       
        SonarEmitterManager.Register(this);

      
        if (!sonarLoopSound.IsNull)
        {
            _loopInstance = RuntimeManager.CreateInstance(sonarLoopSound);
            _loopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(gameObject));
            _loopInstance.start();
        }

        
        if (autoTrigger)
            InvokeRepeating(nameof(TriggerWave), Random.Range(0f, interval), interval);
    }

    private void OnDestroy()
    {
        SonarEmitterManager.Unregister(this);

       
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
    }

    public float CurrentRadius => _currentRadius;

    public void TriggerWave()
    {
        if (_cooldownTimer > 0f) return;
        _cooldownTimer = cooldown;
        _currentRadius = 0f;

        float duration = range / speed;
        SonarEmitterManager.PushFireTime(this, Time.time, range, duration);

        _waveTween?.Kill();
        _waveTween = DOTween.To(
                () => _currentRadius,
                r  => _currentRadius = r,
                range, duration
            ).SetEase(Ease.Linear)
            .OnComplete(() => _currentRadius = 0f);
    }
}