using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Player.Scripts;

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

    [Header("Image UI qui affiche la cinématique")]
    public Image DisplayImage;

    private PlayerController playerController;
    private PlayerInput playerInput;
    private InputAction cinematicAction;

    public static event Action<Cinematic> OnCinematicStart;
    public static event Action<Cinematic> OnCinematicEnd;

    private void Awake()
    {
        if (DisplayImage == null)
        {
            Debug.LogError("CinematicManager : aucune Image UI assignée dans l'inspecteur.");
        }

        playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("CinematicManager : aucun PlayerController trouvé dans la scène.");
        }

        playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogWarning("CinematicManager : aucun PlayerInput trouvé dans la scène.");
        }
        else
        {
            cinematicAction = playerInput.actions["cinematique"];

            if (cinematicAction == null)
            {
                Debug.LogWarning("CinematicManager : aucune action 'cinematique' trouvée.");
            }
        }
    }

    public void PlayCinematic(int _Index)
    {
        if (DisplayImage == null)
        {
            Debug.LogError("CinematicManager : aucune Image UI assignée.");
            return;
        }

        if (_Index < 0 || _Index >= Cinematics.Count)
        {
            Debug.LogWarning("CinematicManager : index invalide.");
            return;
        }

        StopAllCoroutines();
        StartCoroutine(CPlayCinematic(Cinematics[_Index]));
    }

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

        Debug.LogWarning("CinematicManager : aucune cinématique trouvée avec le nom " + _Name);
    }

    private IEnumerator CPlayCinematic(Cinematic _Cinematic)
    {
        if (playerController != null)
        {
            playerController.DisableInput();
        }

        DisplayImage.enabled = true;

        OnCinematicStart?.Invoke(_Cinematic);
        Debug.Log("Début de la cinématique : " + _Cinematic.Name);

        for (int i = 0; i < _Cinematic.Images.Count; i++)
        {
            Sprite _CurrentSprite = _Cinematic.Images[i];

            if (_CurrentSprite == null)
            {
                Debug.LogWarning("CinematicManager : image nulle à l'index " + i);
                continue;
            }

            DisplayImage.sprite = _CurrentSprite;

            float _Timer = 0f;

            while (_Timer < _Cinematic.ImageDuration)
            {
                if (cinematicAction != null && cinematicAction.triggered)
                {
                    break;
                }

                _Timer += Time.deltaTime;
                yield return null;
            }
        }

        DisplayImage.sprite = null;
        DisplayImage.enabled = false;

        Debug.Log("Fin de la cinématique : " + _Cinematic.Name);
        OnCinematicEnd?.Invoke(_Cinematic);

        if (playerController != null)
        {
            playerController.EnableInput();
        }
    }
}