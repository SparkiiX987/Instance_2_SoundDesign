using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class SonarEffect : MonoBehaviour
{
    [Header("Paramètres")]
    [SerializeField] private float maxScale = 3f;
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float interval = 3f;

    [SerializeField] private Material TMPWaveMat;

    [SerializeField] private TextMeshProUGUI[] texts;

    private List<Material> materials = new List<Material>();

    private void Awake()
    {
        foreach (TextMeshProUGUI text in texts)
        {
            text.fontMaterial = Instantiate(TMPWaveMat);
            materials.Add(text.fontMaterial);
        }
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
        Vector3 mouse = Input.mousePosition;
        mouse.z = 10f;

        Vector3 world = Camera.main.ScreenToWorldPoint(mouse);
        ShowMatPos();

        UpdateMatPos(new Vector4(world.x, world.y, 0, 0));

        //DOTween.To(() => radius,
        //    x =>
        //    {
        //        radius = x;
        //        UpdateMatRadius(radius);
        //        ShowMatRadius();
        //    },
        //    maxScale,
        //    duration)
        //    .OnComplete(() => UpdateMatRadius(0));
    }

    private void UpdateMatPos(Vector4 _value)
    {
        foreach (Material mat in materials)
        {
            mat.SetVector("_MousePos", _value);
        }
    }

    private void UpdateMatRadius(float _value)
    {
        foreach (Material mat in materials)
        {
            mat.SetFloat("_WaveRadius", _value);
        }
    }

    private void ShowMatPos()
    {
        foreach (Material mat in materials)
        {
            mat.GetVector("_MousePos");
        }
    }

    private void ShowMatRadius()
    {
        foreach (Material mat in materials)
        {
            mat.GetFloat("_WaveRadius");
        }
    }
}