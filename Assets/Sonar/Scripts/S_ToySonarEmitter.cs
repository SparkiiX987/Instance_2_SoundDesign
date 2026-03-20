using DG.Tweening;
using UnityEngine;

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

    // Index unique attribue par le manager
    [HideInInspector] public int emitterIndex = 0;

    private float _currentRadius;
    private float _cooldownTimer;
    private Tween _waveTween;

    private void Start()
    {
        // S'enregistrer aupres du manager
        SonarEmitterManager.Register(this);

        if (autoTrigger)
            InvokeRepeating(nameof(TriggerWave), Random.Range(0f, interval), interval);
    }

    private void OnDestroy()
    {
        SonarEmitterManager.Unregister(this);
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