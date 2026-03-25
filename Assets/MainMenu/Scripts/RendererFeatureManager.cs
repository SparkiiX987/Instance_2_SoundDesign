using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

public class RendererFeatureManager : MonoBehaviour
{
    [Header("URP Renderer Feature Settings")]
    public UniversalRendererData rendererData;
    public string featureName = "FullScreenPassRendererFeature";

    [Header("UI")]
    public Toggle toggleUI;

    private string playerPrefKey = "FullScreenPassEnabled";

    void Start()
    {
       
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
        
        SetFeatureActive(isOn);

        
        PlayerPrefs.SetInt(playerPrefKey, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
}