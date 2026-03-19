using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public void ReturnMainMenu(int _sceneId)
    {
        SceneManager.LoadScene(_sceneId);
    }
}
