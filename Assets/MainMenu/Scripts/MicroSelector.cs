using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class MicroSelector : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown micDropdown;

    void Start()
    {
        PopulateMicrophoneList();
        Debug.Log("Nombre de micros : " + Microphone.devices.Length);
    }

    void PopulateMicrophoneList()
    {
        micDropdown.ClearOptions();

        if (Microphone.devices.Length == 0)
        {
            micDropdown.AddOptions(new List<string> { "Aucun micro détecté" });
            return;
        }

        var options = new List<string>(Microphone.devices);
        micDropdown.AddOptions(options);
    }
}