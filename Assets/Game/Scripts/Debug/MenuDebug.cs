using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuDebug : MonoBehaviour
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }
}
