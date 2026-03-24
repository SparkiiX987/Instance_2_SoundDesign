using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TutorialInputVerif : MonoBehaviour
{
    private const string PrefKey = "TutorialCompleted";

    [SerializeField] private List<CanvasGroup> canvasGroups = new();
    [SerializeField] private List<Image> imagesList = new();
    [SerializeField] private List<Sprite> spritesList = new();
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] private Color goodColor = Color.green;
    [SerializeField] private bool resetTutorial;

    private TutorialVerifState state;
    private Tween activeTween;
    private bool canProgress;
    private readonly HashSet<Vector2> validatedDirections = new();

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

        EventBus.Subscribe<OnPlayerInputEnter>(TutorialButton);

        foreach (CanvasGroup group in canvasGroups)
            group.alpha = 0;

        state = TutorialVerifState.echolocation;
        imagesList[0].sprite = spritesList[0];

        canProgress = false;
        activeTween = canvasGroups[0].DOFade(1, fadeDuration)
            .OnComplete(() => canProgress = true);
    }

    public void OnDestroy()
    {
        EventBus.Unsubscribe<OnPlayerInputEnter>(TutorialButton);
        activeTween?.Kill();
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

        // If mid-transition (fade in), interrupt and skip to next
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

            Sequence seq = DOTween.Sequence();

            // Fade out current instruction
            seq.Append(canvasGroups[0].DOFade(0, fadeDuration));

            // Swap sprite(s) at the midpoint
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

            // Fade in new instruction(s)
            seq.Append(canvasGroups[0].DOFade(1, fadeDuration));
            if (isMovementNext)
            {
                seq.Join(canvasGroups[1].DOFade(1, fadeDuration));
                seq.Join(canvasGroups[2].DOFade(1, fadeDuration));
                seq.Join(canvasGroups[3].DOFade(1, fadeDuration));
            }

            seq.OnComplete(() => canProgress = true);
            activeTween = seq;
            state++;
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

        Sequence seq = DOTween.Sequence();

        // Fade out all movement images
        for (int i = 0; i < 4; i++)
            seq.Join(canvasGroups[i].DOFade(0, fadeDuration));

        // Swap to leap sprites
        seq.AppendCallback(() =>
        {
            foreach (Image img in imagesList)
                img.color = Color.white;

            imagesList[1].sprite = spritesList[2];
            imagesList[2].sprite = spritesList[7];
            imagesList[3].sprite = spritesList[1];
        });

        // Fade in leap images (1-3)
        seq.Append(canvasGroups[1].DOFade(1, fadeDuration));
        seq.Join(canvasGroups[2].DOFade(1, fadeDuration));
        seq.Join(canvasGroups[3].DOFade(1, fadeDuration));

        seq.OnComplete(() => canProgress = true);
        activeTween = seq;
        state++;
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