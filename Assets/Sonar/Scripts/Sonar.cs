using System.Collections.Generic;
using DG.Tweening;
using Player.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

public class Sonar : PlayerAbility
{
    [Header("Parametres")]
    [SerializeField] private SO_SonarSettings settings;

    [Header("Origine du cone")]
    [SerializeField] private Transform coneOrigin;

    [Header("LayerMask")]
    [SerializeField] private LayerMask detectableLayerMask = ~0;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Debug Raycasts")]
    [SerializeField] private bool  showRaycasts             = true;
    [SerializeField] private float raycastDrawDuration      = 0.5f;
    [SerializeField] private Color raycastHitColor          = Color.cyan;
    [SerializeField] private Color raycastToWallColor       = Color.yellow;
    [SerializeField] private Color raycastWallToTargetColor = Color.red;

    // ── Shader IDs ───────────────────────────────────────────────────
    private static readonly int ID_WaveOrigin        = Shader.PropertyToID("_WaveOrigin");
    private static readonly int ID_WaveRadius        = Shader.PropertyToID("_WaveRadius");
    private static readonly int ID_WaveActive        = Shader.PropertyToID("_WaveActive");
    private static readonly int ID_ConeForward       = Shader.PropertyToID("_ConeForward");
    private static readonly int ID_ConeHalfAngleCos  = Shader.PropertyToID("_ConeHalfAngleCos");
    private static readonly int ID_WaveFireTime      = Shader.PropertyToID("_WaveFireTime");
    private static readonly int ID_WaveMaxRadius     = Shader.PropertyToID("_WaveMaxRadius");
    private static readonly int ID_WaveFadeDuration  = Shader.PropertyToID("_WaveFadeDuration");

    // ── Etat ─────────────────────────────────────────────────────────
    private float                _currentWaveRadius;
    private float                _previousWaveRadius;
    private float                _activeRange;
    private float                _cooldownTimer;
    private Vector3              _frozenConeForward;
    private bool                 _coneIsFrozen;
    private HashSet<IDetectable> _hitObjects = new();
    private Tween                _waveTween;

    private Vector3 _lastPlayerPosition;

    private void Awake()
    {
        if (coneOrigin == null) { coneOrigin = transform; }
        if (settings   == null) { Debug.LogError("[Sonar] SO_SonarSettings non assigne !"); }

        _lastPlayerPosition = coneOrigin.position;
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        // Mini-sonar circulaire quand le joueur bouge
        if (Vector3.Distance(coneOrigin.position, _lastPlayerPosition) > 0.01f)
        {
            TriggerWaveMini();
            _lastPlayerPosition = coneOrigin.position;
        }

        PushShaderGlobals();
    }

    // ── API publique ─────────────────────────────────────────────────
    public override void Execute(InputAction.CallbackContext _context)
    {
        base.Execute(_context);
        TriggerWave();
    }

    public void TriggerWave()
    {
        if (_cooldownTimer > 0f) { return; }
        EmitWave(settings.range, 1f, false, false); // sonar normal
    }

    public void TriggerWaveWithVolume(float _normalizedVolume)
    {
        if (_cooldownTimer > 0f) { return; }
        EmitWave(settings.GetVoiceRange(_normalizedVolume), _normalizedVolume, false, false);
    }

    // ── Mini-sonar pour déplacement ───────────────────────────────────
    private void TriggerWaveMini()
    {
        float miniRange = settings.minVoiceRange;
        float miniVolume = 0.3f;
        EmitWave(miniRange, miniVolume, true, true); // true = ignore cône
    }

    // ── Logique interne ──────────────────────────────────────────────
    private void EmitWave(float _range, float _normalizedVolume, bool isStepWave, bool isCircular)
    {
        if (!isStepWave)
            _cooldownTimer = settings.cooldown;

        _activeRange        = Mathf.Clamp(_range, settings.minVoiceRange, settings.range);
        _currentWaveRadius  = 0f;
        _previousWaveRadius = 0f;
        _hitObjects.Clear();

        _frozenConeForward = coneOrigin.forward;
        _coneIsFrozen      = true;

        Vector3 originPos = coneOrigin.position;
        Vector3 originFwd = coneOrigin.forward;

        float fadeDuration = settings.GetFadeDuration(_normalizedVolume);
        Shader.SetGlobalFloat(ID_WaveFireTime,     Time.time);
        Shader.SetGlobalFloat(ID_WaveMaxRadius,    _activeRange);
        Shader.SetGlobalFloat(ID_WaveFadeDuration, fadeDuration);
        SonarSoundEvent.Emit(originPos, _normalizedVolume);

        _waveTween?.Kill();
        _waveTween = DOTween.To(
            () => _currentWaveRadius,
            radius =>
            {
                _previousWaveRadius = _currentWaveRadius;
                _currentWaveRadius  = radius;
                OnWaveStep(originPos, originFwd, isCircular);
            },
            _activeRange,
            settings.GetWaveDuration(_activeRange))
            .SetEase(settings.waveEase)
            .OnComplete(() =>
            {
                _currentWaveRadius = 0f;
                _coneIsFrozen      = false;
                Shader.SetGlobalFloat(ID_WaveActive, 0f);
            });
    }

    private void OnWaveStep(Vector3 _originPos, Vector3 _originFwd, bool isCircular)
    {
        Collider[] hits = Physics.OverlapSphere(_originPos, _currentWaveRadius, detectableLayerMask);

        foreach (Collider hit in hits)
        {
            IDetectable detectable = hit.GetComponent<IDetectable>();
            if (detectable == null || !detectable.IsActive()) continue;
            if (_hitObjects.Contains(detectable)) continue;

            Vector3 position = detectable.GetPosition();
            float distance = Vector3.Distance(_originPos, position);

            if (distance < _previousWaveRadius) continue;

            // Ignorer le cône si c'est un mini-sonar circulaire
            if (!isCircular && !IsInsideCone(_originPos, _originFwd, position)) continue;

            Vector3 dir = (position - _originPos).normalized;
            if (Physics.Raycast(_originPos, dir, out RaycastHit wallHit, distance, obstacleMask))
            {
                if (showRaycasts)
                {
                    Debug.DrawRay(_originPos, dir * wallHit.distance, raycastToWallColor, raycastDrawDuration);
                    Debug.DrawLine(wallHit.point, position, raycastWallToTargetColor, raycastDrawDuration);
                }
                continue;
            }

            if (showRaycasts)
                Debug.DrawRay(_originPos, dir * distance, raycastHitColor, raycastDrawDuration);

            _hitObjects.Add(detectable);
            float proximity = Mathf.Clamp01(1f - (distance / _activeRange));
            detectable.OnProb(proximity);
        }
    }

    private bool IsInsideCone(Vector3 _origin, Vector3 _forward, Vector3 _target)
        => Vector3.Angle(_forward, (_target - _origin).normalized) <= settings.coneHalfAngle;

    private void PushShaderGlobals()
    {
        Vector3 fwd = _coneIsFrozen ? _frozenConeForward : coneOrigin.forward;
        Shader.SetGlobalVector(ID_WaveOrigin,       coneOrigin.position);
        Shader.SetGlobalFloat( ID_WaveRadius,       _currentWaveRadius);
        Shader.SetGlobalFloat( ID_WaveActive,       _currentWaveRadius > 0f ? 1f : 0f);
        Shader.SetGlobalVector(ID_ConeForward,      fwd);
        Shader.SetGlobalFloat( ID_ConeHalfAngleCos, Mathf.Cos(settings.coneHalfAngle * Mathf.Deg2Rad));
    }
}