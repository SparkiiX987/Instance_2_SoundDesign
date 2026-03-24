using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class RendererFeatureManager : MonoBehaviour
{
    [Header("URP Renderer Feature Settings")]
    public UniversalRendererData rendererData;
    public string featureName = "FullScreenPassRendererFeature";

    [Header("UI")]
    public Toggle toggleUI; // Ton toggle dans les options

    private string playerPrefKey = "FullScreenPassEnabled";

    void Start()
    {
        // Assure que le listener est bien ajouté au toggle
        if (toggleUI != null)
        {
            toggleUI.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    void SetFeatureActive(bool isEnabled)
    {
        foreach (var feature in rendererData.rendererFeatures)
        {
            if (feature.name == featureName)
            {
                feature.SetActive(isEnabled);
                break;
            }
        }
        rendererData.SetDirty();
    }

    public void OnToggleChanged(bool isOn)
    {
        // Active ou désactive la feature seulement quand tu cliques sur le toggle
        SetFeatureActive(isOn);

        // Sauvegarde l'état dans PlayerPrefs pour la prochaine session
        PlayerPrefs.SetInt(playerPrefKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
}