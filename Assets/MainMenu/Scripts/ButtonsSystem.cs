using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsSystem : MonoBehaviour
{
    public void Play(int _sceneId)
    {
        SceneManager.LoadScene(_sceneId);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void URLOpen(string url)
    {
        Application.OpenURL(url);
    }
    
}
