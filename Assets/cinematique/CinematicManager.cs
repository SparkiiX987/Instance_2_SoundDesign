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
    public Sprite Image;
    public float Duration = 1f;
    public float Fade     = 0.5f;
}

[System.Serializable]
public class Cinematic
{
    public string Name;
    public List<CinematicImage> Images = new List<CinematicImage>();
}

public class CinematicManager : MonoBehaviour
{
    [Header("Liste des cinematiques")]
    public List<Cinematic> Cinematics = new List<Cinematic>();

    [Header("Image UI qui affiche la cinematique")]
    public Image DisplayImage;

    public static event Action<Cinematic> OnCinematicStart;
    public static event Action<Cinematic> OnCinematicEnd;

    private PlayerController _playerController;
    private InputAction      _cinematicAction;

    // Flag mis a true quand le joueur appuie sur E pendant la cinematique
    private bool _skipRequested;

    // ── Init ─────────────────────────────────────────────────────────


    
        private void Start()
        {
            if (DisplayImage == null)
            {
                Debug.LogError("CinematicManager : aucune Image UI assignee.");
            }

            // 🔥 Récupère automatiquement le player
            _playerController = FindObjectOfType<PlayerController>();

            if (_playerController == null)
            {
                Debug.LogWarning("CinematicManager : PlayerController introuvable.");
            }

            // 🔥 Input system
            PlayerInput pi = FindObjectOfType<PlayerInput>();
            if (pi != null)
            {
                _cinematicAction = pi.actions["cinematique"];
            }
        }
    

    private void OnEnable()
    {
        if (_cinematicAction != null)
        {
            _cinematicAction.performed += OnSkip;
            _cinematicAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (_cinematicAction != null)
        {
            _cinematicAction.performed -= OnSkip;
        }
    }

    // Callback appele quand le joueur appuie sur E
    private void OnSkip(InputAction.CallbackContext _ctx)
    {
        _skipRequested = true;
    }

    // ── API publique ─────────────────────────────────────────────────

    public void PlayCinematic(int _index)
    {
        if (DisplayImage == null) { return; }
        if (_index < 0 || _index >= Cinematics.Count) { return; }
        StopAllCoroutines();
        StartCoroutine(CPlayCinematic(Cinematics[_index]));
    }

    public void PlayCinematic(string _name)
    {
        for (int i = 0; i < Cinematics.Count; i++)
        {
            if (Cinematics[i].Name == _name)
            {
                PlayCinematic(i);
                return;
            }
        }
        Debug.LogWarning($"CinematicManager : cinematique '{_name}' introuvable.");
    }

    // ── Coroutine principale ─────────────────────────────────────────

    private IEnumerator CPlayCinematic(Cinematic _cinematic)
    {
        // Bloquer les mouvements du joueur
        if (_playerController != null)
        {
            _playerController.DisableInput();
        }

        DisplayImage.enabled = true;

        CanvasGroup cg = DisplayImage.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = DisplayImage.gameObject.AddComponent<CanvasGroup>();
        }
        cg.alpha = 0f;

        _skipRequested = false;

        OnCinematicStart?.Invoke(_cinematic);
        Debug.Log($"[Cinematic] Debut : {_cinematic.Name}");

        foreach (CinematicImage ci in _cinematic.Images)
        {
            if (ci.Image == null) { continue; }

            DisplayImage.sprite = ci.Image;

            // Fade in
            cg.DOFade(1f, ci.Fade);

            // Attendre la duree visible — le joueur peut appuyer sur E pour passer
            float timer      = 0f;
            float visibleTime = Mathf.Max(0f, ci.Duration - ci.Fade);

            while (timer < visibleTime)
            {
                if (_skipRequested)
                {
                    _skipRequested = false;
                    break; // Passe a l'image suivante
                }
                timer += Time.deltaTime;
                yield return null;
            }

            // Fade out
            cg.DOFade(0f, ci.Fade);
            yield return new WaitForSeconds(ci.Fade);
        }

        // Fin
        DisplayImage.sprite  = null;
        DisplayImage.enabled = false;
        cg.alpha             = 0f;

        Debug.Log($"[Cinematic] Fin : {_cinematic.Name}");
        OnCinematicEnd?.Invoke(_cinematic);

        // Rendre les mouvements au joueur
        if (_playerController != null)
        {
            _playerController.EnableInput();
        }
    }
}
