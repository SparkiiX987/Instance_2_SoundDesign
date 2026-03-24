using UnityEngine;

[DefaultExecutionOrder(-10)]
public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;

    public GameObject player { get ; private set; }

    public static GameManager instance;

    private void Start()
    {
        if(instance == null)
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

        player = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnTrapEnter>(KillPlayer);
        EventBus.Unsubscribe<OnDefeat>(Defaite);
        EventBus.Unsubscribe<OnVictory>(Victory);
    }

    private void KillPlayer(OnTrapEnter _enter)
    {
        print("player killed");

        EventBus.Publish(new OnDefeat());
    }

    private void Defaite(OnDefeat _defaite)
    {
        print("defaite");
        //SceneManager.LoadScene(0);
        player.transform.position = playerSpawnPoint.position;
    }

    private void Victory(OnVictory _victory)
    {
        print("victory");
    }

    private void ChangePlayerSpawnPoint(OnLevelEnd _evt)
    {
        playerSpawnPoint.position = _evt.newSpawnPoint;
    }
}
