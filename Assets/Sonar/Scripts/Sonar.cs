using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Radar / ecolocation par onde propagee.
/// Utilise SO_SonarSettings pour tous les parametres.
/// - Touche E : onde a portee maximale.
/// - TriggerWaveWithVolume : portee dynamique selon la voix (VoiceTrigger).
/// - Raycasts visibles dans la vue Scene via Debug.DrawRay.
/// - Gizmos : cone (haut/bas), portee max, portee min/max voix, onde courante.
/// Prerequis : DOTween importe dans le projet.
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

    // ---------------------------------------------------------------

    private float                _currentWaveRadius;
    private float                _previousWaveRadius;
    private float                _activeRange;
    private float                _cooldownTimer;
    private HashSet<IDetectable> _hitObjects = new();
    private Tween                _waveTween;

    // ---------------------------------------------------------------

    private void Awake()
    {
        if (coneOrigin == null) coneOrigin = transform;
        if (settings == null)
            Debug.LogError("[RadarSystem] SO_SonarSettings non assigne !");
    }

    private void Update()
    {
        _cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(activationKey) && _cooldownTimer <= 0f)
            TriggerWave();
    }

    // ---------------------------------------------------------------

    public void TriggerWave()
    {
        if (_cooldownTimer > 0f) return;
        EmitWave(settings.range);
    }

    public void TriggerWaveWithVolume(float _normalizedVolume)
    {
        if (_cooldownTimer > 0f) return;
        EmitWave(settings.GetVoiceRange(_normalizedVolume));
    }

    // ---------------------------------------------------------------

    private void EmitWave(float _range)
    {
        _cooldownTimer      = settings.cooldown;
        _activeRange        = Mathf.Clamp(_range, settings.minVoiceRange, settings.range);
        _currentWaveRadius  = 0f;
        _previousWaveRadius = 0f;
        _hitObjects.Clear();

        Vector3 originPos = coneOrigin.position;
        Vector3 originFwd = coneOrigin.forward;

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
            .OnComplete(() => _currentWaveRadius = 0f);
    }

    private void OnWaveStep(Vector3 _originPos, Vector3 _originFwd)
    {
        Collider[] hits = Physics.OverlapSphere(_originPos, _currentWaveRadius, detectableLayerMask);

        foreach (Collider hit in hits)
        {
            IDetectable detectable = hit.GetComponent<IDetectable>();
            if (detectable == null || !detectable.IsActive()) continue;
            if (_hitObjects.Contains(detectable)) continue;

            Vector3 position = detectable.GetPosition();
            float   distance = Vector3.Distance(_originPos, position);

            if (distance < _previousWaveRadius) continue;
            if (!IsInsideCone(_originPos, _originFwd, position)) continue;

            Vector3 dir = (position - _originPos).normalized;

            RaycastHit wallHit;
            if (Physics.Raycast(_originPos, dir, out wallHit, distance, obstacleMask))
            {
                if (showRaycasts)
                {
                    Debug.DrawRay(_originPos, dir * wallHit.distance, raycastToWallColor,      raycastDrawDuration);
                    Debug.DrawLine(wallHit.point, position,           raycastWallToTargetColor, raycastDrawDuration);
                }
                continue;
            }

            if (showRaycasts)
                Debug.DrawRay(_originPos, dir * distance, raycastHitColor, raycastDrawDuration);

            _hitObjects.Add(detectable);

            float normalizedProximity = Mathf.Clamp01(1f - (distance / _activeRange));
            detectable.OnProb(normalizedProximity);
        }
    }

    private bool IsInsideCone(Vector3 _origin, Vector3 _forward, Vector3 _targetPosition)
    {
        Vector3 direction = (_targetPosition - _origin).normalized;
        return Vector3.Angle(_forward, direction) <= settings.coneHalfAngle;
    }

    // ---------------------------------------------------------------
    // Gizmos

    private void OnDrawGizmosSelected()
    {
        if (settings == null) return;
        Transform origin = coneOrigin != null ? coneOrigin : transform;

        // Portee maximale
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.06f);
        Gizmos.DrawSphere(origin.position, settings.range);
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.5f);
        Gizmos.DrawWireSphere(origin.position, settings.range);

        // Portee minimale voix
        Gizmos.color = new Color(1f, 1f, 0f, 0.06f);
        Gizmos.DrawSphere(origin.position, settings.minVoiceRange);
        Gizmos.color = new Color(1f, 1f, 0f, 0.5f);
        Gizmos.DrawWireSphere(origin.position, settings.minVoiceRange);

        // Portee maximale voix
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.06f);
        Gizmos.DrawSphere(origin.position, settings.maxVoiceRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireSphere(origin.position, settings.maxVoiceRange);

        // Cone haut et bas
        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        DrawConeRay(origin.position, origin.forward,  settings.coneHalfAngle);
        DrawConeRay(origin.position, origin.forward, -settings.coneHalfAngle);

        // Axe forward
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin.position, origin.forward * settings.range);

        // Onde courante (runtime uniquement)
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
        Vector2 forward2D = new Vector2(_forward.x, _forward.y).normalized;

        float   rad   = _angleOffset * Mathf.Deg2Rad;
        float   cos   = Mathf.Cos(rad);
        float   sin   = Mathf.Sin(rad);
        Vector2 dir2D = new Vector2(
            forward2D.x * cos - forward2D.y * sin,
            forward2D.x * sin + forward2D.y * cos);

        Gizmos.DrawRay(_origin, new Vector3(dir2D.x, dir2D.y, 0f) * settings.range);
    }
}
