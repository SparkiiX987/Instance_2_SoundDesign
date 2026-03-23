using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VSyncToggle : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Toggle UI pour activer/désactiver le VSync")]
    public Toggle vsyncToggle;

    [Tooltip("(Optionnel) Texte affichant l'état actuel")]
    public TMP_Text statusLabel;

    private const string PREF_KEY = "VSync";

    private void Start()
    {
        bool savedValue = PlayerPrefs.GetInt(PREF_KEY, 1) == 1;

        vsyncToggle.SetIsOnWithoutNotify(savedValue);
        ApplyVSync(savedValue);
        UpdateLabel(savedValue);

        vsyncToggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        vsyncToggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        ApplyVSync(isOn);
        UpdateLabel(isOn);
        PlayerPrefs.SetInt(PREF_KEY, isOn ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    private void ApplyVSync(bool isOn)
    {
        QualitySettings.vSyncCount = isOn ? 1 : 0;
    }

    private void UpdateLabel(bool isOn)
    {
        if (statusLabel != null)
            statusLabel.text = $"VSync : {(isOn ? "ON" : "OFF")}";
    }

    public void EnableVSync()  => vsyncToggle.isOn = true;

    public void DisableVSync() => vsyncToggle.isOn = false;

    public void ToggleVSync()  => vsyncToggle.isOn = !vsyncToggle.isOn;

    public bool IsVSyncEnabled => QualitySettings.vSyncCount > 0;
}