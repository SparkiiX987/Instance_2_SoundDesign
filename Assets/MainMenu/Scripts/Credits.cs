using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CreditPerson
{
    public Sprite portrait;
    public string personName = "Prénom Nom";
    public string role = "Rôle";
}

public class Credits : MonoBehaviour
{

    [Header("Données")]
    public List<CreditPerson> people = new();

    [Header("Références UI")]
    public Image  leftCard, centerCard, rightCard;
    public TextMeshProUGUI   nameText, roleText;

    [Header("Animation")]
    [Range(0.1f, 0.8f)] public float duration  = 0.35f;
    [Range(0f,   1f)]   public float sideAlpha = 0.4f;
    [Range(0f,   1f)]   public float sideScale = 0.75f;

    private int  _index;
    private bool _busy;
    private CanvasGroup _cgL, _cgC, _cgR;

    void Start()
    {
        _cgL = Ensure(leftCard);
        _cgC = Ensure(centerCard);
        _cgR = Ensure(rightCard);
        Refresh();
        
        centerCard.preserveAspect = true;
    }

    public void GoLeft()  { if (!_busy && people.Count > 1) StartCoroutine(Slide(-1)); }
    public void GoRight() { if (!_busy && people.Count > 1) StartCoroutine(Slide( 1)); }

    IEnumerator Slide(int dir)
    {
        _busy = true;
        float t = 0, w = 420f;

        var cL = leftCard  .GetComponent<RectTransform>();
        var cC = centerCard.GetComponent<RectTransform>();
        var cR = rightCard .GetComponent<RectTransform>();

        Vector2 sL = cL.anchoredPosition, sC = cC.anchoredPosition, sR = cR.anchoredPosition;
        Vector2 eL = sL + Vector2.right * (-dir * w);
        Vector2 eC = sC + Vector2.right * (-dir * w);
        Vector2 eR = sR + Vector2.right * (-dir * w);

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0, 1, t / duration);

            cL.anchoredPosition = Vector2.Lerp(sL, eL, k);
            cC.anchoredPosition = Vector2.Lerp(sC, eC, k);
            cR.anchoredPosition = Vector2.Lerp(sR, eR, k);

            _cgC.alpha = Mathf.Lerp(1f, sideAlpha, k);
            if (dir == 1) { _cgR.alpha = Mathf.Lerp(sideAlpha, 1f, k); _cgL.alpha = Mathf.Lerp(sideAlpha, 0f, k); }
            else          { _cgL.alpha = Mathf.Lerp(sideAlpha, 1f, k); _cgR.alpha = Mathf.Lerp(sideAlpha, 0f, k); }

            yield return null;
        }

        _index = (_index + dir + people.Count) % people.Count;
        Refresh();
        _busy = false;
    }

    void Refresh()
    {
        int n = people.Count, l = (_index - 1 + n) % n, r = (_index + 1) % n;

        leftCard  .sprite = people[l].portrait;
        centerCard.sprite = people[_index].portrait;
        rightCard .sprite = people[r].portrait;

        if (nameText) nameText.text = people[_index].personName;
        if (roleText) roleText.text = people[_index].role;

        var cL = leftCard  .GetComponent<RectTransform>();
        var cC = centerCard.GetComponent<RectTransform>();
        var cR = rightCard .GetComponent<RectTransform>();

        cL.anchoredPosition = new Vector2(-420f, 0f);
        cC.anchoredPosition = Vector2.zero;
        cR.anchoredPosition = new Vector2( 420f, 0f);

        cL.localScale = Vector3.one * sideScale;
        cC.localScale = Vector3.one;
        cR.localScale = Vector3.one * sideScale;

        _cgL.alpha = sideAlpha;
        _cgC.alpha = 1f;
        _cgR.alpha = sideAlpha;
        
        centerCard.preserveAspect = true;
    }

    static CanvasGroup Ensure(Component c)
    {
        var cg = c.GetComponent<CanvasGroup>();
        return cg ? cg : c.gameObject.AddComponent<CanvasGroup>();
    }
}