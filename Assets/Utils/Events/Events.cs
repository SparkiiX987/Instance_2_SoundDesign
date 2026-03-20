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

}
public struct OnTutorialFinish
{

}

public struct OnPlayerDetectGround
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
    public string input;
    public Vector2 moveDirection;
}
