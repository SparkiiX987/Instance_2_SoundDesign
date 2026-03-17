using UnityEngine;

public class TutorialInputVerif : MonoBehaviour
{
    bool keyZPressed = false;
    bool keySPressed = false;
    bool keyQPressed = false;
    bool keyDPressed = false;
    bool keyEPressed = false;
    bool keyJumpPressed = false;

    void KeyZPressed()
    {
        keyZPressed = true;
        LookIfAllKeyPressed();
    }

    void KeySPressed()
    {
        keySPressed = true;
        LookIfAllKeyPressed();
    }
    void KeyQPressed()
    {
        keySPressed = true;
        LookIfAllKeyPressed();
    }
    void KeyDPressed()
    {
        keySPressed = true;
        LookIfAllKeyPressed();
    }
    void KeyEPressed()
    {
        keySPressed = true;
        LookIfAllKeyPressed();
    }
    void KeySpacePressed()
    {
        keySPressed = true;
        LookIfAllKeyPressed();

    }
    void LookIfAllKeyPressed()
    {

    }
}
