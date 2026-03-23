using DG.Tweening;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    private RectTransform rectTransform;
    private CanvasGroup group;
    [SerializeField] private float WaveCooldown;
    [SerializeField] private float WaveDuration;
    [SerializeField] private float WaveMaxSize;
    [SerializeField, Range(0, 1)] private float WaveMinimumAlpha;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        group = GetComponent<CanvasGroup>();
    }

    void Start()
    {
        DOVirtual.DelayedCall(0.25f, StartCheckLoop);
    }

    private void StartCheckLoop()
    {
        group.alpha = 1;
        transform.position = Input.mousePosition;
        rectTransform.sizeDelta = new Vector2(0, 0);
        print(group.alpha);

        rectTransform.DOSizeDelta(new Vector2(WaveMaxSize, WaveMaxSize), WaveDuration);

        DOVirtual.DelayedCall(WaveDuration, () => group.DOFade(WaveMinimumAlpha, WaveCooldown/3f));

        DOVirtual.DelayedCall(WaveCooldown, StartCheckLoop);
    }
}