using UnityEngine;

[DefaultExecutionOrder(-10)]
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private CanvasGroup victoryCanvasGroup;
    [SerializeField] private RectTransform victoryRectTransform;
    public GameObject player { get ; private set; }

    public static GameManager instance;

    private void Start()
    {
        if(instance != null)
        {
            Destroy(this);
        }

        instance = this;

        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EventBus.Subscribe<OnTrapEnter>(KillPlayer);
        EventBus.Subscribe<OnDefeat>(Defaite);
        EventBus.Subscribe<OnVictory>(Victory);
        EventBus.Subscribe<OnLevelEnd>(ChangePlayerSpawnPoint);

        player = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnTrapEnter>(KillPlayer);
        EventBus.Unsubscribe<OnDefeat>(Defaite);
        EventBus.Unsubscribe<OnVictory>(Victory);
        EventBus.Unsubscribe<OnLevelEnd>(ChangePlayerSpawnPoint);

        if(instance == this)
        {
            instance = null;
        }
    }

    private void KillPlayer(OnTrapEnter _enter)
    {
        EventBus.Publish(new OnDefeat());
    }

    private void Defaite(OnDefeat _defaite)
    {
        print("defaite");
        player.transform.position = playerSpawnPoint.position;
    }

    private void Victory(OnVictory _victory)
    {
        if (victoryRectTransform != null && victoryCanvasGroup!= null)
        {
            victoryRectTransform.gameObject.SetActive(true);
            Utils.AnimationHelper.FadeInScreen(victoryCanvasGroup, victoryRectTransform, 500f);
        }

        print("victory");
    }

    private void ChangePlayerSpawnPoint(OnLevelEnd _evt)
    {
        playerSpawnPoint.position = _evt.newSpawnPoint;
    }
}
