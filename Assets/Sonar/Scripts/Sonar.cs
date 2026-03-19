using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Sonar / écholocalisation.
/// Cri = onde en cône.
/// Mouvement = onde circulaire autour du joueur.
/// </summary>
public class Sonar : MonoBehaviour
{
    [Header("Paramètres")]
    [SerializeField] private SO_SonarSettings settings;

    [Header("Activation clavier")]
    [SerializeField] private KeyCode activationKey = KeyCode.E;

    [Header("Origine du cône")]
    [SerializeField] private Transform coneOrigin;

    [Header("LayerMask")]
    [SerializeField] private LayerMask detectableLayerMask = ~0;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Ondes de mouvement")]
    [SerializeField] private float movementWaveRange = 3f;
    [SerializeField] private float movementWaveInterval = 0.35f;
    [SerializeField] private float movementThreshold = 0.05f;

    // ── Shader IDs
    private static readonly int ID_WaveOrigin       = Shader.PropertyToID("_WaveOrigin");
    private static readonly int ID_WaveRadius       = Shader.PropertyToID("_WaveRadius");
    private static readonly int ID_WaveActive       = Shader.PropertyToID("_WaveActive");
    private static readonly int ID_ConeForward      = Shader.PropertyToID("_ConeForward");
    private static readonly int ID_ConeHalfAngleCos = Shader.PropertyToID("_ConeHalfAngleCos");
    private static readonly int ID_WaveFireTime     = Shader.PropertyToID("_WaveFireTime");
    private static readonly int ID_WaveMaxRadius    = Shader.PropertyToID("_WaveMaxRadius");
    private static readonly int ID_WaveFadeDuration = Shader.PropertyToID("_WaveFadeDuration");

    // ── Etat
    private float _currentWaveRadius;
    private float _previousWaveRadius;
    private float _activeRange;
    private float _cooldownTimer;

    private Vector3 _frozenConeForward;
    private bool _coneIsFrozen;

    private Tween _waveTween;
    private HashSet<IDetectable> _hitObjects = new();

    // ── Mouvement
    private Vector3 lastPosition;
    private float movementTimer;

    // ── Type d’onde
    private bool _isMovementWave = false;

    private void Awake()
    {
        if (coneOrigin == null) coneOrigin = transform;
        lastPosition = transform.position;
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(activationKey) && _cooldownTimer <= 0f)
            TriggerWave();

        HandleMovementWave();
        PushShaderGlobals();
    }

    // ──────────────────────────────
    // CRI
    // ──────────────────────────────

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

    // ──────────────────────────────
    // ONDE DE MOUVEMENT (CERCLE)
    // ──────────────────────────────

    private void HandleMovementWave()
    {
        float moveAmount = Vector3.Distance(transform.position, lastPosition);
        lastPosition = transform.position;

        if (moveAmount < movementThreshold) return;

        movementTimer -= Time.deltaTime;
        if (movementTimer > 0f) return;

        movementTimer = movementWaveInterval;
        _isMovementWave = true;
        EmitMovementWave();
    }

    private void EmitMovementWave()
    {
        EmitWave(movementWaveRange, 0.4f);
    }

    // ──────────────────────────────
    // EMISSION ONDE
    // ──────────────────────────────

    private void EmitWave(float range, float duration)
    {
        _activeRange = range;
        _currentWaveRadius = 0f;
        _previousWaveRadius = 0f;
        _hitObjects.Clear();

        _frozenConeForward = coneOrigin.forward;
        _coneIsFrozen = true;

        Vector3 originPos = coneOrigin.position;
        Vector3 originFwd = coneOrigin.forward;

        _waveTween?.Kill();
        _waveTween = DOTween.To(
            () => _currentWaveRadius,
            radius =>
            {
                _previousWaveRadius = _currentWaveRadius;
                _currentWaveRadius = radius;
                OnWaveStep(originPos, originFwd);
            },
            range,
            duration
        ).SetEase(Ease.OutQuad)
         .OnComplete(() =>
         {
             _currentWaveRadius = 0f;
             _coneIsFrozen = false;
             _isMovementWave = false;
             Shader.SetGlobalFloat(ID_WaveActive, 0f);
         });

        // Push initial shader values pour fade progressif
        Shader.SetGlobalFloat(ID_WaveFireTime, Time.time);
        Shader.SetGlobalFloat(ID_WaveMaxRadius, range);
        Shader.SetGlobalFloat(ID_WaveFadeDuration, duration);
    }

    // ──────────────────────────────
    // DETECTION OBJETS
    // ──────────────────────────────

    private void OnWaveStep(Vector3 originPos, Vector3 originFwd)
    {
        Collider[] hits = Physics.OverlapSphere(originPos, _currentWaveRadius, detectableLayerMask);

        foreach (Collider hit in hits)
        {
            IDetectable detectable = hit.GetComponent<IDetectable>();
            if (detectable == null || !detectable.IsActive()) continue;
            if (_hitObjects.Contains(detectable)) continue;

            Vector3 position = detectable.GetPosition();
            float distance = Vector3.Distance(originPos, position);

            if (distance < _previousWaveRadius) continue;

            Vector3 dir = (position - originPos).normalized;
            if (Physics.Raycast(originPos, dir, distance, obstacleMask)) continue;

            _hitObjects.Add(detectable);

            float proximity = Mathf.Clamp01(1f - (distance / _activeRange));
            detectable.OnProb(proximity);
        }
    }

    // ──────────────────────────────
    // SHADER GLOBALS
    // ──────────────────────────────

    private void PushShaderGlobals()
    {
        Vector3 fwd = _coneIsFrozen ? _frozenConeForward : coneOrigin.forward;

        Shader.SetGlobalVector(ID_WaveOrigin, coneOrigin.position);
        Shader.SetGlobalFloat(ID_WaveRadius, _currentWaveRadius);
        Shader.SetGlobalFloat(ID_WaveActive, _currentWaveRadius > 0f ? 1f : 0f);
        Shader.SetGlobalVector(ID_ConeForward, fwd);

        if (_isMovementWave)
        {
            Shader.SetGlobalFloat(ID_ConeHalfAngleCos, -1000f); // cercle complet
        }
        else if (_coneIsFrozen)
        {
            Shader.SetGlobalFloat(ID_ConeHalfAngleCos, Mathf.Cos(settings.coneHalfAngle * Mathf.Deg2Rad));
        }

        // Pour fade progressif
        Shader.SetGlobalFloat(ID_WaveFireTime, Time.time);
        Shader.SetGlobalFloat(ID_WaveMaxRadius, _activeRange);
        Shader.SetGlobalFloat(ID_WaveFadeDuration, settings.GetWaveDuration(_activeRange));
    }
}