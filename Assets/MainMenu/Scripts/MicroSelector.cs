using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class MicroSelector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TMP_Dropdown    micDropdown;
    [SerializeField] private TextMeshProUGUI statusLabel;
    [SerializeField] private Button          applyButton;

    private List<(int fmodIndex, string name, int rate, int channels)> _drivers
        = new List<(int, string, int, int)>();

    private void Start()
    {
        applyButton.onClick.AddListener(OnApply);
        micDropdown.onValueChanged.AddListener(_ => { });
        RefreshDriverList();
    }

    private void OnEnable() => RefreshDriverList();

    private void RefreshDriverList()
    {
        _drivers.Clear();
        micDropdown.ClearOptions();

        FMOD.System core = FMODUnity.RuntimeManager.CoreSystem;
        core.getRecordNumDrivers(out int numDrivers, out int _);

        var options = new List<string>();

        for (int i = 0; i < numDrivers; i++)
        {
            core.getRecordDriverInfo(
                i, out string name, 256,
                out System.Guid _, out int rate,
                out FMOD.SPEAKERMODE _, out int channels,
                out FMOD.DRIVER_STATE state);

            if (name.ToLower().Contains("loopback")) { continue; }

            _drivers.Add((i, name, rate, channels));
            options.Add($"{name}  ({rate / 1000}kHz)");
        }

        micDropdown.AddOptions(options.Count > 0 ? options : new List<string> { "Aucun micro detecte" });
        micDropdown.interactable = _drivers.Count > 0;
        applyButton.interactable = _drivers.Count > 0;

        SyncDropdownToActive();
        UpdateStatusLabel();
    }

    // ---------------------------------------------------------------
    //  Synchronisation dropdown / driver actif
    // ---------------------------------------------------------------

    private void SyncDropdownToActive()
    {
        VoiceTrigger voiceTrigger = new VoiceTrigger();
        
        int active = voiceTrigger.GetActiveDriverIndex();

        for (int i = 0; i < _drivers.Count; i++)
        {
            if (_drivers[i].fmodIndex == active)
            {
                micDropdown.SetValueWithoutNotify(i);
                return;
            }
        }

        micDropdown.SetValueWithoutNotify(0);
    }

    // ---------------------------------------------------------------
    //  Validation — appelle SelectMicrophone(), repris de StartRecordingOnDriver()
    // ---------------------------------------------------------------

    private void OnApply()
    {
        VoiceTrigger voiceTrigger = new VoiceTrigger();
        
        int chosen = micDropdown.value;
        if (chosen < 0 || chosen >= _drivers.Count) { return; }

        var (fmodIndex, name, rate, channels) = _drivers[chosen];
        voiceTrigger.SelectMicrophone(fmodIndex, rate, channels);

        UpdateStatusLabel();
        Debug.Log($"[MicrophoneSelector] Micro appliqué : [{fmodIndex}] {name}");
    }

    // ---------------------------------------------------------------
    //  Label statut
    // ---------------------------------------------------------------

    private void UpdateStatusLabel()
    {
        VoiceTrigger voiceTrigger = new VoiceTrigger();
        
        if (statusLabel == null) { return; }

        int active = voiceTrigger.GetActiveDriverIndex();
        if (active < 0) { statusLabel.text = "Micro actif : aucun"; return; }

        FMODUnity.RuntimeManager.CoreSystem.getRecordDriverInfo(
            active, out string name, 256,
            out System.Guid _, out int _,
            out FMOD.SPEAKERMODE _, out int _,
            out FMOD.DRIVER_STATE _);

        statusLabel.text = $"Micro actif : {name}";
    }
}