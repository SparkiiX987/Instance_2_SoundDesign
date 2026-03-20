using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Sonar : MonoBehaviour
{
    [Header("Parametres")]
    [SerializeField] private SO_SonarSettings settings;
    [SerializeField] private KeyCode activationKey = KeyCode.E;
    [SerializeField] private Transform coneOrigin;
    [SerializeField] private LayerMask detectableLayerMask = ~0;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Ondes de mouvement")]
    [SerializeField] private float movementWaveRange    = 3f;
    [SerializeField] private float movementWaveInterval = 0.35f;
    [SerializeField] private float movementThreshold    = 0.05f;

    private static readonly int ID_WaveOrigin       = Shader.PropertyToID("_WaveOrigin");
    private static readonly int ID_WaveRadius       = Shader.PropertyToID("_WaveRadius");
    private static readonly int ID_WaveActive       = Shader.PropertyToID("_WaveActive");
    private static readonly int ID_ConeForward      = Shader.PropertyToID("_ConeForward");
    private static readonly int ID_ConeHalfAngleCos = Shader.PropertyToID("_ConeHalfAngleCos");
    private static readonly int ID_WaveFireTime     = Shader.PropertyToID("_WaveFireTime");
    private static readonly int ID_WaveMaxRadius    = Shader.PropertyToID("_WaveMaxRadius");
    private static readonly int ID_WaveFadeDuration = Shader.PropertyToID("_WaveFadeDuration");

    private float   _currentWaveRadius;
    private float   _previousWaveRadius;
    private float   _activeRange;
    private float   _cooldownTimer;
    private Vector3 _frozenConeForward;
    private bool    _coneIsFrozen;
    private Tween   _waveTween;
    private HashSet<IDetectable> _hitObjects = new();
    private Vector3 _lastPosition;
    private float   _movementTimer;
    private bool    _isMovementWave;

    // Stockes au tir, jamais ecrases ensuite
    private float _waveFireTime;
    private float _waveMaxRadius;
    private float _waveFadeDuration;

    private void Awake()
    {
        if (coneOrigin == null) coneOrigin = transform;
        _lastPosition = transform.position;
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        // Defreeze le cone quand le cooldown est termine
        if (_cooldownTimer <= 0f && !_isMovementWave)
            _coneIsFrozen = false;

        if (Input.GetKeyDown(activationKey) && _cooldownTimer <= 0f)
            TriggerWave();
        HandleMovementWave();
        PushShaderGlobals();
    }

    public void TriggerWave()
    {
        if (_cooldownTimer > 0f) return;
        _isMovementWave = false;
        EmitWave(settings.range, settings.GetWaveDuration(settings.range));
        _cooldownTimer = settings.cooldown;
    }

    public void TriggerWaveWithVolume(float normalizedVolume)
    {
        if (_cooldownTimer > 0f) return;
        _isMovementWave = false;
        float range = settings.GetVoiceRange(normalizedVolume);
        EmitWave(range, settings.GetWaveDuration(range));
        _cooldownTimer = settings.cooldown;
    }

    private void HandleMovementWave()
    {
        float moved = Vector3.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;
        if (moved < movementThreshold) return;
        _movementTimer -= Time.deltaTime;
        if (_movementTimer > 0f) return;
        _movementTimer  = movementWaveInterval;
        _isMovementWave = true;
        EmitWave(movementWaveRange, 0.4f);
    }

    private void EmitWave(float range, float duration)
    {
        _activeRange        = range;
        _currentWaveRadius  = 0f;
        _previousWaveRadius = 0f;
        _hitObjects.Clear();
        _frozenConeForward  = coneOrigin.forward;
        _coneIsFrozen       = true;

        // Figer les valeurs de fade au moment du tir
        _waveFireTime     = Time.time;
        _waveMaxRadius    = range;
        _waveFadeDuration = duration;

        Shader.SetGlobalFloat(ID_WaveFireTime,     _waveFireTime);
        Shader.SetGlobalFloat(ID_WaveMaxRadius,    _waveMaxRadius);
        Shader.SetGlobalFloat(ID_WaveFadeDuration, _waveFadeDuration);

        Vector3 originPos = coneOrigin.position;
        Vector3 originFwd = coneOrigin.forward;

        _waveTween?.Kill();
        _waveTween = DOTween.To(
            () => _currentWaveRadius,
            r =>
            {
                _previousWaveRadius = _currentWaveRadius;
                _currentWaveRadius  = r;
                OnWaveStep(originPos, originFwd);
            },
            range, duration
        ).SetEase(Ease.OutQuad)
         .OnComplete(() =>
         {
             _currentWaveRadius = 0f;
             _isMovementWave    = false;
             Shader.SetGlobalFloat(ID_WaveActive, 0f);
             // _coneIsFrozen reste true jusqu'a la fin du cooldown
         });
    }

    private void OnWaveStep(Vector3 originPos, Vector3 originFwd)
    {
        Collider[] hits = Physics.OverlapSphere(originPos, _currentWaveRadius, detectableLayerMask);
        foreach (Collider hit in hits)
        {
            IDetectable detectable = hit.GetComponent<IDetectable>();
            if (detectable == null || !detectable.IsActive()) continue;
            if (_hitObjects.Contains(detectable)) continue;

            Vector3 position = detectable.GetPosition();
            float   distance = Vector3.Distance(originPos, position);
            if (distance < _previousWaveRadius) continue;

            Vector3 dir = (position - originPos).normalized;
            if (Physics.Raycast(originPos, dir, distance, obstacleMask)) continue;

            _hitObjects.Add(detectable);
            float proximity = Mathf.Clamp01(1f - (distance / _activeRange));
            detectable.OnProb(proximity);
        }
    }

    private void PushShaderGlobals()
    {
        Shader.SetGlobalVector(ID_WaveOrigin,  coneOrigin.position);
        Shader.SetGlobalFloat(ID_WaveRadius,   _currentWaveRadius);
        Shader.SetGlobalFloat(ID_WaveActive,   _currentWaveRadius > 0f ? 1f : 0f);

        if (_isMovementWave)
        {
            // Onde de mouvement : cercle complet
            Shader.SetGlobalFloat(ID_ConeHalfAngleCos, -1000f);
            Shader.SetGlobalVector(ID_ConeForward, coneOrigin.forward);
        }
        else
        {
            // Cri : cone toujours actif, fige a la direction du tir
            // _frozenConeForward est set dans EmitWave et ne change plus jamais
            Shader.SetGlobalVector(ID_ConeForward, _frozenConeForward);
            Shader.SetGlobalFloat(ID_ConeHalfAngleCos,
                Mathf.Cos(settings.coneHalfAngle * Mathf.Deg2Rad));
        }
    }
}