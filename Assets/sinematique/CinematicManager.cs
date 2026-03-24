using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Cinematic
{
    public string Name;
    public List<Sprite> Images = new List<Sprite>();
    public float ImageDuration = 1f;
}

public class CinematicManager : MonoBehaviour
{
    [Header("Liste des cinématiques")]
    public List<Cinematic> Cinematics = new List<Cinematic>();

    private Image displayImage;

    public static event Action<Cinematic> OnCinematicStart;
    public static event Action<Cinematic> OnCinematicEnd;

    private void Awake()
    {
        displayImage = FindObjectOfType<Image>();

        if (displayImage == null)
        {
            Debug.LogError("S_Cinematic : aucune Image UI trouvée dans la scène.");
        }
    }

    /// <summary>
    /// Lance une cinématique par son index.
    /// </summary>
    public void PlayCinematic(int _Index)
    {
        if (displayImage == null)
        {
            Debug.LogError("S_Cinematic : impossible de lancer la cinématique, aucune Image UI trouvée.");
            return;
        }

        if (_Index < 0 || _Index >= Cinematics.Count)
        {
            Debug.LogWarning("S_Cinematic : index de cinématique invalide.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(CPlayCinematic(Cinematics[_Index]));
    }

    /// <summary>
    /// Lance une cinématique par son nom.
    /// </summary>
    public void PlayCinematic(string _Name)
    {
        for (int i = 0; i < Cinematics.Count; i++)
        {
            if (Cinematics[i].Name == _Name)
            {
                PlayCinematic(i);
                return;
            }
        }

        Debug.LogWarning("S_Cinematic : aucune cinématique trouvée avec le nom " + _Name);
    }

    private IEnumerator CPlayCinematic(Cinematic _Cinematic)
    {
        OnCinematicStart?.Invoke(_Cinematic);
        Debug.Log("Début de la cinématique : " + _Cinematic.Name);

        for (int i = 0; i < _Cinematic.Images.Count; i++)
        {
            if (_Cinematic.Images[i] == null)
            {
                Debug.LogWarning("S_Cinematic : image nulle à l'index " + i + " dans la cinématique " + _Cinematic.Name);
                continue;
            }

            displayImage.sprite = _Cinematic.Images[i];
            Debug.Log("Image affichée : " + _Cinematic.Images[i].name);

            yield return new WaitForSeconds(_Cinematic.ImageDuration);
        }

        Debug.Log("Fin de la cinématique : " + _Cinematic.Name);
        OnCinematicEnd?.Invoke(_Cinematic);
    }
}