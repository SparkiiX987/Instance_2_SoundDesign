using DG.Tweening;
using UnityEngine;

public class S_ToySonarEmitter : MonoBehaviour
{
    [Header("Sonar Settings")]
    [SerializeField] private float range    = 15f;
    [SerializeField] private float duration = 2f;
    [SerializeField] private float cooldown = 1f;
    [SerializeField] private Color waveColor = Color.red;

    [Header("Auto Trigger")]
    [SerializeField] private bool  autoTrigger = true;
    [SerializeField] private float interval    = 2f;

    private float _currentRadius;
    private float _cooldownTimer;
    private Tween _waveTween;

    // Shader IDs
    private static readonly int ID_EnemyOrigin       = Shader.PropertyToID("_EnemyWaveOrigin");
    private static readonly int ID_EnemyRadius       = Shader.PropertyToID("_EnemyWaveRadius");
    private static readonly int ID_EnemyActive       = Shader.PropertyToID("_EnemyWaveActive");
    private static readonly int ID_EnemyColor        = Shader.PropertyToID("_EnemyRingColor");
    private static readonly int ID_EnemyFireTime     = Shader.PropertyToID("_EnemyWaveFireTime");
    private static readonly int ID_EnemyMaxRadius    = Shader.PropertyToID("_EnemyWaveMaxRadius");
    private static readonly int ID_EnemyFadeDuration = Shader.PropertyToID("_EnemyWaveFadeDuration");

    private void Start()
    {
        if (autoTrigger)
            InvokeRepeating(nameof(TriggerWave), 0f, interval);
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        Shader.SetGlobalVector(ID_EnemyOrigin, transform.position);
        Shader.SetGlobalFloat(ID_EnemyRadius,  _currentRadius);
        Shader.SetGlobalFloat(ID_EnemyActive,  _currentRadius > 0f ? 1f : 0f);
        Shader.SetGlobalColor(ID_EnemyColor,   waveColor);
    }

    public void TriggerWave()
    {
        if (_cooldownTimer > 0f) return;
        _cooldownTimer = cooldown;

        _waveTween?.Kill();
        _currentRadius = 0f;

        // Variables correctes pour le fade ennemi dans le shader
        Shader.SetGlobalFloat(ID_EnemyFireTime,     Time.time);
        Shader.SetGlobalFloat(ID_EnemyMaxRadius,    range);
        Shader.SetGlobalFloat(ID_EnemyFadeDuration, duration);

        _waveTween = DOTween.To(
            () => _currentRadius,
            r  => _currentRadius = r,
            range,
            duration
        )
        .SetEase(Ease.Linear)
        .OnComplete(() => _currentRadius = 0f);
    }
}
