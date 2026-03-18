using AudioSystem;
using FMODUnity;
using UnityEngine;

/// <summary>
/// Composant a attacher sur tout objet detectable par le radar.
/// Utilise exclusivement FMOD pour le son — aucun AudioSource, aucun AudioClip.
/// Prerequis : FMOD for Unity importe dans le projet.
/// </summary>
public class DetectableObject : MonoBehaviour, IDetectable
{
    [Header("Detection")]
    [SerializeField] private string detectableTag = "default";
    [SerializeField] private bool isActiveOnStart = true;

    [Header("Son FMOD")]
    [Tooltip("Evenement FMOD joue quand l'onde radar touche cet objet.")]
    [SerializeField] private EventReference sound;

    [Header("Volume selon la proximite")]
    [Range(0f, 1f)][SerializeField] private float volumeMin = 0.1f;
    [Range(0f, 1f)][SerializeField] private float volumeMax = 1.0f;

    [Header("Pitch selon la proximite")]
    [Range(0.1f, 3f)][SerializeField] private float pitchMin = 0.5f;
    [Range(0.1f, 3f)][SerializeField] private float pitchMax = 2.0f;

    [Header("Enveloppe sonore")]
    [SerializeField] private float fadeInDuration = 0.08f;
    [SerializeField] private float sustainDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.5f;

    // ---------------------------------------------------------------

    private bool _active;

    // ---------------------------------------------------------------

    private void Awake()
    {
        _active = isActiveOnStart;
    }

    // ---------------------------------------------------------------
    // IDetectable

    public Vector3 GetPosition() => transform.position;
    public string GetDetectableTag() => detectableTag;
    public bool IsActive() => _active && gameObject.activeInHierarchy;

    /// <summary>
    /// Appele par RadarSystem quand l'onde touche cet objet.
    /// Lance un evenement FMOD avec enveloppe fade in -> sustain -> fade out.
    /// normalizedProximity : 0 = loin, 1 = proche.
    /// </summary>
    public void OnProb(float normalizedProximity)
    {
        if (!sound.IsNull)
        {
            GameAudioManager.instance.PlayOneShot(sound, transform.position);
            return;

            //float targetVolume = Mathf.Lerp(volumeMin, volumeMax, normalizedProximity);
            //float targetPitch = Mathf.Lerp(pitchMin, pitchMax, normalizedProximity);
            //
            //EventInstance instance = RuntimeManager.CreateInstance(sound);
            //instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            //instance.setPitch(targetPitch);
            //instance.setVolume(0f);
            //instance.start();
            //
            //float currentVolume = 0f;
            //DOTween.Sequence()
            //    .Append(DOTween.To(
            //        () => currentVolume,
            //        v => { currentVolume = v; instance.setVolume(v); },
            //        targetVolume,
            //        fadeInDuration).SetEase(Ease.OutQuad))
            //    .AppendInterval(sustainDuration)
            //    .Append(DOTween.To(
            //        () => currentVolume,
            //        v => { currentVolume = v; instance.setVolume(v); },
            //        0f,
            //        fadeOutDuration).SetEase(Ease.InQuad))
            //    .OnComplete(() =>
            //    {
            //        instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            //        instance.release();
            //    });
        }


    }

    // ---------------------------------------------------------------

    public void SetActive(bool _value) => _active = _value;
}
