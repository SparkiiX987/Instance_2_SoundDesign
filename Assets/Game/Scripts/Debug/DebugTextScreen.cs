using System.Collections;
using TMPro;
using UnityEngine;

public class DebugTextScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI screenDebugText;

    private void Start()
    {
        EventBus.Subscribe<OnTrapEnter>(KillText);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<OnTrapEnter>(KillText);
    }

    private void KillText(OnTrapEnter evt)
    {
        screenDebugText.text = "Player killed, respawn";
        screenDebugText.color = Color.red;
        StartCoroutine(RemoveText());
    }

    private IEnumerator RemoveText()
    {
        yield return new WaitForSeconds(2);
        screenDebugText.text = "";
    }
}