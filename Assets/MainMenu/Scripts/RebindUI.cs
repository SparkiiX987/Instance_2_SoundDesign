using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class RebindUI : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private InputActionReference actionReference;
    [SerializeField] private int bindingIndex = 0;
    [SerializeField] private TMP_Text bindingDisplayText;
    [SerializeField] private GameObject waitingPanel;

    private InputActionRebindingExtensions.RebindingOperation rebindOperation;
    private const string PREFS_KEY = "InputBindings";

    void Start()
    {
        LoadBindings();
        UpdateDisplay();
    }

    public void StartRebind()
    {
        if (actionReference == null) return;
        
        rebindOperation?.Cancel();
        rebindOperation?.Dispose();
        rebindOperation = null;

        var action = actionReference.action;
        
        action.Disable();

        waitingPanel?.SetActive(true);

        rebindOperation = action
            .PerformInteractiveRebinding(bindingIndex)
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithCancelingThrough("<Keyboard>/escape")
            .OnMatchWaitForAnother(0.1f)
            .OnComplete(op => RebindComplete())
            .OnCancel(op => RebindCanceled())
            .Start();
    }

    private void RebindComplete()
    {
        rebindOperation?.Dispose();
        rebindOperation = null;
        actionReference.action.Enable();
        waitingPanel?.SetActive(false);
        UpdateDisplay();
        SaveBindings();
    }

    private void RebindCanceled()
    {
        rebindOperation?.Dispose();
        rebindOperation = null;
        actionReference.action.Enable();
        waitingPanel?.SetActive(false);
    }

    public void ResetBinding()
    {
        actionReference.action.RemoveBindingOverride(bindingIndex);
        UpdateDisplay();
        SaveBindings();
    }

    private void UpdateDisplay()
    {
        if (bindingDisplayText == null || actionReference == null) return;
        
        bindingDisplayText.text = InputControlPath.ToHumanReadableString(
            actionReference.action.bindings[bindingIndex].effectivePath,
            InputControlPath.HumanReadableStringOptions.OmitDevice
        );
    }

    private void SaveBindings()
    {
        var asset = actionReference.asset;
        string json = asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadBindings()
    {
        if (!PlayerPrefs.HasKey(PREFS_KEY)) return;
        
        string json = PlayerPrefs.GetString(PREFS_KEY);
        actionReference.asset.LoadBindingOverridesFromJson(json);
    }

    void OnDestroy()
    {
        rebindOperation?.Dispose();
    }
}