using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TutorialInputVerif : MonoBehaviour
{
    [SerializeField] public List<Image> imagesList = new List<Image>();
    [SerializeField] public List<Sprite> spritesList = new List<Sprite>();
    private bool fadeFinish = false;

    TutorialVerifState state;
    public void Start()
    {
        EventBus.Subscribe<OnPlayerInputEnter>(TutorialButton);
        imagesList[0].CrossFadeAlpha(0, 0, false);
        imagesList[1].CrossFadeAlpha(0, 0, false);
        imagesList[2].CrossFadeAlpha(0, 0, false);
        imagesList[3].CrossFadeAlpha(0, 0, false);
        UnFade(imagesList[0], spritesList[0]);
        DOVirtual.DelayedCall(3, () => fadeFinish = true);
    }

    public void OnDestroy()
    {
        EventBus.Unsubscribe<OnPlayerInputEnter>(TutorialButton);
    }

    void TutorialButton(OnPlayerInputEnter _InputEnter)
    {
        switch (state)
        {
            case TutorialVerifState.echolocation:
                {
                    TestAbilities(_InputEnter.input);
                    break;
                }
            case TutorialVerifState.jump:
                {
                    TestAbilities(_InputEnter.input);
                    break;
                }
            case TutorialVerifState.crouch:
                {
                    TestAbilities(_InputEnter.input);
                    break;
                }
            case TutorialVerifState.movement:
                {
                    TestMovement(_InputEnter.moveDirection);
                    break;
                }
            case TutorialVerifState.leap:
                {
                    TestAbilities(_InputEnter.input);
                    break;
                }
        }
    }
    void TestAbilities(TutorialVerifState _input)
    {
        if (_input == state && fadeFinish == true && (int)state < 4)
        {
            int i = (int)state + 1;
            Fade(imagesList[0], true, spritesList[i]);
            fadeFinish = false;
            DOVirtual.DelayedCall(5, () => fadeFinish = true);
            if ((int)state == 2)
            {
                for (int j = 1; j <= 3; j++)
                {
                    UnFade(imagesList[j], spritesList[i + j]);
                }
            }
            state++;
        }
        else if (_input == state && fadeFinish == true)
        {
            Fade(imagesList[1], false, spritesList[2]);
            Fade(imagesList[2], false, spritesList[7]);
            Fade(imagesList[3], false, spritesList[1]);
            DOVirtual.DelayedCall(3, () => EventBus.Publish(new OnTutorialFinish
            {

            }));
            DOVirtual.DelayedCall(3, () => Destroy(gameObject));
        }
    }

    void TestMovement(Vector2 _input)
    {
        if (_input == Vector2.up)
        {
            imagesList[0].color = Color.green;
        }
        if (_input == Vector2.left)
        {
            imagesList[1].color = Color.green;
        }
        if (_input == Vector2.down)
        {
            imagesList[2].color = Color.green;
        }
        if (_input == Vector2.right)
        {
            imagesList[3].color = Color.green;
        }
        if (imagesList.All((Image image) => image.color == Color.green))
        {
            List<bool> boolListe = new List<bool>();
            fadeFinish = false;
            DOVirtual.DelayedCall(5, () => fadeFinish = true);
            Fade(imagesList[0], false, null);
            Fade(imagesList[1], true, spritesList[2]);
            Fade(imagesList[2], true, spritesList[7]);
            Fade(imagesList[3], true, spritesList[1]);
            DOVirtual.DelayedCall(3, () => imagesList[0].color = Color.white);
            DOVirtual.DelayedCall(3, () => imagesList[1].color = Color.white);
            DOVirtual.DelayedCall(3, () => imagesList[2].color = Color.white);
            DOVirtual.DelayedCall(3, () => imagesList[3].color = Color.white);
            state++;
        }
    }

    private void Fade(Image _inputImage, bool _reappear, Sprite _newSprite)
    {
        _inputImage.CrossFadeAlpha(0, 2, false);
        if (_reappear == true)
        {
            DOVirtual.DelayedCall(3, () => _inputImage.sprite = _newSprite);
            DOVirtual.DelayedCall(3, () => _inputImage.CrossFadeAlpha(1, 2, false));
        }
    }
    private void UnFade(Image _inputImage, Sprite _newSprite)
    {
        DOVirtual.DelayedCall(3, () => _inputImage.sprite = _newSprite);
        DOVirtual.DelayedCall(3, () => _inputImage.CrossFadeAlpha(1, 2, false));
    }
}

public enum TutorialVerifState
{
    echolocation, jump, crouch, movement, leap
}