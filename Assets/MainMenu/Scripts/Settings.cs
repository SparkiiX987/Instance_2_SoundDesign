using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

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
        List<string> options = new List<string>();

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
    
    void OnToggleFullscreen(bool _isFull)
    {
        Screen.fullScreen = _isFull;
    }

    void OnChangeResolution(int _index)
    {
        Resolution res = resolutions[_index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
    }

    public void ChangeGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(graphicsDropdown.value);
    }
}
