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
    [SerializeField] private Toggle          muteToggle;

    private List<(int fmodIndex, string name, int rate, int channels)> _drivers
        = new List<(int, string, int, int)>();

    private VoiceTrigger _voiceTrigger;

    // ── Lifecycle ────────────────────────────────────────────────────

    private void Awake()
    {
        _voiceTrigger = new GameObject("VoiceTrigger").AddComponent<VoiceTrigger>();
    }

    private void Start()
    {
        applyButton.onClick.AddListener(OnApply);
        micDropdown.onValueChanged.AddListener(_ => { });

        if (muteToggle != null)
        {
            muteToggle.isOn = true;
            muteToggle.onValueChanged.AddListener(OnMuteToggleChanged);
        }

        RefreshDriverList();
    }

    private void OnEnable() => RefreshDriverList();

    // ── Mute ─────────────────────────────────────────────────────────

    /// <summary>Callback du Toggle UI.</summary>
    private void OnMuteToggleChanged(bool muted) => SetMicrophoneMuted(!muted);

    /// <summary>Active ou désactive le micro (appelable par code ou raccourci).</summary>
    public void SetMicrophoneMuted(bool muted)
    {
        _voiceTrigger.SetMuted(muted);

        if (muted)
        {
            VoiceTrigger voiceTrigger = GameManager.instance.player.GetComponent<VoiceTrigger>();
            voiceTrigger.StopRecording();
        }
        else
        {
            VoiceTrigger voiceTrigger = GameManager.instance.player.GetComponent<VoiceTrigger>();
            voiceTrigger.TryStartRecording();
        }

        if (muteToggle != null)
            muteToggle.SetIsOnWithoutNotify(!muted);

        UpdateStatusLabel();

        Debug.Log($"[MicroSelector] Micro {(muted ? "désactivé (muet)" : "activé")}");
    }

    /// <summary>Bascule l'état mute — utile sur un KeyCode dans Update().</summary>
    public void ToggleMute() => SetMicrophoneMuted(!_voiceTrigger.IsMuted);

    // ── Drivers ──────────────────────────────────────────────────────

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

            if (name.ToLower().Contains("loopback")) continue;

            _drivers.Add((i, name, rate, channels));
            options.Add($"{name}  ({rate / 1000}kHz)");
        }

        micDropdown.AddOptions(options.Count > 0 ? options : new List<string> { "Aucun micro detecte" });
        micDropdown.interactable = _drivers.Count > 0;
        applyButton.interactable = _drivers.Count > 0;

        SyncDropdownToActive();
        UpdateStatusLabel();
    }

    private void SyncDropdownToActive()
    {
        int active = _voiceTrigger.GetActiveDriverIndex();
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

    private void OnApply()
    {
        int chosen = micDropdown.value;
        if (chosen < 0 || chosen >= _drivers.Count) return;

        var (fmodIndex, name, rate, channels) = _drivers[chosen];
        _voiceTrigger.SelectMicrophone(fmodIndex, rate, channels);

        // Réapplique l'état mute sur le nouveau driver
        _voiceTrigger.SetMuted(_voiceTrigger.IsMuted);

        UpdateStatusLabel();
        Debug.Log($"[MicroSelector] Micro appliqué : [{fmodIndex}] {name}");
    }

    private void UpdateStatusLabel()
    {
        if (statusLabel == null) return;

        int active = _voiceTrigger.GetActiveDriverIndex();
        if (active < 0) { statusLabel.text = "Micro actif : aucun"; return; }

        FMODUnity.RuntimeManager.CoreSystem.getRecordDriverInfo(
            active, out string name, 256,
            out System.Guid _, out int _,
            out FMOD.SPEAKERMODE _, out int _,
            out FMOD.DRIVER_STATE _);

        statusLabel.text = _voiceTrigger.IsMuted
            ? $"Micro actif : {name}  [MUET]"
            : $"Micro actif : {name}";
    }
}