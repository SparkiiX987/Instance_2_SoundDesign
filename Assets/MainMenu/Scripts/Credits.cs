using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class CreditPerson
{
    public string personName = "Prénom Nom";
    public string role = "Rôle";

    public Button buttonVoirPlus;
    public string urlOuTexte;
}

public class Credits : MonoBehaviour
{

    [Header("Données")]
    public List<CreditPerson> people = new();

    [Header("Références UI")]
    public TextMeshProUGUI   nameText, roleText;

    [Header("Animation")]
    [Range(0.1f, 0.8f)] public float duration  = 0.35f;
    [Range(0f,   1f)]   public float sideAlpha = 0.4f;
    [Range(0f,   1f)]   public float sideScale = 0.75f;

    private int  _index;
    private bool _busy;

    void Start()
    {
        Refresh();
    }

    public void GoLeft()  { if (!_busy && people.Count > 1) StartCoroutine(Slide(-1)); }
    public void GoRight() { if (!_busy && people.Count > 1) StartCoroutine(Slide( 1)); }

    IEnumerator Slide(int dir)
    {
        _busy = true;
        float t = 0, w = 420f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0, 1, t / duration);

            yield return null;
        }

        _index = (_index + dir + people.Count) % people.Count;
        Refresh();
        _busy = false;
    }

    void Refresh()
    {
        int n = people.Count, l = (_index - 1 + n) % n, r = (_index + 1) % n;

        if (nameText) nameText.text = people[_index].personName;
        if (roleText) roleText.text = people[_index].role;

        var current = people[_index];

        if (current.buttonVoirPlus != null)
        {
            current.buttonVoirPlus.onClick.RemoveAllListeners();

            current.buttonVoirPlus.onClick.AddListener(() =>
            {
                if (!string.IsNullOrEmpty(current.urlOuTexte))
                {
                    Application.OpenURL(current.urlOuTexte);
                }
                else
                {
                    Debug.Log("Voir plus sur : " + current.personName);
                }
            });
        }
    }

    static CanvasGroup Ensure(Component c)
    {
        var cg = c.GetComponent<CanvasGroup>();
        return cg ? cg : c.gameObject.AddComponent<CanvasGroup>();
    }
}