using Player.Scripts;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MouseSensitivityUI : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private TMP_Text sensitivityText;

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
        PlayerLook playerLook = GameManager.instance.player.GetComponent<PlayerLook>();
        
        playerLook.lookSpeed = value;
    }
}