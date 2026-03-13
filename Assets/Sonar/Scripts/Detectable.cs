using DG.Tweening;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Composant a attacher sur tout objet devant etre detecte par le sonar.
/// Implemente IDetectable et gere son propre son FMOD via OnProb().
/// </summary>
public class Detectable : MonoBehaviour, IDetectable
{
    [Header("Parametres globaux")]
    [SerializeField] private SO_SonarSettings settings;

    [Header("Detection")]
    [SerializeField] private string detectableTag = "default";
    [SerializeField] private bool isActiveOnStart = true;

    [Header("Son FMOD")]
    [SerializeField] private EventReference sound;

    [Header("Volume selon la distance")]
    [Range(0f, 1f)] [SerializeField] private float volumeMin = 0.1f;
    [Range(0f, 1f)] [SerializeField] private float volumeMax = 1.0f;

    [Header("Pitch selon la distance")]
    [Range(0.1f, 3f)] [SerializeField] private float pitchMin = 0.5f;
    [Range(0.1f, 3f)] [SerializeField] private float pitchMax = 2.0f;

    // ---------------------------------------------------------------

    private bool active;
    private AudioSource audioSource;

    // ---------------------------------------------------------------

    private void Awake()
    {
        active      = isActiveOnStart;
        audioSource = GetComponent<AudioSource>();
    }

    // ---------------------------------------------------------------
    // IDetectable

    public Vector3 GetPosition()     => transform.position;
    public string GetDetectableTag() => detectableTag;
    public bool IsActive()           => active && gameObject.activeInHierarchy;

    /// <summary>
    /// Appele par le Sonar quand l'onde touche cet objet.
    /// _normalizedProximity : 0 = loin, 1 = proche.
    /// </summary>
    public void OnProb(float _normalizedProximity)
    {
        if (sound.IsNull) return;

        float fadeIn  = settings != null ? settings.soundFadeIn  : 0.08f;
        float sustain = settings != null ? settings.soundSustain : 0.25f;
        float fadeOut = settings != null ? settings.soundFadeOut : 0.5f;

        float targetVolume = Mathf.Lerp(volumeMin, volumeMax, _normalizedProximity);
        float targetPitch  = Mathf.Lerp(pitchMin,  pitchMax,  _normalizedProximity);

        FMOD.Studio.EventInstance instance = RuntimeManager.CreateInstance(sound);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        instance.setVolume(0f);
        instance.setPitch(targetPitch);
        instance.start();

        // Enveloppe via DOTween sur le volume FMOD
        float currentVolume = 0f;
        DOTween.Sequence()
            .Append(DOTween.To(
                () => currentVolume,
                volume =>
                {
                    currentVolume = volume;
                    instance.setVolume(volume);
                },
                targetVolume,
                fadeIn).SetEase(Ease.OutQuad))
            .AppendInterval(sustain)
            .Append(DOTween.To(
                () => currentVolume,
                volume =>
                {
                    currentVolume = volume;
                    instance.setVolume(volume);
                },
                0f,
                fadeOut).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                instance.release();
            });
    }

    // ---------------------------------------------------------------

    /// <summary>Active ou desactive la detectabilite de l'objet.</summary>
    public void SetActive(bool _value)
    {
        active = _value;
    }
}
