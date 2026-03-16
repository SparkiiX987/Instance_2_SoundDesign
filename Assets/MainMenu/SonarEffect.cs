using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using DG.Tweening;
using TMPro;
using System.Collections;

public class SonarEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform sonarRingRef;
    [SerializeField] private Image sonarImageRef;

    private static RectTransform sonarRing;
    private static Image sonarImage;

    [Header("Texte à révéler")]
    [SerializeField] private TextMeshProUGUI tmpText;

    [Header("Paramètres")]
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float spawnDelay = 0.5f;
    [SerializeField] private Color sonarColor = Color.white;

    private Sequence sonarSequence;
    private bool isHovered = false;
    private static SonarEffect currentActive = null;

    void Awake()
    {
        if (sonarRingRef != null) sonarRing = sonarRingRef;
        if (sonarImageRef != null) sonarImage = sonarImageRef;

        tmpText.color = new Color(0f, 0f, 0f, 1f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentActive != null && currentActive != this)
            return;

        isHovered = true;
        currentActive = this;
        StartCoroutine(DelayedSonar());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;

        if (currentActive == this)
            currentActive = null;

        HideText();
    }

    private IEnumerator DelayedSonar()
    {
        yield return new WaitForSeconds(spawnDelay);

        if (!isHovered) yield break;

        PlaySonar();
    }

    private void PlaySonar()
    {
        if (sonarSequence != null && sonarSequence.IsActive() && sonarSequence.IsPlaying())
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        sonarRing.position = mousePosition;
        sonarRing.localScale = Vector3.one;
        sonarImage.color = new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0.8f);
        tmpText.color = new Color(0f, 0f, 0f, 1f);

        sonarSequence?.Kill();
        sonarSequence = DOTween.Sequence();

        sonarSequence.Append(
            sonarRing.DOScale(maxScale, duration).SetEase(Ease.OutCubic)
        );
        sonarSequence.Join(
            sonarImage.DOFade(0f, duration).SetEase(Ease.InQuad)
        );
        sonarSequence.Join(
            tmpText.DOColor(Color.white, duration).SetEase(Ease.InQuad)
        );

        sonarSequence.OnComplete(() =>
        {
            sonarRing.localScale = Vector3.one;
            sonarImage.color = new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0f);

            if (isHovered) PlaySonar();
            else
            {
                currentActive = null;
                HideText();
            }
        });
    }

    private void HideText()
    {
        sonarSequence?.Kill();
        tmpText.DOColor(Color.black, 0.3f);

        sonarRing.localScale = Vector3.one;
        sonarImage.color = new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0f);
    }
}