using DG.Tweening;
using UnityEngine;

/// <summary>
/// Parametres du sonar selon le GDD Echo Maze.
/// Valeurs definitives marquees, valeurs test en commentaire.
/// </summary>
[CreateAssetMenu(fileName = "SO_SonarSettings", menuName = "Sonar/SO_SonarSettings")]
public class SO_SonarSettings : ScriptableObject
{
    [Header("Onde")]
    [Tooltip("Vitesse de propagation : 10 m/s (GDD)")]
    public float ondeSpeed = 10f;

    [Tooltip("Portee maximale : 30m (GDD)")]
    public float range = 30f;

    [Tooltip("Cooldown entre deux ondes : 2.5s (GDD)")]
    public float cooldown = 2.5f;

    public Ease waveEase = Ease.Linear;

    [Header("Cone de detection")]
    [Range(5f, 90f)]
    public float coneHalfAngle = 45f;

    [Header("Portee dynamique (charge vocale)")]
    [Tooltip("Portee min : 15m (GDD)")]
    public float minVoiceRange = 15f;

    [Tooltip("Portee max : 30m (GDD)")]
    public float maxVoiceRange = 30f;

    [Header("Duree de revelation (shader)")]
    [Tooltip("Duree min de revelation des aretes : 7s (GDD)")]
    public float fadeDurationMin = 7f;

    [Tooltip("Duree max de revelation des aretes : 15s (GDD)")]
    public float fadeDurationMax = 15f;

    [Header("Charge vocale")]
    [Tooltip("Temps max de charge : 2s (GDD)")]
    public float chargeMaxDuration = 2f;
    public float chargeMinDuration = 0.1f;

    [Header("Detection vocale")]
    [Range(0f, 1f)] public float voiceThreshold  = 0.02f;
    [Range(0f, 1f)] public float voiceMax         = 0.3f;
    [Range(1, 30)]  public int   voiceSmoothFrames = 10;

    // ── Methodes ─────────────────────────────────────────────────────

    public float GetWaveDuration(float _range) => _range / ondeSpeed;

    public float GetVoiceRange(float _normalizedVolume)
        => Mathf.Lerp(minVoiceRange, maxVoiceRange, _normalizedVolume);

    /// <summary>
    /// Duree de revelation interpolee selon le volume normalise [0..1].
    /// 0 = fadeDurationMin, 1 = fadeDurationMax.
    /// </summary>
    public float GetFadeDuration(float _normalizedVolume)
        => Mathf.Lerp(fadeDurationMin, fadeDurationMax, _normalizedVolume);
}
