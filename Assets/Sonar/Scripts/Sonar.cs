using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Sonar / echolocalisation — Echo Maze.
/// Pousse les globals shader chaque frame pour synchroniser SonarSurface.shader.
/// La duree de revelation (_WaveFadeDuration) est proportionnelle a la charge.
/// </summary>
public class Sonar : MonoBehaviour
{
    [Header("Parametres")]
    [SerializeField] private SO_SonarSettings settings;

    [Header("Activation clavier")]
    [SerializeField] private KeyCode activationKey = KeyCode.E;

    [Header("Origine du cone")]
    [Tooltip("Transform depuis lequel part l'onde. Vide = ce GameObject.")]
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

    // ---------------------------------------------------------------

    private void Awake()
    {
        if (coneOrigin == null) { coneOrigin = transform; }
        if (settings   == null) { Debug.LogError("[Sonar] SO_SonarSettings non assigne !"); }
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;
        if (Input.GetKeyDown(activationKey) && _cooldownTimer <= 0f) { TriggerWave(); }
        PushShaderGlobals();
    }

    // ── API publique ─────────────────────────────────────────────────

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
        _cooldownTimer      = settings.cooldown;
        _activeRange        = Mathf.Clamp(_range, settings.minVoiceRange, settings.range);
        _currentWaveRadius  = 0f;
        _previousWaveRadius = 0f;
        _hitObjects.Clear();

        _frozenConeForward = coneOrigin.forward;
        _coneIsFrozen      = true;

        Vector3 originPos = coneOrigin.position;
        Vector3 originFwd = coneOrigin.forward;

        // Pousse les globals de trace residuelle
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
                OnWaveStep(originPos, originFwd);
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

    private void OnWaveStep(Vector3 _originPos, Vector3 _originFwd)
    {
        Collider[] hits = Physics.OverlapSphere(
            _originPos, _currentWaveRadius, detectableLayerMask);

        foreach (Collider hit in hits)
        {
            IDetectable detectable = hit.GetComponent<IDetectable>();
            if (detectable == null || !detectable.IsActive()) { continue; }
            if (_hitObjects.Contains(detectable))             { continue; }

            Vector3 position = detectable.GetPosition();
            float   distance = Vector3.Distance(_originPos, position);

            if (distance < _previousWaveRadius)                  { continue; }
            if (!IsInsideCone(_originPos, _originFwd, position)) { continue; }

            Vector3    dir = (position - _originPos).normalized;
            RaycastHit wallHit;

            if (Physics.Raycast(_originPos, dir, out wallHit, distance, obstacleMask))
            {
                if (showRaycasts)
                {
                    Debug.DrawRay(_originPos, dir * wallHit.distance,
                        raycastToWallColor, raycastDrawDuration);
                    Debug.DrawLine(wallHit.point, position,
                        raycastWallToTargetColor, raycastDrawDuration);
                }
                continue;
            }

            if (showRaycasts)
                Debug.DrawRay(_originPos, dir * distance,
                    raycastHitColor, raycastDrawDuration);

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
        DrawConeRay(origin.position, origin.forward,  settings.coneHalfAngle);
        DrawConeRay(origin.position, origin.forward, -settings.coneHalfAngle);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin.position, origin.forward * settings.range);

        if (Application.isPlaying && _currentWaveRadius > 0f)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawWireSphere(origin.position, _currentWaveRadius);
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.3f);
            Gizmos.DrawWireSphere(origin.position, _activeRange);
        }
    }

    private void DrawConeRay(Vector3 _origin, Vector3 _forward, float _angleOffset)
    {
        Vector2 f2  = new Vector2(_forward.x, _forward.y).normalized;
        float   rad = _angleOffset * Mathf.Deg2Rad;
        Vector2 d2  = new Vector2(
            f2.x * Mathf.Cos(rad) - f2.y * Mathf.Sin(rad),
            f2.x * Mathf.Sin(rad) + f2.y * Mathf.Cos(rad));
        Gizmos.DrawRay(_origin, new Vector3(d2.x, d2.y, 0f) * settings.range);
    }
}
