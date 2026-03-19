using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SonarEffect : MonoBehaviour
{
    [SerializeField] private RectTransform sonarRingRef;
    [SerializeField] private Image sonarImageRef;

    [Header("Sonar Effect")]
    private static RectTransform sonarRing;
    private static Image sonarImage;

    [Header("Textes")]
    [SerializeField] private TextMeshProUGUI[] tmpText;
    [SerializeField] private Image[] images;
    [Header("Paramètres")]
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float interval = 3f;
    [SerializeField] private Color sonarColor = Color.white;

    private Sequence sonarSequence;

    void Awake()
    {
        if (sonarRingRef != null) sonarRing = sonarRingRef;
        if (sonarImageRef != null) sonarImage = sonarImageRef;

        sonarImage.color = new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0f);

        /*foreach (TextMeshProUGUI text in tmpText)
            text.color = new Color(0f, 0f, 0f, 1f);

        if (images != null)
        {
            foreach (Image image in images)
                image.color = new Color(0f, 0f, 0f, 1f);
        }*/
    }

    private void Start()
    {
        StartCoroutine(SonarLoop());
    }

    private IEnumerator SonarLoop()
    {
        while (true)
        {
            PlaySonar();
            yield return new WaitForSeconds(interval);
        }
    }

    private void PlaySonar()
    {
        Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        sonarRing.position = mousePosition;
        sonarRing.localScale = Vector3.one;
        sonarImage.color = new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0.0f);
        sonarImage.gameObject.SetActive(true);

        /*foreach (TextMeshProUGUI text in tmpText)
            text.color = new Color(0f, 0f, 0f, 1f);

        foreach (Image image in images)
            image.color = new Color(0f, 0f, 0f, 1f);*/

        sonarSequence?.Kill();
        sonarSequence = DOTween.Sequence();

        sonarSequence.Append(
            sonarRing.DOScale(maxScale, duration).SetEase(Ease.OutCubic)
        );
        sonarSequence.Join(
            sonarImage.DOFade(0f, duration).SetEase(Ease.InQuad)
        );

        /*foreach (TextMeshProUGUI text in tmpText)
            sonarSequence.Join(
                text.DOColor(Color.white, 0.5f).SetEase(Ease.InQuad)
            );

        foreach (Image image in images)
            sonarSequence.Join(
                image.DOColor(Color.white, 0.5f).SetEase(Ease.InQuad)
            );*/

        sonarSequence.OnComplete(() =>
        {
            sonarRing.localScale = Vector3.one;
            sonarImage.color = new Color(sonarColor.r, sonarColor.g, sonarColor.b, 0f);
            sonarImage.gameObject.SetActive(false);

            /*foreach (TextMeshProUGUI text in tmpText)
                text.DOColor(Color.black, 1.5f);

            foreach (Image image in images)
                image.DOColor(Color.black, 1.5f);*/
        });
    }
}