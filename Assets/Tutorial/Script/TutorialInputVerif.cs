using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils;

public class TutorialInputVerif : MonoBehaviour
{
    bool keyZPressed = false;
    bool keySPressed = false;
    bool keyQPressed = false;
    bool keyDPressed = false;
    bool keyEPressed = false;
    bool keyJumpPressed = false;
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
    [SerializeField] TextMeshPro upText;
    [SerializeField] TextMeshPro leftText;
    [SerializeField] TextMeshPro rightText;
    [SerializeField] TextMeshPro DownText;
    [SerializeField] private Rigidbody cageRigidBody;

    public void Start()
    {
        EventBus.Subscribe<OnPlayerInputEnter>(TutorialButton);
    }

    public void OnDestroy()
    {
        EventBus.Unsubscribe<OnPlayerInputEnter>(LookIfAllKeyPressed);
    }

    void TutorialButton(OnPlayerInputEnter _InputEnter)
    {
        if(_InputEnter.input == "echolocation" && keyEPressed ==false)
        {
            upButton.enabled = false;
            keyEPressed = true;
            upButton.enabled = true;
        }
        if(_InputEnter.input == "jump" && keyJumpPressed == false  && keyEPressed ==true)
        {
            keyJumpPressed = true;
            leftButton.enabled = true;
            rightButton.enabled = true;
            downButton.enabled = true;
        }
    }
    void LookIfAllKeyPressed(OnPlayerInputEnter _InputEnter)
    {
       if (_InputEnter.input == "jump")
        {
            keyJumpPressed = true;
        }
        if (_InputEnter.input == "echolocation")
        {
            keyEPressed = true;
        }
        if (_InputEnter.moveDirection.Equals(upDirection))
        {
            keyZPressed = true;
        }
        if (_InputEnter.moveDirection.Equals(downDirection))
        {
            keySPressed = true;
        }
        if (_InputEnter.moveDirection.Equals(leftDirection))
        {
            keyQPressed = true;
        }
        if (_InputEnter.moveDirection.Equals(rightDirection))
        {
            keyDPressed = true;
        }
        if (keyZPressed && keySPressed &&  keyQPressed && keyDPressed && keyEPressed && keyJumpPressed)
        {
            FinishTutorial();
        }
    }

    private void FinishTutorial()
    {
        cageRigidBody.isKinematic = false;
    }
    
    private void Fade(Image button)
    {

    }

    private void UnFade(Image button)
    {

    }
}
