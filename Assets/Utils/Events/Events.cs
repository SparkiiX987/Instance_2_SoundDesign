using UnityEngine;

public struct OnSwitchOnEvent
{
    public int switchId;
}

public struct OnSwitchOffEvent
{
    public int switchId;
}

public struct OnTrapEnter
{

}

public struct OnDefeat
{

}

public struct OnVictory
{
    
    public CanvasGroup canvasGroup;
    public RectTransform rectTransform;

    public OnVictory(CanvasGroup _canvasGroup, RectTransform _rectTransform)
    {
        canvasGroup = _canvasGroup;
        rectTransform = _rectTransform;
    }
}
public struct OnTutorialFinish
{

}

public struct OnPlayerDetectGround
{

}

public struct OnPlayerLeaveGround
{

}

public struct OnPlayerCrouch
{
    
}

public struct OnPlayerUnCrouch
{

}

public struct OnPaused
{
    
}

public struct OnEnableInput
{

}

public struct OnDisableInput
{

}

public struct OnPlayerEnterConduit
{

}

public struct OnPlayerExitConduit
{

}

public struct OnPlayerInputEnter
{
    public TutorialVerifState input;
    public Vector2 moveDirection;
}
public struct AlarmeSetActive
{
    public Vector3 playerPosition;
}

public struct  OnLevelEnd
{
    public Vector3 newSpawnPoint;
}