using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialInputVerif : MonoBehaviour
{
    bool keyZPressed = false;
    bool keySPressed = false;
    bool keyQPressed = false;
    bool keyDPressed = false;
    bool keyEPressed = false;
    bool keyCPressed = false;
    bool keyJumpPressed = false;
    bool fadeFinish = false;
    bool movementDone = false;
    [SerializeField] int fadeValue;
    [SerializeField] int fadeDuration;
    Vector2 upDirection = new Vector2 (0, 1);
    Vector2 downDirection = new Vector2(0, -1);
    Vector2 leftDirection = new Vector2(-1, 0);
    Vector2 rightDirection = new Vector2(1, 0);
    [SerializeField] Image upButton;
    [SerializeField] Image downButton;
    [SerializeField] Image leftButton;
    [SerializeField] Image rightButton;
    [SerializeField] Sprite z;
    [SerializeField] Sprite q;
    [SerializeField] Sprite s;
    [SerializeField] Sprite d;
    [SerializeField] Sprite e;
    [SerializeField] Sprite space;
    [SerializeField] Sprite c;
    [SerializeField] Sprite plus;

    public void Start()
    {
        EventBus.Subscribe<OnPlayerInputEnter>(TutorialButton);
        upButton.CrossFadeAlpha(0, 0, false);
        downButton.CrossFadeAlpha(0, 0, false);
        rightButton.CrossFadeAlpha(0, 0, false);
        leftButton.CrossFadeAlpha(0, 0, false);
        UnFade(upButton, e);
        DOVirtual.DelayedCall(3, () => fadeFinish =true);
    }

    public void OnDestroy()
    {
        EventBus.Unsubscribe<OnPlayerInputEnter>(TutorialButton);
    }

    void TutorialButton(OnPlayerInputEnter _InputEnter)
    {
        if(_InputEnter.input == "echolocation" && keyEPressed ==false && fadeFinish==true)
        {
            Fade(upButton, true, space);
            fadeFinish = false;
            keyEPressed = true;
            DOVirtual.DelayedCall(5, () => fadeFinish = true);
        }
        if(_InputEnter.input == "jump" && keyEPressed ==true && fadeFinish==true)
        {
            Fade(upButton, true, c);
            fadeFinish = false;
            keyJumpPressed = true;
            DOVirtual.DelayedCall(5, () => fadeFinish = true);
        }
        if(_InputEnter.input == "crouch" && keyJumpPressed == true  && fadeFinish == true)
        {
            Fade(upButton, true, z);
            fadeFinish = false;
            DOVirtual.DelayedCall(5, () => fadeFinish = true);
            keyCPressed = true;
            UnFade(leftButton, q);
            UnFade(downButton, s);
            UnFade(rightButton, d);
        }
        if (_InputEnter.moveDirection.Equals(upDirection) && keyCPressed == true && fadeFinish == true)
        {
            keyZPressed = true;
            upButton.color = Color.green;
        }
        if (_InputEnter.moveDirection.Equals(downDirection) && keyCPressed == true && fadeFinish == true)
        {
            keySPressed = true;
            downButton.color = Color.green;
        }
        if (_InputEnter.moveDirection.Equals(leftDirection) && keyCPressed == true && fadeFinish == true)
        {
            keyQPressed = true;
            leftButton.color = Color.green;
        }
        if (_InputEnter.moveDirection.Equals(rightDirection) && keyCPressed == true && fadeFinish == true)
        {
            keyDPressed = true;
            rightButton.color = Color.green;
        }
        if (keyZPressed && keySPressed && keyQPressed && keyDPressed && movementDone == false)
        {
            DOVirtual.DelayedCall(3, () => downButton.color = Color.white);
            DOVirtual.DelayedCall(3, () => upButton.color = Color.white);
            DOVirtual.DelayedCall(3, () => leftButton.color = Color.white);
            DOVirtual.DelayedCall(3, () => rightButton.color = Color.white);
            Fade(upButton, false, null);
            Fade(leftButton, true, c);
            Fade(downButton, true, plus);
            Fade(rightButton, true, space);
            DOVirtual.DelayedCall(5, () => fadeFinish = true);
            movementDone = true;
        }
        if (_InputEnter.input == "leap" && movementDone == true && fadeFinish == true)
        {
            Fade(leftButton, false, c);
            Fade(downButton, false, plus);
            Fade(rightButton, false, space);
            DOVirtual.DelayedCall(3, () => EventBus.Publish(new OnTutorialFinish
            {

            }));
            Destroy(gameObject);
        }
    }

    private void Fade(Image _inputImage, bool _reappear,Sprite _newSprite)
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
        DOVirtual.DelayedCall(3,() => _inputImage.sprite = _newSprite);
        DOVirtual.DelayedCall(3,() => _inputImage.CrossFadeAlpha(1, 2, false));
    }
}
