using DG.Tweening;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasGroup group;
    [SerializeField] private float waveCooldown;
    [SerializeField] private float waveDuration;
    [SerializeField] private float waveMaxSize;
    [SerializeField] private float fadeDuration;
    [SerializeField, Range(0, 1)] private float waveMinimumAlpha;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        DOVirtual.DelayedCall(0.25f, PlayWave);
    }

    void PlayWave()
    {
        rectTransform.sizeDelta = Vector2.zero;
        group.alpha = 1;
        rectTransform.position = Input.mousePosition;

        DOTween.Sequence()
            .Append(rectTransform.DOSizeDelta(new Vector2(waveMaxSize, waveMaxSize), waveDuration))
            .Append(group.DOFade(waveMinimumAlpha, fadeDuration))
            .OnComplete(PlayWave);
    }
}