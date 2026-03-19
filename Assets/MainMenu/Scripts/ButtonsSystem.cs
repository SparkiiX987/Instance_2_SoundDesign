using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsSystem : MonoBehaviour
{
    public void Play()
    {
        SceneManager.LoadScene("ProtoDeplacements");
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
