using DG.Tweening;
using UnityEngine;

/// <summary>
/// ScriptableObject centralisant tous les parametres du Sonar.
/// Creer via : Assets > Create > Sonar > SO_SonarSettings
/// </summary>
[CreateAssetMenu(fileName = "SO_SonarSettings", menuName = "Sonar/SO_SonarSettings")]
public class SO_SonarSettings : ScriptableObject
{
    [Header("Onde")]
    public float ondeSpeed = 15f;
    public float range     = 20f;
    public float cooldown  = 0.8f;
    public Ease  waveEase  = Ease.Linear;

    [Header("Cone de detection")]
    [Range(5f, 90f)]
    public float coneHalfAngle = 45f;

    [Header("Portee dynamique (voix)")]
    public float minVoiceRange = 3f;
    public float maxVoiceRange = 20f;

    [Header("Detection vocale")]
    [Range(0f, 1f)] public float voiceThreshold   = 0.02f;
    [Range(0f, 1f)] public float voiceMax          = 0.3f;
    [Range(1,  30)] public int   voiceSmoothFrames = 10;
    public float voiceSampleWindow = 0.1f;

    // ---------------------------------------------------------------

    public float GetWaveDuration(float _range)          => _range / ondeSpeed;
    public float GetVoiceRange(float _normalizedVolume) => Mathf.Lerp(minVoiceRange, maxVoiceRange, _normalizedVolume);
    public float GetNormalizedVolume(float _rawVolume)  => Mathf.Clamp01(Mathf.InverseLerp(voiceThreshold, voiceMax, _rawVolume));
}