using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Radar / ecolocation par onde propagee.
/// - Appui sur la touche E : onde a portee maximale.
/// - Voix du joueur : portee dynamique selon le volume du micro (VoiceTrigger).
/// - Quand l'onde touche un objet, elle appelle OnProb() sur l'objet.
///   C'est l'objet lui-meme qui gere son son.
/// Prerequis : DOTween importe dans le projet.
/// </summary>
public class RadarSystem : MonoBehaviour
{
    [Header("Activation clavier")]
    [SerializeField] private KeyCode activationKey = KeyCode.E;

    [Header("Origine du cone")]
    [Tooltip("Transform depuis lequel part l'onde. Vide = ce GameObject.")]
    [SerializeField] private Transform coneOrigin;

    [Header("Forme du cone")]
    [SerializeField] private float detectionRange = 20f;
    [Tooltip("Demi-angle du cone en degres (45 = cone de 90 total).")]
    [Range(5f, 90f)]
    [SerializeField] private float coneHalfAngle = 45f;

    [Header("Portee dynamique (voix)")]
    [Tooltip("Portee minimale quand le joueur parle doucement.")]
    [SerializeField] private float minVoiceRange = 3f;
    [Tooltip("Portee maximale quand le joueur crie fort.")]
    [SerializeField] private float maxVoiceRange = 20f;

    [Header("Onde")]
    [Tooltip("Duree que met l'onde pour traverser toute la portee (secondes).")]
    [SerializeField] private float waveDuration = 1.5f;
    [Tooltip("Courbe d'animation de l'onde.")]
    [SerializeField] private Ease waveEase = Ease.Linear;
    [Tooltip("Delai minimum entre deux ondes (secondes).")]
    [SerializeField] private float waveCooldown = 0.8f;

    [Header("LayerMask des objets detectables")]
    [SerializeField] private LayerMask detectableLayerMask = ~0;

    [Tooltip("LayerMask des obstacles qui bloquent l'onde (murs, sols, etc.).")]
    [SerializeField] private LayerMask obstacleMask;

    // ---------------------------------------------------------------

    private float currentWaveRadius;
    private float previousWaveRadius;
    private float activeRange;
    private HashSet<IDetectable> hitObjects = new();
    private Tween waveTween;
    private float cooldownTimer;

    // ---------------------------------------------------------------

    private void Awake()
    {
        if (coneOrigin == null) coneOrigin = transform;
        maxVoiceRange = Mathf.Min(maxVoiceRange, detectionRange);
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;
        if (Input.GetKeyDown(activationKey) && cooldownTimer <= 0f)
            EmitWave(detectionRange);
    }

    // ---------------------------------------------------------------

    /// <summary>Declenche l'onde a portee maximale (touche E).</summary>
    public void TriggerWave()
    {
        if (cooldownTimer <= 0f)
            EmitWave(detectionRange);
    }

    /// <summary>
    /// Declenche l'onde avec une portee dynamique selon le volume normalise [0..1].
    /// Utilisee par VoiceTrigger.
    /// </summary>
    public void TriggerWaveWithVolume(float _normalizedVolume)
    {
        if (cooldownTimer > 0f) return;
        float range = Mathf.Lerp(minVoiceRange, maxVoiceRange, _normalizedVolume);
        EmitWave(range);
    }

    /// <summary>Lance l'onde avec la portee specifiee.</summary>
    private void EmitWave(float _range)
    {
        cooldownTimer      = waveCooldown;
        activeRange        = Mathf.Clamp(_range, minVoiceRange, detectionRange);
        currentWaveRadius  = 0f;
        previousWaveRadius = 0f;
        hitObjects.Clear();

        Vector3 originPos = coneOrigin.position;
        Vector3 originFwd = coneOrigin.forward;

        waveTween?.Kill();
        waveTween = DOTween.To(
            () => currentWaveRadius,
            radius =>
            {
                previousWaveRadius = currentWaveRadius;
                currentWaveRadius  = radius;
                OnWaveStep(originPos, originFwd);
            },
            activeRange,
            waveDuration * (activeRange / detectionRange)
        )
        .SetEase(waveEase)
        .OnComplete(() => currentWaveRadius = 0f);
    }

    private void OnWaveStep(Vector3 _originPos, Vector3 _originFwd)
    {
        Collider[] hits = Physics.OverlapSphere(_originPos, currentWaveRadius, detectableLayerMask);

        foreach (var hit in hits)
        {
            var detectable = hit.GetComponent<IDetectable>();
            if (detectable == null || !detectable.IsActive()) continue;
            if (hitObjects.Contains(detectable)) continue;

            Vector3 position = detectable.GetPosition();
            float distance   = Vector3.Distance(_originPos, position);

            if (distance < previousWaveRadius) continue;
            if (!IsInsideCone(_originPos, _originFwd, position)) continue;

            Vector3 dir = (position - _originPos).normalized;
            if (Physics.Raycast(_originPos, dir, distance, obstacleMask)) continue;

            hitObjects.Add(detectable);

            // L'objet gere lui-meme son son via OnProb()
            float normalizedProximity = Mathf.Clamp01(1f - (distance / activeRange));
            detectable.OnProb(normalizedProximity);
        }
    }

    private bool IsInsideCone(Vector3 _origin, Vector3 _forward, Vector3 _targetPosition)
    {
        Vector3 direction = (_targetPosition - _origin).normalized;
        return Vector3.Angle(_forward, direction) <= coneHalfAngle;
    }

    // ---------------------------------------------------------------
    // Gizmos de debug

    private void OnDrawGizmosSelected()
    {
        Transform origin = coneOrigin != null ? coneOrigin : transform;

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.06f);
        Gizmos.DrawSphere(origin.position, detectionRange);

        Gizmos.color = new Color(1f, 1f, 0f, 0.06f);
        Gizmos.DrawSphere(origin.position, minVoiceRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.06f);
        Gizmos.DrawSphere(origin.position, maxVoiceRange);

        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        DrawConeRay(origin, origin.up,     coneHalfAngle);
        DrawConeRay(origin, -origin.up,    coneHalfAngle);
        DrawConeRay(origin, origin.right,  coneHalfAngle);
        DrawConeRay(origin, -origin.right, coneHalfAngle);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin.position, origin.forward * detectionRange);

        if (Application.isPlaying && currentWaveRadius > 0f)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawWireSphere(origin.position, currentWaveRadius);
        }
    }

    private void DrawConeRay(Transform _origin, Vector3 _axis, float _halfAngle)
    {
        Vector3 direction = Quaternion.AngleAxis(_halfAngle, _axis) * _origin.forward;
        Gizmos.DrawRay(_origin.position, direction * detectionRange);
    }
}
