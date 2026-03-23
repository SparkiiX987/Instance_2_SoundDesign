using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using Player.Scripts;

public class Sonar : PlayerAbility
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

    private float _waveFireTime;
    private float _waveMaxRadius;
    private float _waveFadeDuration;

    private Collider[] _selfColliders;

    public override void Init(PlayerController _playerController)
    {
        base.Init(_playerController);
        if (coneOrigin == null) coneOrigin = transform;
        _lastPosition      = transform.position;
        _frozenConeForward = coneOrigin.forward;
        _selfColliders     = GetComponentsInChildren<Collider>();
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        if (_cooldownTimer <= 0f && !_isMovementWave)
            _coneIsFrozen = false;

        // Fallback clavier si pas d'input system
        if (Input.GetKeyDown(activationKey) && _cooldownTimer <= 0f)
            TriggerWave();

        HandleMovementWave();
        PushShaderGlobals();
    }

    /// <summary>
    /// Execute via PlayerAbility — appele par le PlayerController via InputAction.
    /// </summary>
    public override void Execute(InputAction.CallbackContext _context)
    {
        if (!CanExecute()) return;
        if (_context.phase != InputActionPhase.Started) return;
        TriggerWave();
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

        _waveFireTime     = Time.time;
        _waveMaxRadius    = range;
        _waveFadeDuration = duration;

        Shader.SetGlobalFloat(ID_WaveFireTime,     _waveFireTime);
        Shader.SetGlobalFloat(ID_WaveMaxRadius,    _waveMaxRadius);
        Shader.SetGlobalFloat(ID_WaveFadeDuration, _waveFadeDuration);

        Vector3 originPos = coneOrigin.position;
        Vector3 originFwd = coneOrigin.forward;
        bool    isMvt     = _isMovementWave;

        _waveTween?.Kill();
        _waveTween = DOTween.To(
            () => _currentWaveRadius,
            r =>
            {
                _previousWaveRadius = _currentWaveRadius;
                _currentWaveRadius  = r;
                OnWaveStep(originPos, originFwd, isMvt);
            },
            range, duration
        ).SetEase(Ease.Linear)
         .OnComplete(() =>
         {
             _currentWaveRadius = 0f;
             _isMovementWave    = false;
             Shader.SetGlobalFloat(ID_WaveActive, 0f);
         });
    }

    private void OnWaveStep(Vector3 originPos, Vector3 originFwd, bool isMovementWave)
    {
        float halfCos    = Mathf.Cos(settings.coneHalfAngle * Mathf.Deg2Rad);
        float scanRadius = Mathf.Max(_currentWaveRadius, 0.5f);

        // Debug visuel OverlapSphere
        int segs = 24;
        for (int i = 0; i < segs; i++)
        {
            float a0 = (i / (float)segs) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segs) * Mathf.PI * 2f;
            Debug.DrawLine(
                originPos + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * scanRadius,
                originPos + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * scanRadius,
                Color.yellow, 0.15f);
            Debug.DrawLine(
                originPos + new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0) * scanRadius,
                originPos + new Vector3(Mathf.Cos(a1), Mathf.Sin(a1), 0) * scanRadius,
                Color.yellow, 0.15f);
        }

        Collider[] hits = Physics.OverlapSphere(originPos, scanRadius, detectableLayerMask);
        Debug.Log($"[Sonar] OverlapSphere radius={scanRadius:F2} => {hits.Length} colliders trouves");

        foreach (Collider hit in hits)
        {
            IDetectable detectable = hit.GetComponent<IDetectable>();

            if (detectable == null)
            {
                Debug.Log($"[Sonar] SKIP {hit.name} (layer={LayerMask.LayerToName(hit.gameObject.layer)}) : pas de IDetectable");
                continue;
            }

            if (!detectable.IsActive())
            {
                Debug.Log($"[Sonar] SKIP {hit.name} : IDetectable inactif");
                continue;
            }

            if (_hitObjects.Contains(detectable))
            {
                Debug.Log($"[Sonar] SKIP {hit.name} : deja detecte cette onde");
                continue;
            }

            Vector3 position = detectable.GetPosition();
            float   distance = Vector3.Distance(originPos, position);

            if (!isMovementWave)
            {
                Vector3 dir2obj  = (position - originPos).normalized;
                float   cosAngle = Vector3.Dot(dir2obj, originFwd.normalized);
                bool    inCone   = cosAngle >= halfCos;
                Debug.Log($"[Sonar] CONE {hit.name} : cosAngle={cosAngle:F3} halfCos={halfCos:F3} => {(inCone ? "DANS le cone" : "HORS cone")}");
                if (!inCone) continue;
            }

            Vector3 dir = (position - originPos).normalized;

            // Raycast en ignorant les colliders du joueur
            bool blocked = false;
            RaycastHit[] rayHits = Physics.RaycastAll(originPos, dir, distance, obstacleMask);
            foreach (RaycastHit rh in rayHits)
            {
                bool isSelf = false;
                foreach (Collider sc in _selfColliders)
                    if (rh.collider == sc) { isSelf = true; break; }
                if (!isSelf) { blocked = true; break; }
            }

            Debug.DrawLine(originPos, originPos + dir * distance,
                blocked ? Color.red : Color.green, 0.5f);
            Debug.Log($"[Sonar] RAYCAST {hit.name} : distance={distance:F2} blocked={blocked}");
            if (blocked) continue;

            _hitObjects.Add(detectable);
            float proximity = Mathf.Clamp01(1f - (distance / _activeRange));
            detectable.OnProb(proximity);
            Debug.Log($"[Sonar] >>> DETECTE {hit.name} proximity={proximity:F2}");
        }
    }

    private void PushShaderGlobals()
    {
        Shader.SetGlobalVector(ID_WaveOrigin, coneOrigin.position);
        Shader.SetGlobalFloat(ID_WaveRadius,  _currentWaveRadius);
        Shader.SetGlobalFloat(ID_WaveActive,  _currentWaveRadius > 0f ? 1f : 0f);

        if (_isMovementWave)
        {
            Shader.SetGlobalFloat(ID_ConeHalfAngleCos, -1000f);
            Shader.SetGlobalVector(ID_ConeForward, coneOrigin.forward);
        }
        else
        {
            Shader.SetGlobalVector(ID_ConeForward, _frozenConeForward);
            Shader.SetGlobalFloat(ID_ConeHalfAngleCos,
                Mathf.Cos(settings.coneHalfAngle * Mathf.Deg2Rad));
        }
    }
}