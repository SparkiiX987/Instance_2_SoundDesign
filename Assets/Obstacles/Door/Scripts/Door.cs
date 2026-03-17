using UnityEngine;

[RequireComponent (typeof(Animator))]
public class Door : Interactible
{
    protected Animator doorAnimator;

    protected bool doorOpen;

    private void Awake()
    {
        if (doorAnimator == null)
        {
            doorAnimator = GetComponent<Animator>();
        }
    }

    protected virtual void Open()
    {
        doorOpen = true;
        Interact();
    }

    protected virtual void Close()
    {
        doorOpen = false;
        Interact();
    }

    public override void Interact()
    {
        base.Interact();

        if (doorOpen)
        {
            doorAnimator.SetTrigger("Open");
        }
        else
        {
            doorAnimator.SetTrigger("Close");
        }
    }
}
