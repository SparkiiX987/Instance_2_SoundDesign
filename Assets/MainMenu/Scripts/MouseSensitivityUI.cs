using Player.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(10)]
public class MouseSensitivityUI : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityText;
    [SerializeField] private PlayerLook playerLook;

    private string playerPrefsKey = "MouseSensitivity";
    private float defaultSensitivity = 1f;
    private int decimals = 2;

    private void Start()
    {
        float saved = PlayerPrefs.GetFloat(playerPrefsKey, defaultSensitivity);
        sensitivitySlider.value = saved;
        UpdateText(saved);

        sensitivitySlider.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDestroy()
    {
        sensitivitySlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    private void OnSliderChanged(float _value)
    {
        UpdateText(_value);
        PlayerPrefs.SetFloat(playerPrefsKey, _value);
        PlayerPrefs.Save();
        SetSensitivity(_value);
    }

    private void UpdateText(float _value)
    {
        sensitivityText.text = _value.ToString("F" + decimals);
    }

    public void SetSensitivity(float _value)
    {
        if (playerLook == null)
        {
            if (GameManager.instance.player)
            {
                playerLook = GameManager.instance.player.GetComponent<PlayerLook>();
            }
        }
            playerLook.lookSpeed = _value;

    }
}