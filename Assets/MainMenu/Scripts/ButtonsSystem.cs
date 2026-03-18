using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonsSystem : MonoBehaviour
{
    [SerializeField] private GameObject panelMainMenu;
    [SerializeField] private GameObject panelSettings;
    
    public void Play(int _sceneId)
    {
        SceneManager.LoadScene(_sceneId);
    }

    public void Settings()
    {
        panelSettings.SetActive(true);
        panelMainMenu.SetActive(false);
    }

    public void Credits()
    {
        SceneManager.LoadScene("Credits");
    }

    public void Quit()
    {
        Application.Quit();
    }
}
