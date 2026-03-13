using DG.Tweening;
using UnityEngine;

/// <summary>
/// ScriptableObject centralisant tous les parametres de reglage du Sonar.
/// Creer plusieurs presets via : Assets > Create > Sonar > SO_SonarSettings
/// </summary>
[CreateAssetMenu(fileName = "SO_SonarSettings", menuName = "Sonar/SO_SonarSettings")]
public class SO_SonarSettings : ScriptableObject
{
    [Header("Onde")]
    [Tooltip("Vitesse de propagation de l'onde en metres par seconde.")]
    public float ondeSpeed = 15f;

    [Tooltip("Portee maximale de l'onde en metres.")]
    public float range = 20f;

    [Tooltip("Delai minimum entre deux ondes en secondes.")]
    public float cooldown = 0.8f;

    [Tooltip("Courbe d'acceleration de l'onde.")]
    public Ease waveEase = Ease.Linear;

    [Header("Cone de detection")]
    [Tooltip("Demi-angle du cone en degres (45 = cone de 90 degres total).")]
    [Range(5f, 90f)]
    public float coneHalfAngle = 45f;

    [Header("Portee dynamique (voix)")]
    [Tooltip("Portee minimale quand le joueur parle doucement.")]
    public float minVoiceRange = 3f;

    [Tooltip("Portee maximale quand le joueur crie fort.")]
    public float maxVoiceRange = 20f;

    [Header("Enveloppe sonore des objets detectes")]
    [Tooltip("Duree du fade in en secondes.")]
    public float soundFadeIn = 0.08f;

    [Tooltip("Duree de maintien du son avant le fade out.")]
    public float soundSustain = 0.25f;

    [Tooltip("Duree du fade out en secondes.")]
    public float soundFadeOut = 0.5f;

    [Header("Detection vocale")]
    [Tooltip("Volume micro minimum pour declencher l'onde.")]
    [Range(0f, 1f)]
    public float voiceThreshold = 0.02f;

    [Tooltip("Volume micro considere comme cri maximum.")]
    [Range(0f, 1f)]
    public float voiceMax = 0.3f;

    [Tooltip("Lissage du volume sur N frames pour eviter les faux declenchements.")]
    [Range(1, 30)]
    public int voiceSmoothFrames = 10;

    [Tooltip("Duree d'analyse du micro en secondes.")]
    public float voiceSampleWindow = 0.1f;

    // ---------------------------------------------------------------
    // Methodes calculees

    /// <summary>Duree de l'onde pour une portee donnee selon ondeSpeed.</summary>
    public float GetWaveDuration(float _range)
    {
        return _range / ondeSpeed;
    }

    /// <summary>Portee interpolee selon un volume normalise [0..1].</summary>
    public float GetVoiceRange(float _normalizedVolume)
    {
        return Mathf.Lerp(minVoiceRange, maxVoiceRange, _normalizedVolume);
    }

    /// <summary>Volume normalise entre voiceThreshold et voiceMax.</summary>
    public float GetNormalizedVolume(float _rawVolume)
    {
        return Mathf.Clamp01(Mathf.InverseLerp(voiceThreshold, voiceMax, _rawVolume));
    }
}
