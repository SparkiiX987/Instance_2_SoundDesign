using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    
    public Toggle toggleFullscreen;
    public TMP_Dropdown dropdownResolution;

    Resolution[] resolutions;
    
    void Start()
    {
        resolutions = Screen.resolutions;

        dropdownResolution.ClearOptions();
        var options = new System.Collections.Generic.List<string>();

        int indexResolutionActuelle = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            if (!options.Contains(option))
                options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                indexResolutionActuelle = i;
            }
        }

        dropdownResolution.AddOptions(options);
        dropdownResolution.value = indexResolutionActuelle;
        dropdownResolution.RefreshShownValue();

        // toggleFullscreen.isOn = Screen.fullScreen;

        toggleFullscreen.onValueChanged.AddListener(OnToggleFullscreen);
        dropdownResolution.onValueChanged.AddListener(OnChangeResolution);
    }
    
    void OnToggleFullscreen(bool isFull)
    {
        Screen.fullScreen = isFull;
    }

    void OnChangeResolution(int index)
    {
        Resolution res = resolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void ChangeGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(graphicsDropdown.value);
    }
}
