using UnityEngine;

/// <summary>
/// Profil sonore associé à une catégorie d'objet détectable.
/// Déclenché une fois quand l'onde radar touche l'objet :
/// fade in → maintien → fade out automatique.
/// Créer via : Assets > Create > Radar > RadarSoundProfile
/// </summary>
[CreateAssetMenu(fileName = "NewRadarSoundProfile", menuName = "Radar/RadarSoundProfile")]
public class RadarSoundProfile : ScriptableObject
{
    [Header("Identification")]
    public string detectableTag = "default";

    [Header("Audio")]
    [Tooltip("Clip joué quand l'onde touche l'objet.")]
    public AudioClip soundClip;

    [Header("Tonalité selon la distance")]
    [Tooltip("Pitch quand l'objet est au maximum de la portée.")]
    [Range(0.1f, 3f)]
    public float pitchMin = 0.5f;

    [Tooltip("Pitch quand l'objet est tout proche du joueur.")]
    [Range(0.1f, 3f)]
    public float pitchMax = 2.0f;

    [Header("Volume selon la distance")]
    [Tooltip("Volume quand l'objet est au maximum de la portée.")]
    [Range(0f, 1f)]
    public float volumeMin = 0.1f;

    [Tooltip("Volume quand l'objet est tout proche du joueur.")]
    [Range(0f, 1f)]
    public float volumeMax = 1.0f;

    [Header("Enveloppe sonore (DOTween)")]
    [Tooltip("Durée du fade in en secondes.")]
    public float fadeInDuration = 0.1f;

    [Tooltip("Durée de maintien du son avant le fade out (secondes).")]
    public float sustainDuration = 0.3f;

    [Tooltip("Durée du fade out en secondes.")]
    public float fadeOutDuration = 0.5f;

    // ---------------------------------------------------------------

    /// <summary>Pitch interpolé selon la proximité normalisée [0=loin, 1=proche].</summary>
    public float EvaluatePitch(float _normalizedProximity)
    {
        return Mathf.Lerp(pitchMin, pitchMax, _normalizedProximity);
    }

    /// <summary>Volume cible interpolé selon la proximité normalisée [0=loin, 1=proche].</summary>
    public float EvaluateVolume(float _normalizedProximity)
    {
        return Mathf.Lerp(volumeMin, volumeMax, _normalizedProximity);
    }
}
