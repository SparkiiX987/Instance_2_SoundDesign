using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;

    private GameObject player;
    private void Start()
    {
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        EventBus.Subscribe<OnTrapEnter>(KillPlayer);
        EventBus.Subscribe<OnDefaite>(Defaite);
        EventBus.Subscribe<OnVictory>(Victory);

        player = Instantiate(playerPrefab, playerSpawnPoint.position, playerSpawnPoint.rotation);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnTrapEnter>(KillPlayer);
        EventBus.Unsubscribe<OnDefaite>(Defaite);
        EventBus.Unsubscribe<OnVictory>(Victory);
    }

    private void KillPlayer(OnTrapEnter enter)
    {
        print("player killed");

        EventBus.Publish(new OnDefaite());
    }

    private void Defaite(OnDefaite defaite)
    {
        print("defaite");
        //SceneManager.LoadScene(0);
        player.transform.position = playerSpawnPoint.position;
    }

    private void Victory(OnVictory victory)
    {
        print("victory");
    }
}
