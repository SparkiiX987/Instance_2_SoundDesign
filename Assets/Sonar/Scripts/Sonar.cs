using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Composant Sonar du joueur.
/// Gere l'emission de l'onde, la detection des objets et l'appel OnProb().
/// Utilise SO_SonarSettings pour tous les parametres de reglage.
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

    // ---------------------------------------------------------------

    private float currentWaveRadius;
    private float previousWaveRadius;
    private float activeRange;
    private float cooldownTimer;
    private HashSet<IDetectable> hitObjects = new HashSet<IDetectable>();
    private Tween waveTween;

    // ---------------------------------------------------------------

    private void Awake()
    {
        if (coneOrigin == null) coneOrigin = transform;

        if (settings == null)
            Debug.LogError("[Sonar] SO_SonarSettings non assigne !");
    }

    private void Update()
    {
        cooldownTimer -= Time.deltaTime;

        if (Input.GetKeyDown(activationKey) && cooldownTimer <= 0f)
            TriggerWave();
    }

    // ---------------------------------------------------------------

    /// <summary>Declenche l'onde a portee maximale (touche E ou appel externe).</summary>
    public void TriggerWave()
    {
        if (cooldownTimer > 0f) return;
        EmitWave(settings.range);
    }

    /// <summary>
    /// Declenche l'onde avec portee dynamique selon volume normalise [0..1].
    /// Appelee par VoiceTrigger — plus le joueur crie fort, plus l'onde va loin.
    /// </summary>
    public void TriggerWaveWithVolume(float _normalizedVolume)
    {
        if (cooldownTimer > 0f) return;
        EmitWave(settings.GetVoiceRange(_normalizedVolume));
    }

    // ---------------------------------------------------------------

    private void EmitWave(float _range)
    {
        cooldownTimer      = settings.cooldown;
        activeRange        = Mathf.Clamp(_range, settings.minVoiceRange, settings.range);
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
            settings.GetWaveDuration(activeRange)
        )
        .SetEase(settings.waveEase)
        .OnComplete(() => currentWaveRadius = 0f);
    }

    private void OnWaveStep(Vector3 _originPos, Vector3 _originFwd)
    {
        Collider[] hits = Physics.OverlapSphere(_originPos, currentWaveRadius, detectableLayerMask);

        foreach (Collider hit in hits)
        {
            IDetectable detectable = hit.GetComponent<IDetectable>();
            if (detectable == null || !detectable.IsActive()) continue;
            if (hitObjects.Contains(detectable)) continue;

            Vector3 position = detectable.GetPosition();
            float distance   = Vector3.Distance(_originPos, position);

            if (distance < previousWaveRadius) continue;
            if (!IsInsideCone(_originPos, _originFwd, position)) continue;

            Vector3 dir = (position - _originPos).normalized;
            if (Physics.Raycast(_originPos, dir, distance, obstacleMask)) continue;

            hitObjects.Add(detectable);

            float normalizedProximity = Mathf.Clamp01(1f - (distance / activeRange));
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

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.06f);
        Gizmos.DrawSphere(origin.position, settings.range);

        Gizmos.color = new Color(1f, 1f, 0f, 0.06f);
        Gizmos.DrawSphere(origin.position, settings.minVoiceRange);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.06f);
        Gizmos.DrawSphere(origin.position, settings.maxVoiceRange);

        Gizmos.color = new Color(0f, 1f, 0.5f, 1f);
        DrawConeRay(origin, origin.up,     settings.coneHalfAngle);
        DrawConeRay(origin, -origin.up,    settings.coneHalfAngle);
        DrawConeRay(origin, origin.right,  settings.coneHalfAngle);
        DrawConeRay(origin, -origin.right, settings.coneHalfAngle);

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin.position, origin.forward * settings.range);

        if (Application.isPlaying && currentWaveRadius > 0f)
        {
            Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.9f);
            Gizmos.DrawWireSphere(origin.position, currentWaveRadius);
        }
    }

    private void DrawConeRay(Transform _origin, Vector3 _axis, float _halfAngle)
    {
        Vector3 direction = Quaternion.AngleAxis(_halfAngle, _axis) * _origin.forward;
        Gizmos.DrawRay(_origin.position, direction * settings.range);
    }
}
