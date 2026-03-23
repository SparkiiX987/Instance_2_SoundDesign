
using Player.Scripts;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using Player.Scripts;

/// <summary>
/// Sonar / echolocalisation — Echo Maze.
/// Pousse les globals shader chaque frame pour synchroniser SonarSurface.shader.
/// La duree de revelation (_WaveFadeDuration) est proportionnelle a la charge.
/// </summary>
public class Sonar : PlayerAbility
{
    [Header("Parametres")]
    [SerializeField] private SO_SonarSettings settings;

    //[Header("Activation clavier")]
    //[SerializeField] private KeyCode activationKey = KeyCode.E;

    [Header("Origine du cone")]
    [Tooltip("Transform depuis lequel part l'onde. Vide = ce GameObject.")]
    [SerializeField] private Transform coneOrigin;

    [Header("LayerMask")]
    [SerializeField] private LayerMask detectableLayerMask = ~0;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Debug Raycasts")]
    [SerializeField] private bool showRaycasts = true;
    [SerializeField] private float raycastDrawDuration = 0.5f;
    [SerializeField] private Color raycastHitColor = Color.cyan;
    [SerializeField] private Color raycastToWallColor = Color.yellow;
    [SerializeField] private Color raycastWallToTargetColor = Color.red;

    // ── Shader IDs ───────────────────────────────────────────────────
    private static readonly int ID_WaveOrigin = Shader.PropertyToID("_WaveOrigin");
    private static readonly int ID_WaveRadius = Shader.PropertyToID("_WaveRadius");
    private static readonly int ID_WaveActive = Shader.PropertyToID("_WaveActive");
    private static readonly int ID_ConeForward = Shader.PropertyToID("_ConeForward");
    private static readonly int ID_ConeHalfAngleCos = Shader.PropertyToID("_ConeHalfAngleCos");
    private static readonly int ID_WaveFireTime = Shader.PropertyToID("_WaveFireTime");
    private static readonly int ID_WaveMaxRadius = Shader.PropertyToID("_WaveMaxRadius");
    private static readonly int ID_WaveFadeDuration = Shader.PropertyToID("_WaveFadeDuration");

    // ── Etat ─────────────────────────────────────────────────────────
    private float _currentWaveRadius;
    private float _previousWaveRadius;
    private float _activeRange;
    private float _cooldownTimer;
    private Vector3 _frozenConeForward;
    private bool _coneIsFrozen;
    private HashSet<IDetectable> _hitObjects = new();
    private Tween _waveTween;

    // ---------------------------------------------------------------
    private float _waveFireTime;
    private float _waveMaxRadius;
    private float _waveFadeDuration;

    private Collider[] _selfColliders;

    public override void Init(PlayerController _playerController)
    {
        if (coneOrigin == null) { coneOrigin = transform; }
        if (settings == null) { Debug.LogError("[Sonar] SO_SonarSettings non assigne !"); }
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
        if (_cooldownTimer > 0f) { return; }
        EmitWave(settings.range, 1f);
    }

    /// <summary>
    /// Declenche l'onde avec portee et duree de revelation dynamiques.
    /// _normalizedVolume [0..1] : 0 = min, 1 = max.
    /// </summary>
    public void TriggerWaveWithVolume(float _normalizedVolume)
    {
        if (_cooldownTimer > 0f) { return; }
        EmitWave(
            settings.GetVoiceRange(_normalizedVolume),
            _normalizedVolume);
    }

    // ── Logique interne ──────────────────────────────────────────────

    private void EmitWave(float _range, float _normalizedVolume)
    {
        _cooldownTimer = settings.cooldown;
        _activeRange = Mathf.Clamp(_range, settings.minVoiceRange, settings.range);
        _currentWaveRadius = 0f;
        _previousWaveRadius = 0f;
        _hitObjects.Clear();

        _waveFireTime     = Time.time;
        _waveMaxRadius    = range;
        _waveFadeDuration = duration;

        Shader.SetGlobalFloat(ID_WaveFireTime,     _waveFireTime);
        Shader.SetGlobalFloat(ID_WaveMaxRadius,    _waveMaxRadius);
        Shader.SetGlobalFloat(ID_WaveFadeDuration, _waveFadeDuration);

        Vector3 originPos = coneOrigin.position;
        Vector3 originFwd = coneOrigin.forward;
        bool    isMvt     = _isMovementWave;

        // Pousse les globals de trace residuelle
        float fadeDuration = settings.GetFadeDuration(_normalizedVolume);
        Shader.SetGlobalFloat(ID_WaveFireTime, Time.time);
        Shader.SetGlobalFloat(ID_WaveMaxRadius, _activeRange);
        Shader.SetGlobalFloat(ID_WaveFadeDuration, fadeDuration);
        SonarSoundEvent.Emit(originPos, _normalizedVolume);

        _waveTween?.Kill();
        _waveTween = DOTween.To(
            () => _currentWaveRadius,
            radius =>
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

    private bool IsInsideCone(Vector3 _origin, Vector3 _forward, Vector3 _target)
        => Vector3.Angle(_forward, (_target - _origin).normalized) <= settings.coneHalfAngle;

    private void PushShaderGlobals()
    {
        Shader.SetGlobalVector(ID_WaveOrigin, coneOrigin.position);
        Shader.SetGlobalFloat(ID_WaveRadius,  _currentWaveRadius);
        Shader.SetGlobalFloat(ID_WaveActive,  _currentWaveRadius > 0f ? 1f : 0f);

    // ── Gizmos ───────────────────────────────────────────────────────

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

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.06f);
        Gizmos.DrawSphere(origin.position, settings.maxVoiceRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(origin.position, settings.maxVoiceRange);

        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        DrawConeRay(origin.position, origin.forward, settings.coneHalfAngle);
        DrawConeRay(origin.position, origin.forward, -settings.coneHalfAngle);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin.position, origin.forward * settings.range);

        if (Application.isPlaying && _currentWaveRadius > 0f)
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

    private void DrawConeRay(Vector3 _origin, Vector3 _forward, float _angleOffset)
    {
        Vector2 f2 = new Vector2(_forward.x, _forward.y).normalized;
        float rad = _angleOffset * Mathf.Deg2Rad;
        Vector2 d2 = new Vector2(
            f2.x * Mathf.Cos(rad) - f2.y * Mathf.Sin(rad),
            f2.x * Mathf.Sin(rad) + f2.y * Mathf.Cos(rad));
        Gizmos.DrawRay(_origin, new Vector3(d2.x, d2.y, 0f) * settings.range);
    }
}
