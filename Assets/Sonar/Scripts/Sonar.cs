using Player.Scripts;
using System.Collections.Generic;
using DG.Tweening;
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

    [Header("Onde de mouvement")]
    [SerializeField] private float movementWaveRange    = 3f;
    [SerializeField] private float movementWaveInterval = 0.5f;
    [SerializeField] private float movementThreshold    = 0.05f;

    [Header("Debug")]
    [SerializeField] private KeyCode activationKey = KeyCode.E;

    // ── Shader IDs cri ───────────────────────────────────────────────
    private static readonly int ID_WaveOrigin       = Shader.PropertyToID("_WaveOrigin");
    private static readonly int ID_WaveRadius       = Shader.PropertyToID("_WaveRadius");
    private static readonly int ID_WaveActive       = Shader.PropertyToID("_WaveActive");
    private static readonly int ID_ConeForward      = Shader.PropertyToID("_ConeForward");
    private static readonly int ID_ConeHalfAngleCos = Shader.PropertyToID("_ConeHalfAngleCos");
    private static readonly int ID_WaveFireTime     = Shader.PropertyToID("_WaveFireTime");
    private static readonly int ID_WaveMaxRadius    = Shader.PropertyToID("_WaveMaxRadius");
    private static readonly int ID_WaveFadeDuration = Shader.PropertyToID("_WaveFadeDuration");

    // ── Shader IDs mouvement (variables separees) ────────────────────
    private static readonly int ID_MoveOrigin       = Shader.PropertyToID("_MoveWaveOrigin");
    private static readonly int ID_MoveRadius       = Shader.PropertyToID("_MoveWaveRadius");
    private static readonly int ID_MoveActive       = Shader.PropertyToID("_MoveWaveActive");
    private static readonly int ID_MoveFireTime     = Shader.PropertyToID("_MoveWaveFireTime");
    private static readonly int ID_MoveMaxRadius    = Shader.PropertyToID("_MoveWaveMaxRadius");
    private static readonly int ID_MoveFadeDuration = Shader.PropertyToID("_MoveWaveFadeDuration");

    // ── Etat cri ─────────────────────────────────────────────────────
    private float      _currentWaveRadius;
    private float      _previousWaveRadius;
    private float      _activeRange;
    private float      _cooldownTimer;
    private Vector3    _frozenConeForward;
    private Vector3    _frozenConeOrigin;  // origine figee au moment du tir
    private bool       _coneIsFrozen;
    private HashSet<IDetectable> _hitObjects = new();
    private Tween      _waveTween;
    private float      _waveFireTime;
    private float      _waveMaxRadius;
    private float      _waveFadeDuration;
    private Collider[] _selfColliders;

    // ── Etat mouvement ───────────────────────────────────────────────
    private float  _moveCurrentRadius;
    private float  _movePreviousRadius;
    private Tween  _moveTween;
    private float  _movementTimer;
    private Vector3 _lastPosition;

    // ── Init ─────────────────────────────────────────────────────────

    public override void Init(PlayerController _playerController)
    {
        base.Init(_playerController);
        if (coneOrigin == null) { coneOrigin = transform; }
        if (settings == null)  { Debug.LogError("[Sonar] SO_SonarSettings non assigne !"); }

        _frozenConeForward = coneOrigin.forward;
        _frozenConeOrigin  = coneOrigin.position;
        _selfColliders     = GetComponentsInChildren<Collider>();
        _lastPosition      = transform.position;

        VoiceTrigger.OnSoundFired += OnVoiceFired;
    }

    private void OnDestroy()
    {
        VoiceTrigger.OnSoundFired -= OnVoiceFired;
    }

   

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

       
        if (_cooldownTimer <= 0f && _coneIsFrozen)
        {
            _coneIsFrozen = false;
        }

       

        HandleMovementWave();
        PushShaderGlobals();
    }

   

    public override void Execute(InputAction.CallbackContext _context)
    {
        if (!CanExecute()) { return; }
        if (_context.phase != InputActionPhase.Started) { return; }
        TriggerWave();
        EventBus.Publish(new OnPlayerInputEnter
        {
            input = TutorialVerifState.echolocation
        });
    }

  
    private void OnVoiceFired(float normalizedVolume)
    {
        TriggerWaveWithVolume(normalizedVolume);
    }

   
    public void TriggerWave()
    {
        if (_cooldownTimer > 0f) { return; }
        EmitWave(settings.range, settings.GetWaveDuration(settings.range), 1f);
        _cooldownTimer = settings.cooldown;
    }

    public void TriggerWaveWithVolume(float _normalizedVolume)
    {
        if (_cooldownTimer > 0f) { return; }
        float range = settings.GetVoiceRange(_normalizedVolume);
        EmitWave(range, settings.GetWaveDuration(range), _normalizedVolume);
        _cooldownTimer = settings.cooldown;
    }

   
    private void HandleMovementWave()
    {
        float moved = Vector3.Distance(transform.position, _lastPosition);
        _lastPosition = transform.position;
        if (moved < movementThreshold) { return; }

        _movementTimer -= Time.deltaTime;
        if (_movementTimer > 0f) { return; }
        _movementTimer = movementWaveInterval;

        EmitMovementWave();
    }

    private void EmitMovementWave()
    {
        float duration = movementWaveRange / Mathf.Max(settings.ondeSpeed, 0.1f);
        Vector3 originPos = coneOrigin.position;

        Shader.SetGlobalFloat(ID_MoveFireTime,     Time.time);
        Shader.SetGlobalFloat(ID_MoveMaxRadius,    movementWaveRange);
        Shader.SetGlobalFloat(ID_MoveFadeDuration, duration);

        _moveCurrentRadius  = 0f;
        _movePreviousRadius = 0f;

        _moveTween?.Kill();
        _moveTween = DOTween.To(
            () => _moveCurrentRadius,
            r =>
            {
                _movePreviousRadius = _moveCurrentRadius;
                _moveCurrentRadius  = r;
                // Pousse le rayon chaque frame
                Shader.SetGlobalVector(ID_MoveOrigin, originPos);
                Shader.SetGlobalFloat(ID_MoveRadius,  _moveCurrentRadius);
                Shader.SetGlobalFloat(ID_MoveActive,  _moveCurrentRadius > 0f ? 1f : 0f);
            },
            movementWaveRange, duration
        ).SetEase(Ease.Linear)
         .OnComplete(() =>
         {
             _moveCurrentRadius = 0f;
             Shader.SetGlobalFloat(ID_MoveActive, 0f);
         });
    }

   

    private void EmitWave(float _range, float _duration, float _normalizedVolume)
    {
        _activeRange        = _range;
        _currentWaveRadius  = 0f;
        _previousWaveRadius = 0f;
        _hitObjects.Clear();

       
        _frozenConeForward = coneOrigin.forward;
        _frozenConeOrigin  = coneOrigin.position;
        _coneIsFrozen      = true;

        _waveFireTime     = Time.time;
        _waveMaxRadius    = _range;
        _waveFadeDuration = _duration;

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
            _range, _duration
        ).SetEase(Ease.Linear)
         .OnComplete(() =>
         {
             _currentWaveRadius = 0f;
             Shader.SetGlobalFloat(ID_WaveActive, 0f);
         });
    }

    

    private void OnWaveStep(Vector3 originPos, Vector3 originFwd)
    {
        float halfCos    = Mathf.Cos(settings.coneHalfAngle * Mathf.Deg2Rad);
        float scanRadius = Mathf.Max(_currentWaveRadius, 0.5f);

        
        int segs = 24;
        for (int i = 0; i < segs; i++)
        {
            float a0 = (i / (float)segs) * Mathf.PI * 2f;
            float a1 = ((i + 1) / (float)segs) * Mathf.PI * 2f;
            /*Debug.DrawLine(
                originPos + new Vector3(Mathf.Cos(a0), 0, Mathf.Sin(a0)) * scanRadius,
                originPos + new Vector3(Mathf.Cos(a1), 0, Mathf.Sin(a1)) * scanRadius,
                Color.yellow, 0.15f);*/
        }

        Collider[] hits = Physics.OverlapSphere(originPos, scanRadius, detectableLayerMask);
        //Debug.Log($"[Sonar] radius={scanRadius:F2} hits={hits.Length}");

        foreach (Collider hit in hits)
        {
            IDetectable detectable = hit.GetComponent<IDetectable>();
            if (detectable == null)
            {
                //Debug.Log($"[Sonar] SKIP {hit.name} : pas IDetectable");
                continue;
            }
            if (!detectable.IsActive())
            {
                //Debug.Log($"[Sonar] SKIP {hit.name} : inactif");
                continue;
            }
            if (_hitObjects.Contains(detectable))
            {
                //Debug.Log($"[Sonar] SKIP {hit.name} : deja detecte");
                continue;
            }

            Vector3 position = detectable.GetPosition();
            float   distance = Vector3.Distance(originPos, position);

            
            Vector3 dir2obj  = (position - originPos).normalized;
            float   cosAngle = Vector3.Dot(dir2obj, originFwd.normalized);
            bool    inCone   = cosAngle >= halfCos;
            //.Log($"[Sonar] CONE {hit.name} : {(inCone ? "DANS" : "HORS")} (cos={cosAngle:F2} vs {halfCos:F2})");
            if (!inCone) { continue; }

           
            Vector3 dir = dir2obj;

           
            Vector3 right = Vector3.Cross(dir, Vector3.up).normalized * 0.3f;
            Vector3 up    = Vector3.up * 0.3f;
            Vector3[] targets = new Vector3[]
            {
                position,           
                position + right,   
                position - right,   
                position + up,      
                position - up,      
            };

            bool anyUnblocked = false;
            foreach (Vector3 target in targets)
            {
                Vector3 toTarget  = (target - originPos);
                float   targetDist = toTarget.magnitude;
                Vector3 toTargetDir = toTarget.normalized;

                bool thisBlocked = false;
                RaycastHit[] rhs = Physics.RaycastAll(originPos, toTargetDir, targetDist, obstacleMask);
                System.Array.Sort(rhs, (a, b) => a.distance.CompareTo(b.distance));
                foreach (RaycastHit rh in rhs)
                {
                    bool isSelf = false;
                    foreach (Collider sc in _selfColliders)
                    {
                        if (rh.collider == sc) { isSelf = true; break; }
                    }
                    if (!isSelf) { thisBlocked = true; break; }
                }

                /*Debug.DrawLine(originPos, target,
                    thisBlocked ? Color.red : Color.green, 0.5f);*/

                if (!thisBlocked)
                {
                    anyUnblocked = true;
                    break; 
                }
            }

            //Debug.Log($"[Sonar] RAYCAST {hit.name} : anyUnblocked={anyUnblocked}");
            if (!anyUnblocked) { continue; }

            _hitObjects.Add(detectable);
            float proximity = Mathf.Clamp01(1f - (distance / _activeRange));
            detectable.OnProb(proximity);
            //Debug.Log($"[Sonar] >>> DETECTE {hit.name} prox={proximity:F2}");
        }
    }

  
    private void PushShaderGlobals()
    {
        if (_coneIsFrozen)
        {
           
            Shader.SetGlobalVector(ID_WaveOrigin, _frozenConeOrigin);
            Shader.SetGlobalFloat(ID_WaveRadius,  _currentWaveRadius);
            Shader.SetGlobalFloat(ID_WaveActive,  _currentWaveRadius > 0f ? 1f : 0f);
            Shader.SetGlobalVector(ID_ConeForward, _frozenConeForward);
            Shader.SetGlobalFloat(ID_ConeHalfAngleCos,
                Mathf.Cos(settings.coneHalfAngle * Mathf.Deg2Rad));
        }
        else
        {
           
            Shader.SetGlobalVector(ID_WaveOrigin, coneOrigin.position);
            Shader.SetGlobalFloat(ID_WaveRadius,  0f);
            Shader.SetGlobalFloat(ID_WaveActive,  0f);
            Shader.SetGlobalVector(ID_ConeForward, coneOrigin.forward);
            Shader.SetGlobalFloat(ID_ConeHalfAngleCos,
                Mathf.Cos(settings.coneHalfAngle * Mathf.Deg2Rad));
        }
    }

   
    private void OnDrawGizmosSelected()
    {
        if (settings == null) { return; }
        Transform origin = coneOrigin != null ? coneOrigin : transform;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.06f);
        Gizmos.DrawSphere(origin.position, settings.range);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(origin.position, settings.range);

        Gizmos.color = new Color(1f, 1f, 0f, 0.06f);
        Gizmos.DrawSphere(origin.position, settings.minVoiceRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(origin.position, settings.minVoiceRange);

        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        DrawConeRay(origin.position, origin.forward,  settings.coneHalfAngle);
        DrawConeRay(origin.position, origin.forward, -settings.coneHalfAngle);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin.position, origin.forward * settings.range);
    }

    private void DrawConeRay(Vector3 _origin, Vector3 _forward, float _angleOffset)
    {
        Vector2 f2  = new Vector2(_forward.x, _forward.z).normalized;
        float   rad = _angleOffset * Mathf.Deg2Rad;
        Vector2 d2  = new Vector2(
            f2.x * Mathf.Cos(rad) - f2.y * Mathf.Sin(rad),
            f2.x * Mathf.Sin(rad) + f2.y * Mathf.Cos(rad));
        Gizmos.DrawRay(_origin, new Vector3(d2.x, 0f, d2.y) * settings.range);
    }
}
