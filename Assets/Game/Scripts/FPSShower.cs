using TMPro;
using UnityEngine;

public class FPSShower : MonoBehaviour
{
    private TextMeshProUGUI fpsText;

    private float deltaTime = 0.0f;

    private void Awake()
    {
        fpsText = GetComponent<TextMeshProUGUI>();

        deltaTime = Time.deltaTime;
    }

    private void Update()
    {
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        float fps = 1.0f / deltaTime;
        fpsText.text = $"{Mathf.Ceil(fps)} fps";
    }
}
