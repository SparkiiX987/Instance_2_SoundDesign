using UnityEngine;
using UnityEngine.UI;

public class VoiceToggleUI : MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private const string PREF_KEY="VoiceEnabled";

    private void Start()
    {
        bool saved = PlayerPrefs.GetInt(PREF_KEY,1)==1;
        toggle.isOn = saved;

        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool value)
    {
        PlayerPrefs.SetInt(PREF_KEY,value?1:0);
        PlayerPrefs.Save();
    }
}