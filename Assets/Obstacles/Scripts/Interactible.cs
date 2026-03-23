using DG.Tweening;
using FMOD;
using FMOD.Studio;
using FMODUnity;
using UnityEngine;

public class Interactible : MonoBehaviour, IInteracable
{
    [SerializeField] protected EventReference sound;

    [Header("Son FMOD")]
    [Tooltip("Evenement FMOD joue quand l'onde radar touche cet objet.")]

    [Header("Volume selon la proximite")]
    [Range(0f, 1f)][SerializeField] private float volumeMin = 0.1f;
    [Range(0f, 1f)][SerializeField] private float targetVolume = 0.1f;
    [Range(0f, 1f)][SerializeField] private float volumeMax = 1.0f;

    [Header("Pitch selon la proximite")]
    [Range(0.1f, 3f)][SerializeField] private float pitchMin = 0.5f;
    [Range(0.1f, 3f)][SerializeField] protected float targetPitch = 0.5f;
    [Range(0.1f, 3f)][SerializeField] private float pitchMax = 2.0f;

    [Header("Enveloppe sonore")]
    [SerializeField] private float fadeInDuration = 0.08f;
    [SerializeField] private float sustainDuration = 0.25f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    // TODO add FMOD sound and add methode to play sound

    public virtual void Interact()
    {
        if (!sound.IsNull)
        {
            print($"Interact with {name}");
            EventInstance instance = RuntimeManager.CreateInstance(sound);
            instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            instance.setPitch(targetPitch);
            instance.setVolume(0f);
            instance.start();

            float currentVolume = 0f;
            DOTween.Sequence()
                .Append(DOTween.To(
                    () => currentVolume,
                    v => { currentVolume = v; instance.setVolume(v); },
                    targetVolume,
                    fadeInDuration).SetEase(Ease.OutQuad))
                .AppendInterval(sustainDuration)
                .Append(DOTween.To(
                    () => currentVolume,
                    v => { currentVolume = v; instance.setVolume(v); },
                    0f,
                    fadeOutDuration).SetEase(Ease.InQuad))
                .OnComplete(() =>
                {
                    instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                    instance.release();
                });
        }   
    }
}
