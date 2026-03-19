using UnityEngine;

public class Interactible : MonoBehaviour, IInteracable
{
    // TODO add FMOD sound and add methode to play sound

    public virtual void Interact()
    {
        print($"Interact with {name}");
    }
}
