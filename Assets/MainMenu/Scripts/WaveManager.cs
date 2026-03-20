using DG.Tweening;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private float WaveCooldown;
    [SerializeField] private float WaveDuration;
    [SerializeField] private float WaveMaxSize;

    void Start()
    {
        StartCheckLoop();
    }

    private void StartCheckLoop()
    {
        transform.position = Input.mousePosition;
        rectTransform.sizeDelta = new Vector2(0, 0);

        rectTransform.DOSizeDelta(new Vector2(WaveMaxSize, WaveMaxSize), WaveDuration)/*.SetEase(AnimationHelper.IN_SMOOTH)*/;

        DOVirtual.DelayedCall(WaveCooldown, StartCheckLoop);
    }
}