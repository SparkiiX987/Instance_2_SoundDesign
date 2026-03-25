using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialInputVerif : MonoBehaviour
{
    private const string PrefKey = "TutorialCompleted";

    [SerializeField] private List<CanvasGroup> canvasGroups = new();
    [SerializeField] private List<TextMeshProUGUI> textList = new();
    [SerializeField] private List<Image> imagesList = new();
    [SerializeField] private List<Sprite> spritesList = new();
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private bool resetTutorial;

    private TutorialVerifState state;
    private Tween activeTween;
    private bool canProgress;
    private readonly HashSet<Vector2> validatedDirections = new();

    // Cached key names
    private string sonarKey;
    private string jumpKey;
    private string crouchKey;
    private string moveUpKey;
    private string moveDownKey;
    private string moveLeftKey;
    private string moveRightKey;

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
        EventBus.Subscribe<OnPlayerInputEnter>(TutorialButton);

        foreach (CanvasGroup group in canvasGroups)
            group.alpha = 0;

        state = TutorialVerifState.echolocation;
        imagesList[0].sprite = spritesList[0];
        SetTextForState(state);

        canProgress = false;
        activeTween = canvasGroups[0].DOFade(1, fadeDuration)
            .OnComplete(() => canProgress = true);
    }

    public void OnDestroy()
    {
        EventBus.Unsubscribe<OnPlayerInputEnter>(TutorialButton);
        activeTween?.Kill();
    }

    private void CacheBindingNames()
    {
        InputActionMap playerMap = inputActions.FindActionMap("Player");
        InputActionMap uiMap = inputActions.FindActionMap("UI");

        sonarKey = FormatKey(uiMap?.FindAction("Sonar"));
        jumpKey = FormatKey(playerMap?.FindAction("Jump"));
        crouchKey = FormatKey(playerMap?.FindAction("Crouch"));

        InputAction moveAction = playerMap?.FindAction("Move");
        if (moveAction != null && moveAction.bindings.Count > 4)
        {
            moveUpKey = FormatKeyPath(moveAction.bindings[1].effectivePath);
            moveDownKey = FormatKeyPath(moveAction.bindings[2].effectivePath);
            moveLeftKey = FormatKeyPath(moveAction.bindings[3].effectivePath);
            moveRightKey = FormatKeyPath(moveAction.bindings[4].effectivePath);
        }
        else
        {
            moveUpKey = "W";
            moveDownKey = "S";
            moveLeftKey = "A";
            moveRightKey = "D";
        }
    }

    private static string FormatKey(InputAction action)
    {
        if (action == null) return "?";
        string display = action.GetBindingDisplayString(0);
        return NormalizeKeyName(display);
    }

    private static string FormatKeyPath(string path)
    {
        string display = InputControlPath.ToHumanReadableString(path);
        return NormalizeKeyName(display);
    }

    private static string NormalizeKeyName(string key)
    {
        if (string.IsNullOrEmpty(key)) return "?";

        string lower = key.ToLower();
        if (lower == "space" || lower == "espace")
            return "ESP";

        // Return uppercase single letter
        if (key.Length == 1)
            return key.ToUpper();

        return key;
    }

    private void SetTextForState(TutorialVerifState tutorialState)
    {
        switch (tutorialState)
        {
            case TutorialVerifState.echolocation:
                if (textList.Count > 0) textList[0].text = sonarKey;
                break;
            case TutorialVerifState.jump:
                if (textList.Count > 0) textList[0].text = jumpKey;
                break;
            case TutorialVerifState.crouch:
                if (textList.Count > 0) textList[0].text = crouchKey;
                break;
            case TutorialVerifState.movement:
                if (textList.Count > 0) textList[0].text = moveUpKey;
                if (textList.Count > 1) textList[1].text = moveLeftKey;
                if (textList.Count > 2) textList[2].text = moveDownKey;
                if (textList.Count > 3) textList[3].text = moveRightKey;
                break;
            case TutorialVerifState.leap:
                if (textList.Count > 1) textList[1].text = crouchKey;
                if (textList.Count > 2) textList[2].text = "+";
                if (textList.Count > 3) textList[3].text = jumpKey;
                break;
        }
    }

    private void TutorialButton(OnPlayerInputEnter inputEnter)
    {
        if (state == TutorialVerifState.movement)
            TestMovement(inputEnter.moveDirection);
        else
            TestAbilities(inputEnter.input);
    }

    private void TestAbilities(TutorialVerifState input)
    {
        if (input != state)
            return;

        if (!canProgress)
            activeTween?.Kill();

        AdvanceState();
    }

    private void AdvanceState()
    {
        canProgress = false;
        activeTween?.Kill();

        int stateIndex = (int)state;

        if (stateIndex < 3)
        {
            int nextSprite = stateIndex + 1;
            bool isMovementNext = stateIndex == 2;

            state++;
            SetTextForState(state);

            Sequence seq = DOTween.Sequence();

            seq.Append(canvasGroups[0].DOFade(0, fadeDuration));

            seq.AppendCallback(() =>
            {
                imagesList[0].sprite = spritesList[nextSprite];

                if (isMovementNext)
                {
                    for (int j = 1; j <= 3; j++)
                    {
                        imagesList[j].sprite = spritesList[nextSprite + j];
                        canvasGroups[j].alpha = 0;
                    }
                }
            });

            seq.Append(canvasGroups[0].DOFade(1, fadeDuration));
            if (isMovementNext)
            {
                seq.Join(canvasGroups[1].DOFade(1, fadeDuration));
                seq.Join(canvasGroups[2].DOFade(1, fadeDuration));
                seq.Join(canvasGroups[3].DOFade(1, fadeDuration));
            }

            seq.OnComplete(() => canProgress = true);
            activeTween = seq;
        }
        else if (state == TutorialVerifState.leap)
        {
            Sequence seq = DOTween.Sequence();
            for (int i = 1; i <= 3; i++)
                seq.Join(canvasGroups[i].DOFade(0, fadeDuration));

            seq.OnComplete(CompleteTutorial);
            activeTween = seq;
        }
    }

    private void TestMovement(Vector2 input)
    {
        if (!canProgress)
            return;

        if (input == Vector2.up)    { validatedDirections.Add(Vector2.up);    imagesList[0].DOColor(goodColor, fadeDuration); }
        if (input == Vector2.left)  { validatedDirections.Add(Vector2.left);  imagesList[1].DOColor(goodColor, fadeDuration); }
        if (input == Vector2.down)  { validatedDirections.Add(Vector2.down);  imagesList[2].DOColor(goodColor, fadeDuration); }
        if (input == Vector2.right) { validatedDirections.Add(Vector2.right); imagesList[3].DOColor(goodColor, fadeDuration); }

        if (validatedDirections.Count < 4)
            return;

        canProgress = false;
        state++;
        SetTextForState(state);

        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < 4; i++)
            seq.Join(canvasGroups[i].DOFade(0, fadeDuration));

        seq.AppendCallback(() =>
        {
            foreach (Image img in imagesList)
                img.color = Color.white;

            imagesList[1].sprite = spritesList[2];
            imagesList[2].sprite = spritesList[7];
            imagesList[3].sprite = spritesList[1];
        });

        seq.Append(canvasGroups[1].DOFade(1, fadeDuration));
        seq.Join(canvasGroups[2].DOFade(1, fadeDuration));
        seq.Join(canvasGroups[3].DOFade(1, fadeDuration));

        seq.OnComplete(() => canProgress = true);
        activeTween = seq;
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
    jump,
    crouch,
    movement,
    leap
}