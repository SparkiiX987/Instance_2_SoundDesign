using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BrightnessSlider : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Le Slider UI pour la luminosité")]
    public Slider brightnessSlider;

    [Tooltip("Le TextMeshPro qui affiche la valeur numérique")]
    public TMP_Text valueLabel;

    [Header("Paramètres")]
    [Range(0f, 100f)]
    [Tooltip("Valeur initiale de luminosité (0 = noir, 100 = normal, 200 = très lumineux)")]
    public float defaultBrightness = 100f;

    [Tooltip("Afficher le symbole % après la valeur")]
    public bool showPercentSymbol = true;

    [Tooltip("Image ou RawImage à affecter")]
    public Graphic targetGraphic;

    void Start()
    {
        if (brightnessSlider == null)
        {
            Debug.LogError("[BrightnessSlider] Aucun Slider assigné !");
            return;
        }

        brightnessSlider.minValue = 0f;
        brightnessSlider.maxValue = 100f;
        brightnessSlider.wholeNumbers = true;
        brightnessSlider.value = defaultBrightness;

        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);

        UpdateLabel(defaultBrightness);
        ApplyBrightness(defaultBrightness);
    }
    
    void OnBrightnessChanged(float value)
    {
        UpdateLabel(value);
        ApplyBrightness(value);
    }
    
    void UpdateLabel(float value)
    {
        if (valueLabel == null) return;

        string text = Mathf.RoundToInt(value).ToString();
        if (showPercentSymbol) text += "%";
        valueLabel.text = text;
    }
    
    void ApplyBrightness(float value)
    {
        float normalized = value / 100f;

        if (targetGraphic != null)
        {
            Color c = targetGraphic.color;
            c.r = normalized;
            c.g = normalized;
            c.b = normalized;
            targetGraphic.color = c;
        }
    }

    void OnDestroy()
    {
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.RemoveListener(OnBrightnessChanged);
    }
}