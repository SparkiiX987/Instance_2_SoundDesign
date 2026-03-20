using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private bool isActive;

    private void OnEnable()
    {
        EventBus.Subscribe<OnPaused>(AnimsPauseMenu);
    }
    
    private void OnDisable()
    {
        EventBus.Unsubscribe<OnPaused>(AnimsPauseMenu);
    }
    
    public void ReturnPauseMenu(int _sceneId)
    {
        SceneManager.LoadScene(_sceneId);
    }

    public void AnimsPauseMenu(OnPaused _onPaused)
    {
        if (!isActive)
        {
            animator.SetBool("IsOpen", true);
            animator.SetBool("IsClose", false);
            isActive = true;
            return;
        }

        if (isActive)
        {
            animator.SetBool("IsClose", true);
            animator.SetBool("IsOpen", false);
            isActive = false;
        }
    }
}
