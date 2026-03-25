using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class Settings : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown graphicsDropdown;
    [SerializeField] private Toggle toggleFullscreen;
    [SerializeField] private TMP_Dropdown dropdownResolution;

    private List<Resolution> uniqueResolutions = new List<Resolution>();

    void Start()
    {
        PopulateResolutionDropdown();
        LoadSettings();

        toggleFullscreen.onValueChanged.AddListener(OnToggleFullscreen);
        dropdownResolution.onValueChanged.AddListener(OnChangeResolution);
    }

    void PopulateResolutionDropdown()
    {
        Resolution[] allResolutions = Screen.resolutions;

        if (dropdownResolution != null)
        {
            dropdownResolution.ClearOptions();
            uniqueResolutions.Clear();
        }

        List<string> options = new List<string>();
        int currentIndex = 0;

        foreach (Resolution res in allResolutions)
        {
            string label = res.width + " x " + res.height;

            bool alreadyExists = uniqueResolutions.Exists(r =>
                r.width == res.width && r.height == res.height);

            if (!alreadyExists)
            {
                uniqueResolutions.Add(res);
                options.Add(label);

                if (res.width == Screen.currentResolution.width &&
                    res.height == Screen.currentResolution.height)
                {
                    currentIndex = uniqueResolutions.Count - 1;
                }
            }
        }
        
        dropdownResolution.AddOptions(options);
        dropdownResolution.value = currentIndex;
        dropdownResolution.RefreshShownValue();
    }

    void OnToggleFullscreen(bool isFull)
    {
        Screen.fullScreen = isFull;
        PlayerPrefs.SetInt("Fullscreen", isFull ? 1 : 0);
        PlayerPrefs.Save();
    }

    void OnChangeResolution(int index)
    {
        Resolution res = uniqueResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);

        PlayerPrefs.SetInt("ResolutionIndex", index);
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        // Fullscreen
        if (PlayerPrefs.HasKey("Fullscreen"))
        {
            bool isFull = PlayerPrefs.GetInt("Fullscreen") == 1;
            toggleFullscreen.SetIsOnWithoutNotify(isFull);
            Screen.fullScreen = isFull;
        }
        else
        {
            toggleFullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
        }

        // Résolution
        if (PlayerPrefs.HasKey("ResolutionIndex"))
        {
            int savedIndex = PlayerPrefs.GetInt("ResolutionIndex");
            if (savedIndex < uniqueResolutions.Count)
            {
                dropdownResolution.SetValueWithoutNotify(savedIndex);
                dropdownResolution.RefreshShownValue();

                Resolution res = uniqueResolutions[savedIndex];
                Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            }
        }
    }

    public void ChangeGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(graphicsDropdown.value);
        PlayerPrefs.SetInt("GraphicsQuality", graphicsDropdown.value);
        PlayerPrefs.Save();
    }
}