using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool isActive;

    PlayerPause playerPause;

    private void Start()
    {
        EventBus.Subscribe<OnPaused>(AnimsPauseMenu);

        playerPause = GameManager.instance.player.GetComponent<PlayerPause>();
    }
    
    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnPaused>(AnimsPauseMenu);
    }
    
    public void ReturnPauseMenu(int _sceneId)
    {
        SceneManager.LoadScene(_sceneId);
    }

    public void AnimsPauseMenu(OnPaused _onPaused)
    {
        ChangePauseState();
    }

    public void ChangePauseState()
    {
        animator.SetBool("IsClose", isActive);
        animator.SetBool("IsOpen", !isActive);
        playerPause.SetPlayerInputActive(isActive);
        isActive = !isActive;
    }

    public void ReturnToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
