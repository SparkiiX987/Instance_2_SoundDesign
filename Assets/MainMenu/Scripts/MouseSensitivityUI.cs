using Player.Scripts;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private void OnSliderChanged(float value)
    {
        UpdateText(value);
        PlayerPrefs.SetFloat(playerPrefsKey, value);
        PlayerPrefs.Save();
    }

    private void UpdateText(float value)
    {
        sensitivityText.text = value.ToString("F" + decimals);
    }
    
    public void SetSensitivity(float value)
    {
        if (playerLook == null)
        {
            PlayerLook _playerLook = GameManager.instance.player.GetComponent<PlayerLook>();
            _playerLook.lookSpeed = value;
        }
        else
        {
            playerLook.lookSpeed = value;
        }
        
    }
}