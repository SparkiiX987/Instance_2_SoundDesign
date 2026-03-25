using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialInputVerif : MonoBehaviour
{
    private const string PrefKey = "TutorialCompleted";

    [SerializeField] private List<TutorialStep> steps = new();
    [SerializeField] private List<CanvasGroup> canvasGroups = new();
    [SerializeField] private List<TextMeshProUGUI> textList = new();
    [SerializeField] private List<Image> imagesList = new();
    [SerializeField] private List<Sprite> spritesList = new();
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color goodColor = Color.gray;
    [SerializeField] private bool resetTutorial;
    [SerializeField] private TextMeshProUGUI actionInputDesc;

    private int currentStepIndex;
    private Tween activeTween;
    private bool canProgress;
    private readonly HashSet<Vector2> validatedDirections = new();
    private Dictionary<string, string> cachedKeys = new();

    public void Start()
    {
        if (resetTutorial)
            PlayerPrefs.DeleteKey(PrefKey);

        if (PlayerPrefs.GetInt(PrefKey, 0) == 1)
        {
            EventBus.Publish(new OnTutorialFinish());
            Destroy(gameObject);
            return;
        }

        CacheBindingNames();
        EventBus.Subscribe<OnPlayerInputEnter>(OnPlayerInput);

        foreach (CanvasGroup group in canvasGroups)
            group.alpha = 0;

        currentStepIndex = 0;
        ApplyStep(currentStepIndex);
        FadeInStep(currentStepIndex, _onComplete: () => canProgress = true);
    }

    public void OnDestroy()
    {
        EventBus.Unsubscribe<OnPlayerInputEnter>(OnPlayerInput);
        activeTween?.Kill();
    }

    private void OnPlayerInput(OnPlayerInputEnter _inputEnter)
    {
        if (currentStepIndex >= steps.Count) return;

        TutorialStep step = steps[currentStepIndex];

        switch (step.stepType)
        {
            case StepType.Movement:
                HandleMovementInput(_inputEnter.moveDirection);
                break;

            case StepType.SingleInput:
                HandleSingleInput(_inputEnter.input);
                break;
        }
    }

    private void HandleSingleInput(TutorialVerifState _input)
    {
        int expectedStateValue = currentStepIndex; 
        if ((int)_input != expectedStateValue) return;

        if (!canProgress) activeTween?.Kill();
        AdvanceStep();
    }

    private void HandleMovementInput(Vector2 _input)
    {
        if (!canProgress) return;

        TutorialStep step = steps[currentStepIndex];

        if (_input == Vector2.up) { validatedDirections.Add(Vector2.up); imagesList[0].DOColor(goodColor, fadeDuration); }
        if (_input == Vector2.left) { validatedDirections.Add(Vector2.left); imagesList[1].DOColor(goodColor, fadeDuration); }
        if (_input == Vector2.down) { validatedDirections.Add(Vector2.down); imagesList[2].DOColor(goodColor, fadeDuration); }
        if (_input == Vector2.right) { validatedDirections.Add(Vector2.right); imagesList[3].DOColor(goodColor, fadeDuration); }

        if (validatedDirections.Count < 4) return;

        canProgress = false;
        validatedDirections.Clear();

        Sequence seq = DOTween.Sequence();

        foreach (int i in step.canvasGroupIndices)
            seq.Join(canvasGroups[i].DOFade(0, fadeDuration));

        seq.AppendCallback(() =>
        {
            foreach (Image img in imagesList)
                img.color = Color.white;
        });

        seq.AppendCallback(() => AdvanceStep());
        activeTween = seq;
    }

    private void AdvanceStep()
    {
        canProgress = false;
        activeTween?.Kill();

        Sequence seq = DOTween.Sequence();

        TutorialStep current = steps[currentStepIndex];
        foreach (int i in current.canvasGroupIndices)
            seq.Join(canvasGroups[i].DOFade(0, fadeDuration));

        seq.AppendInterval(fadeDuration);

        currentStepIndex++;

        if (currentStepIndex >= steps.Count)
        {
            seq.OnComplete(CompleteTutorial);
        }
        else
        {
            seq.AppendCallback(() => ApplyStep(currentStepIndex));
            seq.Append(BuildFadeInSequence(currentStepIndex));
            seq.OnComplete(() => canProgress = true);
        }

        activeTween = seq;
    }

    private void ApplyStep(int _index)
    {
        TutorialStep step = steps[_index];

        if (step.spriteIndex < spritesList.Count)
            imagesList[0].sprite = spritesList[step.spriteIndex];

        foreach (Vector2Int pair in step.extraSprites)
        {
            if (pair.x < imagesList.Count && pair.y < spritesList.Count)
                imagesList[pair.x].sprite = spritesList[pair.y];
        }

        foreach (TextSlotBinding slot in step.textSlots)
        {
            if (slot.textIndex >= textList.Count) continue;

            string resolved = string.IsNullOrEmpty(slot.actionName)
                ? slot.fixedText
                : cachedKeys.GetValueOrDefault(slot.actionName, "?");

            textList[slot.textIndex].text = resolved;
        }

        actionInputDesc.text = step.inputAction;

        foreach (int i in step.canvasGroupIndices)
            canvasGroups[i].alpha = 0;
    }

    private void FadeInStep(int _index, TweenCallback _onComplete = null)
    {
        activeTween = BuildFadeInSequence(_index);
        if (_onComplete != null) activeTween.OnComplete(_onComplete);
        activeTween.Play();
    }

    private Sequence BuildFadeInSequence(int _index)
    {
        Sequence seq = DOTween.Sequence();
        TutorialStep step = steps[_index];

        foreach (int i in step.canvasGroupIndices)
            seq.Join(canvasGroups[i].DOFade(1, fadeDuration));

        return seq;
    }

    private void CacheBindingNames()
    {
        InputActionMap playerMap = inputActions.FindActionMap("Player");
        if (playerMap == null) return;

        foreach (InputAction action in playerMap.actions)
            cachedKeys[action.name] = FormatKey(action);

        InputAction moveAction = playerMap.FindAction("Move");
        if (moveAction != null && moveAction.bindings.Count > 4)
        {
            cachedKeys["MoveUp"] = FormatKeyPath(moveAction.GetBindingDisplayString(1));
            cachedKeys["MoveDown"] = FormatKeyPath(moveAction.GetBindingDisplayString(2));
            cachedKeys["MoveLeft"] = FormatKeyPath(moveAction.GetBindingDisplayString(3));
            cachedKeys["MoveRight"] = FormatKeyPath(moveAction.GetBindingDisplayString(4));
        }
    }

    private static string FormatKey(InputAction _action)
    {
        if (_action == null) return "?";
        string display = _action.GetBindingDisplayString(0);
        display = display.Replace("Press ", string.Empty);
        return NormalizeKeyName(display);
    }

    private static string FormatKeyPath(string _path)
    {
        string display = InputControlPath.ToHumanReadableString(_path);
        display = display.Replace(" [Keyboard]", string.Empty)
                         .Replace("[", string.Empty)
                         .Replace("]", string.Empty)
                         .Replace(" ", string.Empty);
        return NormalizeKeyName(display);
    }

    private static string NormalizeKeyName(string _key)
    {
        if (string.IsNullOrEmpty(_key)) return "?";
        string lower = _key.ToLower();
        if (lower == "space" || lower == "espace") return "ESP";
        if (_key.Length == 1) return _key.ToUpper();
        return _key;
    }

    private void CompleteTutorial()
    {
        PlayerPrefs.SetInt(PrefKey, 1);
        PlayerPrefs.Save();
        EventBus.Publish(new OnTutorialFinish());
        Destroy(gameObject);
    }
}
public enum TutorialVerifState
{
    echolocation,
    movement,
    sprint,
    jump,
    crouch,
    leap
}