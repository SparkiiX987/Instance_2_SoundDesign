using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;
using Player.Scripts;

[System.Serializable]
public class CinematicImage
{
    public Sprite Image;          // L'image à afficher
    public float Duration = 1f;   // Durée pendant laquelle elle reste visible
    public float Fade = 0.5f;     // Durée du fondu d'entrée et de sortie
}

[System.Serializable]
public class Cinematic
{
    public string Name;
    public List<CinematicImage> Images = new List<CinematicImage>();
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
            Debug.LogError("CinematicManager : aucune Image UI assignée dans l'inspecteur.");

        playerController = FindObjectOfType<PlayerController>();
        playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null)
            cinematicAction = playerInput.actions["cinematique"];
    }

    public void PlayCinematic(int _Index)
    {
        if (DisplayImage == null) return;
        if (_Index < 0 || _Index >= Cinematics.Count) return;

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
    }

    private IEnumerator CPlayCinematic(Cinematic _Cinematic)
    {
        if (playerController != null)
            playerController.DisableInput();

        DisplayImage.enabled = true;

        // CanvasGroup pour gérer l'alpha
        CanvasGroup cg = DisplayImage.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = DisplayImage.gameObject.AddComponent<CanvasGroup>();
        cg.alpha = 0f;

        OnCinematicStart?.Invoke(_Cinematic);
        Debug.Log("Début de la cinématique : " + _Cinematic.Name);

        foreach (var cinematicImage in _Cinematic.Images)
        {
            if (cinematicImage.Image == null) continue;

            DisplayImage.sprite = cinematicImage.Image;

            // Fade in
            cg.DOFade(1f, cinematicImage.Fade);

            // Attend la durée visible moins le fade
            float timer = 0f;
            float visibleTime = Mathf.Max(0f, cinematicImage.Duration - cinematicImage.Fade);

            while (timer < visibleTime)
            {
                if (cinematicAction != null && cinematicAction.triggered)
                    break;

                timer += Time.deltaTime;
                yield return null;
            }

            // Fade out
            cg.DOFade(0f, cinematicImage.Fade);
            yield return new WaitForSeconds(cinematicImage.Fade);
        }

        DisplayImage.sprite = null;
        DisplayImage.enabled = false;

        Debug.Log("Fin de la cinématique : " + _Cinematic.Name);
        OnCinematicEnd?.Invoke(_Cinematic);

        if (playerController != null)
            playerController.EnableInput();
    }
}