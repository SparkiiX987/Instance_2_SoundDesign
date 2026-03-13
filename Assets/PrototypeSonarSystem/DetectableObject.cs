using DG.Tweening;
using UnityEngine;

/// <summary>
/// Composant a attacher sur tout objet devant etre detecte par le radar.
/// L'objet gere lui-meme son son quand l'onde le touche (OnProb).
/// Prerequis : DOTween importe dans le projet.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DetectableObject : MonoBehaviour, IDetectable
{
    [Header("Detection")]
    [SerializeField] private string detectableTag = "default";
    [SerializeField] private bool isActiveOnStart = true;

    [Header("Son de l'objet")]
    [Tooltip("Clip joue quand l'onde radar touche cet objet.")]
    [SerializeField] private AudioClip soundClip;

    [Header("Volume selon la distance")]
    [Tooltip("Volume quand l'objet est loin du joueur.")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMin = 0.1f;

    [Tooltip("Volume quand l'objet est proche du joueur.")]
    [Range(0f, 1f)]
    [SerializeField] private float volumeMax = 1.0f;

    [Header("Pitch selon la distance")]
    [Tooltip("Pitch quand l'objet est loin du joueur.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float pitchMin = 0.5f;

    [Tooltip("Pitch quand l'objet est proche du joueur.")]
    [Range(0.1f, 3f)]
    [SerializeField] private float pitchMax = 2.0f;

    [Header("Enveloppe sonore")]
    [SerializeField] private float fadeInDuration  = 0.08f;
    [SerializeField] private float sustainDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    // ---------------------------------------------------------------

    private bool active;
    private AudioSource audioSource;

    // ---------------------------------------------------------------

    private void Awake()
    {
        active      = isActiveOnStart;
        audioSource = GetComponent<AudioSource>();

        // Configurer l'AudioSource
        audioSource.clip        = soundClip;
        audioSource.loop        = false;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1f;
        audioSource.volume      = 0f;
    }

    // ---------------------------------------------------------------
    // IDetectable

    public Vector3 GetPosition()      => transform.position;
    public string GetDetectableTag()  => detectableTag;
    public bool IsActive()            => active && gameObject.activeInHierarchy;

    /// <summary>
    /// Appele par le RadarSystem quand l'onde touche cet objet.
    /// Joue le son avec une enveloppe fade in -> sustain -> fade out.
    /// normalizedProximity : 0 = loin, 1 = proche.
    /// </summary>
    public void OnProb(float normalizedProximity)
    {
        if (soundClip == null) return;

        // Stopper le tween en cours si l'objet est detecte plusieurs fois
        DOTween.Kill(audioSource);

        float targetVolume = Mathf.Lerp(volumeMin, volumeMax, normalizedProximity);
        float targetPitch  = Mathf.Lerp(pitchMin,  pitchMax,  normalizedProximity);

        audioSource.pitch  = targetPitch;
        audioSource.volume = 0f;
        audioSource.Play();

        DOTween.Sequence()
            .SetTarget(audioSource)
            .Append(DOTween.To(
                () => audioSource.volume,
                v  => audioSource.volume = v,
                targetVolume,
                fadeInDuration).SetEase(Ease.OutQuad))
            .AppendInterval(sustainDuration)
            .Append(DOTween.To(
                () => audioSource.volume,
                v  => audioSource.volume = v,
                0f,
                fadeOutDuration).SetEase(Ease.InQuad))
            .OnComplete(() => audioSource.Stop());
    }

    // ---------------------------------------------------------------

    /// <summary>Active ou desactive la detectabilite de l'objet.</summary>
    public void SetActive(bool _value) => active = _value;
}
