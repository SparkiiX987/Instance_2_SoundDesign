using DG.Tweening;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasGroup group;
    [SerializeField] private float WaveCooldown;
    [SerializeField] private float WaveDuration;
    [SerializeField] private float WaveMaxSize;
    [SerializeField] private float FadeDuration;
    [SerializeField, Range(0, 1)] private float WaveMinimumAlpha;

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
            .Append(rectTransform.DOSizeDelta(new Vector2(WaveMaxSize, WaveMaxSize), WaveDuration))
            .Append(group.DOFade(WaveMinimumAlpha, FadeDuration))
            .OnComplete(PlayWave);
    }
}